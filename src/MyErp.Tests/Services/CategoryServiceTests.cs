using Moq;
using MyErp.Application.Abstractions;
using MyErp.Application.Common;
using MyErp.Application.DTOs;
using MyErp.Application.Services;
using MyErp.Domain.Entities;
using Xunit;

namespace MyErp.Tests.Services;

public class CategoryServiceTests
{
    private readonly Mock<ICategoryRepository> _categoryRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly CategoryService _sut;

    public CategoryServiceTests()
    {
        _sut = new CategoryService(_categoryRepository.Object, _unitOfWork.Object);
    }

    [Fact]
    public async Task CreateAsync_應該新增分類並設定稽核欄位()
    {
        var request = new CreateCategoryRequest { Name = "飲料" };

        Category? added = null;
        _categoryRepository.Setup(r => r.Add(It.IsAny<Category>())).Callback<Category>(c => added = c);

        var result = await _sut.CreateAsync(request, currentUsername: "alice");

        Assert.NotNull(added);
        Assert.Equal("飲料", added!.Name);
        Assert.Equal("alice", result.CreatedBy);
        Assert.Equal("alice", result.UpdatedBy);
        Assert.Equal(result.CreatedAt, result.UpdatedAt);
        Assert.False(result.IsDeleted);
    }

    [Fact]
    public async Task CreateAsync_應該修剪名稱頭尾空白再查重與存檔()
    {
        var request = new CreateCategoryRequest { Name = "  飲料  " };

        Category? added = null;
        _categoryRepository.Setup(r => r.Add(It.IsAny<Category>())).Callback<Category>(c => added = c);

        var result = await _sut.CreateAsync(request, currentUsername: "alice");

        // 查重比對跟最後存進去的名稱都應該是修剪過的版本，不會帶頭尾空白。
        _categoryRepository.Verify(r => r.NameExistsAsync("飲料", null, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal("飲料", added!.Name);
        Assert.Equal("飲料", result.Name);
    }

    [Fact]
    public async Task CreateAsync_名稱重複時應該拋出BusinessRuleException_且不新增()
    {
        _categoryRepository.Setup(r => r.NameExistsAsync("飲料", null, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var request = new CreateCategoryRequest { Name = "飲料" };

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.CreateAsync(request, currentUsername: "alice"));

        _categoryRepository.Verify(r => r.Add(It.IsAny<Category>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_名稱重複時應該拋出BusinessRuleException_且排除自己()
    {
        var category = new Category { Id = 1, Name = "舊名稱" };
        _categoryRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(category);
        _categoryRepository.Setup(r => r.NameExistsAsync("飲料", 1, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var request = new UpdateCategoryRequest { Name = "飲料" };

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.UpdateAsync(1, request, currentUsername: "alice"));

        // NameExistsAsync 有帶 excludeId=1，代表更新時允許沿用自己原本的名稱，不會被自己擋自己。
        _categoryRepository.Verify(r => r.NameExistsAsync("飲料", 1, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal("舊名稱", category.Name);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_底下還有未刪除商品時應該拋出BusinessRuleException_且不軟刪除()
    {
        var category = new Category { Id = 1, Name = "飲料" };
        _categoryRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(category);
        _categoryRepository.Setup(r => r.HasProductsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.DeleteAsync(1, currentUsername: "alice"));

        Assert.False(category.IsDeleted);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_底下沒有商品時應該軟刪除分類並記錄UpdatedBy()
    {
        var category = new Category { Id = 1, Name = "飲料", CreatedBy = "alice", UpdatedBy = "alice" };
        _categoryRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(category);
        _categoryRepository.Setup(r => r.HasProductsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        await _sut.DeleteAsync(1, currentUsername: "bob");

        // 軟刪除：IsDeleted=true，UpdatedBy 變成執行刪除的人，CreatedBy 不變，不會呼叫任何 Remove()。
        Assert.True(category.IsDeleted);
        Assert.Equal("bob", category.UpdatedBy);
        Assert.Equal("alice", category.CreatedBy);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_分類不存在時應該拋出BusinessRuleException()
    {
        _categoryRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((Category?)null);

        var request = new UpdateCategoryRequest { Name = "新名稱" };

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.UpdateAsync(999, request, currentUsername: "alice"));
    }

    [Fact]
    public async Task GetAllAsync_應該把includeDeleted參數傳給Repository()
    {
        _categoryRepository.Setup(r => r.GetAllAsync(true, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        await _sut.GetAllAsync(includeDeleted: true);

        _categoryRepository.Verify(r => r.GetAllAsync(true, It.IsAny<CancellationToken>()), Times.Once);
    }
}
