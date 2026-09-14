using System.Net;
using System.Text.Json;
using MyErp.Application.Common;

namespace MyErp.Api.Middleware;

/// <summary>
/// 統一例外處理：BusinessRuleException（庫存不足、找不到資料等「預期內」的業務規則違反）轉成 400，
/// 訊息直接回給前端顯示；其他所有未預期的例外一律轉成 500，並且不把詳細例外內容洩漏給前端，
/// 只記錄到 log。
/// </summary>
public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (BusinessRuleException ex)
        {
            logger.LogWarning(ex, "Business rule violation: {Message}", ex.Message);
            await WriteProblemAsync(context, HttpStatusCode.BadRequest, ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");
            await WriteProblemAsync(context, HttpStatusCode.InternalServerError, "系統發生未預期的錯誤，請稍後再試。");
        }
    }

    private static async Task WriteProblemAsync(HttpContext context, HttpStatusCode statusCode, string message)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var payload = JsonSerializer.Serialize(new { message });
        await context.Response.WriteAsync(payload);
    }
}
