using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace JJDevHub.Notifications.Worker.Data
{

    public sealed class NotificationsContextFactory : IDesignTimeDbContextFactory<NotificationsContext>
    {
        public NotificationsContext CreateDbContext(string[] args)
        {
            var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__notifications")
                ?? "Host=localhost;Port=5433;Database=notifications;Username=postgres;Password=password";
            var options = new DbContextOptionsBuilder<NotificationsContext>()
                .UseNpgsql(connectionString)
                .Options;
            return new NotificationsContext(options);
        }
    }
}
