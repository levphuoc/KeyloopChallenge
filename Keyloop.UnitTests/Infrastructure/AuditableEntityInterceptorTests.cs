using Keyloop.Application.Abstractions;
using Keyloop.Domain.Entities;
using Keyloop.Infrastructure.Persistence.Context;
using Keyloop.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;

namespace Keyloop.UnitTests.Infrastructure;

public sealed class AuditableEntityInterceptorTests
{
    private static readonly DateTimeOffset FixedUtc = new(2026, 10, 31, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task SavingChanges_OnAdded_SetsCreatedAndUpdatedAudit()
    {
        await using var context = CreateContext("user-1");

        context.Notes.Add(new TestNote { Title = "hello" });
        await context.SaveChangesAsync();

        var note = await context.Notes.SingleAsync();
        Assert.Equal("hello", note.Title);
        Assert.Equal(FixedUtc, note.CreatedAt);
        Assert.Equal("user-1", note.CreatedBy);
        Assert.Equal(FixedUtc, note.UpdatedAt);
        Assert.Equal("user-1", note.UpdatedBy);
        Assert.False(note.IsDeleted);
    }

    [Fact]
    public async Task SavingChanges_OnModified_UpdatesAudit_AndPreservesCreated()
    {
        await using var context = CreateContext("user-1");
        var note = new TestNote { Title = "hello" };
        context.Notes.Add(note);
        await context.SaveChangesAsync();

        var originalCreatedAt = note.CreatedAt;
        var originalCreatedBy = note.CreatedBy;

        note.Title = "updated";
        await context.SaveChangesAsync();

        Assert.Equal(originalCreatedAt, note.CreatedAt);
        Assert.Equal(originalCreatedBy, note.CreatedBy);
        Assert.Equal("user-1", note.UpdatedBy);
        Assert.Equal(FixedUtc, note.UpdatedAt);
    }

    [Fact]
    public async Task SavingChanges_OnDeleted_ConvertsToSoftDelete()
    {
        await using var context = CreateContext("user-2");
        var note = new TestNote { Title = "hello" };
        context.Notes.Add(note);
        await context.SaveChangesAsync();

        context.Notes.Remove(note);
        await context.SaveChangesAsync();

        var visible = await context.Notes.ToListAsync();
        Assert.Empty(visible);

        var deleted = await context.Notes.IgnoreQueryFilters().SingleAsync();
        Assert.True(deleted.IsDeleted);
        Assert.Equal(FixedUtc, deleted.DeletedAt);
        Assert.Equal("user-2", deleted.DeletedBy);
    }

    private static TestAppDbContext CreateContext(string userId)
    {
        var interceptor = new AuditableEntityInterceptor(
            new FakeCurrentUser { UserId = userId },
            new FixedTimeProvider(FixedUtc));

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(interceptor)
            .Options;

        return new TestAppDbContext(options);
    }

    private sealed class TestAppDbContext(DbContextOptions<AppDbContext> options) : AppDbContext(options)
    {
        public DbSet<TestNote> Notes => Set<TestNote>();
    }

    private sealed class TestNote : AuditableEntity
    {
        public string Title { get; set; } = string.Empty;
    }

    private sealed class FakeCurrentUser : ICurrentUser
    {
        public string? UserId { get; init; }
        public bool IsAuthenticated => !string.IsNullOrWhiteSpace(UserId);
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
