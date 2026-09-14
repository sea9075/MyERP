using MyErp.Application.Abstractions;
using MyErp.Application.Common;
using MyErp.Application.DTOs;
using MyErp.Domain.Entities;

namespace MyErp.Application.Services;

/// <summary>ERP.md §8 Phase 2 項目 11：客戶管理 CRUD。</summary>
public interface ICustomerService
{
    Task<List<CustomerDto>> GetAllAsync(bool includeDeleted, CancellationToken ct = default);
    Task<CustomerDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<CustomerDto> CreateAsync(CreateCustomerRequest request, string currentUsername, CancellationToken ct = default);
    Task<CustomerDto> UpdateAsync(int id, UpdateCustomerRequest request, string currentUsername, CancellationToken ct = default);

    /// <summary>軟刪除（IsDeleted=true）。刪除前檢查是否還有出貨單引用。</summary>
    Task DeleteAsync(int id, string currentUsername, CancellationToken ct = default);
}

public class CustomerService(ICustomerRepository customerRepository, IUnitOfWork unitOfWork) : ICustomerService
{
    public async Task<List<CustomerDto>> GetAllAsync(bool includeDeleted, CancellationToken ct = default)
    {
        var customers = await customerRepository.GetAllAsync(includeDeleted, ct);
        return customers.Select(ToDto).ToList();
    }

    public async Task<CustomerDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var customer = await customerRepository.GetByIdAsync(id, ct)
            ?? throw new BusinessRuleException($"找不到客戶 (Id={id})。");
        return ToDto(customer);
    }

    public async Task<CustomerDto> CreateAsync(CreateCustomerRequest request, string currentUsername, CancellationToken ct = default)
    {
        var customer = new Customer
        {
            Name = request.Name,
            Phone = request.Phone,
            Note = request.Note,
        };
        customer.InitializeAudit(currentUsername);

        customerRepository.Add(customer);
        await unitOfWork.SaveChangesAsync(ct);
        return ToDto(customer);
    }

    public async Task<CustomerDto> UpdateAsync(int id, UpdateCustomerRequest request, string currentUsername, CancellationToken ct = default)
    {
        var customer = await customerRepository.GetByIdAsync(id, ct)
            ?? throw new BusinessRuleException($"找不到客戶 (Id={id})。");

        customer.Name = request.Name;
        customer.Phone = request.Phone;
        customer.Note = request.Note;
        customer.TouchUpdated(currentUsername);

        await unitOfWork.SaveChangesAsync(ct);
        return ToDto(customer);
    }

    public async Task DeleteAsync(int id, string currentUsername, CancellationToken ct = default)
    {
        var customer = await customerRepository.GetByIdAsync(id, ct)
            ?? throw new BusinessRuleException($"找不到客戶 (Id={id})。");

        if (await customerRepository.HasSalesOrdersAsync(id, ct))
        {
            throw new BusinessRuleException("這個客戶還有出貨單記錄，不能刪除（可避免歷史單據變成孤兒資料）。");
        }

        customer.SoftDelete(currentUsername);
        await unitOfWork.SaveChangesAsync(ct);
    }

    private static CustomerDto ToDto(Customer customer) => new()
    {
        Id = customer.Id,
        Name = customer.Name,
        Phone = customer.Phone,
        Note = customer.Note,
        CreatedAt = customer.CreatedAt,
        UpdatedAt = customer.UpdatedAt,
        CreatedBy = customer.CreatedBy,
        UpdatedBy = customer.UpdatedBy,
        IsDeleted = customer.IsDeleted,
    };
}
