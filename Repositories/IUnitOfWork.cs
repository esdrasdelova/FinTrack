using FinTrack.Data;
using FinTrack.Models;

namespace FinTrack.Repositories;

public interface IUnitOfWork : IDisposable
{
    ApplicationDbContext Context { get; }
    IRepository<Category> Categories { get; }
    IRepository<Account> Accounts { get; }
    IRepository<Transaction> Transactions { get; }
    IRepository<Attachment> Attachments { get; }
    IRepository<CreditCard> CreditCards { get; }
    IRepository<CreditCardInvoice> CreditCardInvoices { get; }
    IRepository<Budget> Budgets { get; }
    IRepository<FinancialGoal> Goals { get; }
    IRepository<GoalContribution> GoalContributions { get; }
    IRepository<Investment> Investments { get; }
    IRepository<InvestmentTransaction> InvestmentTransactions { get; }
    IRepository<RecurringTransaction> RecurringTransactions { get; }
    IRepository<Notification> Notifications { get; }
    Task<int> SaveChangesAsync();
}
