using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SRVS.Domain.Entities;

namespace SRVS.Web.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
	public DbSet<Department> Departments => Set<Department>();

	public DbSet<CourseAssignment> CourseAssignments => Set<CourseAssignment>();


	public DbSet<RegistrationRequest> RegistrationRequests => Set<RegistrationRequest>();

	public DbSet<SyllabusDocument> SyllabusDocuments => Set<SyllabusDocument>();

	public DbSet<SyllabusVersion> SyllabusVersions => Set<SyllabusVersion>();

	public DbSet<SyllabusAssignment> SyllabusAssignments => Set<SyllabusAssignment>();


	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		// Add index to speed up DeptHead lookups for syllabi by DepartmentId + Status
		modelBuilder.Entity<SyllabusDocument>().HasIndex(s => new { s.DepartmentId, s.Status });

		modelBuilder.Entity<SyllabusAssignment>(entity =>
		{
			entity.Property(item => item.StudentId).HasMaxLength(450);
			entity.Property(item => item.AssignedBy).HasMaxLength(450);
			entity.HasIndex(item => new { item.StudentId, item.IsActive });
			entity.HasIndex(item => item.SyllabusId);
			entity.HasIndex(item => item.AssignedBy);
			entity.HasOne<ApplicationUser>()
				.WithMany()
				.HasForeignKey(item => item.StudentId)
				.OnDelete(DeleteBehavior.Cascade);
			entity.HasOne<ApplicationUser>()
				.WithMany()
				.HasForeignKey(item => item.AssignedBy)
				.OnDelete(DeleteBehavior.NoAction);
			entity.HasOne<SyllabusDocument>()
				.WithMany()
				.HasForeignKey(item => item.SyllabusId)
				.OnDelete(DeleteBehavior.Cascade);
		});
	}
}
