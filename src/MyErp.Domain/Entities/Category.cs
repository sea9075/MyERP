using MyErp.Domain.Common;

namespace MyErp.Domain.Entities;

/// <summary>商品分類（ERP.md §5.1 Category）。</summary>
public class Category : IAuditable
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 使用者自訂的分類編號，例如飲料 DRI、餅乾 COK（最長 5 碼，只允許英文大寫與數字）。
    /// 用來當作商品標號（Product.Sku）的前綴字串，見 ProductService.CreateAsync。
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 這個分類下一個要用的商品標號流水號。商品標號＝Code + "-" + 7 碼流水號（例如 COK-0000001），
    /// 每新增一個商品就 +1，由 ProductService.CreateAsync 負責讀取／遞增，不開放使用者直接修改。
    /// </summary>
    public int NextSequence { get; set; } = 1;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;

    /// <summary>是否已刪除（軟刪除）。刪除前會檢查底下是否還有商品在用，見 CategoryService.DeleteAsync。</summary>
    public bool IsDeleted { get; set; }

    public ICollection<Product> Products { get; set; } = new List<Product>();
}
