using System.ComponentModel.DataAnnotations;

namespace MyErp.Application.DTOs;

public class CategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class CreateCategoryRequest
{
    [Required, StringLength(50)]
    public string Name { get; set; } = string.Empty;
}

public class UpdateCategoryRequest
{
    [Required, StringLength(50)]
    public string Name { get; set; } = string.Empty;
}
