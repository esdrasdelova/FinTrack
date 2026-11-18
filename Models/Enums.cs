namespace FinTrack.Models;

public enum TransactionType
{
    Income = 1,
    Expense = 2
}

public enum TransactionStatus
{
    Completed = 1,
    Pending = 2,
    Cancelled = 3
}

public enum AccountType
{
    Checking = 1,
    Savings = 2,
    Wallet = 3,
    Digital = 4,
    Investments = 5
}

public enum RecurrenceFrequency
{
    Daily = 1,
    Weekly = 2,
    Monthly = 3,
    Yearly = 4
}

public enum InvestmentCategory
{
    FixedIncome = 1,
    Stocks = 2,
    Reits = 3,
    Funds = 4,
    Crypto = 5
}

public enum InvestmentTransactionType
{
    Buy = 1,
    Sell = 2,
    Dividend = 3,
    Contribution = 4
}

public enum NotificationType
{
    Info = 1,
    Success = 2,
    Warning = 3,
    Danger = 4
}

public enum NotificationCategory
{
    DueDate = 1,
    Budget = 2,
    Goal = 3,
    Invoice = 4,
    Transaction = 5,
    Alert = 6,
    Recurring = 7
}

public enum InvoiceStatus
{
    Open = 1,
    Closed = 2,
    Paid = 3,
    Overdue = 4
}

public enum PeriodPreset
{
    Today = 1,
    Last7Days = 2,
    Last30Days = 3,
    ThisMonth = 4,
    Last6Months = 5,
    ThisYear = 6,
    Custom = 7
}

public enum ReportKind
{
    IncomeByPeriod = 1,
    ExpensesByPeriod = 2,
    CashFlow = 3,
    MonthlyResult = 4,
    ExpensesByCategory = 5,
    NetWorthEvolution = 6,
    YearlyComparison = 7,
    Performance = 8
}
