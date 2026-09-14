using Moq;
using MyErp.Application.Abstractions;
using MyErp.Application.Common;
using MyErp.Application.DTOs;
using MyErp.Application.Services;
using MyErp.Domain.Entities;
using MyErp.Domain.Enums;
using Xunit;

namespace MyErp.Tests.Services;

public class InventoryServiceTests
{
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<IInventoryTransactionRepository> _inventoryTransactionRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly InventoryService _sut;

    public InventoryServiceTests()
    {
        _sut = new InventoryService(
            _productRepository.Object,
            _inventoryTransactionRepository.Object,
            _unitOfWork.Object);
    }

    [Fact]
    public async Task AdjustAsync_調整量為正時應該增加庫存並寫入ManualAdjustment異動()
    {
        var product = new Product { Id = 1, Name = "測試商品", Sku = "SKU-1", CurrentStock = 10 };
        _productRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        var request = new AdjustInventoryRequest { ProductId = 1, AdjustmentQuantity = 5, Reason = "盤點多了 5 個" };

        var result = await _sut.AdjustAsync(request, currentUserId: 9, currentUsername: "alice");

        Assert.Equal(15, product.CurrentStock);
        Assert.Equal(15, result.CurrentStock);
        Assert.Equal("alice", product.UpdatedBy);

        _inventoryTransactionRepository.Verify(r => r.Add(It.Is<InventoryTransaction>(t =>
            t.ProductId == 1 &&
            t.ChangeType == InventoryChangeType.ManualAdjustment &&
            t.QuantityChange == 5 &&
            t.StockAfter == 15 &&
            t.Reason == "盤點多了 5 個" &&
            t.CreatedByUserId == 9)), Times.Once);
    }

    [Fact]
    public async Task AdjustAsync_調整量為負時應該減少庫存()
    {
        var product = new Product { Id = 1, Name = "測試商品", Sku = "SKU-1", CurrentStock = 10 };
        _productRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        var request = new AdjustInventoryRequest { ProductId = 1, AdjustmentQuantity = -3, Reason = "盤點少了 3 個" };

        var result = await _sut.AdjustAsync(request, currentUserId: 9, currentUsername: "alice");

        Assert.Equal(7, product.CurrentStock);
        Assert.Equal(7, result.CurrentStock);
        Assert.Equal("alice", product.UpdatedBy);

        _inventoryTransactionRepository.Verify(r => r.Add(It.Is<InventoryTransaction>(t =>
            t.ProductId == 1 &&
            t.ChangeType == InventoryChangeType.ManualAdjustment &&
            t.QuantityChange == -3 &&
            t.StockAfter == 7)), Times.Once);
    }

    [Fact]
    public async Task AdjustAsync_調整後庫存會變成負數時應該拋出BusinessRuleException_且不變動庫存()
    {
        var product = new Product { Id = 1, Name = "測試商品", Sku = "SKU-1", CurrentStock = 5 };
        _productRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        var request = new AdjustInventoryRequest { ProductId = 1, AdjustmentQuantity = -10, Reason = "測試" };

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.AdjustAsync(request, currentUserId: 9, currentUsername: "alice"));

        Assert.Equal(5, product.CurrentStock);
        _inventoryTransactionRepository.Verify(r => r.Add(It.IsAny<InventoryTransaction>()), Times.Never);
    }

    [Fact]
    public async Task AdjustAsync_調整量為0時應該拋出BusinessRuleException()
    {
        var request = new AdjustInventoryRequest { ProductId = 1, AdjustmentQuantity = 0, Reason = "測試" };

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.AdjustAsync(request, currentUserId: 9, currentUsername: "alice"));

        _productRepository.Verify(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AdjustAsync_商品不存在時應該拋出BusinessRuleException()
    {
        _productRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((Product?)null);

        var request = new AdjustInventoryRequest { ProductId = 999, AdjustmentQuantity = 1, Reason = "測試" };

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.AdjustAsync(request, currentUserId: 9, currentUsername: "alice"));
    }

    [Fact]
    public async Task GetTransactionsAsync_商品不存在時應該拋出BusinessRuleException()
    {
        _productRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((Product?)null);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.GetTransactionsAsync(999));
    }

    [Fact]
    public async Task GetTransactionsAsync_應該回傳該商品的異動紀錄()
    {
        var product = new Product { Id = 1, Name = "測試商品", Sku = "SKU-1", CurrentStock = 15 };
        _productRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        var transactions = new List<InventoryTransaction>
        {
            new()
            {
                Id = 1,
                ProductId = 1,
                ChangeType = InventoryChangeType.ManualAdjustment,
                QuantityChange = 5,
                StockAfter = 15,
                RefTable = "Manual",
                Reason = "盤點",
                CreatedByUserId = 9,
                CreatedAt = DateTime.UtcNow,
            },
        };
        _inventoryTransactionRepository
            .Setup(r => r.GetByProductIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactions);

        var result = await _sut.GetTransactionsAsync(1);

        Assert.Single(result);
        Assert.Equal(nameof(InventoryChangeType.ManualAdjustment), result[0].ChangeType);
        Assert.Equal(5, result[0].QuantityChange);
        Assert.Equal(15, result[0].StockAfter);
    }
}
