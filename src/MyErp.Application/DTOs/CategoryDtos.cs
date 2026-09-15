using System.ComponentModel.DataAnnotations;

namespace MyErp.Application.DTOs;

public class CategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
}

public class CreateCategoryRequest
{
    [Required, StringLength(50)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 分類編號，最長 5 碼，只允許英文大寫與數字（例如飲料 DRI、餅乾 COK）。
    /// 用來當作商品標號的前綴字串，一旦有商品在用就不建議再改（見 ProductService 的說明）。
    /// </summary>
    [Required, StringLength(5, MinimumLength = 1), RegularExpression("^[A-Z0-9]+$", ErrorMessage = "分類編號只能使用英文大寫與數字，最長 5 碼。")]
    public string Code { get; set; } = string.Empty;
}

public class UpdateCategoryRequest
{
    [Required, StringLength(50)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(5, MinimumLength = 1), RegularExpression("^[A-Z0-9]+$", ErrorMessage = "分類編號只能使用英文大寫與數字，最長 5 碼。")]
    public string Code { get; set; } = string.Empty;
}
