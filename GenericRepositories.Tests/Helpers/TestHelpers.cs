using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GenericRepositories.Tests.Helpers
{
    // --- Guid-keyed test fixtures ---

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

    public class TestRepository : GenericRepository<TestEntity, TestDbContext, Guid>
    {
        public TestRepository(TestDbContext context, ILogger<GenericRepository<TestEntity, TestDbContext, Guid>> logger)
            : base(context, logger) { }
    }

    // --- Int-keyed test fixtures ---

    public class IntTestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class IntTestDbContext : DbContext
    {
        public IntTestDbContext(DbContextOptions<IntTestDbContext> options) : base(options) { }

        public DbSet<IntTestEntity> IntTestEntities { get; set; }
    }

    public class IntTestRepository : GenericRepository<IntTestEntity, IntTestDbContext, int>
    {
        public IntTestRepository(IntTestDbContext context, ILogger<GenericRepository<IntTestEntity, IntTestDbContext, int>> logger)
            : base(context, logger) { }
    }
}
