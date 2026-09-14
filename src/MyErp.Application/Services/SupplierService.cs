using MyErp.Application.Abstractions;
using MyErp.Application.Common;
using MyErp.Application.DTOs;
using MyErp.Domain.Entities;

namespace MyErp.Application.Services;

public interface ISupplierService
{
    Task<List<SupplierDto>> GetAllAsync(bool includeDeleted, CancellationToken ct = default);
    Task<SupplierDto> CreateAsync(CreateSupplierRequest request, string currentUsername, CancellationToken ct = default);
    Task<SupplierDto> UpdateAsync(int id, UpdateSupplierRequest request, string currentUsername, CancellationToken ct = default);

    /// <summary>Supplier 採軟刪除（IsDeleted=true），不做實體刪除。</summary>
    Task DeleteAsync(int id, string currentUsername, CancellationToken ct = default);
}

public class SupplierService(ISupplierRepository supplierRepository, IUnitOfWork unitOfWork) : ISupplierService
{
    public async Task<List<SupplierDto>> GetAllAsync(bool includeDeleted, CancellationToken ct = default)
    {
        var suppliers = await supplierRepository.GetAllAsync(includeDeleted, ct);
        return suppliers.Select(ToDto).ToList();
    }

    public async Task<SupplierDto> CreateAsync(CreateSupplierRequest request, string currentUsername, CancellationToken ct = default)
    {
        var name = request.Name.TrimRequired();
        if (await supplierRepository.NameExistsAsync(name, excludeId: null, ct))
        {
            throw new BusinessRuleException($"供應商名稱 '{name}' 已經存在。");
        }

        var supplier = new Supplier
        {
            Name = name,
            ContactPerson = request.ContactPerson.TrimOrNull(),
            Phone = request.Phone.TrimOrNull(),
            Address = request.Address.TrimOrNull(),
            Note = request.Note.TrimOrNull(),
        };
        supplier.InitializeAudit(currentUsername);

        supplierRepository.Add(supplier);
        await unitOfWork.SaveChangesAsync(ct);
        return ToDto(supplier);
    }

    public async Task<SupplierDto> UpdateAsync(int id, UpdateSupplierRequest request, string currentUsername, CancellationToken ct = default)
    {
        var supplier = await supplierRepository.GetByIdAsync(id, ct)
            ?? throw new BusinessRuleException($"找不到供應商 (Id={id})。");

        var name = request.Name.TrimRequired();
        if (await supplierRepository.NameExistsAsync(name, excludeId: id, ct))
        {
            throw new BusinessRuleException($"供應商名稱 '{name}' 已經存在。");
        }

        supplier.Name = name;
        supplier.ContactPerson = request.ContactPerson.TrimOrNull();
        supplier.Phone = request.Phone.TrimOrNull();
        supplier.Address = request.Address.TrimOrNull();
        supplier.Note = request.Note.TrimOrNull();
        supplier.IsDeleted = request.IsDeleted;
        supplier.TouchUpdated(currentUsername);

        await unitOfWork.SaveChangesAsync(ct);
        return ToDto(supplier);
    }

    public async Task DeleteAsync(int id, string currentUsername, CancellationToken ct = default)
    {
        var supplier = await supplierRepository.GetByIdAsync(id, ct)
            ?? throw new BusinessRuleException($"找不到供應商 (Id={id})。");

        supplier.SoftDelete(currentUsername);
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
        CreatedAt = supplier.CreatedAt,
        UpdatedAt = supplier.UpdatedAt,
        CreatedBy = supplier.CreatedBy,
        UpdatedBy = supplier.UpdatedBy,
        IsDeleted = supplier.IsDeleted,
    };
}
