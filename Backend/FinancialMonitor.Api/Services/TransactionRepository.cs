using System.Collections.Concurrent;
using FinancialMonitor.Api.Models;

namespace FinancialMonitor.Api.Services;

public class TransactionRepository : ITransactionRepository
{
    private readonly ConcurrentDictionary<Guid, Transaction> _transactions = new();

    public bool Add(Transaction transaction)
    {
        return _transactions.TryAdd(transaction.TransactionId, transaction);
    }

    public IEnumerable<Transaction> GetAll()
    {
        return _transactions.ToArray()
            .Select(item => item.Value)
            .OrderByDescending(t => t.Timestamp)
            .ToArray();
    }

    // חדש: שליפת עסקה ספציפית בבטחה מהזיכרון
    public Transaction? GetById(Guid id)
    {
        _transactions.TryGetValue(id, out var transaction);
        return transaction;
    }
}