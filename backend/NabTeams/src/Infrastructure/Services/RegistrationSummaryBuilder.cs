using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NabTeams.Application.Abstractions;
using NabTeams.Domain.Entities;
using NabTeams.Domain.Enums;

namespace NabTeams.Infrastructure.Services;

public class RegistrationSummaryBuilder : IRegistrationSummaryBuilder
{
    private readonly IRegistrationDocumentStorage _storage;

    public RegistrationSummaryBuilder(IRegistrationDocumentStorage storage)
    {
        _storage = storage;
    }

    public async Task<StoredRegistrationDocument> BuildSummaryAsync(
        ParticipantRegistration registration,
        CancellationToken cancellationToken = default)
    {
        if (registration is null)
        {
            throw new ArgumentNullException(nameof(registration));
        }

        var content = BuildSummaryContent(registration);
        await using var memoryStream = new MemoryStream();
        await using (var writer = new StreamWriter(memoryStream, Encoding.UTF8, leaveOpen: true))
        {
            await writer.WriteAsync(content.AsMemory(), cancellationToken);
            await writer.FlushAsync();
        }

        memoryStream.Position = 0;
        var fileName = GenerateFileName(registration);
        return await _storage.SaveAsync(fileName, memoryStream, cancellationToken);
    }

    private static string BuildSummaryContent(ParticipantRegistration registration)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# خلاصه ثبت‌نام تیم");
        builder.AppendLine();
        builder.AppendLine("## اطلاعات سرپرست");
        builder.AppendLine($"نام: {registration.HeadFirstName} {registration.HeadLastName}");
        builder.AppendLine($"کد ملی: {registration.NationalId}");
        builder.AppendLine($"شماره تماس: {registration.PhoneNumber}");
        if (!string.IsNullOrWhiteSpace(registration.Email))
        {
            builder.AppendLine($"ایمیل: {registration.Email}");
        }
        if (registration.BirthDate is { } birthDate)
        {
            builder.AppendLine($"تاریخ تولد: {birthDate:yyyy-MM-dd}");
        }
        builder.AppendLine($"مدرک تحصیلی: {registration.EducationDegree}");
        builder.AppendLine($"رشته تحصیلی: {registration.FieldOfStudy}");
        builder.AppendLine();

        builder.AppendLine("## اطلاعات تیم");
        builder.AppendLine($"نام تیم: {registration.TeamName}");
        builder.AppendLine($"تیم دارد: {(registration.HasTeam ? "بله" : "خیر")}");
        builder.AppendLine($"تیم تکمیل است: {(registration.TeamCompleted ? "بله" : "خیر")}");
        if (!string.IsNullOrWhiteSpace(registration.AdditionalNotes))
        {
            builder.AppendLine($"توضیحات تکمیلی: {registration.AdditionalNotes}");
        }
        builder.AppendLine();

        builder.AppendLine("## اعضای تیم");
        if (registration.Members.Count == 0)
        {
            builder.AppendLine("- عضوی ثبت نشده است.");
        }
        else
        {
            foreach (var member in registration.Members)
            {
                builder.AppendLine($"- {member.FullName} | نقش: {member.Role} | حوزه: {member.FocusArea}");
            }
        }
        builder.AppendLine();

        builder.AppendLine("## مدارک");
        if (registration.Documents.Count == 0)
        {
            builder.AppendLine("- مدرکی بارگذاری نشده است.");
        }
        else
        {
            foreach (var document in registration.Documents)
            {
                builder.AppendLine($"- {TranslateDocumentCategory(document.Category)}: {document.FileName} ({document.FileUrl})");
            }
        }
        builder.AppendLine();

        builder.AppendLine("## لینک‌ها");
        if (registration.Links.Count == 0)
        {
            builder.AppendLine("- لینکی ثبت نشده است.");
        }
        else
        {
            foreach (var link in registration.Links)
            {
                builder.AppendLine($"- {TranslateLinkType(link.Type)}: {link.Label} ({link.Url})");
            }
        }
        builder.AppendLine();

        builder.AppendLine("## وضعیت پرداخت");
        if (registration.Payment is null)
        {
            builder.AppendLine("- پرداختی ثبت نشده است.");
        }
        else
        {
            builder.AppendLine($"- مبلغ: {registration.Payment.Amount} {registration.Payment.Currency}");
            builder.AppendLine($"- وضعیت: {registration.Payment.Status}");
            if (!string.IsNullOrWhiteSpace(registration.Payment.PaymentUrl))
            {
                builder.AppendLine($"- لینک پرداخت: {registration.Payment.PaymentUrl}");
            }
            if (!string.IsNullOrWhiteSpace(registration.Payment.GatewayReference))
            {
                builder.AppendLine($"- شناسه تراکنش: {registration.Payment.GatewayReference}");
            }
            if (registration.Payment.CompletedAt is { } completed)
            {
                builder.AppendLine($"- تاریخ تکمیل: {completed:yyyy-MM-dd HH:mm}");
            }
        }
        builder.AppendLine();

        builder.AppendLine("## اعلان‌های ارسال‌شده");
        if (registration.Notifications.Count == 0)
        {
            builder.AppendLine("- اعلان ثبت نشده است.");
        }
        else
        {
            foreach (var notification in registration.Notifications.OrderBy(n => n.SentAt))
            {
                builder.AppendLine($"- {notification.SentAt:yyyy-MM-dd HH:mm} | {notification.Channel}: {notification.Subject} => {notification.Recipient}");
            }
        }
        builder.AppendLine();

        builder.AppendLine("## تحلیل‌های هوش مصنوعی");
        if (registration.BusinessPlanReviews.Count == 0)
        {
            builder.AppendLine("- تحلیلی انجام نشده است.");
        }
        else
        {
            foreach (var review in registration.BusinessPlanReviews.OrderByDescending(r => r.CreatedAt))
            {
                builder.AppendLine($"- {review.CreatedAt:yyyy-MM-dd HH:mm} | امتیاز: {(review.OverallScore.HasValue ? review.OverallScore.Value.ToString("0.##") : "نامشخص")}");
                builder.AppendLine($"  خلاصه: {review.Summary}");
            }
        }
        builder.AppendLine();

        builder.AppendLine($"وضعیت فعلی: {registration.Status}");
        builder.AppendLine($"تاریخ ثبت اولیه: {registration.SubmittedAt:yyyy-MM-dd HH:mm}");
        if (registration.FinalizedAt is { } finalized)
        {
            builder.AppendLine($"تاریخ نهایی‌سازی: {finalized:yyyy-MM-dd HH:mm}");
        }

        return builder.ToString();
    }

    private static string GenerateFileName(ParticipantRegistration registration)
    {
        var baseName = string.IsNullOrWhiteSpace(registration.TeamName)
            ? $"participant-{registration.Id:N}"
            : registration.TeamName;

        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(baseName.Select(ch => invalidChars.Contains(ch) ? '-' : ch).ToArray()).Trim('-');

        if (string.IsNullOrWhiteSpace(sanitized))
        {
            sanitized = $"participant-{registration.Id:N}";
        }

        return $"{sanitized}-summary.txt";
    }

    private static string TranslateDocumentCategory(RegistrationDocumentCategory category) => category switch
    {
        RegistrationDocumentCategory.ProjectArchive => "فایل پروژه",
        RegistrationDocumentCategory.Resume => "رزومه",
        RegistrationDocumentCategory.PitchDeck => "پیچ‌دک",
        RegistrationDocumentCategory.BusinessPlan => "طرح کسب‌وکار",
        _ => category.ToString()
    };

    private static string TranslateLinkType(RegistrationLinkType type) => type switch
    {
        RegistrationLinkType.LinkedIn => "لینکدین",
        RegistrationLinkType.GitHub => "گیت‌هاب",
        RegistrationLinkType.Website => "وب‌سایت",
        RegistrationLinkType.Instagram => "اینستاگرام",
        _ => type.ToString()
    };
}
