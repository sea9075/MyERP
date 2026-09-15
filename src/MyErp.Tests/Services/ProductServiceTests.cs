using Moq;
using MyErp.Application.Abstractions;
using MyErp.Application.Common;
using MyErp.Application.DTOs;
using MyErp.Application.Services;
using MyErp.Domain.Entities;
using Xunit;

namespace MyErp.Tests.Services;

public class ProductServiceTests
{
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<ISupplierRepository> _supplierRepository = new();
    private readonly Mock<ICategoryRepository> _categoryRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly ProductService _sut;

    public ProductServiceTests()
    {
        _sut = new ProductService(
            _productRepository.Object,
            _supplierRepository.Object,
            _categoryRepository.Object,
            _unitOfWork.Object);
    }

    private static CreateProductRequest MakeRequest(int categoryId = 1, string name = "孔雀餅乾", int? supplierId = null) => new()
    {
        Name = name,
        CategoryId = categoryId,
        Unit = "包",
        CostPrice = 10,
        SalePrice = 15,
        SafetyStock = 5,
        SupplierId = supplierId,
    };

    [Fact]
    public async Task CreateAsync_應該依分類編號與流水號自動產生Sku與Barcode_並把分類的NextSequence加一()
    {
        var category = new Category { Id = 1, Name = "餅乾", Code = "COK", NextSequence = 1 };
        _categoryRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(category);

        Product? added = null;
        _productRepository.Setup(r => r.Add(It.IsAny<Product>())).Callback<Product>(p => added = p);

        var result = await _sut.CreateAsync(MakeRequest(), currentUsername: "alice");

        // 商品標號＝分類編號 + "-" + 7 碼流水號；條碼＝標號去掉 "-"。
        Assert.Equal("COK-0000001", added!.Sku);
        Assert.Equal("COK0000001", added.Barcode);
        Assert.Equal("COK-0000001", result.Sku);
        Assert.Equal("COK0000001", result.Barcode);

        // 流水號用完要 +1，下一個商品才會拿到 COK-0000002，且分類要記錄是誰／何時改的。
        Assert.Equal(2, category.NextSequence);
        Assert.Equal("alice", category.UpdatedBy);

        // 新商品一律從 0 庫存開始。
        Assert.Equal(0, added.CurrentStock);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_同一分類連續新增第二個商品時流水號應該繼續遞增()
    {
        // 模擬分類已經用掉一個流水號（上一個商品是 COK-0000005），這次新增應該接著拿 0000006。
        var category = new Category { Id = 1, Name = "餅乾", Code = "COK", NextSequence = 6 };
        _categoryRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(category);

        Product? added = null;
        _productRepository.Setup(r => r.Add(It.IsAny<Product>())).Callback<Product>(p => added = p);

        await _sut.CreateAsync(MakeRequest(), currentUsername: "alice");

        Assert.Equal("COK-0000006", added!.Sku);
        Assert.Equal(7, category.NextSequence);
    }

    [Fact]
    public async Task CreateAsync_分類不存在時應該拋出BusinessRuleException_且不新增商品()
    {
        _categoryRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((Category?)null);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.CreateAsync(MakeRequest(categoryId: 999), currentUsername: "alice"));

        _productRepository.Verify(r => r.Add(It.IsAny<Product>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_分類已經被刪除時應該拋出BusinessRuleException_且不新增商品()
    {
        var category = new Category { Id = 1, Name = "餅乾", Code = "COK", NextSequence = 1, IsDeleted = true };
        _categoryRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(category);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.CreateAsync(MakeRequest(), currentUsername: "alice"));

        _productRepository.Verify(r => r.Add(It.IsAny<Product>()), Times.Never);
        // 分類已刪除就直接擋掉，流水號不應該被消耗掉。
        Assert.Equal(1, category.NextSequence);
    }

    [Fact]
    public async Task CreateAsync_供應商不存在時應該拋出BusinessRuleException_且不新增商品()
    {
        var category = new Category { Id = 1, Name = "餅乾", Code = "COK", NextSequence = 1 };
        _categoryRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(category);
        _supplierRepository.Setup(r => r.ExistsAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.CreateAsync(MakeRequest(supplierId: 999), currentUsername: "alice"));

        _productRepository.Verify(r => r.Add(It.IsAny<Product>()), Times.Never);
        Assert.Equal(1, category.NextSequence);
    }

    [Fact]
    public async Task CreateAsync_應該修剪名稱與單位頭尾空白再存檔()
    {
        var category = new Category { Id = 1, Name = "餅乾", Code = "COK", NextSequence = 1 };
        _categoryRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(category);

        Product? added = null;
        _productRepository.Setup(r => r.Add(It.IsAny<Product>())).Callback<Product>(p => added = p);

        var request = MakeRequest(name: "  孔雀餅乾  ");
        request.Unit = "  包  ";

        await _sut.CreateAsync(request, currentUsername: "alice");

        Assert.Equal("孔雀餅乾", added!.Name);
        Assert.Equal("包", added.Unit);
    }

    [Fact]
    public async Task UpdateAsync_不應該修改CategoryId或Sku或Barcode_只更新其他欄位()
    {
        var product = new Product
        {
            Id = 1,
            Name = "舊名稱",
            CategoryId = 1,
            Sku = "COK-0000001",
            Barcode = "COK0000001",
            Unit = "包",
            CostPrice = 10,
            SalePrice = 15,
            SafetyStock = 5,
        };
        _productRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        var request = new UpdateProductRequest
        {
            Name = "新名稱",
            Unit = "箱",
            CostPrice = 12,
            SalePrice = 18,
            SafetyStock = 8,
            SupplierId = null,
            IsDeleted = false,
        };

        var result = await _sut.UpdateAsync(1, request, currentUsername: "bob");

        // 分類／商品標號／條碼建立後就固定，UpdateProductRequest 根本沒有這幾個欄位可以傳，
        // 這裡直接驗證 Update 之後這三個值完全沒被動過。
        Assert.Equal(1, product.CategoryId);
        Assert.Equal("COK-0000001", product.Sku);
        Assert.Equal("COK0000001", product.Barcode);

        // 其他允許修改的欄位應該正確更新。
        Assert.Equal("新名稱", product.Name);
        Assert.Equal("箱", product.Unit);
        Assert.Equal(12, product.CostPrice);
        Assert.Equal(18, product.SalePrice);
        Assert.Equal(8, product.SafetyStock);
        Assert.Equal("bob", product.UpdatedBy);

        Assert.Equal("COK-0000001", result.Sku);
        Assert.Equal(1, result.CategoryId);
    }

    [Fact]
    public async Task UpdateAsync_商品不存在時應該拋出BusinessRuleException()
    {
        _productRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((Product?)null);

        var request = new UpdateProductRequest { Name = "新名稱", Unit = "包" };

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.UpdateAsync(999, request, currentUsername: "alice"));
    }

    [Fact]
    public async Task UpdateAsync_供應商不存在時應該拋出BusinessRuleException_且不儲存()
    {
        var product = new Product { Id = 1, Name = "舊名稱", Unit = "包", Sku = "COK-0000001" };
        _productRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(product);
        _supplierRepository.Setup(r => r.ExistsAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var request = new UpdateProductRequest { Name = "新名稱", Unit = "包", SupplierId = 999 };

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.UpdateAsync(1, request, currentUsername: "alice"));

        Assert.Equal("舊名稱", product.Name);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_應該軟刪除商品並記錄UpdatedBy()
    {
        var product = new Product { Id = 1, Name = "測試商品", Sku = "COK-0000001", CreatedBy = "alice", UpdatedBy = "alice" };
        _productRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        await _sut.DeleteAsync(1, currentUsername: "bob");

        Assert.True(product.IsDeleted);
        Assert.Equal("bob", product.UpdatedBy);
        Assert.Equal("alice", product.CreatedBy);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_商品不存在時應該拋出BusinessRuleException()
    {
        _productRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((Product?)null);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.GetByIdAsync(999));
    }
}
