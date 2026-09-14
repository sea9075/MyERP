namespace MyErp.Application.Common;

/// <summary>
/// 代表「業務規則」被違反（例如庫存不足、找不到對應的供應商），
/// 用來跟真正的系統錯誤（例如資料庫斷線）區分開。
/// Api 層的 ExceptionHandlingMiddleware 會把這種例外轉成 HTTP 400，其他例外轉成 500。
/// </summary>
public class BusinessRuleException(string message) : Exception(message);
