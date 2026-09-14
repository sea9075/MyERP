using MyErp.Application.Abstractions;
using MyErp.Application.Common;
using MyErp.Application.DTOs;
using MyErp.Domain.Entities;

namespace MyErp.Application.Services;

public interface ICategoryService
{
    Task<List<CategoryDto>> GetAllAsync(bool includeDeleted, CancellationToken ct = default);
    Task<CategoryDto> CreateAsync(CreateCategoryRequest request, string currentUsername, CancellationToken ct = default);
    Task<CategoryDto> UpdateAsync(int id, UpdateCategoryRequest request, string currentUsername, CancellationToken ct = default);

    /// <summary>軟刪除（IsDeleted=true）；刪除前檢查底下是否還有商品在用。</summary>
    Task DeleteAsync(int id, string currentUsername, CancellationToken ct = default);
}

public class CategoryService(ICategoryRepository categoryRepository, IUnitOfWork unitOfWork) : ICategoryService
{
    public async Task<List<CategoryDto>> GetAllAsync(bool includeDeleted, CancellationToken ct = default)
    {
        var categories = await categoryRepository.GetAllAsync(includeDeleted, ct);
        return categories.Select(ToDto).ToList();
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryRequest request, string currentUsername, CancellationToken ct = default)
    {
        var category = new Category { Name = request.Name };
        category.InitializeAudit(currentUsername);

        categoryRepository.Add(category);
        await unitOfWork.SaveChangesAsync(ct);
        return ToDto(category);
    }

    public async Task<CategoryDto> UpdateAsync(int id, UpdateCategoryRequest request, string currentUsername, CancellationToken ct = default)
    {
        var category = await categoryRepository.GetByIdAsync(id, ct)
            ?? throw new BusinessRuleException($"找不到分類 (Id={id})。");

        category.Name = request.Name;
        category.TouchUpdated(currentUsername);

        await unitOfWork.SaveChangesAsync(ct);
        return ToDto(category);
    }

    public async Task DeleteAsync(int id, string currentUsername, CancellationToken ct = default)
    {
        var category = await categoryRepository.GetByIdAsync(id, ct)
            ?? throw new BusinessRuleException($"找不到分類 (Id={id})。");

        // 只算「未刪除」的商品（Product 的 Global Query Filter 自動套用），
        // 如果底下的商品都已經被刪除了，就不會擋這次分類刪除。
        if (await categoryRepository.HasProductsAsync(id, ct))
        {
            throw new BusinessRuleException("此分類仍有商品使用中，請先將商品改分類或停用後再刪除。");
        }

        category.SoftDelete(currentUsername);
        await unitOfWork.SaveChangesAsync(ct);
    }

    private static CategoryDto ToDto(Category category) => new()
    {
        Id = category.Id,
        Name = category.Name,
        CreatedAt = category.CreatedAt,
        UpdatedAt = category.UpdatedAt,
        CreatedBy = category.CreatedBy,
        UpdatedBy = category.UpdatedBy,
        IsDeleted = category.IsDeleted,
    };
}
