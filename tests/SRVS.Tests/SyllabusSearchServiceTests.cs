using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SRVS.Application.Abstractions;
using SRVS.Application.Models;
using SRVS.Domain.Entities;
using SRVS.Domain.Enums;
using SRVS.Infrastructure.Services;
using SRVS.Web.Data;

namespace SRVS.Tests;

public class SyllabusSearchServiceTests
{
    [Fact]
    public async Task SearchAsync_ViewerOnlySeesApprovedPublishedDocuments()
    {
        await using var context = await CreateContextAsync();
        SeedData(context);

        ISyllabusSearchService service = new SyllabusSearchService(context);

        var results = await service.SearchAsync(new SyllabusSearchRequest(), UserRoleType.Viewer, null, "viewer-user");

        Assert.Equal(1, results.Items.Count);
        Assert.Contains(results.Items, item => item.CourseCode == "CE101" && item.CanDownload);
    }

    [Fact]
    public async Task SearchAsync_EducatorSeesDepartmentAndOwnedDocuments()
    {
        await using var context = await CreateContextAsync();
        SeedData(context);

        ISyllabusSearchService service = new SyllabusSearchService(context);

        var results = await service.SearchAsync(new SyllabusSearchRequest(), UserRoleType.Educator, Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "educator-1");

        Assert.Equal(2, results.TotalCount);
        Assert.Contains(results.Items, item => item.CourseCode == "CE101");
        Assert.Contains(results.Items, item => item.CourseCode == "CE102");
    }

    [Fact]
    public async Task GetAccessibleDocumentAsync_RejectsViewerAccessToDraft()
    {
        await using var context = await CreateContextAsync();
        SeedData(context);

        ISyllabusSearchService service = new SyllabusSearchService(context);

        var document = await service.GetAccessibleDocumentAsync(Guid.Parse("22222222-2222-2222-2222-222222222222"), UserRoleType.Viewer, null, "viewer-user");

        Assert.Null(document);
    }

    private static async Task<ApplicationDbContext> CreateContextAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    private static void SeedData(ApplicationDbContext context)
    {
        var department = new Department { Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), Code = "CE", Name = "Computer Engineering" };
        var otherDepartment = new Department { Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), Code = "BUS", Name = "Business" };

        context.Departments.AddRange(department, otherDepartment);
        context.SyllabusDocuments.AddRange(
            new SyllabusDocument
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                DepartmentId = department.Id,
                CourseCode = "CE101",
                CourseTitle = "Introduction to Computer Engineering",
                AcademicYear = "2025-2026",
                Semester = "1st Semester",
                InstructorName = "Dr. Rivera",
                OwnerUserId = "educator-1",
                Status = SyllabusStatus.Approved,
                IsPublished = true,
                CurrentVersionNumber = 2,
                CurrentFileName = "CE101-V2.pdf",
                CurrentStoragePath = "C:\\temp\\CE101-V2.pdf"
            },
            new SyllabusDocument
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                DepartmentId = department.Id,
                CourseCode = "CE102",
                CourseTitle = "Digital Logic",
                AcademicYear = "2025-2026",
                Semester = "2nd Semester",
                InstructorName = "Dr. Rivera",
                OwnerUserId = "educator-1",
                Status = SyllabusStatus.Draft,
                IsPublished = false,
                CurrentVersionNumber = 1,
                CurrentFileName = "CE102-V1.pdf",
                CurrentStoragePath = "C:\\temp\\CE102-V1.pdf"
            },
            new SyllabusDocument
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                DepartmentId = otherDepartment.Id,
                CourseCode = "BUS200",
                CourseTitle = "Business Law",
                AcademicYear = "2025-2026",
                Semester = "1st Semester",
                InstructorName = "Prof. Santos",
                OwnerUserId = "educator-2",
                Status = SyllabusStatus.Approved,
                IsPublished = true,
                CurrentVersionNumber = 1,
                CurrentFileName = "BUS200-V1.pdf",
                CurrentStoragePath = "C:\\temp\\BUS200-V1.pdf"
            });

        context.SaveChanges();
    }
}