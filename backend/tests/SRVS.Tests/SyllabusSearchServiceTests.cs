using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SRVS.Application.Abstractions;
using SRVS.Application.Models;
using SRVS.Application.Services;
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
        await using var factory = await CreateFactoryAsync();
        await using var context = await factory.CreateDbContextAsync();
        SeedData(context);

        ISyllabusSearchService service = new SyllabusSearchService(factory);

        var results = await service.SearchAsync(new SyllabusSearchRequest(), UserRoleType.Student, "10004");

        Assert.Single(results.Items);
        Assert.Contains(results.Items, item => item.CourseCode == "CE101" && item.CanDownload);
    }

    [Fact]
    public async Task SearchAsync_EducatorOnlySeesOwnedDocuments()
    {
        await using var factory = await CreateFactoryAsync();
        await using var context = await factory.CreateDbContextAsync();
        SeedData(context);

        ISyllabusSearchService service = new SyllabusSearchService(factory);

        var results = await service.SearchAsync(new SyllabusSearchRequest(), UserRoleType.Educator, "10001");

        Assert.Equal(2, results.TotalCount);
        Assert.Contains(results.Items, item => item.CourseCode == "CE101");
        Assert.Contains(results.Items, item => item.CourseCode == "CE102");
        Assert.DoesNotContain(results.Items, item => item.CourseCode == "CE103");
    }

    [Fact]
    public async Task SearchAsync_EducatorDraftFilterShowsDraftsForSubmission()
    {
        await using var factory = await CreateFactoryAsync();
        await using var context = await factory.CreateDbContextAsync();
        SeedData(context);

        ISyllabusSearchService service = new SyllabusSearchService(factory);

        var results = await service.SearchAsync(
            new SyllabusSearchRequest(Status: SyllabusStatus.Draft),
            UserRoleType.Educator,
            "10001");

        Assert.Single(results.Items);
        Assert.Contains(results.Items, item => item.CourseCode == "CE102");
        Assert.DoesNotContain(results.Items, item => item.CourseCode == "CE103");
    }

    [Fact]
    public async Task GetAccessibleDocumentAsync_EducatorCannotOpenAnotherFacultyDraft()
    {
        await using var factory = await CreateFactoryAsync();
        await using var context = await factory.CreateDbContextAsync();
        SeedData(context);

        ISyllabusSearchService service = new SyllabusSearchService(factory);

        var document = await service.GetAccessibleDocumentAsync(
            Guid.Parse("44444444-4444-4444-4444-444444444444"),
            UserRoleType.Educator,
            "10001");

        Assert.Null(document);
    }

    [Fact]
    public async Task GetAccessibleDocumentAsync_DepartmentHeadCanOpenSameDepartmentFacultyDraft()
    {
        await using var factory = await CreateFactoryAsync();
        await using var context = await factory.CreateDbContextAsync();
        SeedData(context);

        ISyllabusSearchService service = new SyllabusSearchService(factory);

        var document = await service.GetAccessibleDocumentAsync(
            Guid.Parse("44444444-4444-4444-4444-444444444444"),
            UserRoleType.DepartmentHead,
            "10005");

        Assert.NotNull(document);
        Assert.Equal("CE103", document.CourseCode);
    }

    [Fact]
    public void SyllabusAccessPolicy_EducatorCannotReviewSubmittedSyllabus()
    {
        var document = new SyllabusDocument
        {
            DepartmentName = "Civil Engineering",
            Status = SyllabusStatus.Submitted,
            OwnerUserId = "10001"
        };

        Assert.False(SyllabusAccessPolicy.CanReview(document, UserRoleType.Educator, "Civil Engineering"));
    }

    [Fact]
    public void SyllabusAccessPolicy_DepartmentHeadCanOnlyReviewSubmittedSyllabusFromOwnDepartment()
    {
        var document = new SyllabusDocument
        {
            DepartmentName = "Civil Engineering",
            Status = SyllabusStatus.Submitted,
            OwnerUserId = "10001"
        };

        Assert.True(SyllabusAccessPolicy.CanReview(document, UserRoleType.DepartmentHead, "Civil Engineering"));
        Assert.False(SyllabusAccessPolicy.CanReview(document, UserRoleType.DepartmentHead, "Computer Engineering"));
    }

    private static async Task<TestDbContextFactory> CreateFactoryAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();

        return new TestDbContextFactory(options, connection);
    }

    private static void SeedData(ApplicationDbContext context)
    {
        context.Users.AddRange(
            new ApplicationUser { Id = "10001", UserName = "ed1@test.com", Email = "ed1@test.com", DepartmentName = "Computer Engineering", FirstName = "Educator", LastName = "One", FullName = "Educator One" },
            new ApplicationUser { Id = "10002", UserName = "ed2@test.com", Email = "ed2@test.com", DepartmentName = "Civil Engineering", FirstName = "Educator", LastName = "Two", FullName = "Educator Two" },
            new ApplicationUser { Id = "10003", UserName = "ed3@test.com", Email = "ed3@test.com", DepartmentName = "Computer Engineering", FirstName = "Educator", LastName = "Three", FullName = "Educator Three" },
            new ApplicationUser { Id = "10004", UserName = "view@test.com", Email = "view@test.com", DepartmentName = "Computer Engineering", FirstName = "Viewer", LastName = "User", FullName = "Viewer User" },
            new ApplicationUser { Id = "10005", UserName = "head@test.com", Email = "head@test.com", DepartmentName = "Computer Engineering", FirstName = "Dept", LastName = "Head", FullName = "Dept Head" }
        );

        context.SyllabusDocuments.AddRange(
            new SyllabusDocument
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                DepartmentName = "Computer Engineering",
                CourseCode = "CE101",
                CourseTitle = "Introduction to Computer Engineering",
                AcademicYear = "2025-2026",
                Semester = "1st Semester",
                InstructorId = "10001",
                OwnerUserId = "10001",
                Status = SyllabusStatus.Approved,
                IsPublished = true,
                CurrentVersionNumber = 2,
                CurrentFileName = "CE101-V2.pdf",
                CurrentStoragePath = "C:\\temp\\CE101-V2.pdf"
            },
            new SyllabusDocument
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                DepartmentName = "Computer Engineering",
                CourseCode = "CE102",
                CourseTitle = "Digital Logic",
                AcademicYear = "2025-2026",
                Semester = "2nd Semester",
                InstructorId = "10001",
                OwnerUserId = "10001",
                Status = SyllabusStatus.Draft,
                IsPublished = false,
                CurrentVersionNumber = 1,
                CurrentFileName = "CE102-V1.pdf",
                CurrentStoragePath = "C:\\temp\\CE102-V1.pdf"
            },
            new SyllabusDocument
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                DepartmentName = "Civil Engineering",
                CourseCode = "BUS200",
                CourseTitle = "Business Law",
                AcademicYear = "2025-2026",
                Semester = "1st Semester",
                InstructorId = "10002",
                OwnerUserId = "10002",
                Status = SyllabusStatus.Approved,
                IsPublished = true,
                CurrentVersionNumber = 1,
                CurrentFileName = "BUS200-V1.pdf",
                CurrentStoragePath = "C:\\temp\\BUS200-V1.pdf"
            },
            new SyllabusDocument
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                DepartmentName = "Computer Engineering",
                CourseCode = "CE103",
                CourseTitle = "Signals and Systems",
                AcademicYear = "2025-2026",
                Semester = "1st Semester",
                InstructorId = "10003",
                OwnerUserId = "10003",
                Status = SyllabusStatus.Draft,
                IsPublished = false,
                CurrentVersionNumber = 1,
                CurrentFileName = "CE103-V1.pdf",
                CurrentStoragePath = "C:\\temp\\CE103-V1.pdf"
            });

        context.SyllabusAssignments.Add(
            new SyllabusAssignment
            {
                StudentId = "10004",
                SyllabusDocId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                AssignedBy = "10005",
                AssignedAtDate = DateTimeOffset.UtcNow,
                IsActive = true
            }
        );

        context.SaveChanges();
    }

    private sealed class TestDbContextFactory : IDbContextFactory<ApplicationDbContext>, IAsyncDisposable
    {
        private readonly DbContextOptions<ApplicationDbContext> options;
        private readonly SqliteConnection connection;

        public TestDbContextFactory(DbContextOptions<ApplicationDbContext> options, SqliteConnection connection)
        {
            this.options = options;
            this.connection = connection;
        }

        public ApplicationDbContext CreateDbContext()
        {
            return new ApplicationDbContext(options);
        }

        public ValueTask<ApplicationDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(CreateDbContext());
        }

        public ValueTask DisposeAsync()
        {
            return connection.DisposeAsync();
        }
    }
}
