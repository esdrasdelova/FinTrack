using FinTrack.Extensions;
using FinTrack.Models;
using FinTrack.Repositories;
using FinTrack.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.Services;

public interface IInvestmentService
{
    Task<InvestmentListViewModel> GetListAsync();
    Task<InvestmentFormViewModel?> GetFormAsync(int id);
    Task<InvestmentFormViewModel> GetNewFormAsync();
    Task<(bool Success, string Message)> SaveAsync(InvestmentFormViewModel model);
    Task<(bool Success, string Message)> DeleteAsync(int id);
    Task<InvestmentDetailsViewModel?> GetDetailsAsync(int id);
    Task<InvestmentTransactionFormViewModel?> GetTransactionFormAsync(int investmentId);
    Task<(bool Success, string Message)> AddTransactionAsync(InvestmentTransactionFormViewModel model);
    Task<(bool Success, string Message)> DeleteTransactionAsync(int id);
}

public class InvestmentService : IInvestmentService
{
    private readonly IUnitOfWork _uow;

    public InvestmentService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<InvestmentListViewModel> GetListAsync()
    {
        var investments = await _uow.Investments.Query()
            .Include(i => i.Transactions)
            .OrderBy(i => i.Name)
            .ToListAsync();

        var items = investments.Select(i => new InvestmentRowViewModel
        {
            Id = i.Id, Name = i.Name, Ticker = i.Ticker, Category = i.Category,
            Institution = i.Institution, Quantity = i.Quantity, AveragePrice = i.AveragePrice,
            CurrentPrice = i.CurrentPrice, CurrentValue = i.Quantity * i.CurrentPrice,
            Invested = i.Quantity * i.AveragePrice, Profit = i.Quantity * (i.CurrentPrice - i.AveragePrice),
            ProfitPercentage = i.AveragePrice <= 0 ? 0 : (i.CurrentPrice - i.AveragePrice) / i.AveragePrice * 100,
            Color = i.Color, TransactionCount = i.Transactions.Count
        }).ToList();

        var tx = await _uow.InvestmentTransactions.Query().ToListAsync();
        var evolution = tx.GroupBy(t => new { t.Date.Year, t.Date.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g => new
            {
                Label = $"{g.Key.Month:00}/{g.Key.Year.ToString().Substring(2)}",
                Value = g.Sum(t => t.Type == InvestmentTransactionType.Sell ? -t.Amount : t.Amount)
            }).ToList();

        var byCategory = items.GroupBy(i => i.Category)
            .OrderByDescending(g => g.Sum(i => i.CurrentValue))
            .Select(g => new { Name = FinTrackFormat.InvestmentName(g.Key), Value = g.Sum(i => i.CurrentValue) })
            .ToList();

        return new InvestmentListViewModel
        {
            Items = items,
            TotalInvested = items.Sum(i => i.Invested),
            TotalCurrent = items.Sum(i => i.CurrentValue),
            TotalProfit = items.Sum(i => i.Profit),
            TotalDividends = tx.Where(t => t.Type == InvestmentTransactionType.Dividend).Sum(t => t.Amount),
            CategoryChartJson = ChartBuilder.Doughnut(byCategory.Select(c => c.Name), byCategory.Select(c => c.Value)),
            EvolutionChartJson = ChartBuilder.Line(evolution.Select(e => e.Label),
                new[] { new ChartSeriesDto { Name = "Movimentação", Color = "#06b6d4", Data = evolution.Select(e => e.Value).ToList() } })
        };
    }

    public async Task<InvestmentFormViewModel?> GetFormAsync(int id)
    {
        var i = await _uow.Investments.GetByIdAsync(id);
        return i is null ? null : new InvestmentFormViewModel
        {
            Id = i.Id, Name = i.Name, Ticker = i.Ticker, Category = i.Category,
            Institution = i.Institution, Quantity = i.Quantity, AveragePrice = i.AveragePrice,
            CurrentPrice = i.CurrentPrice, PurchaseDate = i.PurchaseDate, Color = i.Color, Notes = i.Notes
        };
    }

    public Task<InvestmentFormViewModel> GetNewFormAsync() => Task.FromResult(new InvestmentFormViewModel
    {
        Quantity = 1, PurchaseDate = DateTime.Today, Category = InvestmentCategory.FixedIncome, Color = "#06b6d4"
    });

    public async Task<(bool Success, string Message)> SaveAsync(InvestmentFormViewModel model)
    {
        Investment entity;
        var isNew = model.Id <= 0;
        if (isNew)
        {
            entity = new Investment { CreatedAt = DateTime.UtcNow };
            await _uow.Investments.AddAsync(entity);
        }
        else
        {
            var existing = await _uow.Investments.GetByIdAsync(model.Id, asNoTracking: false);
            if (existing is null)
                return (false, "Ativo não encontrado.");
            entity = existing;
        }

        entity.Name = model.Name.Trim();
        entity.Ticker = string.IsNullOrWhiteSpace(model.Ticker) ? null : model.Ticker.Trim().ToUpperInvariant();
        entity.Category = model.Category;
        entity.Institution = string.IsNullOrWhiteSpace(model.Institution) ? null : model.Institution.Trim();
        entity.Quantity = model.Quantity;
        entity.AveragePrice = model.AveragePrice;
        entity.CurrentPrice = model.CurrentPrice;
        entity.PurchaseDate = model.PurchaseDate.Date;
        entity.Color = string.IsNullOrWhiteSpace(model.Color) ? "#06b6d4" : model.Color;
        entity.Notes = string.IsNullOrWhiteSpace(model.Notes) ? null : model.Notes.Trim();

        await _uow.SaveChangesAsync();
        return (true, isNew ? "Ativo cadastrado com sucesso." : "Ativo atualizado com sucesso.");
    }

    public async Task<(bool Success, string Message)> DeleteAsync(int id)
    {
        var entity = await _uow.Investments.Query(false).Include(i => i.Transactions).FirstOrDefaultAsync(i => i.Id == id);
        if (entity is null)
            return (false, "Ativo não encontrado.");
        if (entity.Transactions.Count > 0)
            return (false, "Não é possível excluir este ativo pois existem movimentações vinculadas. Remova as movimentações primeiro.");

        _uow.Investments.Remove(entity);
        await _uow.SaveChangesAsync();
        return (true, "Ativo excluído com sucesso.");
    }

    public async Task<InvestmentDetailsViewModel?> GetDetailsAsync(int id)
    {
        var i = await _uow.Investments.Query().Include(i => i.Transactions).FirstOrDefaultAsync(i => i.Id == id);
        if (i is null)
            return null;

        var txs = i.Transactions.OrderByDescending(t => t.Date).ThenByDescending(t => t.Id).ToList();
        var evolution = txs.OrderBy(t => t.Date).Select(t => t.Date.ToString("dd/MM/yy")).ToList();

        return new InvestmentDetailsViewModel
        {
            Investment = new InvestmentRowViewModel
            {
                Id = i.Id, Name = i.Name, Ticker = i.Ticker, Category = i.Category, Institution = i.Institution,
                Quantity = i.Quantity, AveragePrice = i.AveragePrice, CurrentPrice = i.CurrentPrice,
                CurrentValue = i.Quantity * i.CurrentPrice, Invested = i.Quantity * i.AveragePrice,
                Profit = i.Quantity * (i.CurrentPrice - i.AveragePrice),
                ProfitPercentage = i.AveragePrice <= 0 ? 0 : (i.CurrentPrice - i.AveragePrice) / i.AveragePrice * 100,
                Color = i.Color, TransactionCount = txs.Count
            },
            Transactions = txs.Select(t => new InvestmentTransactionRowViewModel
            {
                Id = t.Id, Type = t.Type, Quantity = t.Quantity, Price = t.Price, Amount = t.Amount,
                Date = t.Date, Notes = t.Notes
            }).ToList(),
            EvolutionChartJson = ChartBuilder.Area(evolution, new[]
            {
                new ChartSeriesDto { Name = "Valor movimentado", Color = i.Color, Data = txs.OrderBy(t => t.Date).Select(t => t.Amount).ToList() }
            })
        };
    }

    public async Task<InvestmentTransactionFormViewModel?> GetTransactionFormAsync(int investmentId)
    {
        var i = await _uow.Investments.GetByIdAsync(investmentId);
        return i is null ? null : new InvestmentTransactionFormViewModel
        {
            InvestmentId = i.Id, InvestmentName = i.Name, Quantity = 1, Date = DateTime.Today
        };
    }

    public async Task<(bool Success, string Message)> AddTransactionAsync(InvestmentTransactionFormViewModel model)
    {
        var investment = await _uow.Investments.GetByIdAsync(model.InvestmentId, asNoTracking: false);
        if (investment is null)
            return (false, "Ativo não encontrado.");

        if (model.Type is InvestmentTransactionType.Buy or InvestmentTransactionType.Sell && model.Quantity <= 0)
            return (false, "Informe uma quantidade maior que zero para compras e vendas.");
        if (model.Type == InvestmentTransactionType.Sell && model.Quantity > investment.Quantity)
            return (false, "Quantidade insuficiente para esta venda.");

        decimal amount;
        if (model.Type == InvestmentTransactionType.Buy)
        {
            var newQty = investment.Quantity + model.Quantity;
            investment.AveragePrice = newQty <= 0 ? 0 : (investment.Quantity * investment.AveragePrice + model.Quantity * model.Price) / newQty;
            investment.Quantity = newQty;
            amount = model.Quantity * model.Price;
        }
        else if (model.Type == InvestmentTransactionType.Sell)
        {
            investment.Quantity -= model.Quantity;
            amount = model.Quantity * model.Price;
        }
        else
        {
            amount = model.Amount;
        }

        await _uow.InvestmentTransactions.AddAsync(new InvestmentTransaction
        {
            InvestmentId = investment.Id, Type = model.Type, Quantity = model.Quantity,
            Price = model.Price, Amount = amount, Date = model.Date.Date,
            Notes = string.IsNullOrWhiteSpace(model.Notes) ? null : model.Notes.Trim()
        });
        await _uow.SaveChangesAsync();
        return (true, "Movimentação registrada com sucesso.");
    }

    public async Task<(bool Success, string Message)> DeleteTransactionAsync(int id)
    {
        var tx = await _uow.InvestmentTransactions.GetByIdAsync(id, asNoTracking: false);
        if (tx is null)
            return (false, "Movimentação não encontrada.");
        var investment = await _uow.Investments.GetByIdAsync(tx.InvestmentId, asNoTracking: false);
        if (investment is null)
            return (false, "Ativo não encontrado.");

        if (tx.Type == InvestmentTransactionType.Buy)
        {
            var newQty = investment.Quantity - tx.Quantity;
            if (newQty <= 0)
            {
                investment.Quantity = 0;
                investment.AveragePrice = 0;
            }
            else
            {
                investment.AveragePrice = (investment.Quantity * investment.AveragePrice - tx.Quantity * tx.Price) / newQty;
                investment.Quantity = newQty;
            }
        }
        else if (tx.Type == InvestmentTransactionType.Sell)
        {
            investment.Quantity += tx.Quantity;
        }

        _uow.InvestmentTransactions.Remove(tx);
        await _uow.SaveChangesAsync();
        return (true, "Movimentação removida com sucesso.");
    }
}
