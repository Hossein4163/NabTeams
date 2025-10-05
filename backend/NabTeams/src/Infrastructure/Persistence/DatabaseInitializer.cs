using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NabTeams.Domain.Entities;
using NabTeams.Domain.Enums;

namespace NabTeams.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.MigrateAsync(cancellationToken);

        if (!await context.KnowledgeBaseItems.AnyAsync(cancellationToken))
        {
            context.KnowledgeBaseItems.AddRange(new[]
            {
                new KnowledgeBaseItemEntity
                {
                    Id = "event-rules",
                    Title = "قوانین کلی رویداد",
                    Body = "شرکت‌کنندگان باید قوانین اخلاقی و حرفه‌ای را رعایت کنند. ساعات برگزاری از 9 تا 18 می‌باشد.",
                    Audience = "participant",
                    Tags = new List<string> { "rules", "schedule" },
                    UpdatedAt = DateTimeOffset.UtcNow
                },
                new KnowledgeBaseItemEntity
                {
                    Id = "mentor-support",
                    Title = "نقش منتورها",
                    Body = "منتورها می‌توانند از طریق داشبورد منتور مستقیماً با تیم‌ها گفتگو کنند و دسترسی به اتاق‌های منتورینگ دارند.",
                    Audience = "mentor",
                    Tags = new List<string> { "mentor", "access" },
                    UpdatedAt = DateTimeOffset.UtcNow
                },
                new KnowledgeBaseItemEntity
                {
                    Id = "contact-admin",
                    Title = "راه‌های ارتباطی با ادمین",
                    Body = "برای مسائل اضطراری با شماره 021-000000 تماس بگیرید یا از فرم تیکت در داشبورد استفاده کنید.",
                    Audience = "all",
                    Tags = new List<string> { "contact", "support" },
                    UpdatedAt = DateTimeOffset.UtcNow
                },
                new KnowledgeBaseItemEntity
                {
                    Id = "investor-brief",
                    Title = "دسترسی سرمایه‌گذاران",
                    Body = "سرمایه‌گذاران به داشبورد ارزیابی مالی و گزارش‌های تیم‌ها دسترسی دارند. نسخه به‌روزشده هر روز ساعت 12 منتشر می‌شود.",
                    Audience = "investor",
                    Tags = new List<string> { "investor", "reports" },
                    UpdatedAt = DateTimeOffset.UtcNow
                }
            });

            await context.SaveChangesAsync(cancellationToken);
        }

        if (!await context.Events.AnyAsync(cancellationToken))
        {
            context.Events.AddRange(new[]
            {
                new EventEntity
                {
                    Id = Guid.Parse("97A1E4F0-5B85-4F1F-9EAA-9F0A9E1B7123"),
                    Name = "رویداد شتابدهی پاییز ۱۴۰۳",
                    Description = "رویداد سه‌ماهه با تمرکز بر استارتاپ‌های هوش مصنوعی و کشاورزی هوشمند.",
                    StartsAt = DateTimeOffset.UtcNow.AddDays(-7),
                    EndsAt = DateTimeOffset.UtcNow.AddMonths(2),
                    AiTaskManagerEnabled = true,
                    CreatedAt = DateTimeOffset.UtcNow
                },
                new EventEntity
                {
                    Id = Guid.Parse("5FD3C9F8-6D69-4C42-AE95-4F2B5DBA5E57"),
                    Name = "رویداد سرمایه‌گذاری زمستانی",
                    Description = "گردهمایی سرمایه‌گذاران با تمرکز بر استارتاپ‌های مرحله رشد.",
                    StartsAt = DateTimeOffset.UtcNow.AddMonths(1),
                    EndsAt = DateTimeOffset.UtcNow.AddMonths(3),
                    AiTaskManagerEnabled = false,
                    CreatedAt = DateTimeOffset.UtcNow
                }
            });

            await context.SaveChangesAsync(cancellationToken);
        }

        var defaultEvent = await context.Events
            .AsNoTracking()
            .OrderBy(e => e.StartsAt ?? e.CreatedAt)
            .FirstAsync(cancellationToken);

        if (!await context.ParticipantRegistrations.AnyAsync(cancellationToken))
        {
            var participantRegistration = new ParticipantRegistrationEntity
            {
                Id = Guid.Parse("58F3FC6F-0F73-4C81-8F5A-64F0D3D1BEBF"),
                EventId = defaultEvent.Id,
                HeadFirstName = "سارا",
                HeadLastName = "محمودی",
                NationalId = "1234567890",
                PhoneNumber = "09120000000",
                Email = "sara.mahmoudi@example.com",
                BirthDate = new DateOnly(1995, 5, 12),
                EducationDegree = "کارشناسی ارشد",
                FieldOfStudy = "مهندسی نرم‌افزار",
                TeamName = "نوآوران هوش",
                HasTeam = true,
                TeamCompleted = true,
                AdditionalNotes = "تیم ما روی سامانهٔ تحلیل داده برای کشاورزی هوشمند کار می‌کند.",
                Status = RegistrationStatus.PaymentRequested,
                SubmittedAt = DateTimeOffset.UtcNow.AddDays(-2),
                Members = new List<TeamMemberEntity>
                {
                    new TeamMemberEntity
                    {
                        Id = Guid.Parse("3B0C1F77-6411-4F8B-9AEC-4CF430280E22"),
                        FullName = "علی رضایی",
                        Role = "Backend Developer",
                        FocusArea = "Node.js"
                    },
                    new TeamMemberEntity
                    {
                        Id = Guid.Parse("4E1F3A52-119D-4F6C-A9F0-1C37B642D777"),
                        FullName = "نگار احمدی",
                        Role = "Product Designer",
                        FocusArea = "UX/UI"
                    }
                },
                Documents = new List<RegistrationDocumentEntity>
                {
                    new RegistrationDocumentEntity
                    {
                        Id = Guid.Parse("BF6CBDA0-952A-470D-9E5F-803E2BC6D63F"),
                        Category = RegistrationDocumentCategory.ProjectArchive,
                        FileName = "pitch-deck.pdf",
                        FileUrl = "https://example.com/demo/pitch-deck.pdf"
                    },
                    new RegistrationDocumentEntity
                    {
                        Id = Guid.Parse("0F4E3F94-5BD9-4DB0-8A4E-A6BB3D6F8351"),
                        Category = RegistrationDocumentCategory.TeamResume,
                        FileName = "team-cv.zip",
                        FileUrl = "https://example.com/demo/team-cv.zip"
                    }
                },
                Links = new List<RegistrationLinkEntity>
                {
                    new RegistrationLinkEntity
                    {
                        Id = Guid.Parse("3180A5C4-863D-404E-A4C5-4B93543E5A9B"),
                        Type = RegistrationLinkType.GitHub,
                        Label = "گیت‌هاب",
                        Url = "https://github.com/demo-team"
                    },
                    new RegistrationLinkEntity
                    {
                        Id = Guid.Parse("8AC121AF-E0E5-4B40-B87E-29B1D43E6B10"),
                        Type = RegistrationLinkType.LinkedIn,
                        Label = "لینکدین",
                        Url = "https://linkedin.com/company/demo-team"
                    }
                },
                Payment = new RegistrationPaymentEntity
                {
                    Id = Guid.Parse("C0E075F0-20D1-4F3B-956B-9CE814A5BCE5"),
                    Amount = 2500000m,
                    Currency = "IRR",
                    PaymentUrl = "https://payments.example.com/session/C0E075F0-20D1-4F3B-956B-9CE814A5BCE5",
                    Status = RegistrationPaymentStatus.Pending,
                    RequestedAt = DateTimeOffset.UtcNow.AddDays(-1)
                },
                Notifications = new List<RegistrationNotificationEntity>
                {
                    new RegistrationNotificationEntity
                    {
                        Id = Guid.Parse("5F9F3B13-1EB6-4AF0-9D34-3FA3C370AF0F"),
                        Channel = NotificationChannel.Email,
                        Recipient = "sara.mahmoudi@example.com",
                        Subject = "دریافت لینک پرداخت مرحله دوم",
                        Message = "لطفاً هزینه ورود به مرحله دوم را از طریق لینک پرداخت ارسال شده تکمیل کنید.",
                        SentAt = DateTimeOffset.UtcNow.AddHours(-20)
                    }
                },
                BusinessPlanReviews = new List<BusinessPlanReviewEntity>
                {
                    new BusinessPlanReviewEntity
                    {
                        Id = Guid.Parse("1E3C7C6F-8E34-4B6F-B8BB-9F6E2B1AF5A1"),
                        Status = BusinessPlanReviewStatus.Completed,
                        OverallScore = 82,
                        Summary = "طرح کشاورزی هوشمند پتانسیل بازار بالایی دارد و تیم تجربهٔ پیاده‌سازی اولیه را نشان داده است.",
                        Strengths = "تیم چندتخصصی، تمرکز روی بازار کشاورزی دیجیتال، نمونه اولیه فعال.",
                        Risks = "وابستگی به داده‌های دولتی و چالش در دسترسی به کشاورزان کوچک.",
                        Recommendations = "برنامه جذب مشتریان پایلوت در استان‌های مختلف تدوین شود و مدل درآمدی بر اساس اشتراک سالانه مشخص گردد.",
                        RawResponse = "{\"score\":82,\"summary\":\"...\"}",
                        Model = "gemini-1.5-pro",
                        SourceDocumentUrl = "https://example.com/demo/pitch-deck.pdf",
                        CreatedAt = DateTimeOffset.UtcNow.AddHours(-4)
                    }
                },
                Tasks = new List<ParticipantTaskEntity>
                {
                    new ParticipantTaskEntity
                    {
                        Id = Guid.Parse("F3B8A1E5-1F8A-4A2F-9C10-6E8A12DAA1C2"),
                        EventId = defaultEvent.Id,
                        Title = "بازبینی مدل کسب‌وکار",
                        Description = "همه مفروضات درآمدی در بوم مدل کسب‌وکار مرور و با داده‌های کشاورزی تطبیق داده شود.",
                        Status = ParticipantTaskStatus.InProgress,
                        AssignedTo = "سارا محمودی",
                        DueAt = DateTimeOffset.UtcNow.AddDays(5),
                        CreatedAt = DateTimeOffset.UtcNow.AddDays(-1)
                    },
                    new ParticipantTaskEntity
                    {
                        Id = Guid.Parse("C5C6D51E-7E1C-4B22-A07F-779A9EC19235"),
                        EventId = defaultEvent.Id,
                        Title = "دموی محصول برای منتورها",
                        Description = "نسخه نمایشی سامانه برای ارائه هفتگی به منتورها آماده شود.",
                        Status = ParticipantTaskStatus.Todo,
                        AssignedTo = "علی رضایی",
                        DueAt = DateTimeOffset.UtcNow.AddDays(10),
                        CreatedAt = DateTimeOffset.UtcNow
                    }
                }
            };

            context.ParticipantRegistrations.Add(participantRegistration);
            await context.SaveChangesAsync(cancellationToken);
        }

        if (!await context.JudgeRegistrations.AnyAsync(cancellationToken))
        {
            context.JudgeRegistrations.Add(new JudgeRegistrationEntity
            {
                Id = Guid.Parse("5F5A7811-A410-43F6-9EAF-99C7BDDB2E97"),
                FirstName = "مهدی",
                LastName = "اکبری",
                NationalId = "2233445566",
                PhoneNumber = "09123334455",
                Email = "mehdi.akbari@example.com",
                BirthDate = new DateOnly(1987, 3, 21),
                FieldOfExpertise = "سرمایه‌گذاری خطرپذیر",
                HighestDegree = "دکتری مدیریت کسب‌وکار",
                Biography = "۱۵ سال سابقهٔ مشاورهٔ استارتاپ و داوری رویدادهای کارآفرینی.",
                Status = RegistrationStatus.Submitted,
                SubmittedAt = DateTimeOffset.UtcNow.AddDays(-1)
            });

            await context.SaveChangesAsync(cancellationToken);
        }

        if (!await context.InvestorRegistrations.AnyAsync(cancellationToken))
        {
            context.InvestorRegistrations.Add(new InvestorRegistrationEntity
            {
                Id = Guid.Parse("904F5A2F-9B8D-4C73-B2D8-4E0F59ACB9EE"),
                FirstName = "لیلا",
                LastName = "حسینی",
                NationalId = "9988776655",
                PhoneNumber = "09125557788",
                Email = "leila.hosseini@example.com",
                InterestAreas = new List<string> { "AgriTech", "AI", "Robotics" },
                AdditionalNotes = "به دنبال تیم‌هایی با مدل درآمدی مشخص و مشتریان پایلوت هستم.",
                Status = RegistrationStatus.Submitted,
                SubmittedAt = DateTimeOffset.UtcNow.AddDays(-3)
            });

            await context.SaveChangesAsync(cancellationToken);
        }

        var now = DateTimeOffset.UtcNow;

        if (!await context.IntegrationSettings.AnyAsync(cancellationToken))
        {
            context.IntegrationSettings.AddRange(new[]
            {
                new IntegrationSettingEntity
                {
                    Id = Guid.Parse("A8E4E4D1-9E8E-4E5E-9BDE-5B2D39E1D5B4"),
                    Type = IntegrationProviderType.Gemini,
                    ProviderKey = "gemini",
                    DisplayName = "Google Gemini Sandbox",
                    Configuration = JsonSerializer.Serialize(new
                    {
                        ApiKey = "YOUR_GEMINI_API_KEY",
                        Endpoint = "https://generativelanguage.googleapis.com/v1beta",
                        BaseUrl = "https://generativelanguage.googleapis.com",
                        ModerationModel = "gemini-1.5-pro",
                        RagModel = "gemini-1.5-pro",
                        BusinessPlanModel = "gemini-1.5-pro",
                        BusinessPlanTemperature = 0.2
                    }),
                    IsActive = true,
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new IntegrationSettingEntity
                {
                    Id = Guid.Parse("B2C3D4E5-F6A7-48B9-9C1D-0E2F3A4B5C6D"),
                    Type = IntegrationProviderType.PaymentGateway,
                    ProviderKey = "idpay",
                    DisplayName = "IdPay Sandbox",
                    Configuration = JsonSerializer.Serialize(new
                    {
                        Provider = "idpay",
                        BaseUrl = "https://api.idpay.ir",
                        ApiKey = "IDPAY_SANDBOX_KEY",
                        CreatePath = "/v1.1/payment",
                        VerifyPath = "/v1.1/payment/verify",
                        CallbackBaseUrl = "https://localhost:5000",
                        Sandbox = true
                    }),
                    IsActive = true,
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new IntegrationSettingEntity
                {
                    Id = Guid.Parse("C4D5E6F7-A8B9-4C0D-9E1F-203040506070"),
                    Type = IntegrationProviderType.Email,
                    ProviderKey = "smtp",
                    DisplayName = "SMTP Demo",
                    Configuration = JsonSerializer.Serialize(new
                    {
                        Provider = "smtp",
                        Host = "smtp.example.com",
                        Port = 587,
                        UseSsl = true,
                        Username = "demo",
                        Password = "demo-password",
                        SenderAddress = "no-reply@example.com",
                        SenderDisplayName = "NabTeams"
                    }),
                    IsActive = true,
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new IntegrationSettingEntity
                {
                    Id = Guid.Parse("D6E7F8A9-B0C1-4D2E-9F30-405060708090"),
                    Type = IntegrationProviderType.Sms,
                    ProviderKey = "kavenegar",
                    DisplayName = "Kavenegar Sandbox",
                    Configuration = JsonSerializer.Serialize(new
                    {
                        Provider = "kavenegar",
                        BaseUrl = "https://api.kavenegar.com",
                        ApiKey = "KAVENEGAR_SANDBOX_KEY",
                        SenderNumber = "10004321",
                        Path = "v1/{apiKey}/sms/send.json"
                    }),
                    IsActive = true,
                    CreatedAt = now,
                    UpdatedAt = now
                }
            });

            await context.SaveChangesAsync(cancellationToken);
        }

        if (!await context.OperationsChecklistItems.AnyAsync(cancellationToken))
        {
            var checklistCreatedAt = DateTimeOffset.UtcNow;
            context.OperationsChecklistItems.AddRange(new[]
            {
                new OperationsChecklistItemEntity
                {
                    Id = Guid.Parse("A32B21F1-2E57-4D1E-8D03-3E42B8D78211"),
                    Key = "security-scan",
                    Title = "اسکن امنیتی OWASP ZAP",
                    Description = "اجرای اسکریپت zap-baseline و مستندسازی نتایج بدون مورد High.",
                    Category = "امنیت",
                    Status = OperationsChecklistStatus.Pending,
                    CreatedAt = checklistCreatedAt,
                    UpdatedAt = checklistCreatedAt
                },
                new OperationsChecklistItemEntity
                {
                    Id = Guid.Parse("B45AC6DF-5D94-4D1F-8F6C-FD7FA71C1F23"),
                    Key = "monitoring-alerts",
                    Title = "پیکربندی هشدارهای مانیتورینگ",
                    Description = "اتصال متریک‌ها به Prometheus و تنظیم هشدار برای شاخص‌های کلیدی.",
                    Category = "عملیاتی",
                    Status = OperationsChecklistStatus.Pending,
                    CreatedAt = checklistCreatedAt,
                    UpdatedAt = checklistCreatedAt
                },
                new OperationsChecklistItemEntity
                {
                    Id = Guid.Parse("C56B77E8-4A15-4DF0-9D54-8F0D3FB49318"),
                    Key = "operator-training",
                    Title = "آموزش اپراتورها",
                    Description = "تمرین سناریوهای قطع سرویس AI و پردازش اعتراضات طبق Runbook.",
                    Category = "عملیاتی",
                    Status = OperationsChecklistStatus.Pending,
                    CreatedAt = checklistCreatedAt,
                    UpdatedAt = checklistCreatedAt
                },
                new OperationsChecklistItemEntity
                {
                    Id = Guid.Parse("D67889F9-7F1A-43F2-94C3-8D42B5F5C3C2"),
                    Key = "load-test",
                    Title = "اجرای تست بار",
                    Description = "اجرای اسکریپت chat-smoke و ثبت نتایج در Runbook.",
                    Category = "کارایی",
                    Status = OperationsChecklistStatus.Pending,
                    CreatedAt = checklistCreatedAt,
                    UpdatedAt = checklistCreatedAt
                },
                new OperationsChecklistItemEntity
                {
                    Id = Guid.Parse("E7899A0A-82C5-4C7A-9C19-A00D5F3B54B2"),
                    Key = "integrations-config",
                    Title = "تنظیم کلیدهای یکپارچه‌سازی",
                    Description = "فعال‌سازی کلیدهای تولیدی برای Gemini، پرداخت، ایمیل و پیامک.",
                    Category = "پیکربندی",
                    Status = OperationsChecklistStatus.Pending,
                    CreatedAt = checklistCreatedAt,
                    UpdatedAt = checklistCreatedAt
                },
                new OperationsChecklistItemEntity
                {
                    Id = Guid.Parse("F89AAB1B-9C36-49AA-82E4-6AC3E4F6D3A2"),
                    Key = "storage-hardening",
                    Title = "پیکربندی فضای ذخیره‌سازی",
                    Description = "تنظیم سطح دسترسی فایل‌ها و مسیر ذخیره‌سازی مدارک ثبت‌نام.",
                    Category = "پیکربندی",
                    Status = OperationsChecklistStatus.Pending,
                    CreatedAt = checklistCreatedAt,
                    UpdatedAt = checklistCreatedAt
                },
                new OperationsChecklistItemEntity
                {
                    Id = Guid.Parse("0A1BBC2C-7E48-4DE9-B4AF-DBA6450B6FAE"),
                    Key = "privacy-policy",
                    Title = "انتشار سیاست حریم خصوصی",
                    Description = "تهیه و انتشار سند حریم خصوصی مرتبط با داده‌های ثبت‌نام.",
                    Category = "انطباق",
                    Status = OperationsChecklistStatus.Pending,
                    CreatedAt = checklistCreatedAt,
                    UpdatedAt = checklistCreatedAt
                }
            });

            await context.SaveChangesAsync(cancellationToken);
        }

        var overrides = IntegrationSettingsEnvironmentLoader.Load(now);
        if (overrides.Count > 0)
        {
            var hasChanges = false;

            foreach (var setting in overrides)
            {
                var existing = await context.IntegrationSettings
                    .FirstOrDefaultAsync(
                        s => s.Type == setting.Type && s.ProviderKey == setting.ProviderKey,
                        cancellationToken);

                if (existing is null)
                {
                    context.IntegrationSettings.Add(setting);
                    hasChanges = true;
                    continue;
                }

                if (existing.Configuration != setting.Configuration ||
                    existing.DisplayName != setting.DisplayName ||
                    existing.IsActive != setting.IsActive)
                {
                    existing.DisplayName = setting.DisplayName;
                    existing.Configuration = setting.Configuration;
                    existing.IsActive = setting.IsActive;
                    existing.UpdatedAt = now;
                    hasChanges = true;
                }
            }

            if (hasChanges)
            {
                await context.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
