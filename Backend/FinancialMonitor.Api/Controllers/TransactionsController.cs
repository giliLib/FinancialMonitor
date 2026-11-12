using FinancialMonitor.Api.Models;
using FinancialMonitor.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FinancialMonitor.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionRepository _repository;
    private readonly ITransactionNotifier _notifier;

    public TransactionsController(
        ITransactionRepository repository,
        ITransactionNotifier notifier)
    {
        _repository = repository;
        _notifier = notifier;
    }

    // GET: api/transactions - שליפת כל העסקאות
    [HttpGet]
    public ActionResult<IEnumerable<Transaction>> GetAll()
    {
        var transactions = _repository.GetAll();
        return Ok(transactions);
    }

    // GET: api/transactions/{id} - שליפת עסקה ספציפית לפי מזהה
    [HttpGet("{id:guid}")]
    public ActionResult<Transaction> GetById(Guid id)
    {
        var transaction = _repository.GetById(id);

        if (transaction is null)
        {
            return NotFound();
        }

        return Ok(transaction);
    }

    // POST: api/transactions - הוספת עסקה חדשה ושידור דרך ה-Notifier
    [HttpPost]
    public async Task<ActionResult<Transaction>> Create([FromBody] Transaction transaction)
    {
        var added = _repository.Add(transaction);

        if (!added)
        {
            return Conflict(new { message = $"Transaction '{transaction.TransactionId}' already exists." });
        }

        // שידור עצמאי דרך ממשק ה-Notifier
        await _notifier.NotifyTransactionAddedAsync(transaction);

        return CreatedAtAction(
            nameof(GetById),
            new { id = transaction.TransactionId },
            transaction
        );
    }
}