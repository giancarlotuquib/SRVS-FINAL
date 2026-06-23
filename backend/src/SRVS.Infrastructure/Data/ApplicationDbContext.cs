using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SRVS.Domain.Entities;
namespace SRVS.Web.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
	public DbSet<SyllabusDocument> SyllabusDocuments => Set<SyllabusDocument>();

	public DbSet<SyllabusVersion> SyllabusVersions => Set<SyllabusVersion>();

	public DbSet<SyllabusAssignment> SyllabusAssignments => Set<SyllabusAssignment>();
	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		modelBuilder.Entity<ApplicationUser>().ToTable("users");
		modelBuilder.Entity<IdentityRole>().ToTable("roles", "identity");
		modelBuilder.Entity<IdentityUserRole<string>>().ToTable("user_roles", "identity");
		modelBuilder.Entity<IdentityUserClaim<string>>().ToTable("user_claims", "identity");
		modelBuilder.Entity<IdentityUserLogin<string>>().ToTable("user_logins", "identity");
		modelBuilder.Entity<IdentityRoleClaim<string>>().ToTable("role_claims", "identity");
		modelBuilder.Entity<IdentityUserToken<string>>().ToTable("user_tokens", "identity");

		modelBuilder.Entity<SyllabusDocument>().ToTable("syllabi");
		modelBuilder.Entity<SyllabusVersion>().ToTable("syllabus_versions");
		modelBuilder.Entity<SyllabusAssignment>().ToTable("syllabus_assignments");

		modelBuilder.Entity<SyllabusDocument>().HasIndex(s => s.Status);
		modelBuilder.Entity<SyllabusAssignment>().HasIndex(a => new { a.StudentId, a.IsActive });
		modelBuilder.Entity<SyllabusAssignment>().HasIndex(a => a.SyllabusId);
	}
}
