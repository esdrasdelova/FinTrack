using FinTrack.Data;
using FinTrack.Models;
using FinTrack.Services;

namespace FinTrack.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly Dictionary<Type, object> _repositories = new();
    private bool _disposed;

    public UnitOfWork(ApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public ApplicationDbContext Context => _context;

    public IRepository<Category> Categories => Repo<Category>();
    public IRepository<Account> Accounts => Repo<Account>();
    public IRepository<Transaction> Transactions => Repo<Transaction>();
    public IRepository<Attachment> Attachments => Repo<Attachment>();
    public IRepository<CreditCard> CreditCards => Repo<CreditCard>();
    public IRepository<CreditCardInvoice> CreditCardInvoices => Repo<CreditCardInvoice>();
    public IRepository<Budget> Budgets => Repo<Budget>();
    public IRepository<FinancialGoal> Goals => Repo<FinancialGoal>();
    public IRepository<GoalContribution> GoalContributions => Repo<GoalContribution>();
    public IRepository<Investment> Investments => Repo<Investment>();
    public IRepository<InvestmentTransaction> InvestmentTransactions => Repo<InvestmentTransaction>();
    public IRepository<RecurringTransaction> RecurringTransactions => Repo<RecurringTransaction>();
    public IRepository<Notification> Notifications => Repo<Notification>();

    public Task<int> SaveChangesAsync() => _context.SaveChangesAsync();

    private IRepository<T> Repo<T>() where T : class, IUserOwnedEntity
    {
        if (_repositories.TryGetValue(typeof(T), out var repo))
            return (IRepository<T>)repo;

        var created = new Repository<T>(_context, _currentUser);
        _repositories[typeof(T)] = created;
        return created;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
            _context.Dispose();
        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
