
using FinancialMonitor.Api.Models;

namespace FinancialMonitor.Api.Services;

public interface ITransactionNotifier
{
    Task NotifyTransactionAddedAsync(
        Transaction transaction,
        CancellationToken cancellationToken = default
    );
}