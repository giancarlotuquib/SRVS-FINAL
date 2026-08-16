using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SRVS.Domain.Entities;
using SRVS.Domain.Enums;
namespace SRVS.Web.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
	private readonly bool _isNpgsql;

	public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
	{
		_isNpgsql = options.Extensions.Any(e => e.GetType().Name.Contains("Npgsql", StringComparison.OrdinalIgnoreCase));
	}

	public DbSet<SyllabusDocument> SyllabusDocuments => Set<SyllabusDocument>();

	public DbSet<SyllabusAssignment> SyllabusAssignments => Set<SyllabusAssignment>();

	public DbSet<AuditLogEntry> AuditLogEntries => Set<AuditLogEntry>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		// ── Table mappings ──────────────────────────────────────────
		modelBuilder.Entity<ApplicationUser>().ToTable("users");
		modelBuilder.Entity<IdentityRole>().ToTable("roles", "identity");
		modelBuilder.Entity<IdentityUserRole<string>>().ToTable("user_roles", "identity");
		modelBuilder.Entity<IdentityUserClaim<string>>().ToTable("user_claims", "identity");
		modelBuilder.Entity<IdentityUserLogin<string>>().ToTable("user_logins", "identity");
		modelBuilder.Entity<IdentityRoleClaim<string>>().ToTable("role_claims", "identity");
		modelBuilder.Entity<IdentityUserToken<string>>().ToTable("user_tokens", "identity");

		modelBuilder.Entity<SyllabusDocument>().ToTable("syllabi");
		modelBuilder.Entity<SyllabusAssignment>().ToTable("syllabus_assignments");
		modelBuilder.Entity<AuditLogEntry>().ToTable("audit_logs");

		// ── Remove hashed / normalized / unused Identity columns ─────
		modelBuilder.Entity<ApplicationUser>().Ignore(u => u.SecurityStamp);
		modelBuilder.Entity<ApplicationUser>().Ignore(u => u.ConcurrencyStamp);
		modelBuilder.Entity<ApplicationUser>().Ignore(u => u.NormalizedUserName);
		modelBuilder.Entity<ApplicationUser>().Ignore(u => u.NormalizedEmail);
		modelBuilder.Entity<ApplicationUser>().Ignore(u => u.AccessFailedCount);
		modelBuilder.Entity<ApplicationUser>().Ignore(u => u.EmailConfirmed);
		modelBuilder.Entity<ApplicationUser>().Ignore(u => u.PhoneNumber);
		modelBuilder.Entity<ApplicationUser>().Ignore(u => u.PhoneNumberConfirmed);
		modelBuilder.Entity<ApplicationUser>().Ignore(u => u.TwoFactorEnabled);
		modelBuilder.Entity<ApplicationUser>().Ignore(u => u.LockoutEnd);
		modelBuilder.Entity<ApplicationUser>().Ignore(u => u.LockoutEnabled);

		modelBuilder.Entity<SyllabusAssignment>().Ignore(a => a.SyllabusId);
		modelBuilder.Entity<SyllabusAssignment>().Ignore(a => a.AssignedAt);

		modelBuilder.Entity<IdentityRole>().Ignore(r => r.ConcurrencyStamp);
		modelBuilder.Entity<IdentityRole>().Ignore(r => r.NormalizedName);

		// ── Convert String IDs to PostgreSQL BIGINT ─────────────────
		modelBuilder.Entity<ApplicationUser>()
			.Property(u => u.Id)
			.HasConversion(v => ParseLongSafe(v), v => v.ToString());

		modelBuilder.Entity<SyllabusDocument>()
			.Property(s => s.InstructorId)
			.HasConversion(v => ParseLongSafe(v), v => v.ToString());

		modelBuilder.Entity<SyllabusDocument>()
			.Property(s => s.OwnerUserId)
			.HasConversion(v => ParseLongSafe(v), v => v.ToString());

		modelBuilder.Entity<SyllabusDocument>()
			.Property(s => s.ReviewedByUserId)
			.HasConversion(v => ParseNullableLongSafe(v), v => v != null ? v.ToString() : null);

		modelBuilder.Entity<SyllabusAssignment>()
			.Property(a => a.StudentId)
			.HasConversion(v => ParseLongSafe(v), v => v.ToString());

		modelBuilder.Entity<SyllabusAssignment>()
			.Property(a => a.AssignedBy)
			.HasConversion(v => ParseLongSafe(v), v => v.ToString());

		modelBuilder.Entity<AuditLogEntry>()
			.Property(a => a.UserId)
			.HasConversion(v => ParseNullableLongSafe(v), v => v != null ? v.ToString() : null);

			// ── Convert DateTimeOffset to PostgreSQL DATE (DateOnly) ───────
			var dateOnlyConverter = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTimeOffset, DateOnly>(
				dto => DateOnly.FromDateTime(dto.DateTime),
				dOnly => new DateTimeOffset(dOnly.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero));

			var nullableDateOnlyConverter = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTimeOffset?, DateOnly?>(
				dto => dto.HasValue ? DateOnly.FromDateTime(dto.Value.DateTime) : null,
				dOnly => dOnly.HasValue ? new DateTimeOffset(dOnly.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero) : null);

			modelBuilder.Entity<ApplicationUser>().Property(u => u.CreatedAtUtc).HasConversion(dateOnlyConverter);
			modelBuilder.Entity<ApplicationUser>().Property(u => u.LastLoginAtUtc).HasConversion(nullableDateOnlyConverter);

			modelBuilder.Entity<SyllabusDocument>().Property(s => s.SubmittedAtUtc).HasConversion(nullableDateOnlyConverter);
			modelBuilder.Entity<SyllabusDocument>().Property(s => s.ReviewedAtUtc).HasConversion(nullableDateOnlyConverter);
			modelBuilder.Entity<SyllabusDocument>().Property(s => s.CreatedAtUtc).HasConversion(dateOnlyConverter);
			modelBuilder.Entity<SyllabusDocument>().Property(s => s.UpdatedAtUtc).HasConversion(nullableDateOnlyConverter);

			modelBuilder.Entity<SyllabusAssignment>().Property(a => a.AssignedAtDate).HasConversion(dateOnlyConverter);
			modelBuilder.Entity<SyllabusAssignment>().Property(a => a.DeletedAt).HasConversion(nullableDateOnlyConverter);
			modelBuilder.Entity<SyllabusAssignment>().Property(a => a.CreatedAtUtc).HasConversion(dateOnlyConverter);
			modelBuilder.Entity<SyllabusAssignment>().Property(a => a.UpdatedAtUtc).HasConversion(nullableDateOnlyConverter);

			modelBuilder.Entity<AuditLogEntry>().Property(a => a.CreatedAtUtc).HasConversion(dateOnlyConverter);
			modelBuilder.Entity<AuditLogEntry>().Property(a => a.UpdatedAtUtc).HasConversion(nullableDateOnlyConverter);

		// ── Store enums as text ─────────────────────────────────────
		modelBuilder.Entity<ApplicationUser>()
			.Property(u => u.Role)
			.HasConversion<string>();

		modelBuilder.Entity<ApplicationUser>()
			.Property(u => u.AccountStatus)
			.HasConversion<string>();

		modelBuilder.Entity<SyllabusDocument>()
			.Property(s => s.Status)
			.HasConversion<string>();

		modelBuilder.Entity<AuditLogEntry>()
			.Property(a => a.ActionType)
			.HasConversion<string>();

		modelBuilder.Entity<AuditLogEntry>()
			.Property(a => a.ResultStatus)
			.HasConversion<string>();

		// ── Indexes ─────────────────────────────────────────────────
		modelBuilder.Entity<ApplicationUser>().HasIndex(u => u.DepartmentName);
		modelBuilder.Entity<SyllabusDocument>().HasIndex(s => s.DepartmentName);
		modelBuilder.Entity<SyllabusDocument>().HasIndex(s => s.Status);
		modelBuilder.Entity<SyllabusAssignment>().HasIndex(a => new { a.StudentId, a.IsActive });
		modelBuilder.Entity<SyllabusAssignment>().HasIndex(a => a.SyllabusDocId);

		// ── Foreign Keys ────────────────────────────────────────────
		
		// SyllabusAssignment relationships
		modelBuilder.Entity<SyllabusAssignment>()
			.HasOne<ApplicationUser>()
			.WithMany()
			.HasForeignKey(a => a.StudentId)
			.OnDelete(DeleteBehavior.Cascade);
			
		modelBuilder.Entity<SyllabusAssignment>()
			.HasOne<ApplicationUser>()
			.WithMany()
			.HasForeignKey(a => a.AssignedBy)
			.OnDelete(DeleteBehavior.Restrict);

		modelBuilder.Entity<SyllabusAssignment>()
			.HasOne<SyllabusDocument>()
			.WithMany()
			.HasForeignKey(a => a.SyllabusDocId)
			.OnDelete(DeleteBehavior.Cascade);

		// SyllabusDocument relationships
		modelBuilder.Entity<SyllabusDocument>()
			.HasOne<ApplicationUser>()
			.WithMany()
			.HasForeignKey(s => s.OwnerUserId)
			.OnDelete(DeleteBehavior.Restrict);

		modelBuilder.Entity<SyllabusDocument>()
			.HasOne<ApplicationUser>()
			.WithMany()
			.HasForeignKey(s => s.ReviewedByUserId)
			.OnDelete(DeleteBehavior.SetNull);

		// AuditLogEntry relationships
		modelBuilder.Entity<AuditLogEntry>()
			.HasOne<ApplicationUser>()
			.WithMany()
			.HasForeignKey(a => a.UserId)
			.OnDelete(DeleteBehavior.SetNull);
	}

	private static long ParseLongSafe(string? value)
	{
		if (string.IsNullOrWhiteSpace(value)) return 0L;
		return long.TryParse(value, out var result) ? result : 0L;
	}

	private static long? ParseNullableLongSafe(string? value)
	{
		if (string.IsNullOrWhiteSpace(value)) return null;
		return long.TryParse(value, out var result) ? result : null;
	}
}
