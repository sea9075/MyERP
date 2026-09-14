using MyErp.Application.Abstractions;
using MyErp.Application.Common;
using MyErp.Application.DTOs;
using MyErp.Domain.Entities;

namespace MyErp.Application.Services;

public interface ISupplierService
{
    Task<List<SupplierDto>> GetAllAsync(bool includeInactive, CancellationToken ct = default);
    Task<SupplierDto> CreateAsync(CreateSupplierRequest request, CancellationToken ct = default);
    Task<SupplierDto> UpdateAsync(int id, UpdateSupplierRequest request, CancellationToken ct = default);

    /// <summary>Supplier 有 IsActive 欄位（ERP.md §5.1），DELETE 採軟刪除，不做實體刪除。</summary>
    Task DeleteAsync(int id, CancellationToken ct = default);
}

public class SupplierService(ISupplierRepository supplierRepository, IUnitOfWork unitOfWork) : ISupplierService
{
    public async Task<List<SupplierDto>> GetAllAsync(bool includeInactive, CancellationToken ct = default)
    {
        var suppliers = await supplierRepository.GetAllAsync(includeInactive, ct);
        return suppliers.Select(ToDto).ToList();
    }

    public async Task<SupplierDto> CreateAsync(CreateSupplierRequest request, CancellationToken ct = default)
    {
        var supplier = new Supplier
        {
            Name = request.Name,
            ContactPerson = request.ContactPerson,
            Phone = request.Phone,
            Address = request.Address,
            Note = request.Note,
            IsActive = true,
        };

        supplierRepository.Add(supplier);
        await unitOfWork.SaveChangesAsync(ct);
        return ToDto(supplier);
    }

    public async Task<SupplierDto> UpdateAsync(int id, UpdateSupplierRequest request, CancellationToken ct = default)
    {
        var supplier = await supplierRepository.GetByIdAsync(id, ct)
            ?? throw new BusinessRuleException($"找不到供應商 (Id={id})。");

        supplier.Name = request.Name;
        supplier.ContactPerson = request.ContactPerson;
        supplier.Phone = request.Phone;
        supplier.Address = request.Address;
        supplier.Note = request.Note;
        supplier.IsActive = request.IsActive;

        await unitOfWork.SaveChangesAsync(ct);
        return ToDto(supplier);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var supplier = await supplierRepository.GetByIdAsync(id, ct)
            ?? throw new BusinessRuleException($"找不到供應商 (Id={id})。");

        supplier.IsActive = false;
        await unitOfWork.SaveChangesAsync(ct);
    }

    private static SupplierDto ToDto(Supplier supplier) => new()
    {
        Id = supplier.Id,
        Name = supplier.Name,
        ContactPerson = supplier.ContactPerson,
        Phone = supplier.Phone,
        Address = supplier.Address,
        Note = supplier.Note,
        IsActive = supplier.IsActive,
    };
}
