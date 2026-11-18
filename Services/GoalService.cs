using FinTrack.Models;
using FinTrack.Repositories;
using FinTrack.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.Services;

public interface IGoalService
{
    Task<GoalListViewModel> GetListAsync();
    Task<GoalFormViewModel?> GetFormAsync(int id);
    Task<GoalFormViewModel> GetNewFormAsync();
    Task<(bool Success, string Message)> SaveAsync(GoalFormViewModel model);
    Task<(bool Success, string Message)> DeleteAsync(int id);
    Task<GoalDetailsViewModel?> GetDetailsAsync(int id);
    Task<ContributionFormViewModel?> GetContributionFormAsync(int goalId);
    Task<(bool Success, string Message)> AddContributionAsync(ContributionFormViewModel model);
    Task<(bool Success, string Message)> DeleteContributionAsync(int id);
}

public class GoalService : IGoalService
{
    private readonly IUnitOfWork _uow;

    public GoalService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<GoalListViewModel> GetListAsync()
    {
        var goals = await _uow.Goals.Query()
            .Include(g => g.Contributions)
            .OrderBy(g => g.IsCompleted ? 1 : 0).ThenBy(g => g.Deadline)
            .ToListAsync();

        var items = goals.Select(g => new GoalRowViewModel
        {
            Id = g.Id, Name = g.Name, Description = g.Description, Icon = g.Icon, Color = g.Color,
            Target = g.TargetAmount, Current = g.CurrentAmount, Deadline = g.Deadline,
            IsCompleted = g.IsCompleted || g.CurrentAmount >= g.TargetAmount,
            ContributionCount = g.Contributions.Count,
            LastContribution = g.Contributions.OrderByDescending(c => c.Date).Select(c => (DateTime?)c.Date).FirstOrDefault()
        }).ToList();

        return new GoalListViewModel
        {
            Items = items,
            TotalTarget = items.Sum(g => g.Target),
            TotalCurrent = items.Sum(g => g.Current),
            CompletedCount = items.Count(g => g.IsCompleted),
            ChartJson = ChartBuilder.Progress(items.Select(g => g.Name), items.Select(g => g.Percent), items.Select(g => g.Color))
        };
    }

    public async Task<GoalFormViewModel?> GetFormAsync(int id)
    {
        var g = await _uow.Goals.GetByIdAsync(id);
        return g is null ? null : new GoalFormViewModel
        {
            Id = g.Id, Name = g.Name, Description = g.Description, TargetAmount = g.TargetAmount,
            CurrentAmount = g.CurrentAmount, Deadline = g.Deadline, Color = g.Color,
            Icon = g.Icon, ImageUrl = g.ImageUrl
        };
    }

    public Task<GoalFormViewModel> GetNewFormAsync() => Task.FromResult(new GoalFormViewModel
    {
        Deadline = DateTime.Today.AddMonths(12), Color = "#10b981", Icon = "bi-stars"
    });

    public async Task<(bool Success, string Message)> SaveAsync(GoalFormViewModel model)
    {
        FinancialGoal entity;
        var isNew = model.Id <= 0;
        if (isNew)
        {
            entity = new FinancialGoal { CreatedAt = DateTime.UtcNow };
            await _uow.Goals.AddAsync(entity);
        }
        else
        {
            var existing = await _uow.Goals.GetByIdAsync(model.Id, asNoTracking: false);
            if (existing is null)
                return (false, "Meta não encontrada.");
            entity = existing;
        }

        entity.Name = model.Name.Trim();
        entity.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
        entity.TargetAmount = model.TargetAmount;
        entity.CurrentAmount = model.CurrentAmount;
        entity.Deadline = model.Deadline.Date;
        entity.Color = string.IsNullOrWhiteSpace(model.Color) ? "#10b981" : model.Color;
        entity.Icon = string.IsNullOrWhiteSpace(model.Icon) ? "bi-stars" : model.Icon;
        entity.ImageUrl = string.IsNullOrWhiteSpace(model.ImageUrl) ? null : model.ImageUrl.Trim();
        entity.IsCompleted = entity.CurrentAmount >= entity.TargetAmount;
        entity.CompletedAt = entity.IsCompleted ? (entity.CompletedAt ?? DateTime.UtcNow) : null;

        await _uow.SaveChangesAsync();
        return (true, isNew ? "Meta criada com sucesso." : "Meta atualizada com sucesso.");
    }

    public async Task<(bool Success, string Message)> DeleteAsync(int id)
    {
        var entity = await _uow.Goals.GetByIdAsync(id, asNoTracking: false);
        if (entity is null)
            return (false, "Meta não encontrada.");
        _uow.Goals.Remove(entity);
        await _uow.SaveChangesAsync();
        return (true, "Meta excluída com sucesso.");
    }

    public async Task<GoalDetailsViewModel?> GetDetailsAsync(int id)
    {
        var goal = await _uow.Goals.Query().Include(g => g.Contributions).FirstOrDefaultAsync(g => g.Id == id);
        if (goal is null)
            return null;

        var contributions = goal.Contributions.OrderByDescending(c => c.Date).ThenByDescending(c => c.Id).ToList();
        var running = 0m;
        var evolution = contributions.OrderBy(c => c.Date).Select(c =>
        {
            running += c.Amount;
            return new { Label = c.Date.ToString("dd/MM/yy"), Value = running };
        }).ToList();

        return new GoalDetailsViewModel
        {
            Goal = new GoalRowViewModel
            {
                Id = goal.Id, Name = goal.Name, Description = goal.Description, Icon = goal.Icon,
                Color = goal.Color, Target = goal.TargetAmount, Current = goal.CurrentAmount,
                Deadline = goal.Deadline, IsCompleted = goal.IsCompleted || goal.CurrentAmount >= goal.TargetAmount,
                ContributionCount = contributions.Count,
                LastContribution = contributions.FirstOrDefault()?.Date
            },
            Contributions = contributions.Select(c => new ContributionRowViewModel
            {
                Id = c.Id, Amount = c.Amount, Date = c.Date, Notes = c.Notes
            }).ToList(),
            EvolutionChartJson = ChartBuilder.Area(evolution.Select(e => e.Label),
                new[] { new ChartSeriesDto { Name = "Acumulado", Color = goal.Color, Data = evolution.Select(e => e.Value).ToList() } })
        };
    }

    public async Task<ContributionFormViewModel?> GetContributionFormAsync(int goalId)
    {
        var goal = await _uow.Goals.GetByIdAsync(goalId);
        return goal is null ? null : new ContributionFormViewModel { GoalId = goalId, GoalName = goal.Name };
    }

    public async Task<(bool Success, string Message)> AddContributionAsync(ContributionFormViewModel model)
    {
        var goal = await _uow.Goals.GetByIdAsync(model.GoalId, asNoTracking: false);
        if (goal is null)
            return (false, "Meta não encontrada.");

        await _uow.GoalContributions.AddAsync(new GoalContribution
        {
            FinancialGoalId = goal.Id, Amount = model.Amount, Date = model.Date.Date,
            Notes = string.IsNullOrWhiteSpace(model.Notes) ? null : model.Notes.Trim()
        });
        goal.CurrentAmount += model.Amount;
        goal.IsCompleted = goal.CurrentAmount >= goal.TargetAmount;
        if (goal.IsCompleted)
            goal.CompletedAt ??= DateTime.UtcNow;

        await _uow.SaveChangesAsync();
        return (true, goal.IsCompleted ? "Aporte registrado. Meta concluída! Parabéns!" : "Aporte registrado com sucesso.");
    }

    public async Task<(bool Success, string Message)> DeleteContributionAsync(int id)
    {
        var contribution = await _uow.GoalContributions.GetByIdAsync(id, asNoTracking: false);
        if (contribution is null)
            return (false, "Aporte não encontrado.");

        var goal = await _uow.Goals.GetByIdAsync(contribution.FinancialGoalId, asNoTracking: false);
        if (goal is not null)
        {
            goal.CurrentAmount = Math.Max(0, goal.CurrentAmount - contribution.Amount);
            goal.IsCompleted = goal.CurrentAmount >= goal.TargetAmount;
            if (!goal.IsCompleted)
                goal.CompletedAt = null;
        }
        _uow.GoalContributions.Remove(contribution);
        await _uow.SaveChangesAsync();
        return (true, "Aporte removido com sucesso.");
    }
}
