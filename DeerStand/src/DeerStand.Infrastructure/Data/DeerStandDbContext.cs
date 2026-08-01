using DeerStand.Infrastructure.Tenants;
using Microsoft.EntityFrameworkCore;

namespace DeerStand.Infrastructure.Data;

public sealed class DeerStandDbContext(
    DbContextOptions<DeerStandDbContext> options,
    ITenantContext tenantContext) : DbContext(options)
{
    public DbSet<Profile> Profiles => Set<Profile>();
    public DbSet<Club> Clubs => Set<Club>();
    public DbSet<ClubMember> ClubMembers => Set<ClubMember>();
    public DbSet<Stand> Stands => Set<Stand>();
    public DbSet<ActiveCheckIn> ActiveCheckIns => Set<ActiveCheckIn>();
    public DbSet<CheckInHistory> CheckInHistory => Set<CheckInHistory>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();

    // Captured for HasQueryFilter; evaluated per query against the scoped tenant context.
    private readonly ITenantContext _tenant = tenantContext;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureProfile(modelBuilder);
        ConfigureClub(modelBuilder);
        ConfigureClubMember(modelBuilder);
        ConfigureStand(modelBuilder);
        ConfigureActiveCheckIn(modelBuilder);
        ConfigureCheckInHistory(modelBuilder);
        ConfigureActivityLog(modelBuilder);
    }

    private void ConfigureProfile(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Profile>();
        entity.ToTable("profiles");
        entity.HasKey(p => p.Id);
        entity.Property(p => p.Id).HasColumnName("id").HasMaxLength(64);
        entity.Property(p => p.FullName).HasColumnName("full_name").HasMaxLength(256).IsRequired();
        entity.Property(p => p.AvatarUrl).HasColumnName("avatar_url").HasMaxLength(1024);
        entity.Property(p => p.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");
    }

    private void ConfigureClub(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Club>();
        entity.ToTable("clubs");
        entity.HasKey(c => c.Id);
        entity.Property(c => c.Id).HasColumnName("id");
        entity.Property(c => c.Name).HasColumnName("name").HasMaxLength(128).IsRequired();
        entity.Property(c => c.InviteCode).HasColumnName("invite_code").HasMaxLength(32).IsRequired();
        entity.Property(c => c.OwnerId).HasColumnName("owner_id").HasMaxLength(64).IsRequired();
        entity.Property(c => c.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        entity.HasIndex(c => c.InviteCode).IsUnique();
        entity.HasOne(c => c.Owner)
            .WithMany(p => p.OwnedClubs)
            .HasForeignKey(c => c.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasQueryFilter(c => _tenant.ClubIds.Contains(c.Id));
    }

    private void ConfigureClubMember(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<ClubMember>();
        entity.ToTable("club_members");
        entity.HasKey(m => new { m.ClubId, m.ProfileId });
        entity.Property(m => m.ClubId).HasColumnName("club_id");
        entity.Property(m => m.ProfileId).HasColumnName("profile_id").HasMaxLength(64);
        entity.Property(m => m.Role).HasColumnName("role").HasMaxLength(16).IsRequired();
        entity.HasOne(m => m.Club)
            .WithMany(c => c.Members)
            .HasForeignKey(m => m.ClubId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(m => m.Profile)
            .WithMany(p => p.Memberships)
            .HasForeignKey(m => m.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasQueryFilter(m => _tenant.ClubIds.Contains(m.ClubId));
    }

    private void ConfigureStand(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Stand>();
        entity.ToTable("stands");
        entity.HasKey(s => s.Id);
        entity.Property(s => s.Id).HasColumnName("id");
        entity.Property(s => s.ClubId).HasColumnName("club_id");
        entity.Property(s => s.Name).HasColumnName("name").HasMaxLength(128).IsRequired();
        entity.Property(s => s.Latitude).HasColumnName("latitude").HasPrecision(9, 6);
        entity.Property(s => s.Longitude).HasColumnName("longitude").HasPrecision(9, 6);
        entity.Property(s => s.Notes).HasColumnName("notes").HasMaxLength(4000);
        entity.Property(s => s.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        entity.HasOne(s => s.Club)
            .WithMany(c => c.Stands)
            .HasForeignKey(s => s.ClubId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasQueryFilter(s => _tenant.ClubIds.Contains(s.ClubId));
    }

    private void ConfigureActiveCheckIn(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<ActiveCheckIn>();
        entity.ToTable("active_check_ins");
        entity.HasKey(c => c.StandId);
        entity.Property(c => c.StandId).HasColumnName("stand_id");
        entity.Property(c => c.ProfileId).HasColumnName("profile_id").HasMaxLength(64).IsRequired();
        entity.Property(c => c.CheckedInAt).HasColumnName("checked_in_at").HasColumnType("timestamptz");
        entity.HasOne(c => c.Stand)
            .WithOne(s => s.ActiveCheckIn)
            .HasForeignKey<ActiveCheckIn>(c => c.StandId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(c => c.Profile)
            .WithMany()
            .HasForeignKey(c => c.ProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        // Isolation via stand's club; navigation required for the filter expression.
        entity.HasQueryFilter(c => c.Stand != null && _tenant.ClubIds.Contains(c.Stand.ClubId));
    }

    private void ConfigureCheckInHistory(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<CheckInHistory>();
        entity.ToTable("check_in_history");
        entity.HasKey(h => h.Id);
        entity.Property(h => h.Id).HasColumnName("id");
        entity.Property(h => h.StandId).HasColumnName("stand_id");
        entity.Property(h => h.ProfileId).HasColumnName("profile_id").HasMaxLength(64).IsRequired();
        entity.Property(h => h.CheckedInAt).HasColumnName("checked_in_at").HasColumnType("timestamptz");
        entity.Property(h => h.CheckedOutAt).HasColumnName("checked_out_at").HasColumnType("timestamptz");
        entity.HasOne(h => h.Stand)
            .WithMany(s => s.CheckInHistory)
            .HasForeignKey(h => h.StandId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(h => h.Profile)
            .WithMany()
            .HasForeignKey(h => h.ProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasQueryFilter(h => h.Stand != null && _tenant.ClubIds.Contains(h.Stand.ClubId));
    }

    private void ConfigureActivityLog(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<ActivityLog>();
        entity.ToTable("activity_logs");
        entity.HasKey(a => a.Id);
        entity.Property(a => a.Id).HasColumnName("id");
        entity.Property(a => a.ClubId).HasColumnName("club_id");
        entity.Property(a => a.ProfileId).HasColumnName("profile_id").HasMaxLength(64).IsRequired();
        entity.Property(a => a.StandId).HasColumnName("stand_id");
        entity.Property(a => a.LogType).HasColumnName("log_type").HasMaxLength(32).IsRequired();
        entity.Property(a => a.Details).HasColumnName("details").HasMaxLength(4000);
        entity.Property(a => a.ImageUrl).HasColumnName("image_url").HasMaxLength(1024);
        entity.Property(a => a.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        entity.HasOne(a => a.Club)
            .WithMany(c => c.ActivityLogs)
            .HasForeignKey(a => a.ClubId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(a => a.Profile)
            .WithMany()
            .HasForeignKey(a => a.ProfileId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(a => a.Stand)
            .WithMany(s => s.ActivityLogs)
            .HasForeignKey(a => a.StandId)
            .OnDelete(DeleteBehavior.SetNull);

        entity.HasQueryFilter(a => _tenant.ClubIds.Contains(a.ClubId));
    }
}
