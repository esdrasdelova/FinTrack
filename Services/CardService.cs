using FinTrack.Models;
using FinTrack.Repositories;
using FinTrack.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.Services;

public interface ICardService
{
    Task<CardListViewModel> GetListAsync();
    Task<CardFormViewModel?> GetFormAsync(int id);
    Task<CardFormViewModel> GetNewFormAsync();
    Task<(bool Success, string Message)> SaveAsync(CardFormViewModel model);
    Task<(bool Success, string Message)> DeleteAsync(int id);
    Task<List<SelectOption>> GetAccountOptionsAsync();
    Task<(bool Success, string Message)> PayInvoiceAsync(PayInvoiceViewModel model);
}

public class CardService : ICardService
{
    private readonly IUnitOfWork _uow;
    private readonly ITransactionService _transactions;

    public CardService(IUnitOfWork uow, ITransactionService transactions)
    {
        _uow = uow;
        _transactions = transactions;
    }

    public async Task<CardListViewModel> GetListAsync()
    {
        var cards = await _uow.CreditCards.Query()
            .Include(c => c.Transactions)
            .Include(c => c.Invoices)
            .OrderBy(c => c.IsActive ? 0 : 1).ThenBy(c => c.Name)
            .ToListAsync();

        var reference = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var rows = cards.Select(c => new CardRowViewModel
        {
            Id = c.Id, Name = c.Name, Institution = c.Institution ?? string.Empty, Brand = c.Brand ?? string.Empty,
            LastFourDigits = c.LastFourDigits, CreditLimit = c.CreditLimit, ClosingDay = c.ClosingDay,
            DueDay = c.DueDay, Color = c.Color, IsActive = c.IsActive,
            Used = c.Transactions.Where(t => t.Type == TransactionType.Expense && t.Status == TransactionStatus.Completed
                                            && t.Date >= reference && t.Date < reference.AddMonths(1)).Sum(t => t.Amount)
        }).ToList();

        var invoices = await _uow.CreditCardInvoices.Query()
            .Include(i => i.CreditCard)
            .Where(i => i.Status != InvoiceStatus.Paid)
            .OrderBy(i => i.DueDate)
            .Take(20)
            .ToListAsync();

        var invoiceRows = invoices.Select(i => new InvoiceRowViewModel
        {
            Id = i.Id, CreditCardId = i.CreditCardId,
            CardName = i.CreditCard?.Name ?? "Cartão", CardColor = i.CreditCard?.Color ?? "#8b5cf6",
            Reference = i.ReferenceMonth.ToString("MM/yyyy"), DueDate = i.DueDate,
            TotalAmount = i.TotalAmount, PaidAmount = i.PaidAmount, Status = i.Status
        }).ToList();

        return new CardListViewModel
        {
            Cards = rows,
            Invoices = invoiceRows,
            TotalLimit = rows.Where(c => c.IsActive).Sum(c => c.CreditLimit),
            TotalUsed = rows.Where(c => c.IsActive).Sum(c => c.Used),
            TotalInvoices = invoiceRows.Sum(i => i.Remaining),
            UsageChartJson = ChartBuilder.Bar(
                rows.Where(c => c.IsActive).Select(c => c.Name.Length > 12 ? c.Name[..12] + "…" : c.Name),
                new[]
                {
                    new ChartSeriesDto { Name = "Utilizado", Color = "#f43f5e", Data = rows.Where(c => c.IsActive).Select(c => c.Used).ToList() },
                    new ChartSeriesDto { Name = "Disponível", Color = "#10b981", Data = rows.Where(c => c.IsActive).Select(c => c.Available).ToList() }
                }, stacked: true)
        };
    }

    public async Task<CardFormViewModel?> GetFormAsync(int id)
    {
        var c = await _uow.CreditCards.GetByIdAsync(id);
        return c is null ? null : new CardFormViewModel
        {
            Id = c.Id, Name = c.Name, Institution = c.Institution, Brand = c.Brand,
            LastFourDigits = c.LastFourDigits, CreditLimit = c.CreditLimit, ClosingDay = c.ClosingDay,
            DueDay = c.DueDay, Color = c.Color, IsActive = c.IsActive
        };
    }

    public Task<CardFormViewModel> GetNewFormAsync() => Task.FromResult(new CardFormViewModel
    {
        CreditLimit = 5000, ClosingDay = 25, DueDay = 10, IsActive = true, Color = "#8b5cf6"
    });

    public async Task<(bool Success, string Message)> SaveAsync(CardFormViewModel model)
    {
        CreditCard entity;
        var isNew = model.Id <= 0;
        if (isNew)
        {
            entity = new CreditCard { CreatedAt = DateTime.UtcNow };
            await _uow.CreditCards.AddAsync(entity);
        }
        else
        {
            var existing = await _uow.CreditCards.GetByIdAsync(model.Id, asNoTracking: false);
            if (existing is null)
                return (false, "Cartão não encontrado.");
            entity = existing;
        }

        entity.Name = model.Name.Trim();
        entity.Institution = string.IsNullOrWhiteSpace(model.Institution) ? null : model.Institution.Trim();
        entity.Brand = string.IsNullOrWhiteSpace(model.Brand) ? null : model.Brand.Trim();
        entity.LastFourDigits = model.LastFourDigits;
        entity.CreditLimit = model.CreditLimit;
        entity.ClosingDay = model.ClosingDay;
        entity.DueDay = model.DueDay;
        entity.Color = string.IsNullOrWhiteSpace(model.Color) ? "#8b5cf6" : model.Color;
        entity.IsActive = model.IsActive;

        await _uow.SaveChangesAsync();
        await _transactions.RecalculateBalancesAsync();
        await _uow.SaveChangesAsync();
        return (true, isNew ? "Cartão cadastrado com sucesso." : "Cartão atualizado com sucesso.");
    }

    public async Task<(bool Success, string Message)> DeleteAsync(int id)
    {
        var entity = await _uow.CreditCards.Query(false)
            .Include(c => c.Transactions).Include(c => c.Invoices)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (entity is null)
            return (false, "Cartão não encontrado.");
        if (entity.Transactions.Count > 0 || entity.Invoices.Count > 0)
            return (false, "Não é possível excluir este cartão pois existem transações ou faturas vinculadas. Desative-o para ocultá-lo.");

        _uow.CreditCards.Remove(entity);
        await _uow.SaveChangesAsync();
        return (true, "Cartão excluído com sucesso.");
    }

    public async Task<List<SelectOption>> GetAccountOptionsAsync()
    {
        var accounts = await _uow.Accounts.Query().Where(a => a.IsActive).OrderBy(a => a.Name).ToListAsync();
        return accounts.Select(a => new SelectOption { Value = a.Id.ToString(), Text = a.Name }).ToList();
    }

    public async Task<(bool Success, string Message)> PayInvoiceAsync(PayInvoiceViewModel model)
    {
        if (model.Amount <= 0)
            return (false, "Informe um valor válido.");
        if (!model.AccountId.HasValue || !await _uow.Accounts.AnyAsync(a => a.Id == model.AccountId.Value))
            return (false, "Selecione a conta de débito.");

        var invoice = await _uow.CreditCardInvoices.GetByIdAsync(model.InvoiceId, asNoTracking: false);
        if (invoice is null)
            return (false, "Fatura não encontrada.");
        if (invoice.Status == InvoiceStatus.Paid)
            return (false, "Esta fatura já está quitada.");
        if (model.Amount > invoice.TotalAmount - invoice.PaidAmount)
            return (false, "O valor informado é maior que o saldo devedor da fatura.");

        invoice.PaidAmount += model.Amount;
        invoice.Status = invoice.PaidAmount >= invoice.TotalAmount ? InvoiceStatus.Paid : InvoiceStatus.Open;
        if (invoice.Status == InvoiceStatus.Paid)
            invoice.PaidAt = DateTime.UtcNow;

        var card = await _uow.CreditCards.GetByIdAsync(invoice.CreditCardId);
        var category = await _uow.Categories.Query()
            .FirstOrDefaultAsync(c => c.Type == TransactionType.Expense && c.IsFixed)
            ?? await _uow.Categories.Query().FirstOrDefaultAsync(c => c.Type == TransactionType.Expense);
        if (category is null)
            return (false, "Cadastre ao menos uma categoria de despesa antes de pagar uma fatura.");

        await _uow.Transactions.AddAsync(new Transaction
        {
            Description = $"Pagamento fatura {card?.Name ?? "cartão"} {invoice.ReferenceMonth:MM/yyyy}",
            Amount = model.Amount, Type = TransactionType.Expense, Status = TransactionStatus.Completed,
            Date = model.Date.Date, AccountId = model.AccountId.Value,
            CategoryId = category.Id,
            Notes = "Pagamento de fatura de cartão de crédito."
        });

        await _uow.SaveChangesAsync();
        await _transactions.RecalculateBalancesAsync();
        await _uow.SaveChangesAsync();
        return (true, "Pagamento registrado com sucesso.");
    }
}
