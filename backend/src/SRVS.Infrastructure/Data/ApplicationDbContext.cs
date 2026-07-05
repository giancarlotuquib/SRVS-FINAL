using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SRVS.Domain.Entities;
using SRVS.Domain.Enums;
namespace SRVS.Web.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
	public DbSet<SyllabusDocument> SyllabusDocuments => Set<SyllabusDocument>();

	public DbSet<SyllabusVersion> SyllabusVersions => Set<SyllabusVersion>();

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
		modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserPasskey<string>").ToTable("AspNetUserPasskeys", "identity");

		modelBuilder.Entity<SyllabusDocument>().ToTable("syllabi");
		modelBuilder.Entity<SyllabusVersion>().ToTable("syllabus_versions");
		modelBuilder.Entity<SyllabusAssignment>().ToTable("syllabus_assignments");
		modelBuilder.Entity<AuditLogEntry>().ToTable("audit_logs");

		// ── Remove hashed / normalized Identity columns ─────────────
		modelBuilder.Entity<ApplicationUser>().Ignore(u => u.SecurityStamp);
		modelBuilder.Entity<ApplicationUser>().Ignore(u => u.ConcurrencyStamp);
		modelBuilder.Entity<ApplicationUser>().Ignore(u => u.NormalizedUserName);
		modelBuilder.Entity<ApplicationUser>().Ignore(u => u.NormalizedEmail);

		modelBuilder.Entity<IdentityRole>().Ignore(r => r.ConcurrencyStamp);
		modelBuilder.Entity<IdentityRole>().Ignore(r => r.NormalizedName);

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

		modelBuilder.Entity<SyllabusVersion>()
			.Property(v => v.StatusSnapshot)
			.HasConversion<string>();

		modelBuilder.Entity<AuditLogEntry>()
			.Property(a => a.ActionType)
			.HasConversion<string>();

		modelBuilder.Entity<AuditLogEntry>()
			.Property(a => a.ResultStatus)
			.HasConversion<string>();

		// ── Indexes ─────────────────────────────────────────────────
		modelBuilder.Entity<SyllabusDocument>().HasIndex(s => s.Status);
		modelBuilder.Entity<SyllabusAssignment>().HasIndex(a => new { a.StudentId, a.IsActive });
		modelBuilder.Entity<SyllabusAssignment>().HasIndex(a => a.SyllabusDocId);
	}
}
