using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SRVS.Domain.Entities;

namespace SRVS.Web.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
	public DbSet<Department> Departments => Set<Department>();

	public DbSet<CourseAssignment> CourseAssignments => Set<CourseAssignment>();

	public DbSet<UserDepartment> UserDepartments => Set<UserDepartment>();

	public DbSet<RegistrationRequest> RegistrationRequests => Set<RegistrationRequest>();

	public DbSet<SyllabusDocument> SyllabusDocuments => Set<SyllabusDocument>();

	public DbSet<SyllabusVersion> SyllabusVersions => Set<SyllabusVersion>();

	public DbSet<SyllabusAssignment> SyllabusAssignments => Set<SyllabusAssignment>();

	public DbSet<AuditLogEntry> AuditLogEntries => Set<AuditLogEntry>();

	public DbSet<NotificationEntry> NotificationEntries => Set<NotificationEntry>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		// Add index to speed up DeptHead lookups for syllabi by DepartmentId + Status
		modelBuilder.Entity<SyllabusDocument>().HasIndex(s => new { s.DepartmentId, s.Status });
	}
}