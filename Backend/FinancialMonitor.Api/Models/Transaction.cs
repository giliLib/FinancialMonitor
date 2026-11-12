using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using FinancialMonitor.Api.Constants;

namespace FinancialMonitor.Api.Models;

public enum TransactionStatus
{
    Pending,
    Completed,
    Failed
}

public class Transaction : IValidatableObject
{
    [JsonRequired]
    public Guid TransactionId { get; init; }

    [Range(typeof(decimal), "0.01", "79228162514264337593543950335",
        ErrorMessage = ErrorMessages.AmountGreaterThanZero)]
    [JsonRequired]
    public decimal Amount { get; init; }

    [Required(ErrorMessage = ErrorMessages.CurrencyRequired)]
    [RegularExpression("^[A-Z]{3}$",
        ErrorMessage = ErrorMessages.CurrencyInvalidFormat)]
    [JsonRequired]
    public string Currency { get; init; } = string.Empty;

    [JsonRequired]
    public TransactionStatus Status { get; init; }

    [JsonRequired]
    public DateTimeOffset Timestamp { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (TransactionId == Guid.Empty)
        {
            yield return new ValidationResult(
                ErrorMessages.TransactionIdRequired,
                new[] { nameof(TransactionId) });
        }

        if (!Enum.IsDefined(typeof(TransactionStatus), Status))
        {
            yield return new ValidationResult(
                ErrorMessages.StatusInvalid,
                new[] { nameof(Status) });
        }

        if (Timestamp == default)
        {
            yield return new ValidationResult(
                ErrorMessages.TimestampRequired,
                new[] { nameof(Timestamp) });
        }
    }
}