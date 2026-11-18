using FinTrack.Models;
using FinTrack.Repositories;
using FinTrack.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.Services;

public interface IAccountService
{
    Task<AccountListViewModel> GetListAsync();
    Task<AccountFormViewModel?> GetFormAsync(int id);
    Task<AccountFormViewModel> GetNewFormAsync();
    Task<(bool Success, string Message)> SaveAsync(AccountFormViewModel model);
    Task<(bool Success, string Message)> DeleteAsync(int id);
    Task<AccountDetailsViewModel?> GetDetailsAsync(int id);
}

public class AccountService : IAccountService
{
    private readonly IUnitOfWork _uow;
    private readonly ITransactionService _transactions;

    public AccountService(IUnitOfWork uow, ITransactionService transactions)
    {
        _uow = uow;
        _transactions = transactions;
    }

    public async Task<AccountListViewModel> GetListAsync()
    {
        var accounts = await _uow.Accounts.Query()
            .Include(a => a.Transactions)
            .OrderBy(a => a.IsActive ? 0 : 1).ThenBy(a => a.Name)
            .ToListAsync();

        return new AccountListViewModel
        {
            Items = accounts.Select(a => new AccountRowViewModel
            {
                Id = a.Id, Name = a.Name, Institution = a.Institution ?? string.Empty, Type = a.Type,
                InitialBalance = a.InitialBalance, CurrentBalance = a.CurrentBalance,
                Color = a.Color, Icon = a.Icon, IsActive = a.IsActive,
                TransactionCount = a.Transactions.Count
            }).ToList(),
            TotalBalance = accounts.Where(a => a.IsActive).Sum(a => a.CurrentBalance),
            TotalInitial = accounts.Sum(a => a.InitialBalance),
            ActiveCount = accounts.Count(a => a.IsActive)
        };
    }

    public async Task<AccountFormViewModel?> GetFormAsync(int id)
    {
        var a = await _uow.Accounts.GetByIdAsync(id);
        return a is null ? null : new AccountFormViewModel
        {
            Id = a.Id, Name = a.Name, Institution = a.Institution, Type = a.Type,
            InitialBalance = a.InitialBalance, Color = a.Color, Icon = a.Icon, IsActive = a.IsActive
        };
    }

    public Task<AccountFormViewModel> GetNewFormAsync() => Task.FromResult(new AccountFormViewModel
    {
        Type = AccountType.Checking, InitialBalance = 0, IsActive = true
    });

    public async Task<(bool Success, string Message)> SaveAsync(AccountFormViewModel model)
    {
        Account entity;
        var isNew = model.Id <= 0;
        if (isNew)
        {
            entity = new Account { CreatedAt = DateTime.UtcNow };
            await _uow.Accounts.AddAsync(entity);
        }
        else
        {
            var existing = await _uow.Accounts.GetByIdAsync(model.Id, asNoTracking: false);
            if (existing is null)
                return (false, "Conta não encontrada.");
            entity = existing;
        }

        var balanceChanged = isNew || entity.InitialBalance != model.InitialBalance;
        var oldBalance = entity.CurrentBalance - entity.InitialBalance;

        entity.Name = model.Name.Trim();
        entity.Institution = string.IsNullOrWhiteSpace(model.Institution) ? null : model.Institution.Trim();
        entity.Type = model.Type;
        entity.InitialBalance = model.InitialBalance;
        entity.Color = string.IsNullOrWhiteSpace(model.Color) ? "#6366f1" : model.Color;
        entity.Icon = string.IsNullOrWhiteSpace(model.Icon) ? "bi-wallet2" : model.Icon;
        entity.IsActive = model.IsActive;

        if (balanceChanged)
            entity.CurrentBalance = model.InitialBalance + oldBalance;

        await _uow.SaveChangesAsync();
        await _transactions.RecalculateBalancesAsync();
        await _uow.SaveChangesAsync();

        return (true, isNew ? "Conta cadastrada com sucesso." : "Conta atualizada com sucesso.");
    }

    public async Task<(bool Success, string Message)> DeleteAsync(int id)
    {
        var entity = await _uow.Accounts.Query(false)
            .Include(a => a.Transactions)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (entity is null)
            return (false, "Conta não encontrada.");
        if (entity.Transactions.Count > 0)
            return (false, "Não é possível excluir esta conta pois existem transações vinculadas. Desative-a ou remova as transações primeiro.");

        _uow.Accounts.Remove(entity);
        await _uow.SaveChangesAsync();
        return (true, "Conta excluída com sucesso.");
    }

    public async Task<AccountDetailsViewModel?> GetDetailsAsync(int id)
    {
        var account = await _uow.Accounts.Query()
            .Include(a => a.Transactions)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (account is null)
            return null;

        var movements = await (from t in _uow.Transactions.Query()
                               join c in _uow.Categories.Query() on t.CategoryId equals c.Id
                               where t.AccountId == id
                               orderby t.Date descending
                               select new AccountMovementDto
                               {
                                   Id = t.Id, Description = t.Description, Amount = t.Amount,
                                   Type = t.Type, Date = t.Date, CategoryName = c.Name, CategoryColor = c.Color
                               }).Take(30).ToListAsync();

        var monthly = await _uow.Transactions.Query()
            .Where(t => t.AccountId == id && t.Status == TransactionStatus.Completed)
            .GroupBy(t => new { t.Date.Year, t.Date.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g => new
            {
                Label = g.Key.Month.ToString("00") + "/" + g.Key.Year.ToString().Substring(2),
                Income = g.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount),
                Expenses = g.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount)
            }).ToListAsync();

        return new AccountDetailsViewModel
        {
            Account = new AccountRowViewModel
            {
                Id = account.Id, Name = account.Name, Institution = account.Institution ?? string.Empty,
                Type = account.Type, InitialBalance = account.InitialBalance,
                CurrentBalance = account.CurrentBalance, Color = account.Color, Icon = account.Icon,
                IsActive = account.IsActive, TransactionCount = account.Transactions.Count
            },
            Income = movements.Where(m => m.Type == TransactionType.Income).Sum(m => m.Amount),
            Expenses = movements.Where(m => m.Type == TransactionType.Expense).Sum(m => m.Amount),
            Movements = movements,
            BalanceChartJson = ChartBuilder.Bar(monthly.Select(m => m.Label), new[]
            {
                new ChartSeriesDto { Name = "Entradas", Color = "#10b981", Data = monthly.Select(m => m.Income).ToList() },
                new ChartSeriesDto { Name = "Saídas", Color = "#f43f5e", Data = monthly.Select(m => m.Expenses).ToList() }
            }, stacked: true)
        };
    }
}
