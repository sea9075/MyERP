using Moq;
using MyErp.Application.Abstractions;
using MyErp.Application.Common;
using MyErp.Application.DTOs;
using MyErp.Application.Services;
using MyErp.Domain.Entities;
using Xunit;

namespace MyErp.Tests.Services;

public class CustomerServiceTests
{
    private readonly Mock<ICustomerRepository> _customerRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly CustomerService _sut;

    public CustomerServiceTests()
    {
        _sut = new CustomerService(_customerRepository.Object, _unitOfWork.Object);
    }

    [Fact]
    public async Task GetAllAsync_應該回傳所有客戶()
    {
        var customers = new List<Customer>
        {
            new() { Id = 1, Name = "客戶A", Phone = "0912345678" },
            new() { Id = 2, Name = "客戶B" },
        };
        _customerRepository.Setup(r => r.GetAllAsync(false, It.IsAny<CancellationToken>())).ReturnsAsync(customers);

        var result = await _sut.GetAllAsync(includeDeleted: false);

        Assert.Equal(2, result.Count);
        Assert.Equal("客戶A", result[0].Name);
    }

    [Fact]
    public async Task GetByIdAsync_客戶不存在時應該拋出BusinessRuleException()
    {
        _customerRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((Customer?)null);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.GetByIdAsync(999));
    }

    [Fact]
    public async Task CreateAsync_應該新增客戶並設定稽核欄位()
    {
        var request = new CreateCustomerRequest { Name = "新客戶", Phone = "0987654321", Note = "備註" };

        Customer? added = null;
        _customerRepository.Setup(r => r.Add(It.IsAny<Customer>())).Callback<Customer>(c => added = c);

        var result = await _sut.CreateAsync(request, currentUsername: "alice");

        Assert.NotNull(added);
        Assert.Equal("新客戶", added!.Name);
        Assert.Equal("0987654321", added.Phone);
        Assert.Equal("新客戶", result.Name);

        // 新增時 createdAt/updatedAt/createdBy/updatedBy 都設一樣的值。
        Assert.Equal("alice", result.CreatedBy);
        Assert.Equal("alice", result.UpdatedBy);
        Assert.Equal(result.CreatedAt, result.UpdatedAt);
        Assert.False(result.IsDeleted);

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_應該修剪名稱電話備註頭尾空白()
    {
        var request = new CreateCustomerRequest { Name = "  新客戶  ", Phone = " 0987654321 ", Note = "  備註  " };

        Customer? added = null;
        _customerRepository.Setup(r => r.Add(It.IsAny<Customer>())).Callback<Customer>(c => added = c);

        var result = await _sut.CreateAsync(request, currentUsername: "alice");

        _customerRepository.Verify(r => r.NameExistsAsync("新客戶", null, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal("新客戶", added!.Name);
        Assert.Equal("0987654321", added.Phone);
        Assert.Equal("備註", added.Note);
        Assert.Equal("新客戶", result.Name);
    }

    [Fact]
    public async Task CreateAsync_名稱重複時應該拋出BusinessRuleException_且不新增()
    {
        _customerRepository.Setup(r => r.NameExistsAsync("客戶A", null, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var request = new CreateCustomerRequest { Name = "客戶A" };

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.CreateAsync(request, currentUsername: "alice"));

        _customerRepository.Verify(r => r.Add(It.IsAny<Customer>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_名稱重複時應該拋出BusinessRuleException_且排除自己()
    {
        var customer = new Customer { Id = 1, Name = "舊名稱" };
        _customerRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(customer);
        _customerRepository.Setup(r => r.NameExistsAsync("客戶B", 1, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var request = new UpdateCustomerRequest { Name = "客戶B" };

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.UpdateAsync(1, request, currentUsername: "alice"));

        Assert.Equal("舊名稱", customer.Name);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_客戶不存在時應該拋出BusinessRuleException()
    {
        _customerRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((Customer?)null);

        var request = new UpdateCustomerRequest { Name = "更新名稱" };

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.UpdateAsync(999, request, currentUsername: "alice"));
    }

    [Fact]
    public async Task UpdateAsync_應該更新客戶欄位並更新UpdatedBy()
    {
        var customer = new Customer { Id = 1, Name = "舊名稱", Phone = "0900000000", CreatedBy = "alice", UpdatedBy = "alice" };
        _customerRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(customer);

        var request = new UpdateCustomerRequest { Name = "新名稱", Phone = "0911111111", Note = "更新備註" };

        var result = await _sut.UpdateAsync(1, request, currentUsername: "bob");

        Assert.Equal("新名稱", customer.Name);
        Assert.Equal("0911111111", customer.Phone);
        Assert.Equal("更新備註", customer.Note);
        Assert.Equal("新名稱", result.Name);

        // CreatedBy 維持不變，UpdatedBy 變成執行更新的人。
        Assert.Equal("alice", result.CreatedBy);
        Assert.Equal("bob", result.UpdatedBy);
    }

    [Fact]
    public async Task DeleteAsync_客戶不存在時應該拋出BusinessRuleException()
    {
        _customerRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((Customer?)null);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.DeleteAsync(999, currentUsername: "alice"));
    }

    [Fact]
    public async Task DeleteAsync_客戶還有出貨單時應該拋出BusinessRuleException_且不軟刪除()
    {
        var customer = new Customer { Id = 1, Name = "客戶A" };
        _customerRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(customer);
        _customerRepository.Setup(r => r.HasSalesOrdersAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.DeleteAsync(1, currentUsername: "alice"));

        Assert.False(customer.IsDeleted);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_沒有出貨單時應該軟刪除客戶並記錄UpdatedBy()
    {
        var customer = new Customer { Id = 1, Name = "客戶A", CreatedBy = "alice", UpdatedBy = "alice" };
        _customerRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(customer);
        _customerRepository.Setup(r => r.HasSalesOrdersAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        await _sut.DeleteAsync(1, currentUsername: "bob");

        // 軟刪除：IsDeleted=true，UpdatedBy 變成執行刪除的人，CreatedBy 不變。
        Assert.True(customer.IsDeleted);
        Assert.Equal("bob", customer.UpdatedBy);
        Assert.Equal("alice", customer.CreatedBy);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
