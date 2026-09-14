using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using MyErp.Application.Services;

namespace MyErp.Api.Filters;

/// <summary>
/// 全域 Action Filter：自動記錄「誰在什麼時候呼叫了哪一支會改變資料的 API」，寫進 ActivityLog
/// （見 IActivityLogService）。只記錄 POST/PUT/DELETE/PATCH，不記錄單純查詢(GET)，也不需要在每支
/// API 裡手動加程式碼——在 Program.cs 用 options.Filters.Add&lt;ActivityLogActionFilter&gt;() 全域註冊一次就好，
/// 之後新增的 API 也會自動被記錄。
///
/// 寫入操作紀錄失敗（例如資料庫暫時連不上）不應該影響原本 API 的回應，所以這裡吞掉例外、只寫 log，
/// 不重新丟出。
/// </summary>
public class ActivityLogActionFilter(IActivityLogService activityLogService, ILogger<ActivityLogActionFilter> logger) : IAsyncActionFilter
{
    private static readonly HashSet<string> MutatingMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        HttpMethods.Post,
        HttpMethods.Put,
        HttpMethods.Delete,
        HttpMethods.Patch,
    };

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var httpContext = context.HttpContext;
        var shouldLog = MutatingMethods.Contains(httpContext.Request.Method);

        try
        {
            await next();
        }
        finally
        {
            if (shouldLog)
            {
                await TryLogAsync(httpContext);
            }
        }
    }

    private async Task TryLogAsync(HttpContext httpContext)
    {
        try
        {
            var username = httpContext.User.Identity?.IsAuthenticated == true
                ? httpContext.User.FindFirstValue(ClaimTypes.Name) ?? "unknown"
                : "anonymous";

            var api = $"{httpContext.Request.Method} {httpContext.Request.Path}";

            await activityLogService.LogAsync(api, username, httpContext.RequestAborted);
        }
        catch (Exception ex)
        {
            // 稽核紀錄失敗不應該影響原本 API 的回應，記錄下來就好。
            logger.LogWarning(ex, "寫入操作紀錄 (ActivityLog) 失敗，不影響原本的 API 回應。");
        }
    }
}
