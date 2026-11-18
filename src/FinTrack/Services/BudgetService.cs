using FinTrack.Models;
using FinTrack.Repositories;
using FinTrack.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.Services;

public interface IBudgetService
{
    Task<BudgetListViewModel> GetListAsync();
    Task<BudgetFormViewModel?> GetFormAsync(int id);
    Task<BudgetFormViewModel> GetNewFormAsync();
    Task<(bool Success, string Message)> SaveAsync(BudgetFormViewModel model);
    Task<(bool Success, string Message)> DeleteAsync(int id);
}

public class BudgetService : IBudgetService
{
    private readonly IUnitOfWork _uow;

    public BudgetService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<BudgetListViewModel> GetListAsync()
    {
        var month = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var budgets = await _uow.Budgets.Query()
            .Include(b => b.Category)
            .Where(b => b.Period <= month)
            .OrderByDescending(b => b.Period).ThenBy(b => b.Category!.Name)
            .ToListAsync();

        var items = new List<BudgetRowViewModel>();
        foreach (var group in budgets.GroupBy(b => b.CategoryId))
        {
            var budget = group.First();
            var used = await _uow.Transactions.Query()
                .Where(t => t.CategoryId == budget.CategoryId && t.Type == TransactionType.Expense
                            && t.Status != TransactionStatus.Cancelled
                            && t.Date >= month && t.Date < month.AddMonths(1))
                .SumAsync(t => t.Amount);
            items.Add(new BudgetRowViewModel
            {
                Id = budget.Id, CategoryName = budget.Category?.Name ?? "Categoria",
                CategoryIcon = budget.Category?.Icon ?? "bi-tag", Color = budget.Category?.Color ?? "#6366f1",
                Period = budget.Period.ToString("MM/yyyy"), Limit = budget.LimitAmount,
                Used = used, AlertThreshold = budget.AlertThreshold
            });
        }

        return new BudgetListViewModel
        {
            Items = items.OrderByDescending(b => b.Percent).ToList(),
            TotalLimit = items.Sum(b => b.Limit),
            TotalUsed = items.Sum(b => b.Used),
            ExceededCount = items.Count(b => b.IsExceeded),
            ChartJson = ChartBuilder.Bar(
                items.Select(b => b.CategoryName.Length > 12 ? b.CategoryName[..12] + "…" : b.CategoryName),
                new[]
                {
                    new ChartSeriesDto { Name = "Utilizado", Color = "#f43f5e", Data = items.Select(b => b.Used).ToList() },
                    new ChartSeriesDto { Name = "Limite", Color = "#6366f1", Data = items.Select(b => b.Limit).ToList() }
                })
        };
    }

    public async Task<BudgetFormViewModel?> GetFormAsync(int id)
    {
        var b = await _uow.Budgets.GetByIdAsync(id);
        return b is null ? null : new BudgetFormViewModel
        {
            Id = b.Id, CategoryId = b.CategoryId, Period = b.Period,
            LimitAmount = b.LimitAmount, AlertThreshold = b.AlertThreshold,
            Categories = await CategoryOptions()
        };
    }

    public async Task<BudgetFormViewModel> GetNewFormAsync() => new()
    {
        Period = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1),
        AlertThreshold = 80,
        Categories = await CategoryOptions()
    };

    public async Task<(bool Success, string Message)> SaveAsync(BudgetFormViewModel model)
    {
        if (!await _uow.Categories.AnyAsync(c => c.Id == model.CategoryId && c.Type == TransactionType.Expense))
            return (false, "Selecione uma categoria de despesa válida.");

        var period = new DateTime(model.Period.Year, model.Period.Month, 1);
        var duplicate = await _uow.Budgets.AnyAsync(b => b.CategoryId == model.CategoryId && b.Period == period && b.Id != model.Id);
        if (duplicate)
            return (false, "Já existe um orçamento para esta categoria no período informado.");

        Budget entity;
        var isNew = model.Id <= 0;
        if (isNew)
        {
            entity = new Budget { CreatedAt = DateTime.UtcNow };
            await _uow.Budgets.AddAsync(entity);
        }
        else
        {
            var existing = await _uow.Budgets.GetByIdAsync(model.Id, asNoTracking: false);
            if (existing is null)
                return (false, "Orçamento não encontrado.");
            entity = existing;
        }

        entity.CategoryId = model.CategoryId;
        entity.Period = period;
        entity.LimitAmount = model.LimitAmount;
        entity.AlertThreshold = model.AlertThreshold;
        await _uow.SaveChangesAsync();
        return (true, isNew ? "Orçamento criado com sucesso." : "Orçamento atualizado com sucesso.");
    }

    public async Task<(bool Success, string Message)> DeleteAsync(int id)
    {
        var entity = await _uow.Budgets.GetByIdAsync(id, asNoTracking: false);
        if (entity is null)
            return (false, "Orçamento não encontrado.");
        _uow.Budgets.Remove(entity);
        await _uow.SaveChangesAsync();
        return (true, "Orçamento excluído com sucesso.");
    }

    private async Task<List<SelectOption>> CategoryOptions()
    {
        var categories = await _uow.Categories.Query()
            .Where(c => c.Type == TransactionType.Expense).OrderBy(c => c.Name).ToListAsync();
        return categories.Select(c => new SelectOption { Value = c.Id.ToString(), Text = c.Name, Color = c.Color }).ToList();
    }
}
