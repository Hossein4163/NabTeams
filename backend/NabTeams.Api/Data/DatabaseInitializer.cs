using Microsoft.EntityFrameworkCore;

namespace NabTeams.Api.Data;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
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
    }
}
