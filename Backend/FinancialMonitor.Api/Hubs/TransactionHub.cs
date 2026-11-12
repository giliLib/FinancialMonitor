using Microsoft.AspNetCore.SignalR;

namespace FinancialMonitor.Api.Hubs
{
    public class TransactionHub : Hub
    {
        // ה-Hub יכול להישאר ריק כרגע, משום שאנו משתמשים בו כדי לשדר מהשרת אל הלקוחות.
        // הלקוחות יתחברו לנתיב הזה כדי להאזין לעסקאות חדשות.
    }
}