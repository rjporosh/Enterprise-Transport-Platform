using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;

namespace NotificationService.Infrastructure.Persistence;

/// <summary>
/// Idempotently inserts the core message templates the event consumer expects
/// (welcome, booking held/confirmed/cancelled, payment receipt/failed, ticket
/// issued) in English and Bangla. Runs once at startup — before this, every
/// event-driven notification failed with <c>Error.NotFound</c> (gap P1-10).
/// Keyed by <c>(Key, Channel, Locale)</c>; an operator can edit these rows
/// afterwards and the seeder will not overwrite them.
/// </summary>
public static class CoreTemplateSeeder
{
    public static async Task SeedAsync(NotificationDbContext db, ILogger logger, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var seeded = 0;

        foreach (var (key, locale, subject, body) in Templates())
        {
            var exists = await db.NotificationTemplates
                .AnyAsync(t => t.Key == key && t.Channel == TemplateChannel.Email && t.Locale == locale, ct);
            if (exists) continue;

            db.NotificationTemplates.Add(NotificationTemplate.Create(
                key, TemplateChannel.Email, locale, $"{key} ({locale})", "Seeded core template",
                subject, body, null, now));
            seeded++;
        }

        if (seeded > 0)
        {
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Seeded {Count} core notification template(s).", seeded);
        }
    }

    private static IEnumerable<(string Key, string Locale, string Subject, string Body)> Templates()
    {
        // ---- English -----------------------------------------------------
        yield return ("auth.welcome", "en", "Welcome to Enterprise Transport",
            "<p>Hi {{firstName}},</p><p>Your account is ready. Book your first trip any time.</p>");
        yield return ("auth.password-changed", "en", "Your password was changed",
            "<p>Hi {{firstName}},</p><p>Your password was just changed. If this wasn't you, contact support immediately.</p>");
        yield return ("auth.account-locked", "en", "Your account is temporarily locked",
            "<p>Hi {{firstName}},</p><p>Your account was locked after several failed sign-in attempts. It unlocks automatically shortly.</p>");
        yield return ("booking.held", "en", "Seats held — complete payment",
            "<p>Your seats {{seatNumbers}} are held for 10 minutes. Complete payment to confirm booking {{bookingId}}.</p>");
        yield return ("booking.confirmed", "en", "Booking confirmed — {{originCity}} → {{destinationCity}}",
            "<p>Your booking is confirmed.</p><p><b>{{originCity}} → {{destinationCity}}</b>, departs {{departureUtc}} UTC.<br/>Seats: {{seatNumbers}}. Amount paid: {{totalAmount}} {{currency}}.</p><p>Your e-ticket follows in a separate message.</p>");
        yield return ("booking.cancelled", "en", "Booking cancelled",
            "<p>Booking {{bookingId}} has been cancelled. Reason: {{reason}}. Any payment will be refunded per policy.</p>");
        yield return ("payment.receipt", "en", "Payment receipt",
            "<p>We received your payment of {{totalAmount}} {{currency}} for order {{orderReference}}. Provider ref: {{providerReference}}.</p>");
        yield return ("payment.failed", "en", "Payment failed",
            "<p>Your payment for order {{orderReference}} could not be completed. Reason: {{reason}}. Please try again.</p>");
        yield return ("ticket.issued", "en", "Your e-ticket {{ticketNumber}}",
            "<p>Hi {{customerName}},</p><p>Your e-ticket <b>{{ticketNumber}}</b> for <b>{{originCity}} → {{destinationCity}}</b> (departs {{departureUtc}} UTC) is ready.</p><p><a href=\"{{pdfUrl}}\">Download / print your ticket (PDF)</a></p><p>At the gate, staff scan the QR on the ticket. Verification code: {{verificationCode}}.</p>");

        // ---- Bangla (bn) — fallback is English if a locale row is missing.
        yield return ("auth.welcome", "bn", "এন্টারপ্রাইজ ট্রান্সপোর্টে স্বাগতম",
            "<p>প্রিয় {{firstName}},</p><p>আপনার অ্যাকাউন্ট প্রস্তুত। যেকোনো সময় আপনার প্রথম যাত্রা বুক করুন।</p>");
        yield return ("auth.password-changed", "bn", "আপনার পাসওয়ার্ড পরিবর্তন করা হয়েছে",
            "<p>প্রিয় {{firstName}},</p><p>আপনার পাসওয়ার্ড এইমাত্র পরিবর্তন করা হয়েছে। এটি আপনি না করলে অবিলম্বে সহায়তা কেন্দ্রে যোগাযোগ করুন।</p>");
        yield return ("auth.account-locked", "bn", "আপনার অ্যাকাউন্ট সাময়িকভাবে লক করা হয়েছে",
            "<p>প্রিয় {{firstName}},</p><p>কয়েকবার ভুল সাইন-ইনের কারণে আপনার অ্যাকাউন্ট লক করা হয়েছে। এটি শীঘ্রই স্বয়ংক্রিয়ভাবে খুলে যাবে।</p>");
        yield return ("booking.held", "bn", "সিট সংরক্ষিত — পেমেন্ট সম্পন্ন করুন",
            "<p>আপনার সিট {{seatNumbers}} ১০ মিনিটের জন্য সংরক্ষিত। বুকিং {{bookingId}} নিশ্চিত করতে পেমেন্ট সম্পন্ন করুন।</p>");
        yield return ("booking.confirmed", "bn", "বুকিং নিশ্চিত — {{originCity}} → {{destinationCity}}",
            "<p>আপনার বুকিং নিশ্চিত হয়েছে।</p><p><b>{{originCity}} → {{destinationCity}}</b>, ছাড়বে {{departureUtc}} UTC।<br/>সিট: {{seatNumbers}}। পরিশোধিত: {{totalAmount}} {{currency}}।</p><p>আপনার ই-টিকিট আলাদা বার্তায় পাঠানো হবে।</p>");
        yield return ("booking.cancelled", "bn", "বুকিং বাতিল",
            "<p>বুকিং {{bookingId}} বাতিল করা হয়েছে। কারণ: {{reason}}। নীতিমালা অনুযায়ী পেমেন্ট ফেরত দেওয়া হবে।</p>");
        yield return ("payment.receipt", "bn", "পেমেন্ট রসিদ",
            "<p>অর্ডার {{orderReference}} এর জন্য আপনার {{totalAmount}} {{currency}} পেমেন্ট আমরা পেয়েছি। প্রদানকারী রেফ: {{providerReference}}।</p>");
        yield return ("payment.failed", "bn", "পেমেন্ট ব্যর্থ",
            "<p>অর্ডার {{orderReference}} এর পেমেন্ট সম্পন্ন হয়নি। কারণ: {{reason}}। আবার চেষ্টা করুন।</p>");
        yield return ("ticket.issued", "bn", "আপনার ই-টিকিট {{ticketNumber}}",
            "<p>প্রিয় {{customerName}},</p><p><b>{{originCity}} → {{destinationCity}}</b> ({{departureUtc}} UTC) এর জন্য আপনার ই-টিকিট <b>{{ticketNumber}}</b> প্রস্তুত।</p><p><a href=\"{{pdfUrl}}\">টিকিট ডাউনলোড / প্রিন্ট করুন (PDF)</a></p><p>গেটে কর্মীরা টিকিটের QR স্ক্যান করবেন। যাচাই কোড: {{verificationCode}}।</p>");
    }
}
