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
                NullLogger<GenericRepository<TestEntity, TestDbContext>>.Instance);
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

            var page = await _repository.AllAsync(skip: 2, take: 2);

            Assert.Equal(2, page.Count());
        }

        [Fact]
        public async Task AllAsync_Paginated_SkipBeyondCount_ReturnsEmpty()
        {
            await SeedAsync("A", "B");

            var page = await _repository.AllAsync(skip: 10, take: 5);

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
        public async Task FindFirstAsync_ReturnsFirstMatch()
        {
            await SeedAsync("Alpha", "Alphabet");

            var result = await _repository.FindFirstAsync(e => e.Name.StartsWith("Alpha"));

            Assert.NotNull(result);
        }

        [Fact]
        public async Task FindFirstAsync_ReturnsNullWhenNoMatch()
        {
            await SeedAsync("Alpha");

            var result = await _repository.FindFirstAsync(e => e.Name == "Nonexistent");

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

        // --- CancellationToken ---

        [Fact]
        public async Task AllAsync_ThrowsOnCancelledToken()
        {
            var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => _repository.AllAsync(cts.Token));
        }

        // --- Dispose ---

        [Fact]
        public void Dispose_DoesNotThrow()
        {
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var ctx = new TestDbContext(options);
            var repo = new TestRepository(ctx, NullLogger<GenericRepository<TestEntity, TestDbContext>>.Instance);

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
}
