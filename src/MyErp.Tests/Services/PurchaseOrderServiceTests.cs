using Moq;
using MyErp.Application.Abstractions;
using MyErp.Application.Common;
using MyErp.Application.DTOs;
using MyErp.Application.Services;
using MyErp.Domain.Entities;
using MyErp.Domain.Enums;
using Xunit;

namespace MyErp.Tests.Services;

public class PurchaseOrderServiceTests
{
    private readonly Mock<IPurchaseOrderRepository> _purchaseOrderRepository = new();
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<ISupplierRepository> _supplierRepository = new();
    private readonly Mock<IInventoryTransactionRepository> _inventoryTransactionRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly PurchaseOrderService _sut;

    public PurchaseOrderServiceTests()
    {
        // ExecuteInTransactionAsync 在測試裡不需要真的開資料庫交易，直接執行傳進來的 operation 就好。
        _unitOfWork
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task>, CancellationToken>((operation, _) => operation());

        _sut = new PurchaseOrderService(
            _purchaseOrderRepository.Object,
            _productRepository.Object,
            _supplierRepository.Object,
            _inventoryTransactionRepository.Object,
            _unitOfWork.Object);
    }

    [Fact]
    public async Task CreateAsync_供應商不存在時應該拋出BusinessRuleException()
    {
        _supplierRepository.Setup(r => r.ExistsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var request = new CreatePurchaseOrderRequest
        {
            SupplierId = 999,
            Items = [new CreatePurchaseOrderItemRequest { ProductId = 1, Quantity = 5, UnitPrice = 10 }],
        };

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.CreateAsync(request, currentUserId: 1));
    }

    [Fact]
    public async Task CreateAsync_成功時應該增加商品庫存並寫入InventoryTransaction()
    {
        var product = new Product { Id = 1, Name = "測試商品", Sku = "SKU-1", CurrentStock = 10, CostPrice = 5 };

        _supplierRepository.Setup(r => r.ExistsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _productRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(product);
        _purchaseOrderRepository.Setup(r => r.CountByDatePrefixAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(0);

        PurchaseOrder? savedOrder = null;
        _purchaseOrderRepository
            .Setup(r => r.Add(It.IsAny<PurchaseOrder>()))
            .Callback<PurchaseOrder>(o => savedOrder = o);

        _unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() =>
            {
                // 模擬 EF Core SaveChanges 後拿到自動遞增的 Id。
                if (savedOrder is not null && savedOrder.Id == 0)
                {
                    savedOrder.Id = 100;
                }
            })
            .ReturnsAsync(1);

        _purchaseOrderRepository
            .Setup(r => r.GetByIdAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => savedOrder);

        var request = new CreatePurchaseOrderRequest
        {
            SupplierId = 1,
            Items = [new CreatePurchaseOrderItemRequest { ProductId = 1, Quantity = 20, UnitPrice = 8 }],
        };

        var result = await _sut.CreateAsync(request, currentUserId: 42);

        // 庫存應該從 10 增加 20 筆進貨量，變成 30。
        Assert.Equal(30, product.CurrentStock);
        // 進貨單成立時，商品的參考成本價要更新成這次的進貨單價（ERP.md §5.1「最近一次進貨價」）。
        Assert.Equal(8, product.CostPrice);
        Assert.StartsWith("PI-", result.OrderNo);

        _inventoryTransactionRepository.Verify(r => r.Add(It.Is<InventoryTransaction>(t =>
            t.ProductId == 1 &&
            t.ChangeType == InventoryChangeType.Purchase &&
            t.QuantityChange == 20 &&
            t.StockAfter == 30 &&
            t.CreatedByUserId == 42)), Times.Once);
    }
}
