using Microsoft.EntityFrameworkCore;
using MyErp.Application.Abstractions;
using MyErp.Domain.Entities;

namespace MyErp.Infra.Repositories;

public class InventoryTransactionRepository(MyErpDbContext db) : IInventoryTransactionRepository
{
    public void Add(InventoryTransaction transaction) => db.InventoryTransactions.Add(transaction);

    public Task<List<InventoryTransaction>> GetByProductIdAsync(int productId, CancellationToken ct = default) =>
        db.InventoryTransactions
            .AsNoTracking()
            .Include(t => t.CreatedByUser)
            .Where(t => t.ProductId == productId)
            .OrderByDescending(t => t.CreatedAt)
            .ThenByDescending(t => t.Id)
            .ToListAsync(ct);
}
