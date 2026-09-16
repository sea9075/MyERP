using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MyErp.Application;
using MyErp.Infra;
using MyErp.Worker;

// 這個 worker 是 Hybrid-Cloud.md 規劃的「非同步工作」示範：訂閱 Azure Service Bus 佇列
// "sales-events"，收到出貨單建立／進貨單作廢觸發的庫存減少事件後，非同步檢查是否要寫入
// 低庫存通知（2026-09-15 新增，見 Infra-Progress.md §31、ERP.md §4.7）。
//
// 跟 MyErp.Api 共用同一份 Domain/Application/Infra：呼叫一樣的 AddMyErpApplication()／
// AddMyErpInfra()，代表這裡也會連同一個 Azure SQL Database、也需要設定同一組
// ConnectionStrings:DefaultConnection（本機開發要另外對這個專案跑一次
// `dotnet user-secrets set`，這個專案的 UserSecretsId 跟 MyErp.Api 不是同一組）。
var builder = Host.CreateApplicationBuilder(args);

// User Secrets 這個設定來源，Generic Host（Host.CreateApplicationBuilder）預設只有在
// DOTNET_ENVIRONMENT=Development 時才會自動載入；worker 專案（不像 MyErp.Api 那種 Web 專案）
// 沒有自動附帶 launchSettings.json 設定這個環境變數，`dotnet run` 直接跑起來環境會是預設的
// Production，User Secrets 完全不會被讀取（但 `dotnet user-secrets list` 這個 CLI 指令是直接
// 讀 secrets.json 檔案、不受環境變數影響，才會出現「list 有值、跑起來卻找不到」這種矛盾情況）。
// 這裡明確呼叫 AddUserSecrets，不管當下是什麼環境都會嘗試讀取本機的 secrets.json（optional:
// true，正式環境部署時這個檔案本來就不存在，呼叫這行不會有任何副作用）。
builder.Configuration.AddUserSecrets<Program>(optional: true);

// 跟 MyErp.Api 不一樣：Api 沒設定 ServiceBus:ConnectionString 時會退回 NullEventPublisher，
// 因為發布事件對 Api 只是 best-effort 的附加功能（見 AddMyErpInfra 的說明）。但這個 worker
// 存在的唯一目的就是訂閱 Service Bus，沒有連線字串就完全沒事可做，所以這裡直接 fail fast，
// 用清楚的訊息告訴使用者要補設定，而不是留給 DI 在解析 ServiceBusClient 時丟出一句看不懂的錯誤。
if (string.IsNullOrWhiteSpace(builder.Configuration["ServiceBus:ConnectionString"]))
{
    throw new InvalidOperationException(
        "找不到 ServiceBus:ConnectionString。請先執行 " +
        "`dotnet user-secrets set \"ServiceBus:ConnectionString\" \"...\"`（見交付說明）。");
}

builder.Services.AddMyErpApplication();
builder.Services.AddMyErpInfra(builder.Configuration);

builder.Services.AddHostedService<InventoryEventBackgroundService>();

var host = builder.Build();
host.Run();
