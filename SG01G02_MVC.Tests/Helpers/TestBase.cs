using Microsoft.EntityFrameworkCore;
using SG01G02_MVC.Infrastructure.Data;

namespace SG01G02_MVC.Tests.Helpers
{
    public abstract class TestBase
    {
        protected AppDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new AppDbContext(options);
        }
    }
}