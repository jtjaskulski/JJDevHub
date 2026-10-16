using JJDevHub.EventStore.Postgres;
using Microsoft.EntityFrameworkCore;

namespace JJDevHub.EventStore.Postgres.Tests
{

    public sealed class TestEventStoreContext(DbContextOptions<TestEventStoreContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyEventStore();
        }
    }
}
