using FinTrack.Models;
using FinTrack.Repositories;
using FinTrack.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.Services;

public interface ITransactionService
{
    Task<TransactionListViewModel> GetListAsync(TransactionFilterViewModel filter);
    Task<TransactionFormViewModel?> GetFormAsync(int id);
    Task<TransactionFormViewModel> GetNewFormAsync(TransactionType? type = null);
    Task<(bool Success, string Message)> SaveAsync(TransactionFormViewModel model);
    Task<(bool Success, string Message)> DeleteAsync(int id);
    Task RecalculateBalancesAsync();
    Task<List<Transaction>> GetForExportAsync(TransactionFilterViewModel filter);
}

public class TransactionService : ITransactionService
{
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".pdf", ".doc", ".docx" };
    private const long MaxFileSize = 5 * 1024 * 1024;

    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly INotificationService _notifications;
    private readonly IWebHostEnvironment _environment;

    public TransactionService(IUnitOfWork uow, ICurrentUserService currentUser,
        INotificationService notifications, IWebHostEnvironment environment)
    {
        _uow = uow;
        _currentUser = currentUser;
        _notifications = notifications;
        _environment = environment;
    }

    public async Task<TransactionListViewModel> GetListAsync(TransactionFilterViewModel f)
    {
        IQueryable<Transaction> query = _uow.Transactions.Query()
            .Include(t => t.Category)
            .Include(t => t.Account)
            .Include(t => t.CreditCard)
            .Include(t => t.Attachment);

        if (!string.IsNullOrWhiteSpace(f.Search))
        {
            var term = f.Search.Trim();
            query = query.Where(t => t.Description.Contains(term) ||
                                     (t.Notes != null && t.Notes.Contains(term)) ||
                                     t.Category.Name.Contains(term));
        }

        if (f.CategoryId.HasValue)
            query = query.Where(t => t.CategoryId == f.CategoryId.Value);
        if (f.AccountId.HasValue)
            query = query.Where(t => t.AccountId == f.AccountId.Value);
        if (f.Type.HasValue)
            query = query.Where(t => t.Type == f.Type.Value);
        if (f.Status.HasValue)
            query = query.Where(t => t.Status == f.Status.Value);
        if (f.From.HasValue)
            query = query.Where(t => t.Date >= f.From.Value.Date);
        if (f.To.HasValue)
            query = query.Where(t => t.Date < f.To.Value.Date.AddDays(1));
        if (f.MinAmount.HasValue)
            query = query.Where(t => t.Amount >= f.MinAmount.Value);
        if (f.MaxAmount.HasValue)
            query = query.Where(t => t.Amount <= f.MaxAmount.Value);

        var totals = await query.GroupBy(t => t.Type)
            .Select(g => new { Type = g.Key, Total = g.Sum(t => t.Amount) })
            .ToListAsync();

        IOrderedQueryable<Transaction> ordered = f.SortBy switch
        {
            "description" => f.SortDir == "asc" ? query.OrderBy(t => t.Description) : query.OrderByDescending(t => t.Description),
            "amount" => f.SortDir == "asc" ? query.OrderBy(t => t.Amount) : query.OrderByDescending(t => t.Amount),
            "category" => f.SortDir == "asc" ? query.OrderBy(t => t.Category.Name) : query.OrderByDescending(t => t.Category.Name),
            "account" => f.SortDir == "asc" ? query.OrderBy(t => t.Account.Name) : query.OrderByDescending(t => t.Account.Name),
            "type" => f.SortDir == "asc" ? query.OrderBy(t => t.Type) : query.OrderByDescending(t => t.Type),
            "status" => f.SortDir == "asc" ? query.OrderBy(t => t.Status) : query.OrderByDescending(t => t.Status),
            _ => f.SortDir == "asc" ? query.OrderBy(t => t.Date) : query.OrderByDescending(t => t.Date)
        };

        var page = Math.Max(1, f.Page);
        var pageSize = Math.Clamp(f.PageSize, 5, 100);
        var totalCount = await ordered.CountAsync();
        var rows = await ordered.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return new TransactionListViewModel
        {
            Items = new PagedResult<TransactionRowViewModel>
            {
                Items = rows.Select(ToRow).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            },
            Filter = f,
            Categories = await CategoryOptions(),
            Accounts = await AccountOptions(),
            TotalIncome = totals.FirstOrDefault(x => x.Type == TransactionType.Income)?.Total ?? 0,
            TotalExpense = totals.FirstOrDefault(x => x.Type == TransactionType.Expense)?.Total ?? 0,
            TotalRecords = totalCount
        };
    }

    public async Task<List<Transaction>> GetForExportAsync(TransactionFilterViewModel f)
    {
        var list = await GetListAsync(new TransactionFilterViewModel
        {
            Search = f.Search, CategoryId = f.CategoryId, AccountId = f.AccountId,
            From = f.From, To = f.To, MinAmount = f.MinAmount, MaxAmount = f.MaxAmount,
            Type = f.Type, Status = f.Status, SortBy = f.SortBy, SortDir = f.SortDir,
            Page = 1, PageSize = 10000
        });
        var ids = list.Items.Items.Select(i => i.Id).ToHashSet();
        return await _uow.Transactions.Query()
            .Include(t => t.Category).Include(t => t.Account)
            .Where(t => ids.Contains(t.Id))
            .OrderByDescending(t => t.Date)
            .ToListAsync();
    }

    public async Task<TransactionFormViewModel?> GetFormAsync(int id)
    {
        var entity = await _uow.Transactions.Query()
            .Include(t => t.Attachment)
            .FirstOrDefaultAsync(t => t.Id == id);
        if (entity is null)
            return null;

        return new TransactionFormViewModel
        {
            Id = entity.Id,
            Description = entity.Description,
            Amount = entity.Amount,
            Type = entity.Type,
            Status = entity.Status,
            Date = entity.Date,
            Notes = entity.Notes,
            CategoryId = entity.CategoryId,
            AccountId = entity.AccountId,
            CreditCardId = entity.CreditCardId,
            ExistingAttachmentName = entity.Attachment?.FileName,
            Categories = await CategoryOptions(entity.Type),
            Accounts = await AccountOptions(),
            Cards = await CardOptions()
        };
    }

    public async Task<TransactionFormViewModel> GetNewFormAsync(TransactionType? type = null)
    {
        var selectedType = type ?? TransactionType.Expense;
        var accounts = await _uow.Accounts.Query().Where(a => a.IsActive).OrderBy(a => a.Name).ToListAsync();
        return new TransactionFormViewModel
        {
            Type = selectedType,
            Date = DateTime.Today,
            AccountId = accounts.FirstOrDefault()?.Id ?? 0,
            Categories = await CategoryOptions(selectedType),
            Accounts = await AccountOptions(),
            Cards = await CardOptions()
        };
    }

    public async Task<(bool Success, string Message)> SaveAsync(TransactionFormViewModel model)
    {
        if (!await _uow.Categories.AnyAsync(c => c.Id == model.CategoryId))
            return (false, "Categoria inválida.");
        if (!await _uow.Accounts.AnyAsync(a => a.Id == model.AccountId))
            return (false, "Conta inválida.");
        if (model.CreditCardId.HasValue && !await _uow.CreditCards.AnyAsync(c => c.Id == model.CreditCardId.Value))
            return (false, "Cartão inválido.");

        Transaction entity;
        var isNew = model.Id <= 0;
        if (isNew)
        {
            entity = new Transaction { CreatedAt = DateTime.UtcNow };
            await _uow.Transactions.AddAsync(entity);
        }
        else
        {
            var existing = await _uow.Transactions.Query(false)
                .Include(t => t.Attachment)
                .FirstOrDefaultAsync(t => t.Id == model.Id);
            if (existing is null)
                return (false, "Transação não encontrada.");
            entity = existing;
            entity.UpdatedAt = DateTime.UtcNow;
        }

        var oldAttachment = entity.Attachment;
        var attachmentChanged = false;

        if (model.RemoveAttachment && oldAttachment is not null)
        {
            DeleteFile(oldAttachment.StoredFileName);
            _uow.Attachments.Remove(oldAttachment);
            entity.AttachmentId = null;
            entity.Attachment = null;
            attachmentChanged = true;
        }
        else if (model.Attachment is { Length: > 0 })
        {
            var stored = await SaveFileAsync(model.Attachment);
            if (stored is null)
                return (false, "Anexo inválido. Use JPG, PNG, PDF ou DOC (até 5 MB).");

            if (oldAttachment is not null)
            {
                DeleteFile(oldAttachment.StoredFileName);
                _uow.Attachments.Remove(oldAttachment);
            }

            var attachment = new Attachment
            {
                FileName = Path.GetFileName(model.Attachment.FileName),
                StoredFileName = stored,
                ContentType = model.Attachment.ContentType,
                FileSize = model.Attachment.Length,
                UserId = _currentUser.UserId
            };
            await _uow.Attachments.AddAsync(attachment);
            entity.Attachment = attachment;
            attachmentChanged = true;
        }

        entity.Description = model.Description.Trim();
        entity.Amount = model.Amount;
        entity.Type = model.Type;
        entity.Status = model.Status;
        entity.Date = model.Date.Date;
        entity.Notes = string.IsNullOrWhiteSpace(model.Notes) ? null : model.Notes.Trim();
        entity.CategoryId = model.CategoryId;
        entity.AccountId = model.AccountId;
        entity.CreditCardId = model.CreditCardId;

        if (!attachmentChanged && oldAttachment is not null)
            entity.AttachmentId = oldAttachment.Id;

        await _uow.SaveChangesAsync();
        await RecalculateBalancesAsync();
        await _uow.SaveChangesAsync();

        if (isNew && _currentUser.IsAuthenticated)
        {
            await _notifications.CreateAsync(
                "Transação cadastrada",
                $"{entity.Description} · {entity.Amount:C}",
                NotificationType.Success,
                NotificationCategory.Transaction,
                "/Transactions");
            await _uow.SaveChangesAsync();
        }

        return (true, isNew ? "Transação cadastrada com sucesso." : "Transação atualizada com sucesso.");
    }

    public async Task<(bool Success, string Message)> DeleteAsync(int id)
    {
        var entity = await _uow.Transactions.Query(false)
            .Include(t => t.Attachment)
            .FirstOrDefaultAsync(t => t.Id == id);
        if (entity is null)
            return (false, "Transação não encontrada.");

        if (entity.Attachment is not null)
        {
            DeleteFile(entity.Attachment.StoredFileName);
            _uow.Attachments.Remove(entity.Attachment);
        }

        _uow.Transactions.Remove(entity);
        await _uow.SaveChangesAsync();
        await RecalculateBalancesAsync();
        await _uow.SaveChangesAsync();
        return (true, "Transação excluída com sucesso.");
    }

    public async Task RecalculateBalancesAsync()
    {
        var accounts = await _uow.Accounts.Query(false).ToListAsync();
        var sums = await _uow.Transactions.Query()
            .Where(t => t.Status == TransactionStatus.Completed)
            .GroupBy(t => t.AccountId)
            .Select(g => new
            {
                AccountId = g.Key,
                Income = g.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount),
                Expense = g.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount)
            })
            .ToListAsync();

        foreach (var account in accounts)
        {
            var sum = sums.FirstOrDefault(s => s.AccountId == account.Id);
            account.CurrentBalance = account.InitialBalance + (sum?.Income ?? 0) - (sum?.Expense ?? 0);
        }

        await RecalculateInvoicesAsync();
    }

    private async Task RecalculateInvoicesAsync()
    {
        var today = DateTime.Today;
        var reference = new DateTime(today.Year, today.Month, 1);
        var cards = await _uow.CreditCards.Query(false).ToListAsync();

        foreach (var card in cards)
        {
            var total = await _uow.Transactions.Query()
                .Where(t => t.CreditCardId == card.Id && t.Type == TransactionType.Expense
                            && t.Status == TransactionStatus.Completed
                            && t.Date >= reference && t.Date < reference.AddMonths(1))
                .SumAsync(t => t.Amount);

            var invoice = await _uow.CreditCardInvoices.FirstOrDefaultAsync(i =>
                i.CreditCardId == card.Id && i.ReferenceMonth == reference);

            if (invoice is null)
            {
                if (total <= 0)
                    continue;
                invoice = new CreditCardInvoice
                {
                    CreditCardId = card.Id,
                    ReferenceMonth = reference,
                    DueDate = new DateTime(reference.Year, reference.Month,
                        Math.Min(card.DueDay, DateTime.DaysInMonth(reference.Year, reference.Month))),
                    Status = InvoiceStatus.Open
                };
                await _uow.CreditCardInvoices.AddAsync(invoice);
            }

            invoice.TotalAmount = total;
            invoice.Status = total <= 0 ? InvoiceStatus.Closed : InvoiceStatus.Open;
        }
    }

    private async Task<IEnumerable<SelectOption>> CategoryOptions(TransactionType? type = null)
    {
        IQueryable<Category> query = _uow.Categories.Query().OrderBy(c => c.Name);
        if (type.HasValue)
            query = query.Where(c => c.Type == type.Value);
        var categories = await query.ToListAsync();
        return categories.Select(c => new SelectOption { Value = c.Id.ToString(), Text = c.Name, Color = c.Color });
    }

    private async Task<IEnumerable<SelectOption>> AccountOptions()
    {
        var accounts = await _uow.Accounts.Query().Where(a => a.IsActive).OrderBy(a => a.Name).ToListAsync();
        return accounts.Select(a => new SelectOption { Value = a.Id.ToString(), Text = a.Name });
    }

    private async Task<IEnumerable<SelectOption>> CardOptions()
    {
        var cards = await _uow.CreditCards.Query().Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();
        return cards.Select(c => new SelectOption { Value = c.Id.ToString(), Text = c.Name });
    }

    private async Task<string?> SaveFileAsync(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension) || file.Length > MaxFileSize)
            return null;

        var folder = Path.Combine(_environment.WebRootPath ?? string.Empty, "uploads", _currentUser.UserId);
        Directory.CreateDirectory(folder);
        var storedName = $"{Guid.NewGuid():N}{extension}";
        await using var stream = File.Create(Path.Combine(folder, storedName));
        await file.CopyToAsync(stream);
        return storedName;
    }

    private void DeleteFile(string? storedName)
    {
        if (string.IsNullOrWhiteSpace(storedName))
            return;
        var path = Path.Combine(_environment.WebRootPath ?? string.Empty, "uploads", _currentUser.UserId, storedName);
        if (File.Exists(path))
            File.Delete(path);
    }

    private static TransactionRowViewModel ToRow(Transaction t) => new()
    {
        Id = t.Id,
        Description = t.Description,
        Amount = t.Amount,
        Type = t.Type,
        Status = t.Status,
        Date = t.Date,
        CategoryName = t.Category?.Name ?? "—",
        CategoryColor = t.Category?.Color ?? "#6366f1",
        CategoryIcon = t.Category?.Icon ?? "bi-tag",
        AccountName = t.Account?.Name ?? "—",
        CreditCardName = t.CreditCard?.Name,
        Notes = t.Notes,
        AttachmentName = t.Attachment?.FileName,
        AttachmentId = t.AttachmentId
    };
}
