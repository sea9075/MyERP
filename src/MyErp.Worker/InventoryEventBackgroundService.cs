using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyErp.Application.Abstractions;
using MyErp.Application.Services;

namespace MyErp.Worker;

/// <summary>
/// 訂閱 Azure Service Bus 佇列 "sales-events"，收到 InventoryDecreasedEvent 後，
/// 每則訊息開一個 DI scope 呼叫 IInventoryEventHandler 做實際的低庫存檢查
/// （2026-09-15 新增，見 Infra-Progress.md §31）。用 SDK 提供的 ServiceBusProcessor
/// （而不是自己寫 while 迴圈輪詢），併發控制、訊息鎖定續約這些細節都交給 SDK 處理。
/// </summary>
public class InventoryEventBackgroundService : BackgroundService
{
    private readonly ServiceBusClient client;
    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<InventoryEventBackgroundService> logger;
    private readonly string queueName;
    private ServiceBusProcessor? processor;

    public InventoryEventBackgroundService(
        ServiceBusClient client,
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<InventoryEventBackgroundService> logger)
    {
        this.client = client;
        this.scopeFactory = scopeFactory;
        this.logger = logger;
        queueName = configuration["ServiceBus:QueueName"] ?? "sales-events";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        processor = client.CreateProcessor(queueName, new ServiceBusProcessorOptions
        {
            MaxConcurrentCalls = 1,
            AutoCompleteMessages = false,
        });

        processor.ProcessMessageAsync += ProcessMessageAsync;
        processor.ProcessErrorAsync += ProcessErrorAsync;

        logger.LogInformation("開始監聽 Service Bus 佇列 {QueueName}。", queueName);
        await processor.StartProcessingAsync(stoppingToken);

        // StartProcessingAsync 會立刻返回（訊息處理是在背景執行緒進行），這裡讓 ExecuteAsync
        // 一直等到服務被要求停止，BackgroundService 才不會誤判這個服務「已經跑完了」而結束。
        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // 正常的停止流程，不用特別處理。
        }
    }

    private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
    {
        try
        {
            var @event = JsonSerializer.Deserialize<InventoryDecreasedEvent>(args.Message.Body);
            if (@event is null)
            {
                logger.LogWarning("收到無法解析的訊息 (MessageId={MessageId})，直接標記完成、不重試。", args.Message.MessageId);
                await args.CompleteMessageAsync(args.Message, args.CancellationToken);
                return;
            }

            // 每則訊息開一個新的 DI scope：IInventoryEventHandler／Repository／DbContext
            // 都是 Scoped 生命週期，跟 MyErp.Api 每個 HTTP 請求各自開一個 scope 是同樣的道理。
            using var scope = scopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<IInventoryEventHandler>();
            await handler.HandleInventoryDecreasedAsync(@event, args.CancellationToken);

            await args.CompleteMessageAsync(args.Message, args.CancellationToken);
        }
        catch (Exception ex)
        {
            // 刻意不呼叫 CompleteMessageAsync：訊息會依佇列設定的 max delivery count
            // （目前 sales-events 用建立時的預設值 10，見 Infra-Progress.md §30）自動重新投遞；
            // 重試次數用完後，Service Bus 會把訊息移進佇列內建的死信子佇列，不會無限重試下去。
            logger.LogError(ex, "處理低庫存通知訊息失敗 (MessageId={MessageId})。", args.Message.MessageId);
        }
    }

    private Task ProcessErrorAsync(ProcessErrorEventArgs args)
    {
        logger.LogError(args.Exception, "Service Bus Processor 發生錯誤（ErrorSource={ErrorSource}）。", args.ErrorSource);
        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (processor is not null)
        {
            await processor.StopProcessingAsync(cancellationToken);
            await processor.DisposeAsync();
        }

        await base.StopAsync(cancellationToken);
    }
}
