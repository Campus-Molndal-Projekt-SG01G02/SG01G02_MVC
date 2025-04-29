using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SG01G02_MVC.Infrastructure.Data;

namespace SG01G02_MVC.Tests.Helpers
{
    public static class TestDbContextFactory
    {
        public static AppDbContext CreateSqliteInMemoryContext()
        {
            var connection = new SqliteConnection("Filename=:memory:");
            connection.Open(); // 🔑 Required for SQLite in-memory

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options;

            var context = new AppDbContext(options);
            context.Database.EnsureCreated();

            return context;
        }
    }
}