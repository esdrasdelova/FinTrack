using FinTrack.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.Data;

public interface IDemoDataSeeder
{
    Task SeedAsync();
    Task EnsureCategoriesAsync(string userId);
}

/// <summary>
/// Cria a conta de demonstração e seus dados fictícios. Todo dado gerado aqui
/// pertence ao usuário demo@fintrack.app e é sinalizado na interface como demonstração.
/// A semeadura é idempotente por grupo de entidades: uma falha parcial nunca impede
/// que os grupos restantes sejam concluídos na próxima inicialização.
/// </summary>
public class DemoDataSeeder : IDemoDataSeeder
{
    public const string DemoEmail = "demo@fintrack.app";
    public const string DemoPassword = "Demo@1234";

    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public DemoDataSeeder(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    private static readonly (string Name, string Color, string Icon, bool IsFixed, TransactionType Type)[] CategoryDefinitions =
    {
        ("Salário", "#10b981", "bi-briefcase", true, TransactionType.Income),
        ("Freelance", "#06b6d4", "bi-laptop", false, TransactionType.Income),
        ("Investimentos", "#8b5cf6", "bi-graph-up", false, TransactionType.Income),
        ("Aluguel recebido", "#14b8a6", "bi-house", false, TransactionType.Income),
        ("Outros", "#64748b", "bi-tag", false, TransactionType.Income),
        ("Moradia", "#6366f1", "bi-house", true, TransactionType.Expense),
        ("Alimentação", "#f43f5e", "bi-cart", false, TransactionType.Expense),
        ("Transporte", "#f59e0b", "bi-car-front", true, TransactionType.Expense),
        ("Saúde", "#ec4899", "bi-heart-pulse", false, TransactionType.Expense),
        ("Educação", "#8b5cf6", "bi-mortarboard", false, TransactionType.Expense),
        ("Lazer", "#06b6d4", "bi-controller", false, TransactionType.Expense),
        ("Assinaturas", "#a855f7", "bi-repeat", true, TransactionType.Expense),
        ("Outros", "#64748b", "bi-tag", false, TransactionType.Expense)
    };

    private static readonly (string Name, string? Institution, AccountType Type, decimal InitialBalance, string Color, string Icon)[] AccountDefinitions =
    {
        ("Conta Corrente Principal", "Banco Inter", AccountType.Checking, 14000m, "#6366f1", "bi-bank"),
        ("Poupança", "Caixa Econômica", AccountType.Savings, 22000m, "#10b981", "bi-piggy-bank"),
        ("Carteira", null, AccountType.Wallet, 650m, "#f59e0b", "bi-wallet2"),
        ("Conta Digital", "Nubank", AccountType.Digital, 3200m, "#8b5cf6", "bi-phone"),
        ("Carteira de Investimentos", "XP Investimentos", AccountType.Investments, 0m, "#06b6d4", "bi-graph-up")
    };

    private static readonly (string Name, string Institution, string Brand, string LastFour, decimal Limit, int Closing, int Due, string Color)[] CardDefinitions =
    {
        ("Nubank Platinum", "Nubank", "Mastercard", "4821", 12000m, 25, 10, "#8b5cf6"),
        ("Itaú Visa Infinite", "Itaú", "Visa", "9012", 18000m, 20, 5, "#f43f5e")
    };

    public async Task SeedAsync()
    {
        var demo = await _userManager.FindByEmailAsync(DemoEmail);
        if (demo is null)
        {
            demo = new ApplicationUser
            {
                UserName = DemoEmail,
                Email = DemoEmail,
                FullName = "Ana Beatriz Costa",
                EmailConfirmed = true,
                IsDemo = true,
                Theme = "dark",
                Currency = "BRL"
            };
            var result = await _userManager.CreateAsync(demo, DemoPassword);
            if (!result.Succeeded)
                throw new InvalidOperationException("Falha ao criar usuário de demonstração: " +
                    string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        await EnsureCategoriesAsync(demo.Id);
        var categories = await _context.Categories.Where(c => c.UserId == demo.Id).ToListAsync();
        Category Cat(string name, TransactionType type) => categories.First(c =>
            c.Name == name && c.Type == type);

        var accounts = await EnsureAccountsAsync(demo.Id);
        Account Acc(string name) => accounts.First(a => a.Name == name);
        var checking = Acc("Conta Corrente Principal");
        var savings = Acc("Poupança");
        var wallet = Acc("Carteira");
        var digital = Acc("Conta Digital");
        var investmentAccount = Acc("Carteira de Investimentos");

        var cards = await EnsureCardsAsync(demo.Id);
        CreditCard Card(string name) => cards.First(c => c.Name == name);
        var card1 = Card("Nubank Platinum");
        var card2 = Card("Itaú Visa Infinite");

        if (!await _context.Transactions.AnyAsync(t => t.UserId == demo.Id))
            await SeedTransactionsAsync(demo.Id, Cat, checking, digital, wallet, investmentAccount, card1, card2);

        if (!await _context.Budgets.AnyAsync(b => b.UserId == demo.Id))
            await SeedBudgetsAsync(demo.Id, Cat);

        var goals = await _context.FinancialGoals.Where(g => g.UserId == demo.Id).ToListAsync();
        if (goals.Count == 0)
        {
            goals = await SeedGoalsAsync(demo.Id);
        }

        if (!await _context.GoalContributions.AnyAsync(c => c.UserId == demo.Id))
            await SeedContributionsAsync(demo.Id, goals);

        var investments = await _context.Investments.Where(i => i.UserId == demo.Id).ToListAsync();
        if (investments.Count == 0)
        {
            investments = await SeedInvestmentsAsync(demo.Id);
        }

        if (!await _context.InvestmentTransactions.AnyAsync(t => t.UserId == demo.Id))
            await SeedInvestmentTransactionsAsync(demo.Id, investments);

        if (!await _context.RecurringTransactions.AnyAsync(r => r.UserId == demo.Id))
            await SeedRecurringAsync(demo.Id, Cat, checking, digital);

        if (!await _context.Notifications.AnyAsync(n => n.UserId == demo.Id))
            await SeedNotificationsAsync(demo.Id);

        await RecalculateDemoBalancesAsync(demo.Id);
    }

    public async Task EnsureCategoriesAsync(string userId)
    {
        var existing = await _context.Categories.Where(c => c.UserId == userId).ToListAsync();
        var added = false;
        foreach (var def in CategoryDefinitions)
        {
            if (existing.Any(c => c.Name == def.Name && c.Type == def.Type))
                continue;
            _context.Categories.Add(new Category
            {
                UserId = userId, Name = def.Name, Color = def.Color, Icon = def.Icon,
                IsFixed = def.IsFixed, Type = def.Type, IsDefault = true
            });
            added = true;
        }
        if (added)
            await _context.SaveChangesAsync();
    }

    private async Task<List<Account>> EnsureAccountsAsync(string userId)
    {
        var accounts = await _context.Accounts.Where(a => a.UserId == userId).ToListAsync();
        var added = false;
        foreach (var def in AccountDefinitions)
        {
            if (accounts.Any(a => a.Name == def.Name))
                continue;
            var account = new Account
            {
                UserId = userId, Name = def.Name, Institution = def.Institution, Type = def.Type,
                InitialBalance = def.InitialBalance, Color = def.Color, Icon = def.Icon
            };
            _context.Accounts.Add(account);
            accounts.Add(account);
            added = true;
        }
        if (added)
            await _context.SaveChangesAsync();
        return accounts;
    }

    private async Task<List<CreditCard>> EnsureCardsAsync(string userId)
    {
        var cards = await _context.CreditCards.Where(c => c.UserId == userId).ToListAsync();
        var added = false;
        foreach (var def in CardDefinitions)
        {
            if (cards.Any(c => c.Name == def.Name))
                continue;
            var card = new CreditCard
            {
                UserId = userId, Name = def.Name, Institution = def.Institution, Brand = def.Brand,
                LastFourDigits = def.LastFour, CreditLimit = def.Limit, ClosingDay = def.Closing,
                DueDay = def.Due, Color = def.Color
            };
            _context.CreditCards.Add(card);
            cards.Add(card);
            added = true;
        }
        if (added)
            await _context.SaveChangesAsync();
        return cards;
    }

    private async Task SeedTransactionsAsync(string userId, Func<string, TransactionType, Category> cat,
        Account checking, Account digital, Account wallet, Account investmentAccount,
        CreditCard card1, CreditCard card2)
    {
        var random = new Random(42);
        var today = DateTime.Today;
        var transactions = new List<Transaction>();

        for (var monthsAgo = 5; monthsAgo >= 0; monthsAgo--)
        {
            var month = new DateTime(today.Year, today.Month, 1).AddMonths(-monthsAgo);
            var salary = 8500m + monthsAgo * 75m;
            transactions.Add(MakeTx(userId, "Salário mensal", salary, TransactionType.Income,
                cat("Salário", TransactionType.Income), checking, month.AddDays(4)));

            if (monthsAgo % 2 == 0)
                transactions.Add(MakeTx(userId, "Projeto freelance", 1450m + monthsAgo * 130m, TransactionType.Income,
                    cat("Freelance", TransactionType.Income), digital, month.AddDays(16)));

            transactions.Add(MakeTx(userId, "Aluguel do apartamento", 2200m, TransactionType.Expense,
                cat("Moradia", TransactionType.Expense), checking, month.AddDays(7)));
            transactions.Add(MakeTx(userId, "Contas de consumo", 390m + random.Next(0, 120), TransactionType.Expense,
                cat("Moradia", TransactionType.Expense), checking, month.AddDays(11)));
            transactions.Add(MakeTx(userId, "Mercado semanal", 280m + random.Next(0, 220), TransactionType.Expense,
                cat("Alimentação", TransactionType.Expense), checking, month.AddDays(13)));
            transactions.Add(MakeTx(userId, "Combustível", 160m + random.Next(0, 90), TransactionType.Expense,
                cat("Transporte", TransactionType.Expense), wallet, month.AddDays(15)));
            transactions.Add(MakeTx(userId, "Assinaturas digitais", 118m, TransactionType.Expense,
                cat("Assinaturas", TransactionType.Expense), digital, month.AddDays(18)));
            transactions.Add(MakeTx(userId, "Jantar fora", 95m + random.Next(0, 160), TransactionType.Expense,
                cat("Lazer", TransactionType.Expense), checking, month.AddDays(21), card1));
            transactions.Add(MakeTx(userId, "Farmácia", 78m + random.Next(0, 120), TransactionType.Expense,
                cat("Saúde", TransactionType.Expense), checking, month.AddDays(23), card2));

            if (monthsAgo == 0)
            {
                transactions.Add(MakeTx(userId, "Curso de especialização", 690m, TransactionType.Expense,
                    cat("Educação", TransactionType.Expense), checking, month.AddDays(9), card2));
                transactions.Add(MakeTx(userId, "Compra parcelada — Notebook", 780m, TransactionType.Expense,
                    cat("Outros", TransactionType.Expense), checking, month.AddDays(12), card1));
                transactions.Add(MakeTx(userId, "Dividendos recebidos", 340m, TransactionType.Income,
                    cat("Investimentos", TransactionType.Income), investmentAccount, month.AddDays(20)));
                transactions.Add(new Transaction
                {
                    UserId = userId, Description = "Reembolso pendente", Amount = 420m,
                    Type = TransactionType.Income, Status = TransactionStatus.Pending,
                    Date = today.AddDays(4), CategoryId = cat("Outros", TransactionType.Income).Id,
                    AccountId = checking.Id
                });
            }
        }

        await _context.Transactions.AddRangeAsync(transactions);
        await _context.SaveChangesAsync();
    }

    private async Task SeedBudgetsAsync(string userId, Func<string, TransactionType, Category> cat)
    {
        var currentMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        await _context.Budgets.AddRangeAsync(
            new Budget { UserId = userId, CategoryId = cat("Alimentação", TransactionType.Expense).Id, Period = currentMonth, LimitAmount = 1600, AlertThreshold = 80 },
            new Budget { UserId = userId, CategoryId = cat("Transporte", TransactionType.Expense).Id, Period = currentMonth, LimitAmount = 600, AlertThreshold = 80 },
            new Budget { UserId = userId, CategoryId = cat("Lazer", TransactionType.Expense).Id, Period = currentMonth, LimitAmount = 750, AlertThreshold = 85 },
            new Budget { UserId = userId, CategoryId = cat("Assinaturas", TransactionType.Expense).Id, Period = currentMonth, LimitAmount = 200, AlertThreshold = 90 });
        await _context.SaveChangesAsync();
    }

    private async Task<List<FinancialGoal>> SeedGoalsAsync(string userId)
    {
        var today = DateTime.Today;
        var goals = new List<FinancialGoal>
        {
            new() { UserId = userId, Name = "Reserva de emergência", Description = "Seis meses de custo de vida", TargetAmount = 30000, CurrentAmount = 18400, Deadline = today.AddMonths(8), Color = "#10b981", Icon = "bi-shield-check" },
            new() { UserId = userId, Name = "Viagem para Portugal", Description = "Passagens, hospedagem e passeios", TargetAmount = 15000, CurrentAmount = 6250, Deadline = today.AddMonths(12), Color = "#06b6d4", Icon = "bi-airplane" },
            new() { UserId = userId, Name = "Notebook novo", Description = "Estação de trabalho para estudos", TargetAmount = 9000, CurrentAmount = 3180, Deadline = today.AddMonths(6), Color = "#8b5cf6", Icon = "bi-laptop" },
            new() { UserId = userId, Name = "Carro próprio", Description = "Entrada para financiamento", TargetAmount = 80000, CurrentAmount = 12400, Deadline = today.AddMonths(36), Color = "#f59e0b", Icon = "bi-car-front" }
        };
        await _context.FinancialGoals.AddRangeAsync(goals);
        await _context.SaveChangesAsync();
        return goals;
    }

    private async Task SeedContributionsAsync(string userId, List<FinancialGoal> goals)
    {
        var today = DateTime.Today;
        FinancialGoal Goal(string name) => goals.First(g => g.Name == name);
        await _context.GoalContributions.AddRangeAsync(
            Contribution(userId, Goal("Reserva de emergência").Id, 5000, today.AddMonths(-5)),
            Contribution(userId, Goal("Reserva de emergência").Id, 6400, today.AddMonths(-3)),
            Contribution(userId, Goal("Reserva de emergência").Id, 7000, today.AddMonths(-1)),
            Contribution(userId, Goal("Viagem para Portugal").Id, 3250, today.AddMonths(-4)),
            Contribution(userId, Goal("Viagem para Portugal").Id, 3000, today.AddMonths(-1)),
            Contribution(userId, Goal("Notebook novo").Id, 3180, today.AddMonths(-2)),
            Contribution(userId, Goal("Carro próprio").Id, 12400, today.AddMonths(-6)));
        await _context.SaveChangesAsync();
    }

    private async Task<List<Investment>> SeedInvestmentsAsync(string userId)
    {
        var today = DateTime.Today;
        var investments = new List<Investment>
        {
            new() { UserId = userId, Name = "CDB Liquidez Diária", Ticker = "CDB", Category = InvestmentCategory.FixedIncome, Institution = "Banco Inter", Quantity = 1, AveragePrice = 25000, CurrentPrice = 26780, PurchaseDate = today.AddMonths(-9), Color = "#10b981" },
            new() { UserId = userId, Name = "Petrobras PN", Ticker = "PETR4", Category = InvestmentCategory.Stocks, Institution = "XP", Quantity = 200, AveragePrice = 22.4m, CurrentPrice = 24.85m, PurchaseDate = today.AddMonths(-7), Color = "#6366f1" },
            new() { UserId = userId, Name = "CSHG Logística FII", Ticker = "HGLG11", Category = InvestmentCategory.Reits, Institution = "XP", Quantity = 60, AveragePrice = 157.2m, CurrentPrice = 162.4m, PurchaseDate = today.AddMonths(-5), Color = "#8b5cf6" },
            new() { UserId = userId, Name = "Fundo Multimercado", Ticker = "MULT", Category = InvestmentCategory.Funds, Institution = "Creditas", Quantity = 1000, AveragePrice = 12.35m, CurrentPrice = 13.12m, PurchaseDate = today.AddMonths(-4), Color = "#06b6d4" },
            new() { UserId = userId, Name = "Bitcoin", Ticker = "BTC", Category = InvestmentCategory.Crypto, Institution = "Mercado Bitcoin", Quantity = 0.08m, AveragePrice = 178000, CurrentPrice = 205400, PurchaseDate = today.AddMonths(-3), Color = "#f59e0b" }
        };
        await _context.Investments.AddRangeAsync(investments);
        await _context.SaveChangesAsync();
        return investments;
    }

    private async Task SeedInvestmentTransactionsAsync(string userId, List<Investment> investments)
    {
        var today = DateTime.Today;
        Investment Inv(string ticker) => investments.First(i => i.Ticker == ticker);
        await _context.InvestmentTransactions.AddRangeAsync(
            InvTx(userId, Inv("CDB").Id, InvestmentTransactionType.Buy, 1, 25000, today.AddMonths(-9)),
            InvTx(userId, Inv("PETR4").Id, InvestmentTransactionType.Buy, 200, 22.4m, today.AddMonths(-7)),
            InvTx(userId, Inv("HGLG11").Id, InvestmentTransactionType.Buy, 60, 157.2m, today.AddMonths(-5)),
            InvTx(userId, Inv("MULT").Id, InvestmentTransactionType.Buy, 1000, 12.35m, today.AddMonths(-4)),
            InvTx(userId, Inv("BTC").Id, InvestmentTransactionType.Buy, 0.08m, 178000, today.AddMonths(-3)),
            InvTx(userId, Inv("PETR4").Id, InvestmentTransactionType.Dividend, 0, 380, today.AddDays(-12)));
        await _context.SaveChangesAsync();
    }

    private async Task SeedRecurringAsync(string userId, Func<string, TransactionType, Category> cat,
        Account checking, Account digital)
    {
        var today = DateTime.Today;
        await _context.RecurringTransactions.AddRangeAsync(
            new RecurringTransaction { UserId = userId, Description = "Salário mensal", Amount = 8850, Type = TransactionType.Income, Frequency = RecurrenceFrequency.Monthly, CategoryId = cat("Salário", TransactionType.Income).Id, AccountId = checking.Id, StartDate = today.AddMonths(-6), NextRunDate = new DateTime(today.Year, today.Month, 5).AddMonths(1) },
            new RecurringTransaction { UserId = userId, Description = "Aluguel do apartamento", Amount = 2200, Type = TransactionType.Expense, Frequency = RecurrenceFrequency.Monthly, CategoryId = cat("Moradia", TransactionType.Expense).Id, AccountId = checking.Id, StartDate = today.AddMonths(-6), NextRunDate = new DateTime(today.Year, today.Month, 8).AddMonths(1) },
            new RecurringTransaction { UserId = userId, Description = "Assinaturas digitais", Amount = 118, Type = TransactionType.Expense, Frequency = RecurrenceFrequency.Monthly, CategoryId = cat("Assinaturas", TransactionType.Expense).Id, AccountId = digital.Id, StartDate = today.AddMonths(-6), NextRunDate = new DateTime(today.Year, today.Month, 18).AddMonths(1) });
        await _context.SaveChangesAsync();
    }

    private async Task SeedNotificationsAsync(string userId)
    {
        await _context.Notifications.AddRangeAsync(
            new Notification { UserId = userId, Title = "Bem-vinda ao FinTrack", Message = "Execute o tour rápido e conheça seus indicadores.", Type = NotificationType.Success, Category = NotificationCategory.Alert, CreatedAt = DateTime.UtcNow.AddDays(-2), ActionUrl = "/Dashboard" },
            new Notification { UserId = userId, Title = "Orçamento próximo do limite", Message = "A categoria Assinaturas consumiu mais de 80% do limite.", Type = NotificationType.Warning, Category = NotificationCategory.Budget, CreatedAt = DateTime.UtcNow.AddDays(-1), ActionUrl = "/Budgets" },
            new Notification { UserId = userId, Title = "Fatura próxima do vencimento", Message = "Nubank Platinum vence em breve.", Type = NotificationType.Warning, Category = NotificationCategory.Invoice, CreatedAt = DateTime.UtcNow.AddHours(-6), ActionUrl = "/Cards" },
            new Notification { UserId = userId, Title = "Meta financeira alcançada", Message = "Você atingiu 60% da reserva de emergência.", Type = NotificationType.Success, Category = NotificationCategory.Goal, IsRead = true, CreatedAt = DateTime.UtcNow.AddDays(-4), ActionUrl = "/Goals" });
        await _context.SaveChangesAsync();
    }

    private async Task RecalculateDemoBalancesAsync(string userId)
    {
        var accounts = await _context.Accounts.Where(a => a.UserId == userId).ToListAsync();
        var sums = await _context.Transactions
            .Where(t => t.UserId == userId && t.Status == TransactionStatus.Completed)
            .GroupBy(t => t.AccountId)
            .Select(g => new
            {
                AccountId = g.Key,
                Income = g.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount),
                Expense = g.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount)
            }).ToListAsync();

        foreach (var account in accounts)
        {
            var sum = sums.FirstOrDefault(s => s.AccountId == account.Id);
            account.CurrentBalance = account.InitialBalance + (sum?.Income ?? 0) - (sum?.Expense ?? 0);
        }

        var reference = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var cards = await _context.CreditCards.Where(c => c.UserId == userId).ToListAsync();
        foreach (var card in cards)
        {
            var total = await _context.Transactions
                .Where(t => t.UserId == userId && t.CreditCardId == card.Id && t.Type == TransactionType.Expense
                            && t.Status == TransactionStatus.Completed && t.Date >= reference && t.Date < reference.AddMonths(1))
                .SumAsync(t => t.Amount);
            if (total <= 0)
                continue;
            var exists = await _context.CreditCardInvoices.AnyAsync(i =>
                i.UserId == userId && i.CreditCardId == card.Id && i.ReferenceMonth == reference);
            if (exists)
                continue;
            _context.CreditCardInvoices.Add(new CreditCardInvoice
            {
                UserId = userId,
                CreditCardId = card.Id,
                ReferenceMonth = reference,
                DueDate = new DateTime(reference.Year, reference.Month, Math.Min(card.DueDay, DateTime.DaysInMonth(reference.Year, reference.Month))),
                TotalAmount = total,
                Status = InvoiceStatus.Open
            });
        }

        await _context.SaveChangesAsync();
    }

    private static Transaction MakeTx(string userId, string description, decimal amount, TransactionType type,
        Category category, Account account, DateTime date, CreditCard? card = null) => new()
    {
        UserId = userId,
        Description = description,
        Amount = amount,
        Type = type,
        CategoryId = category.Id,
        AccountId = account.Id,
        CreditCardId = card?.Id,
        Date = date,
        Status = TransactionStatus.Completed
    };

    private static GoalContribution Contribution(string userId, int goalId, decimal amount, DateTime date) => new()
    {
        UserId = userId, FinancialGoalId = goalId, Amount = amount, Date = date,
        Notes = "Aportes de demonstração"
    };

    private static InvestmentTransaction InvTx(string userId, int investmentId, InvestmentTransactionType type,
        decimal quantity, decimal price, DateTime date) => new()
    {
        UserId = userId, InvestmentId = investmentId, Type = type, Quantity = quantity,
        Price = price, Amount = quantity == 0 ? price : quantity * price, Date = date
    };
}
