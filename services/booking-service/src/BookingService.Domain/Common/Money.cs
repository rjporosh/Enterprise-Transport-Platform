namespace BookingService.Domain.Common;

/// <summary>Immutable value object representing a monetary amount in a specific currency.</summary>
public sealed record Money(decimal Amount, string Currency)
{
    public static Money Zero(string currency) => new(0m, currency);

    public static Money operator +(Money a, Money b)
    {
        if (a.Currency != b.Currency)
            throw new InvalidOperationException($"Cannot add {a.Currency} to {b.Currency}.");
        return new (a.Amount + b.Amount, a.Currency);
    }

    public static Money operator *(Money a, int multiplier) => new(a.Amount * multiplier, a.Currency);

    public override string ToString() => $"{Amount:0.00} {Currency}";
}
