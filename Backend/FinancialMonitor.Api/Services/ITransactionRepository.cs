
using FinancialMonitor.Api.Models;

namespace FinancialMonitor.Api.Services;

public interface ITransactionRepository
{
  bool Add(Transaction transaction);
    IEnumerable<Transaction> GetAll();
    Transaction? GetById(Guid id); //  שליפת עסקה בודדת לפי מזהה
    
}
