namespace PaymentService.Domain.Common;

public sealed record Money
{
    public decimal Amount { get; init; }
    public string Currency { get; init; } = string.Empty;

    public Money(decimal amount, string currency)
    {
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative.", nameof(amount));

        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
            throw new ArgumentException("Currency must be a 3-letter ISO code.", nameof(currency));

        Amount = Math.Round(amount, 2);
        Currency = currency.ToUpperInvariant();
    }

    public static Money operator +(Money left, Money right)
    {
        if (left.Currency != right.Currency)
            throw new InvalidOperationException("Cannot add money with different currencies.");

        return new Money(left.Amount + right.Amount, left.Currency);
    }

    public static Money operator -(Money left, Money right)
    {
        if (left.Currency != right.Currency)
            throw new InvalidOperationException("Cannot subtract money with different currencies.");

        return new Money(left.Amount - right.Amount, left.Currency);
    }

    public static Money operator *(Money money, decimal multiplier)
    {
        return new Money(money.Amount * multiplier, money.Currency);
    }

    public static Money operator /(Money money, decimal divisor)
    {
        if (divisor == 0)
            throw new DivideByZeroException("Cannot divide money by zero.");

        return new Money(money.Amount / divisor, money.Currency);
    }

    public bool IsGreaterThan(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException("Cannot compare money with different currencies.");

        return Amount > other.Amount;
    }

    public bool IsLessThan(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException("Cannot compare money with different currencies.");

        return Amount < other.Amount;
    }

    public bool IsGreaterThanOrEqual(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException("Cannot compare money with different currencies.");

        return Amount >= other.Amount;
    }

    public bool IsLessThanOrEqual(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException("Cannot compare money with different currencies.");

        return Amount <= other.Amount;
    }

    public bool IsZero() => Amount == 0;

    public bool IsPositive() => Amount > 0;

    public override string ToString() => $"{Currency} {Amount:N2}";
}
