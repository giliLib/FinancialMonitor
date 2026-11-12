namespace FinancialMonitor.Api.Constants;

public static class ErrorMessages
{
    public const string TransactionIdRequired = "TransactionId cannot be empty or default.";
    public const string AmountGreaterThanZero = "Amount must be greater than zero.";
    public const string CurrencyRequired = "Currency is required.";
    public const string CurrencyInvalidFormat = "Currency must be a 3-letter uppercase code (e.g. USD).";
    public const string StatusInvalid = "Invalid transaction status. Allowed values: Pending, Completed, Failed.";
    public const string TimestampRequired = "Timestamp is required.";
}