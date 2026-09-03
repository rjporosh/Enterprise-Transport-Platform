using System.Globalization;
using System.Text;

namespace PaymentService.Infrastructure.Providers;

/// <summary>
/// Builds an EMVCo Merchant-Presented Mode QR payload (EMVCo "Merchant
/// Presented Mode" spec v1.1) — the format Bangladesh Bank standardised as
/// "Bangla QR". The string is deterministic and self-describing: every field
/// is <c>ID (2 digits) + length (2 digits) + value</c>, terminated by a
/// CRC-16/CCITT-FALSE over everything up to and including the CRC's own
/// ID+length ("6304").
///
/// A scanning bank/MFS app reads: point-of-initiation (dynamic), the merchant
/// account template, MCC, currency, amount, country, merchant name/city, and
/// an additional-data field carrying our payment id as the bill number so the
/// settlement callback can be matched back.
/// </summary>
public static class EmvcoQr
{
    public static string Build(QrCodeOptions o, decimal amount, string paymentId)
    {
        var sb = new StringBuilder();

        // 00 Payload Format Indicator
        sb.Append(Tlv("00", "01"));
        // 01 Point of Initiation Method — 12 = dynamic (amount present, single use)
        sb.Append(Tlv("01", "12"));

        // 26 Merchant Account Information (template) —
        //   00 = globally unique identifier (AID / reverse-domain), 01 = merchant id
        var merchantAccount = Tlv("00", o.MerchantAccountId) + Tlv("01", o.MerchantId);
        sb.Append(Tlv("26", merchantAccount));

        // 52 Merchant Category Code
        sb.Append(Tlv("52", o.MerchantCategoryCode));
        // 53 Transaction Currency (ISO 4217 numeric)
        sb.Append(Tlv("53", o.TransactionCurrency));
        // 54 Transaction Amount
        sb.Append(Tlv("54", amount.ToString("0.00", CultureInfo.InvariantCulture)));
        // 58 Country Code
        sb.Append(Tlv("58", o.CountryCode));
        // 59 Merchant Name / 60 Merchant City
        sb.Append(Tlv("59", Clip(o.MerchantName, 25)));
        sb.Append(Tlv("60", Clip(o.MerchantCity, 15)));

        // 62 Additional Data Field Template — 01 = bill number (our payment id)
        sb.Append(Tlv("62", Tlv("01", paymentId)));

        // 63 CRC — appended over the whole string including "6304"
        sb.Append("6304");
        sb.Append(Crc16(sb.ToString()));

        return sb.ToString();
    }

    /// <summary>Parses a payload back into its top-level tag→value map (used by tests and the webhook matcher).</summary>
    public static IReadOnlyDictionary<string, string> Parse(string payload)
    {
        var map = new Dictionary<string, string>();
        var i = 0;
        while (i + 4 <= payload.Length)
        {
            var id = payload.Substring(i, 2);
            var len = int.Parse(payload.Substring(i + 2, 2), CultureInfo.InvariantCulture);
            if (i + 4 + len > payload.Length) break;
            map[id] = payload.Substring(i + 4, len);
            i += 4 + len;
        }
        return map;
    }

    public static bool IsValid(string payload)
    {
        if (payload.Length < 8 || !payload.Contains("6304")) return false;
        var idx = payload.LastIndexOf("6304", StringComparison.Ordinal);
        var body = payload[..(idx + 4)];
        var crc = payload[(idx + 4)..];
        return crc.Length == 4 && string.Equals(Crc16(body), crc, StringComparison.OrdinalIgnoreCase);
    }

    private static string Tlv(string id, string value) =>
        id + value.Length.ToString("00", CultureInfo.InvariantCulture) + value;

    private static string Clip(string s, int max) => s.Length <= max ? s : s[..max];

    /// <summary>CRC-16/CCITT-FALSE (poly 0x1021, init 0xFFFF), upper-case hex, 4 chars.</summary>
    private static string Crc16(string input)
    {
        ushort crc = 0xFFFF;
        foreach (var b in Encoding.UTF8.GetBytes(input))
        {
            crc ^= (ushort)(b << 8);
            for (var i = 0; i < 8; i++)
                crc = (ushort)((crc & 0x8000) != 0 ? (crc << 1) ^ 0x1021 : crc << 1);
        }
        return crc.ToString("X4", CultureInfo.InvariantCulture);
    }
}
