using GenericRepositories.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace GenericRepositories.Tests
{
    public class GenericRepositoryTests : IDisposable
    {
        private readonly TestDbContext _context;
        private readonly TestRepository _repository;

        public GenericRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new TestDbContext(options);
            _repository = new TestRepository(
                _context,
                NullLogger<GenericRepository<TestEntity, TestDbContext, Guid>>.Instance);
        }

        // --- AddAsync ---

        [Fact]
        public async Task AddAsync_ReturnsAddedEntity()
        {
            var entity = new TestEntity { Id = Guid.NewGuid(), Name = "Alpha" };

            var result = await _repository.AddAsync(entity);

            Assert.Equal(entity.Id, result.Id);
            Assert.Equal(entity.Name, result.Name);
        }

        [Fact]
        public async Task AddAsync_EntityIsPersistableAfterSave()
        {
            var entity = new TestEntity { Id = Guid.NewGuid(), Name = "Beta" };
            await _repository.AddAsync(entity);
            await _repository.SaveChangesAsync();

            var stored = await _context.TestEntities.FindAsync(entity.Id);
            Assert.NotNull(stored);
        }

        // --- AllAsync ---

        [Fact]
        public async Task AllAsync_ReturnsAllEntities()
        {
            await SeedAsync("A", "B", "C");

            var result = await _repository.AllAsync();

            Assert.Equal(3, result.Count());
        }

        [Fact]
        public async Task AllAsync_ReturnsEmptyWhenNoEntities()
        {
            var result = await _repository.AllAsync();

            Assert.Empty(result);
        }

        // --- AllAsync (paginated) ---

        [Fact]
        public async Task AllAsync_Paginated_ReturnsCorrectPage()
        {
            await SeedAsync("A", "B", "C", "D", "E");

            var page = await _repository.AllAsync(skip: 2, take: 2, q => q.OrderBy(e => e.Name));

            Assert.Equal(2, page.Count());
        }

        [Fact]
        public async Task AllAsync_Paginated_RespectsOrderBy()
        {
            await SeedAsync("Bravo", "Alpha", "Charlie");

            var page = await _repository.AllAsync(skip: 0, take: 3, q => q.OrderBy(e => e.Name));

            Assert.Equal(new[] { "Alpha", "Bravo", "Charlie" }, page.Select(e => e.Name));
        }

        [Fact]
        public async Task AllAsync_Paginated_RespectsOrderByDescending()
        {
            await SeedAsync("Bravo", "Alpha", "Charlie");

            var page = await _repository.AllAsync(skip: 0, take: 2, q => q.OrderByDescending(e => e.Name));

            Assert.Equal(new[] { "Charlie", "Bravo" }, page.Select(e => e.Name));
        }

        [Fact]
        public async Task AllAsync_Paginated_SkipBeyondCount_ReturnsEmpty()
        {
            await SeedAsync("A", "B");

            var page = await _repository.AllAsync(skip: 10, take: 5, q => q.OrderBy(e => e.Name));

            Assert.Empty(page);
        }

        // --- FindAsync ---

        [Fact]
        public async Task FindAsync_ReturnsMatchingEntities()
        {
            await SeedAsync("Alpha", "Alphabet", "Beta");

            var result = await _repository.FindAsync(e => e.Name.StartsWith("Alpha"));

            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task FindAsync_ReturnsEmptyWhenNoMatch()
        {
            await SeedAsync("Alpha");

            var result = await _repository.FindAsync(e => e.Name == "Nonexistent");

            Assert.Empty(result);
        }

        // --- FindFirstAsync ---

        [Fact]
        public async Task FindFirstAsync_ReturnsFirstMatchByOrder()
        {
            await SeedAsync("Alphabet", "Alpha", "Alpaca");

            var result = await _repository.FindFirstAsync(
                e => e.Name.StartsWith("Alpha"),
                q => q.OrderBy(e => e.Name));

            Assert.Equal("Alpha", result?.Name);
        }

        [Fact]
        public async Task FindFirstAsync_ReturnsFirstMatchByDescendingOrder()
        {
            await SeedAsync("Alpha", "Alpaca", "Alphabet");

            var result = await _repository.FindFirstAsync(
                e => e.Name.StartsWith("Alpha"),
                q => q.OrderByDescending(e => e.Name));

            Assert.Equal("Alphabet", result?.Name);
        }

        [Fact]
        public async Task FindFirstAsync_ReturnsNullWhenNoMatch()
        {
            await SeedAsync("Alpha");

            var result = await _repository.FindFirstAsync(
                e => e.Name == "Nonexistent",
                q => q.OrderBy(e => e.Name));

            Assert.Null(result);
        }

        // --- GetAsync ---

        [Fact]
        public async Task GetAsync_ReturnsEntityById()
        {
            var entity = new TestEntity { Id = Guid.NewGuid(), Name = "Target" };
            await _repository.AddAsync(entity);
            await _repository.SaveChangesAsync();

            var result = await _repository.GetAsync(entity.Id);

            Assert.NotNull(result);
            Assert.Equal(entity.Id, result.Id);
        }

        [Fact]
        public async Task GetAsync_ReturnsNullForUnknownId()
        {
            var result = await _repository.GetAsync(Guid.NewGuid());

            Assert.Null(result);
        }

        // --- SaveChangesAsync ---

        [Fact]
        public async Task SaveChangesAsync_ReturnsNumberOfChanges()
        {
            await _repository.AddAsync(new TestEntity { Id = Guid.NewGuid(), Name = "A" });
            await _repository.AddAsync(new TestEntity { Id = Guid.NewGuid(), Name = "B" });

            var changes = await _repository.SaveChangesAsync();

            Assert.Equal(2, changes);
        }

        // --- Update ---

        [Fact]
        public async Task Update_PersistsChangedValues()
        {
            var entity = new TestEntity { Id = Guid.NewGuid(), Name = "Original" };
            await _repository.AddAsync(entity);
            await _repository.SaveChangesAsync();

            entity.Name = "Updated";
            _repository.Update(entity);
            await _repository.SaveChangesAsync();

            var stored = await _context.TestEntities.FindAsync(entity.Id);
            Assert.Equal("Updated", stored!.Name);
        }

        [Fact]
        public async Task Update_ReturnsUpdatedEntity()
        {
            var entity = new TestEntity { Id = Guid.NewGuid(), Name = "Original" };
            await _repository.AddAsync(entity);
            await _repository.SaveChangesAsync();

            entity.Name = "Updated";
            var result = _repository.Update(entity);

            Assert.Equal("Updated", result.Name);
        }

        // --- Delete ---

        [Fact]
        public async Task Delete_RemovesEntityFromDatabase()
        {
            var entity = new TestEntity { Id = Guid.NewGuid(), Name = "ToDelete" };
            await _repository.AddAsync(entity);
            await _repository.SaveChangesAsync();

            _repository.Delete(entity);
            await _repository.SaveChangesAsync();

            var stored = await _context.TestEntities.FindAsync(entity.Id);
            Assert.Null(stored);
        }

        [Fact]
        public async Task Delete_ReturnsDeletedEntity()
        {
            var entity = new TestEntity { Id = Guid.NewGuid(), Name = "ToDelete" };
            await _repository.AddAsync(entity);
            await _repository.SaveChangesAsync();

            var result = _repository.Delete(entity);

            Assert.Equal(entity.Id, result.Id);
        }

        // --- Change tracking ---

        [Fact]
        public async Task AllAsync_NoTracking_EntitiesAreNotTracked()
        {
            await SeedAsync("A");

            var results = await _repository.AllAsync(QueryTrackingBehavior.NoTracking);

            Assert.All(results, e =>
                Assert.Equal(EntityState.Detached, _context.Entry(e).State));
        }

        [Fact]
        public async Task AllAsync_TrackAll_EntitiesAreTracked()
        {
            await SeedAsync("A");

            var results = await _repository.AllAsync(QueryTrackingBehavior.TrackAll);

            Assert.All(results, e =>
                Assert.Equal(EntityState.Unchanged, _context.Entry(e).State));
        }

        [Fact]
        public async Task FindAsync_NoTracking_EntitiesAreNotTracked()
        {
            await SeedAsync("Alpha");

            var results = await _repository.FindAsync(e => e.Name == "Alpha", QueryTrackingBehavior.NoTracking);

            Assert.All(results, e =>
                Assert.Equal(EntityState.Detached, _context.Entry(e).State));
        }

        [Fact]
        public async Task FindFirstAsync_NoTracking_EntityIsNotTracked()
        {
            await SeedAsync("Alpha");

            var result = await _repository.FindFirstAsync(e => e.Name == "Alpha", q => q.OrderBy(e => e.Name), QueryTrackingBehavior.NoTracking);

            Assert.NotNull(result);
            Assert.Equal(EntityState.Detached, _context.Entry(result).State);
        }

        [Fact]
        public async Task FindFirstAsync_TrackAll_EntityIsTracked()
        {
            await SeedAsync("Alpha");

            var result = await _repository.FindFirstAsync(e => e.Name == "Alpha", q => q.OrderBy(e => e.Name), QueryTrackingBehavior.TrackAll);

            Assert.NotNull(result);
            Assert.Equal(EntityState.Unchanged, _context.Entry(result).State);
        }

        // --- CancellationToken ---

        [Fact]
        public async Task AllAsync_ThrowsOnCancelledToken()
        {
            var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => _repository.AllAsync(ct: cts.Token));
        }

        // --- Dispose ---

        [Fact]
        public void Dispose_DoesNotThrow()
        {
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var ctx = new TestDbContext(options);
            var repo = new TestRepository(ctx, NullLogger<GenericRepository<TestEntity, TestDbContext, Guid>>.Instance);

            var ex = Record.Exception(() => repo.Dispose());

            Assert.Null(ex);
        }

        public void Dispose()
        {
            _repository.Dispose();
        }

        // --- Helpers ---

        private async Task SeedAsync(params string[] names)
        {
            foreach (var name in names)
                await _repository.AddAsync(new TestEntity { Id = Guid.NewGuid(), Name = name });
            await _repository.SaveChangesAsync();
        }
    }

    public class IntKeyRepositoryTests : IDisposable
    {
        private readonly IntTestDbContext _context;
        private readonly IntTestRepository _repository;

        public IntKeyRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<IntTestDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new IntTestDbContext(options);
            _repository = new IntTestRepository(
                _context,
                NullLogger<GenericRepository<IntTestEntity, IntTestDbContext, int>>.Instance);
        }

        [Fact]
        public async Task GetAsync_Int_ReturnsEntityById()
        {
            var entity = new IntTestEntity { Id = 1, Name = "Target" };
            await _repository.AddAsync(entity);
            await _repository.SaveChangesAsync();

            var result = await _repository.GetAsync(1);

            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
        }

        [Fact]
        public async Task GetAsync_Int_ReturnsNullForUnknownId()
        {
            var result = await _repository.GetAsync(99);

            Assert.Null(result);
        }

        [Fact]
        public async Task AddAsync_Int_EntityIsPersistableAfterSave()
        {
            var entity = new IntTestEntity { Id = 42, Name = "Answer" };
            await _repository.AddAsync(entity);
            await _repository.SaveChangesAsync();

            var stored = await _context.IntTestEntities.FindAsync(42);
            Assert.NotNull(stored);
            Assert.Equal("Answer", stored.Name);
        }

        [Fact]
        public async Task FindAsync_Int_ReturnsMatchingEntities()
        {
            await _repository.AddAsync(new IntTestEntity { Id = 1, Name = "Alpha" });
            await _repository.AddAsync(new IntTestEntity { Id = 2, Name = "Beta" });
            await _repository.SaveChangesAsync();

            var result = await _repository.FindAsync(e => e.Name == "Alpha");

            Assert.Single(result);
        }

        public void Dispose()
        {
            _repository.Dispose();
        }
    }
}
