using MyErp.Application.Abstractions;
using MyErp.Domain.Entities;

namespace MyErp.Infra.Repositories;

public class InventoryTransactionRepository(MyErpDbContext db) : IInventoryTransactionRepository
{
    public void Add(InventoryTransaction transaction) => db.InventoryTransactions.Add(transaction);
}
