using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace MyErp.Api.Extensions;

/// <summary>
/// 從 JWT claims 取目前登入使用者資訊的共用小工具。原本每個 Controller 各自寫一份
/// private GetCurrentUserId()，這次因為新增/修改的 Controller 變多（Category/Product/
/// Supplier/Customer/PurchaseOrder/SalesOrder 都需要 username），統一抽成 extension method。
/// </summary>
public static class ControllerBaseExtensions
{
    /// <summary>
    /// 取出目前登入使用者的 username（ClaimTypes.Name），用來寫入各資料表的 CreatedBy/UpdatedBy 欄位。
    /// Username 一旦建立就不能更改，所以直接存這個字串快照不會有之後改名資料跟著跑掉的問題。
    /// </summary>
    public static string GetCurrentUsername(this ControllerBase controller) =>
        controller.User.FindFirstValue(ClaimTypes.Name)
            ?? throw new InvalidOperationException("JWT 缺少使用者名稱 (ClaimTypes.Name claim)，這裡不應該發生。");

    /// <summary>取出目前登入使用者的 Id（ClaimTypes.NameIdentifier），給還在用 int 外鍵的 InventoryTransaction 用。</summary>
    public static int GetCurrentUserId(this ControllerBase controller)
    {
        var idClaim = controller.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("JWT 缺少使用者 Id (NameIdentifier claim)，這裡不應該發生。");
        return int.Parse(idClaim);
    }
}
