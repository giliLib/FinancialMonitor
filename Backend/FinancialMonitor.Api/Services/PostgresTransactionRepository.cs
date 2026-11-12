
using FinancialMonitor.Api.Data;
using FinancialMonitor.Api.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FinancialMonitor.Api.Services;

public class PostgresTransactionRepository : ITransactionRepository
{
    private readonly FinancialMonitorDbContext _context;

    public PostgresTransactionRepository(
        FinancialMonitorDbContext context)
    {
        _context = context;
    }

    public bool Add(Transaction transaction)
    {
        _context.Transactions.Add(transaction);

        try
        {
            _context.SaveChanges();
            return true;
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation
            })
        {
            return false;
        }
    }

    public IEnumerable<Transaction> GetAll()
    {
        return _context.Transactions
            .AsNoTracking()
            .OrderByDescending(t => t.Timestamp)
            .ToList();
    }

    public Transaction? GetById(Guid id)
    {
        return _context.Transactions
            .AsNoTracking()
            .FirstOrDefault(t => t.TransactionId == id);
    }
}
