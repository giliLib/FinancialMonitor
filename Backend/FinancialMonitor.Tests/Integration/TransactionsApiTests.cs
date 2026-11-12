
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using FinancialMonitor.Api.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace FinancialMonitor.Tests.Integration
{
    public class TransactionsApiTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters =
            {
                new JsonStringEnumConverter(
                    namingPolicy: null,
                    allowIntegerValues: false
                )
            }
        };

        public TransactionsApiTests(WebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        // Helper: יצירת עסקה תקינה
        private static Transaction CreateTransaction()
        {
            return new Transaction
            {
                TransactionId = Guid.NewGuid(),
                Amount = 250.00m,
                Currency = "ILS",
                Status = TransactionStatus.Pending,
                Timestamp = DateTimeOffset.UtcNow
            };
        }

        // 1. יצירת עסקה תקינה
        [Fact]
        public async Task CreateTransaction_ValidTransaction_ShouldReturnCreated()
        {
            // Arrange
            var transaction = CreateTransaction();

            // Act
            var response = await _client.PostAsJsonAsync(
                "/api/transactions",
                transaction,
                JsonOptions
            );

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var createdTransaction =
                await response.Content.ReadFromJsonAsync<Transaction>(JsonOptions);

            createdTransaction.Should().NotBeNull();
            createdTransaction!.TransactionId.Should()
                .Be(transaction.TransactionId);
        }

        // 2. סכום שלילי
        [Fact]
        public async Task CreateTransaction_NegativeAmount_ShouldReturnBadRequest()
        {
            // Arrange
            var transaction = CreateTransaction();

            transaction = new Transaction
            {
                TransactionId = transaction.TransactionId,
                Amount = -50.00m,
                Currency = transaction.Currency,
                Status = transaction.Status,
                Timestamp = transaction.Timestamp
            };

            // Act
            var response = await _client.PostAsJsonAsync(
                "/api/transactions",
                transaction,
                JsonOptions
            );

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        // 3. מזהה עסקה כפול
        [Fact]
        public async Task CreateTransaction_DuplicateId_ShouldReturnConflict()
        {
            // Arrange
            var transaction = CreateTransaction();

            // Act
            var firstResponse = await _client.PostAsJsonAsync(
                "/api/transactions",
                transaction,
                JsonOptions
            );

            var secondResponse = await _client.PostAsJsonAsync(
                "/api/transactions",
                transaction,
                JsonOptions
            );

            // Assert
            firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            secondResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
        }

        // 4. מטבע בפורמט לא תקין
        [Fact]
        public async Task CreateTransaction_InvalidCurrency_ShouldReturnBadRequest()
        {
            // Arrange
            var transaction = CreateTransaction();

            transaction = new Transaction
            {
                TransactionId = transaction.TransactionId,
                Amount = transaction.Amount,
                Currency = "ILS1",
                Status = transaction.Status,
                Timestamp = transaction.Timestamp
            };

            // Act
            var response = await _client.PostAsJsonAsync(
                "/api/transactions",
                transaction,
                JsonOptions
            );

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        // 5. סטטוס לא חוקי
        [Fact]
        public async Task CreateTransaction_InvalidStatus_ShouldReturnBadRequest()
        {
            // Arrange
            var payload = new
            {
                transactionId = Guid.NewGuid(),
                amount = 100.00m,
                currency = "ILS",
                status = "Unknown",
                timestamp = DateTimeOffset.UtcNow
            };

            var json = JsonSerializer.Serialize(payload);

            using var content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json"
            );

            // Act
            var response = await _client.PostAsync(
                "/api/transactions",
                content
            );

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        // 6. שליפת כל העסקאות
        [Fact]
        public async Task GetTransactions_ShouldReturnOkWithTransactions()
        {
            // Arrange
            var transaction = CreateTransaction();

            await _client.PostAsJsonAsync(
                "/api/transactions",
                transaction,
                JsonOptions
            );

            // Act
            var response = await _client.GetAsync("/api/transactions");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var transactions =
                await response.Content.ReadFromJsonAsync<List<Transaction>>(JsonOptions);

            transactions.Should().NotBeNull();
            transactions.Should().Contain(
                t => t.TransactionId == transaction.TransactionId
            );
        }

        // 7. שליפת עסקה שלא קיימת
        [Fact]
        public async Task GetTransaction_NonExistingId_ShouldReturnNotFound()
        {
            // Arrange
            var nonExistingId = Guid.NewGuid();

            // Act
            var response = await _client.GetAsync(
                $"/api/transactions/{nonExistingId}"
            );

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}
