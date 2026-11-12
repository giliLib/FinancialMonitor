
using Xunit;
using FluentAssertions;
using FinancialMonitor.Api.Models;
using FinancialMonitor.Api.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FinancialMonitor.Tests.Services
{
    public class TransactionRepositoryTests
    {
        [Fact]
        public void AddTransaction_ShouldStoreAndRetrieveSuccessfully()
        {
            // Arrange
            var repository = new TransactionRepository();
            var txId = Guid.NewGuid();

            var tx = CreateTransaction(txId);

            // Act
            var added = repository.Add(tx);
            var retrieved = repository.GetById(txId);

            // Assert
            added.Should().BeTrue();
            retrieved.Should().NotBeNull();
            retrieved!.TransactionId.Should().Be(txId);
            retrieved.Amount.Should().Be(150.50m);
            retrieved.Currency.Should().Be("ILS");
            retrieved.Status.Should().Be(TransactionStatus.Pending);
            retrieved.Timestamp.Should().Be(tx.Timestamp);
        }

        [Fact]
        public void AddTransaction_DuplicateId_ShouldReturnFalse()
        {
            // Arrange
            var repository = new TransactionRepository();
            var sharedId = Guid.NewGuid();

            var tx1 = CreateTransaction(sharedId, 100m);
            var tx2 = CreateTransaction(sharedId, 200m);

            // Act
            var firstAdd = repository.Add(tx1);
            var secondAdd = repository.Add(tx2);

            // Assert
            firstAdd.Should().BeTrue();
            secondAdd.Should().BeFalse();

            repository.GetAll().Should().ContainSingle();
            repository.GetById(sharedId)!.Amount.Should().Be(100m);
        }

        [Fact]
        public void GetById_NonExistingId_ShouldReturnNull()
        {
            // Arrange
            var repository = new TransactionRepository();

            // Act
            var result = repository.GetById(Guid.NewGuid());

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void GetAll_ShouldReturnTransactionsOrderedByTimestampDescending()
        {
            // Arrange
            var repository = new TransactionRepository();

            var olderTransaction = CreateTransaction(
                Guid.NewGuid(),
                timestamp: DateTimeOffset.UtcNow.AddMinutes(-10));

            var newerTransaction = CreateTransaction(
                Guid.NewGuid(),
                timestamp: DateTimeOffset.UtcNow);

            repository.Add(olderTransaction);
            repository.Add(newerTransaction);

            // Act
            // Act
            var result = repository.GetAll().ToList();

            // Assert
            result.Should().HaveCount(2);
            result[0].TransactionId.Should().Be(newerTransaction.TransactionId);
            result[1].TransactionId.Should().Be(olderTransaction.TransactionId);
        }

        [Fact]
        public async Task AddTransactions_Concurrently_ShouldStoreAllUniqueTransactions()
        {
            // Arrange
            var repository = new TransactionRepository();

            // Act
            var tasks = Enumerable.Range(0, 100)
                .Select(_ => Task.Run(() =>
                    repository.Add(CreateTransaction(Guid.NewGuid()))));

            var results = await Task.WhenAll(tasks);

            // Assert
            results.Should().AllBeEquivalentTo(true);
            repository.GetAll().Should().HaveCount(100);
        }

        [Fact]
        public async Task AddSameTransactionConcurrently_ShouldAllowOnlyOneInsertion()
        {
            // Arrange
            var repository = new TransactionRepository();
            var sharedId = Guid.NewGuid();

            // Act
            var tasks = Enumerable.Range(0, 100)
                .Select(i => Task.Run(() =>
                    repository.Add(CreateTransaction(sharedId, i + 1m))));

            var results = await Task.WhenAll(tasks);

            // Assert
            results.Count(result => result).Should().Be(1);
            results.Count(result => !result).Should().Be(99);

            repository.GetAll().Should().ContainSingle();
        }

        private static Transaction CreateTransaction(
            Guid id,
            decimal amount = 150.50m,
            DateTimeOffset? timestamp = null)
        {
            return new Transaction
            {
                TransactionId = id,
                Amount = amount,
                Currency = "ILS",
                Status = TransactionStatus.Pending,
                Timestamp = timestamp ?? DateTimeOffset.UtcNow
            };
        }
    }
}
