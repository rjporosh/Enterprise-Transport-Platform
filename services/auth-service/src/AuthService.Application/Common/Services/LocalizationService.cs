using System.Globalization;

namespace AuthService.Application.Common.Services;

public sealed class LocalizationService : AuthService.Application.Common.Interfaces.ILocalizationService
{
    private static readonly Dictionary<string, Dictionary<string, string>> _resources = new(StringComparer.OrdinalIgnoreCase)
    {
        ["en"] = new()
        {
            ["login_success"] = "Login successful.",
            ["login_failed"] = "Invalid email or password.",
            ["account_locked"] = "Account is locked. Try again after {0}.",
            ["account_not_active"] = "Account is not active.",
            ["otp_sent"] = "OTP has been sent to your {0}.",
            ["otp_expired"] = "OTP has expired. Please request a new one.",
            ["otp_invalid"] = "Invalid OTP code.",
            ["otp_rate_limit"] = "Too many OTP requests. Please try again later.",
            ["password_changed"] = "Password changed successfully.",
            ["password_reset_sent"] = "Password reset instructions have been sent to your email.",
            ["password_reset_success"] = "Password has been reset successfully.",
            ["password_too_short"] = "Password must be at least {0} characters long.",
            ["password_reused"] = "Password cannot match one of the previous {0} passwords.",
            ["password_mismatch"] = "New password must be different from the current password.",
            ["security_questions_required"] = "You must configure at least {0} security questions.",
            ["security_questions_not_configured"] = "Security questions have not been configured for this account.",
            ["security_answer_invalid"] = "One or more security answers are incorrect.",
            ["invalid_reset_token"] = "Invalid or expired password reset token.",
            ["user_created"] = "User created successfully.",
            ["user_updated"] = "User updated successfully.",
            ["user_not_found"] = "User not found.",
            ["role_created"] = "Role created successfully.",
            ["role_updated"] = "Role updated successfully.",
            ["role_deleted"] = "Role deleted successfully.",
            ["permission_created"] = "Permission created successfully.",
            ["module_created"] = "Module created successfully.",
            ["generic_error"] = "An unexpected error occurred.",
            ["validation_failed"] = "Validation failed.",
            ["email_required"] = "Email is required.",
            ["email_invalid"] = "Email is invalid.",
            ["password_required"] = "Password is required.",
            ["unauthorized"] = "Unauthorized.",
            ["forbidden"] = "Forbidden.",
            ["not_found"] = "Resource not found.",
            ["conflict"] = "Resource already exists.",
            ["rate_limited"] = "Too many requests. Please try again later."
        },
        ["bn"] = new()
        {
            ["login_success"] = "লগইন সফল হয়েছে।",
            ["login_failed"] = "অবৈধ ইমেইল বা পাসওয়ার্ড।",
            ["account_locked"] = "অ্যাকাউন্ট লক করা আছে। {0} পরে আবার চেষ্টা করুন।",
            ["account_not_active"] = "অ্যাকাউন্ট সক্রিয় নয়।",
            ["otp_sent"] = "OTP আপনার {0}-এ পাঠানো হয়েছে।",
            ["otp_expired"] = "OTPের মেয়াদ শেষ হয়ে গেছে। অনুগ্রহ করে নতুন অনুরোধ করুন।",
            ["otp_invalid"] = "অবৈধ OTP কোড।",
            ["otp_rate_limit"] = "অনেক OTP অনুরোধ। অনুগ্রহ করে পরে আবার চেষ্টা করুন।",
            ["password_changed"] = "পাসওয়ার্ড পরিবর্তন সফল হয়েছে।",
            ["password_reset_sent"] = "পাসওয়ার্ড রিসেট নির্দেশিকা আপনার ইমেইলে পাঠানো হয়েছে।",
            ["password_reset_success"] = "পাসওয়ার্ড রিসেট সফল হয়েছে।",
            ["password_too_short"] = "পাসওয়ার্ড কমপক্ষে {0} অক্ষরের হতে হবে।",
            ["password_reused"] = "পাসওয়ার্ড পূর্বের {0}টি পাসওয়ার্ডের সাথে মিলতে পারবে না।",
            ["password_mismatch"] = "নতুন পাসওয়ার্ড বর্তমান পাসওয়ার্ড থেকে আলাদা হতে হবে।",
            ["security_questions_required"] = "আপনাকে কমপক্ষে {0}টি সুরক্ষা প্রশ্ন কনফিগার করতে হবে।",
            ["security_questions_not_configured"] = "এই অ্যাকাউন্টের জন্য সুরক্ষা প্রশ্ন কনফিগার করা হয়নি।",
            ["security_answer_invalid"] = "এক বা একাধিক সুরক্ষা উত্তর ভুল।",
            ["invalid_reset_token"] = "অবৈধ বা মেয়াদ শেষ পাসওয়ার্ড রিসেট টোকেন।",
            ["user_created"] = "ব্যবহারকারী সফলভাবে তৈরি হয়েছে।",
            ["user_updated"] = "ব্যবহারকারী সফলভাবে আপডেট হয়েছে।",
            ["user_not_found"] = "ব্যবহারকারী পাওয়া যায়নি।",
            ["role_created"] = "রোল সফলভাবে তৈরি হয়েছে।",
            ["role_updated"] = "রোল সফলভাবে আপডেট হয়েছে।",
            ["role_deleted"] = "রোল সফলভাবে মুছে ফেলা হয়েছে।",
            ["permission_created"] = "অনুমতি সফলভাবে তৈরি হয়েছে।",
            ["module_created"] = "মডিউল সফলভাবে তৈরি হয়েছে।",
            ["generic_error"] = "একটি অপ্রত্যাশিত ত্রুটি ঘটেছে।",
            ["validation_failed"] = "বৈধতা পরীক্ষা ব্যর্থ হয়েছে।",
            ["email_required"] = "ইমেইল প্রয়োজন।",
            ["email_invalid"] = "ইমেইল অবৈধ।",
            ["password_required"] = "পাসওয়ার্ড প্রয়োজন।",
            ["unauthorized"] = "অননুমতি।",
            ["forbidden"] = "নিষিদ্ধ।",
            ["not_found"] = "রিসোর্স পাওয়া যায়নি।",
            ["conflict"] = "রিসোর্স ইতিমধ্যে আছে।",
            ["rate_limited"] = "অনেক অনুরোধ। অনুগ্রহ করে পরে আবার চেষ্টা করুন।"
        }
    };

    public string Get(string key, string language = "en")
    {
        var lang = (language ?? "en").ToLowerInvariant();
        if (_resources.TryGetValue(lang, out var dict) && dict.TryGetValue(key, out var value))
            return value;
        if (_resources["en"].TryGetValue(key, out var fallback))
            return fallback;
        return key;
    }

    public string Get(string key, params object[] args)
    {
        var format = Get(key);
        return string.Format(format, args);
    }
}
