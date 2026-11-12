// Services/SignalRTransactionNotifier.cs
using FinancialMonitor.Api.Constants;
using FinancialMonitor.Api.Hubs;
using FinancialMonitor.Api.Models;
using Microsoft.AspNetCore.SignalR;

namespace FinancialMonitor.Api.Services;

public class SignalRTransactionNotifier : ITransactionNotifier
{
    private readonly IHubContext<TransactionHub> _hubContext;

    public SignalRTransactionNotifier(IHubContext<TransactionHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyTransactionAddedAsync(
        Transaction transaction,
        CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.All.SendAsync(
            SignalREvents.ReceiveTransaction,
            transaction,
            cancellationToken
        );
    }
}