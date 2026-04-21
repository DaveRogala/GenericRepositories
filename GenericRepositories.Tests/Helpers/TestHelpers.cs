using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GenericRepositories.Tests.Helpers
{
    public class TestEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

        public DbSet<TestEntity> TestEntities { get; set; }
    }

    public class TestRepository : GenericRepository<TestEntity, TestDbContext>
    {
        public TestRepository(TestDbContext context, ILogger<GenericRepository<TestEntity, TestDbContext>> logger)
            : base(context, logger) { }
    }
}
