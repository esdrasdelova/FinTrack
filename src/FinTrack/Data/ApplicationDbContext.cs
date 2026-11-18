using System.Linq.Expressions;
using FinTrack.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<CreditCard> CreditCards => Set<CreditCard>();
    public DbSet<CreditCardInvoice> CreditCardInvoices => Set<CreditCardInvoice>();
    public DbSet<Budget> Budgets => Set<Budget>();
    public DbSet<FinancialGoal> FinancialGoals => Set<FinancialGoal>();
    public DbSet<GoalContribution> GoalContributions => Set<GoalContribution>();
    public DbSet<Investment> Investments => Set<Investment>();
    public DbSet<InvestmentTransaction> InvestmentTransactions => Set<InvestmentTransaction>();
    public DbSet<RecurringTransaction> RecurringTransactions => Set<RecurringTransaction>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(e =>
        {
            e.ToTable("AspNetUsers");
            e.Property(u => u.FullName).HasMaxLength(120);
            e.Property(u => u.Currency).HasMaxLength(8);
        });

        builder.Entity<Category>().ToTable("Categories");
        builder.Entity<Account>().ToTable("Accounts");
        builder.Entity<Transaction>().ToTable("Transactions");
        builder.Entity<Attachment>().ToTable("Attachments");
        builder.Entity<CreditCard>().ToTable("CreditCards");
        builder.Entity<CreditCardInvoice>().ToTable("CreditCardInvoices");
        builder.Entity<Budget>().ToTable("Budgets");
        builder.Entity<FinancialGoal>().ToTable("FinancialGoals");
        builder.Entity<GoalContribution>().ToTable("GoalContributions");
        builder.Entity<Investment>().ToTable("Investments");
        builder.Entity<InvestmentTransaction>().ToTable("InvestmentTransactions");
        builder.Entity<RecurringTransaction>().ToTable("RecurringTransactions");
        builder.Entity<Notification>().ToTable("Notifications");

        // Isolamento: nenhuma entidade financeira é apagada em cascata com o usuário.
        ConfigureUserRelation<Category>(builder, e => e.User, u => u.Categories);
        ConfigureUserRelation<Account>(builder, e => e.User, u => u.Accounts);
        ConfigureUserRelation<Transaction>(builder, e => e.User, u => u.Transactions);
        ConfigureUserRelation<Attachment>(builder, e => e.User);
        ConfigureUserRelation<CreditCard>(builder, e => e.User);
        ConfigureUserRelation<CreditCardInvoice>(builder, e => e.User);
        ConfigureUserRelation<Budget>(builder, e => e.User);
        ConfigureUserRelation<FinancialGoal>(builder, e => e.User, u => u.Goals);
        ConfigureUserRelation<GoalContribution>(builder, e => e.User);
        ConfigureUserRelation<Investment>(builder, e => e.User, u => u.Investments);
        ConfigureUserRelation<InvestmentTransaction>(builder, e => e.User);
        ConfigureUserRelation<RecurringTransaction>(builder, e => e.User);
        ConfigureUserRelation<Notification>(builder, e => e.User, u => u.Notifications);

        builder.Entity<Transaction>(e =>
        {
            e.HasOne(t => t.Category).WithMany(c => c.Transactions)
                .HasForeignKey(t => t.CategoryId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(t => t.Account).WithMany(a => a.Transactions)
                .HasForeignKey(t => t.AccountId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(t => t.CreditCard).WithMany(c => c.Transactions)
                .HasForeignKey(t => t.CreditCardId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(t => t.Attachment).WithMany()
                .HasForeignKey(t => t.AttachmentId).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(t => new { t.UserId, t.Date });
            e.HasIndex(t => new { t.UserId, t.Type });
            e.HasIndex(t => new { t.UserId, t.CategoryId });
        });

        builder.Entity<CreditCardInvoice>(e =>
        {
            e.HasOne(i => i.CreditCard).WithMany(c => c.Invoices)
                .HasForeignKey(i => i.CreditCardId).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(i => new { i.UserId, i.ReferenceMonth });
        });

        builder.Entity<Budget>(e =>
        {
            e.HasOne(b => b.Category).WithMany(c => c.Budgets)
                .HasForeignKey(b => b.CategoryId).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(b => new { b.UserId, b.Period });
        });

        builder.Entity<GoalContribution>(e =>
        {
            e.HasOne(c => c.Goal).WithMany(g => g.Contributions)
                .HasForeignKey(c => c.FinancialGoalId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<InvestmentTransaction>(e =>
        {
            e.HasOne(t => t.Investment).WithMany(i => i.Transactions)
                .HasForeignKey(t => t.InvestmentId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<RecurringTransaction>(e =>
        {
            e.HasOne(r => r.Category).WithMany()
                .HasForeignKey(r => r.CategoryId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(r => r.Account).WithMany(a => a.RecurringTransactions)
                .HasForeignKey(r => r.AccountId).OnDelete(DeleteBehavior.NoAction);
        });

        builder.Entity<Notification>(e =>
        {
            e.HasIndex(n => new { n.UserId, n.IsRead, n.CreatedAt });
        });
    }

    private static void ConfigureUserRelation<TEntity>(
        ModelBuilder builder,
        Expression<Func<TEntity, ApplicationUser>> userNavigation,
        Expression<Func<ApplicationUser, IEnumerable<TEntity>>>? collectionNavigation = null)
        where TEntity : class, IUserOwnedEntity
    {
        var reference = builder.Entity<TEntity>().HasOne(userNavigation);
        var relationship = collectionNavigation is null
            ? reference.WithMany()
            : reference.WithMany(collectionNavigation);

        relationship.HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.NoAction);
    }
}
