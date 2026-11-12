
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FinancialMonitor.Api.Controllers;
using FinancialMonitor.Api.Models;
using FinancialMonitor.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using FluentAssertions;

namespace FinancialMonitor.Tests.Controllers;

public class TransactionsControllerTests
{
    private readonly TransactionRepository _repository;
    private readonly FakeTransactionNotifier _notifier;
    private readonly TransactionsController _controller;

    public TransactionsControllerTests()
    {
        _repository = new TransactionRepository();
        _notifier = new FakeTransactionNotifier();

        _controller = new TransactionsController(
            _repository,
            _notifier
        );
    }

    [Fact]
    public void GetAll_ShouldReturnAllTransactions()
    {
        // Arrange
        var transaction1 = CreateTransaction();
        var transaction2 = CreateTransaction();

        _repository.Add(transaction1);
        _repository.Add(transaction2);

        // Act
        var result = _controller.GetAll();

        // Assert
        var okResult = result.Result.Should()
            .BeOfType<OkObjectResult>()
            .Subject;

        var transactions = okResult.Value
            .Should()
            .BeAssignableTo<System.Collections.Generic.IEnumerable<Transaction>>()
            .Subject;

        transactions.Should().HaveCount(2);
    }

    [Fact]
    public void GetById_ExistingTransaction_ShouldReturnOk()
    {
        // Arrange
        var transaction = CreateTransaction();
        _repository.Add(transaction);

        // Act
        var result = _controller.GetById(transaction.TransactionId);

        // Assert
        var okResult = result.Result.Should()
            .BeOfType<OkObjectResult>()
            .Subject;

        var returnedTransaction = okResult.Value
            .Should()
            .BeOfType<Transaction>()
            .Subject;

        returnedTransaction.TransactionId
            .Should().Be(transaction.TransactionId);
    }

    [Fact]
    public void GetById_NonExistingTransaction_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act
        var result = _controller.GetById(nonExistingId);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Create_NewTransaction_ShouldReturnCreatedAndNotify()
    {
        // Arrange
        var transaction = CreateTransaction();

        // Act
        var result = await _controller.Create(transaction);

        // Assert
        var createdResult = result.Result.Should()
            .BeOfType<CreatedAtActionResult>()
            .Subject;

        createdResult.ActionName.Should().Be(nameof(TransactionsController.GetById));

        createdResult.RouteValues!["id"]
            .Should().Be(transaction.TransactionId);

        createdResult.Value.Should().BeEquivalentTo(transaction);

        _notifier.CallCount.Should().Be(1);
        _notifier.LastTransaction.Should().Be(transaction);

        _repository.GetById(transaction.TransactionId)
            .Should().NotBeNull();
    }

    [Fact]
    public async Task Create_DuplicateTransaction_ShouldReturnConflictAndNotNotify()
    {
        // Arrange
        var transaction = CreateTransaction();
        _repository.Add(transaction);

        // Act
        var result = await _controller.Create(transaction);

        // Assert
        var conflictResult = result.Result.Should()
            .BeOfType<ConflictObjectResult>()
            .Subject;

        conflictResult.Value.Should().NotBeNull();

        _notifier.CallCount.Should().Be(0);
    }

    private static Transaction CreateTransaction()
    {
        return new Transaction
        {
            TransactionId = Guid.NewGuid(),
            Amount = 150.50m,
            Currency = "ILS",
            Status = TransactionStatus.Pending,
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    private class FakeTransactionNotifier : ITransactionNotifier
    {
        public int CallCount { get; private set; }

        public Transaction? LastTransaction { get; private set; }

        public Task NotifyTransactionAddedAsync(
            Transaction transaction,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastTransaction = transaction;

            return Task.CompletedTask;
        }
    }
}
