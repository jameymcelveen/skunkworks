using DeerStand.Core;
using DeerStand.Infrastructure.Data;
using DeerStand.Infrastructure.Tenants;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace DeerStand.Infrastructure.Tests;

public sealed class ModelConstraintTests
{
    [Fact]
    public void Active_check_in_stand_id_is_primary_key()
    {
        using var db = CreateSqliteDb();
        var entity = db.Model.FindEntityType(typeof(ActiveCheckIn));
        entity.ShouldNotBeNull();

        var key = entity.FindPrimaryKey();
        key.ShouldNotBeNull();
        key.Properties.ShouldHaveSingleItem().Name.ShouldBe(nameof(ActiveCheckIn.StandId));
    }

    [Fact]
    public void Club_member_composite_key_enforces_unique_membership()
    {
        using var db = CreateSqliteDb();
        var entity = db.Model.FindEntityType(typeof(ClubMember));
        entity.ShouldNotBeNull();

        var key = entity.FindPrimaryKey();
        key.ShouldNotBeNull();
        key.Properties.Select(p => p.Name).ShouldBe([
            nameof(ClubMember.ClubId),
            nameof(ClubMember.ProfileId)
        ]);
    }

    [Fact]
    public async Task Duplicate_active_check_in_for_same_stand_throws()
    {
        await using var db = CreateSqliteDb();
        await SeedStandAsync(db);

        db.ActiveCheckIns.Add(new ActiveCheckIn
        {
            StandId = SeedStandId,
            ProfileId = "hunter-a",
            CheckedInAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();

        // New context so the second insert is not blocked by the change tracker.
        await using var db2 = CreateSqliteDb(sharedConnection: db.Database.GetDbConnection());
        db2.ActiveCheckIns.Add(new ActiveCheckIn
        {
            StandId = SeedStandId,
            ProfileId = "hunter-b",
            CheckedInAt = DateTimeOffset.UtcNow
        });

        var act = async () => await db2.SaveChangesAsync();
        await act.ShouldThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Duplicate_club_membership_throws()
    {
        await using var db = CreateSqliteDb();
        await SeedClubAsync(db);

        db.ClubMembers.Add(new ClubMember
        {
            ClubId = SeedClubId,
            ProfileId = "member-a",
            Role = ClubRoles.Member
        });
        await db.SaveChangesAsync();

        await using var db2 = CreateSqliteDb(sharedConnection: db.Database.GetDbConnection());
        db2.ClubMembers.Add(new ClubMember
        {
            ClubId = SeedClubId,
            ProfileId = "member-a",
            Role = ClubRoles.Owner
        });

        var act = async () => await db2.SaveChangesAsync();
        await act.ShouldThrowAsync<DbUpdateException>();
    }

    private static readonly Guid SeedClubId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid SeedStandId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static DeerStandDbContext CreateSqliteDb(System.Data.Common.DbConnection? sharedConnection = null)
    {
        // Bypass club filters for constraint tests: empty ClubIds would hide seeded rows.
        var tenant = new TenantContext
        {
            ProfileId = "test",
            ClubIds = new HashSet<Guid> { SeedClubId }
        };

        SqliteConnection connection;
        if (sharedConnection is SqliteConnection existing)
        {
            connection = existing;
        }
        else
        {
            connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();
        }

        var options = new DbContextOptionsBuilder<DeerStandDbContext>()
            .UseSqlite(connection)
            .Options;

        var db = new DeerStandDbContext(options, tenant);
        db.Database.EnsureCreated();
        return db;
    }

    private static async Task SeedClubAsync(DeerStandDbContext db)
    {
        db.Profiles.Add(new Profile
        {
            Id = "owner-1",
            FullName = "Owner One",
            UpdatedAt = DateTimeOffset.UtcNow
        });
        db.Profiles.Add(new Profile
        {
            Id = "member-a",
            FullName = "Member A",
            UpdatedAt = DateTimeOffset.UtcNow
        });
        db.Clubs.Add(new Club
        {
            Id = SeedClubId,
            Name = "Test Club",
            InviteCode = "TESTCODE",
            OwnerId = "owner-1",
            CreatedAt = DateTimeOffset.UtcNow
        });
        db.ClubMembers.Add(new ClubMember
        {
            ClubId = SeedClubId,
            ProfileId = "owner-1",
            Role = ClubRoles.Owner
        });
        await db.SaveChangesAsync();
    }

    private static async Task SeedStandAsync(DeerStandDbContext db)
    {
        await SeedClubAsync(db);
        db.Profiles.Add(new Profile
        {
            Id = "hunter-a",
            FullName = "Hunter A",
            UpdatedAt = DateTimeOffset.UtcNow
        });
        db.Profiles.Add(new Profile
        {
            Id = "hunter-b",
            FullName = "Hunter B",
            UpdatedAt = DateTimeOffset.UtcNow
        });
        db.Stands.Add(new Stand
        {
            Id = SeedStandId,
            ClubId = SeedClubId,
            Name = "Ridge Stand",
            Latitude = 35.123456m,
            Longitude = -82.654321m,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();
    }
}
