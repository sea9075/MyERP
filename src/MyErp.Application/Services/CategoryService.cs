using MyErp.Application.Abstractions;
using MyErp.Application.Common;
using MyErp.Application.DTOs;
using MyErp.Domain.Entities;

namespace MyErp.Application.Services;

public interface ICategoryService
{
    Task<List<CategoryDto>> GetAllAsync(CancellationToken ct = default);
    Task<CategoryDto> CreateAsync(CreateCategoryRequest request, CancellationToken ct = default);
    Task<CategoryDto> UpdateAsync(int id, UpdateCategoryRequest request, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}

public class CategoryService(ICategoryRepository categoryRepository, IUnitOfWork unitOfWork) : ICategoryService
{
    public async Task<List<CategoryDto>> GetAllAsync(CancellationToken ct = default)
    {
        var categories = await categoryRepository.GetAllAsync(ct);
        return categories.Select(ToDto).ToList();
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryRequest request, CancellationToken ct = default)
    {
        var category = new Category { Name = request.Name };
        categoryRepository.Add(category);
        await unitOfWork.SaveChangesAsync(ct);
        return ToDto(category);
    }

    public async Task<CategoryDto> UpdateAsync(int id, UpdateCategoryRequest request, CancellationToken ct = default)
    {
        var category = await categoryRepository.GetByIdAsync(id, ct)
            ?? throw new BusinessRuleException($"找不到分類 (Id={id})。");

        category.Name = request.Name;
        await unitOfWork.SaveChangesAsync(ct);
        return ToDto(category);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var category = await categoryRepository.GetByIdAsync(id, ct)
            ?? throw new BusinessRuleException($"找不到分類 (Id={id})。");

        // Category 沒有 IsActive 欄位（見 ERP.md §5.1），所以是實體刪除；
        // 先檢查是否還有商品掛在這個分類下，避免刪除後商品的 CategoryId 變成孤兒資料。
        if (await categoryRepository.HasProductsAsync(id, ct))
        {
            throw new BusinessRuleException("此分類仍有商品使用中，請先將商品改分類或停用後再刪除。");
        }

        categoryRepository.Remove(category);
        await unitOfWork.SaveChangesAsync(ct);
    }

    private static CategoryDto ToDto(Category category) => new()
    {
        Id = category.Id,
        Name = category.Name,
    };
}
