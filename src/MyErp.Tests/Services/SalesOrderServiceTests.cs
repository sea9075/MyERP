using Microsoft.Extensions.Logging;
using Moq;
using MyErp.Application.Abstractions;
using MyErp.Application.Common;
using MyErp.Application.DTOs;
using MyErp.Application.Services;
using MyErp.Domain.Entities;
using MyErp.Domain.Enums;
using Xunit;

namespace MyErp.Tests.Services;

public class SalesOrderServiceTests
{
    private readonly Mock<ISalesOrderRepository> _salesOrderRepository = new();
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<ICustomerRepository> _customerRepository = new();
    private readonly Mock<IInventoryTransactionRepository> _inventoryTransactionRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    // 2026-09-15 新增（worker 低庫存自動通知，見 Infra-Progress.md §31）：CreateAsync 成立後
    // 會呼叫 IEventPublisher 發布事件，這裡不特別驗證有沒有呼叫（那是附加的 best-effort 行為，
    // 不是這個 Service 的核心業務規則），只是讓建構子能正常組起來。
    private readonly Mock<IEventPublisher> _eventPublisher = new();
    private readonly Mock<ILogger<SalesOrderService>> _logger = new();
    private readonly SalesOrderService _sut;

    public SalesOrderServiceTests()
    {
        _unitOfWork
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task>, CancellationToken>((operation, _) => operation());

        _sut = new SalesOrderService(
            _salesOrderRepository.Object,
            _productRepository.Object,
            _customerRepository.Object,
            _inventoryTransactionRepository.Object,
            _unitOfWork.Object,
            _eventPublisher.Object,
            _logger.Object);
    }

    [Fact]
    public async Task CreateAsync_庫存不足時應該拋出BusinessRuleException_且不扣庫存()
    {
        var product = new Product { Id = 1, Name = "測試商品", Sku = "SKU-1", CurrentStock = 5 };
        _productRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        var request = new CreateSalesOrderRequest
        {
            Items = [new CreateSalesOrderItemRequest { ProductId = 1, Quantity = 10, UnitPrice = 20 }],
        };

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.CreateAsync(request, currentUserId: 1, currentUsername: "tester"));

        // 庫存不足應該整張單失敗，商品庫存完全不變。
        Assert.Equal(5, product.CurrentStock);
        _salesOrderRepository.Verify(r => r.Add(It.IsAny<SalesOrder>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_同一商品出現在多筆明細時應該加總檢查庫存()
    {
        // 單一明細各自看都不超過庫存（現有 10），但兩筆加起來 6+6=12 已經超過，應該視為庫存不足整張單失敗。
        var product = new Product { Id = 1, Name = "測試商品", Sku = "SKU-1", CurrentStock = 10 };
        _productRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        var request = new CreateSalesOrderRequest
        {
            Items =
            [
                new CreateSalesOrderItemRequest { ProductId = 1, Quantity = 6, UnitPrice = 20 },
                new CreateSalesOrderItemRequest { ProductId = 1, Quantity = 6, UnitPrice = 20 },
            ],
        };

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.CreateAsync(request, currentUserId: 1, currentUsername: "tester"));
        Assert.Equal(10, product.CurrentStock);
    }

    [Fact]
    public async Task CreateAsync_成功時應該扣減商品庫存並寫入InventoryTransaction與稽核欄位()
    {
        var product = new Product { Id = 1, Name = "測試商品", Sku = "SKU-1", CurrentStock = 30 };

        _productRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(product);
        _salesOrderRepository.Setup(r => r.CountByDatePrefixAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(0);

        SalesOrder? savedOrder = null;
        _salesOrderRepository
            .Setup(r => r.Add(It.IsAny<SalesOrder>()))
            .Callback<SalesOrder>(o => savedOrder = o);

        _unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() =>
            {
                if (savedOrder is not null && savedOrder.Id == 0)
                {
                    savedOrder.Id = 200;
                }
            })
            .ReturnsAsync(1);

        _salesOrderRepository
            .Setup(r => r.GetByIdAsync(200, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => savedOrder);

        var request = new CreateSalesOrderRequest
        {
            Items = [new CreateSalesOrderItemRequest { ProductId = 1, Quantity = 12, UnitPrice = 25 }],
        };

        var result = await _sut.CreateAsync(request, currentUserId: 7, currentUsername: "alice");

        Assert.Equal(18, product.CurrentStock);
        Assert.StartsWith("SO-", result.OrderNo);

        Assert.Equal("alice", result.CreatedBy);
        Assert.Equal("alice", result.UpdatedBy);
        Assert.Equal(result.CreatedAt, result.UpdatedAt);

        _inventoryTransactionRepository.Verify(r => r.Add(It.Is<InventoryTransaction>(t =>
            t.ProductId == 1 &&
            t.ChangeType == InventoryChangeType.Sale &&
            t.QuantityChange == -12 &&
            t.StockAfter == 18 &&
            t.CreatedByUserId == 7)), Times.Once);
    }

    [Fact]
    public async Task VoidAsync_成功時應該加回庫存並寫入SaleVoid異動與更新稽核欄位()
    {
        var product = new Product { Id = 1, Name = "測試商品", Sku = "SKU-1", CurrentStock = 18 };
        var order = new SalesOrder
        {
            Id = 200,
            OrderNo = "SO-20260914-001",
            Status = OrderStatus.Normal,
            CreatedBy = "alice",
            UpdatedBy = "alice",
            Items = [new SalesOrderItem { ProductId = 1, Product = product, Quantity = 12, UnitPrice = 25, Subtotal = 300 }],
        };

        _salesOrderRepository.Setup(r => r.GetByIdAsync(200, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _productRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        var result = await _sut.VoidAsync(200, currentUserId: 7, currentUsername: "bob");

        // 原本出貨扣掉的 12 要加回去：18 + 12 = 30。
        Assert.Equal(30, product.CurrentStock);
        Assert.Equal(nameof(OrderStatus.Voided), result.Status);
        Assert.Equal(OrderStatus.Voided, order.Status);

        Assert.Equal("alice", result.CreatedBy);
        Assert.Equal("bob", result.UpdatedBy);

        _inventoryTransactionRepository.Verify(r => r.Add(It.Is<InventoryTransaction>(t =>
            t.ProductId == 1 &&
            t.ChangeType == InventoryChangeType.SaleVoid &&
            t.QuantityChange == 12 &&
            t.StockAfter == 30 &&
            t.CreatedByUserId == 7)), Times.Once);
    }

    [Fact]
    public async Task VoidAsync_已經作廢過的單應該拋出BusinessRuleException()
    {
        var order = new SalesOrder { Id = 200, OrderNo = "SO-20260914-001", Status = OrderStatus.Voided, Items = [] };
        _salesOrderRepository.Setup(r => r.GetByIdAsync(200, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.VoidAsync(200, currentUserId: 7, currentUsername: "bob"));
    }
}
