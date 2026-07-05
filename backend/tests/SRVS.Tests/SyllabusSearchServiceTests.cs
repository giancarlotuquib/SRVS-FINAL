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

        var results = await service.SearchAsync(new SyllabusSearchRequest(), UserRoleType.Student, null, "viewer-user");

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

        var results = await service.SearchAsync(new SyllabusSearchRequest(), UserRoleType.Educator, Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "educator-1");

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
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            "educator-1");

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
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            "educator-1");

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
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            "dept-head-1");

        Assert.NotNull(document);
        Assert.Equal("CE103", document.CourseCode);
    }

    [Fact]
    public async Task GetAccessibleDocumentAsync_DepartmentHeadCanOpenApprovedSyllabusFromAnyDepartment()
    {
        await using var factory = await CreateFactoryAsync();
        await using var context = await factory.CreateDbContextAsync();
        SeedData(context);

        ISyllabusSearchService service = new SyllabusSearchService(factory);

        var document = await service.GetAccessibleDocumentAsync(
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            UserRoleType.DepartmentHead,
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            "dept-head-1");

        Assert.NotNull(document);
        Assert.Equal("BUS200", document.CourseCode);
    }

    [Fact]
    public async Task SearchAsync_DepartmentHeadApprovedFilterShowsRepositoryAcrossDepartments()
    {
        await using var factory = await CreateFactoryAsync();
        await using var context = await factory.CreateDbContextAsync();
        SeedData(context);

        ISyllabusSearchService service = new SyllabusSearchService(factory);

        var results = await service.SearchAsync(
            new SyllabusSearchRequest(Status: SyllabusStatus.Approved),
            UserRoleType.DepartmentHead,
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            "dept-head-1");

        Assert.Equal(2, results.Items.Count);
        Assert.Contains(results.Items, item => item.CourseCode == "CE101" && item.CanDownload);
        Assert.Contains(results.Items, item => item.CourseCode == "BUS200" && item.CanDownload);
    }

    [Fact]
    public async Task SearchAsync_DepartmentHeadSubmittedFilterShowsReviewQueue()
    {
        await using var factory = await CreateFactoryAsync();
        await using var context = await factory.CreateDbContextAsync();
        SeedData(context);

        var submittedDocument = await context.SyllabusDocuments.SingleAsync(document => document.CourseCode == "CE102");
        submittedDocument.Status = SyllabusStatus.Submitted;
        submittedDocument.SubmittedAtUtc = DateTimeOffset.UtcNow;
        var otherDepartmentSubmittedDocument = await context.SyllabusDocuments.SingleAsync(document => document.CourseCode == "BUS200");
        otherDepartmentSubmittedDocument.Status = SyllabusStatus.Submitted;
        otherDepartmentSubmittedDocument.IsPublished = false;
        otherDepartmentSubmittedDocument.SubmittedAtUtc = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync();

        ISyllabusSearchService service = new SyllabusSearchService(factory);

        var results = await service.SearchAsync(
            new SyllabusSearchRequest(Status: SyllabusStatus.Submitted),
            UserRoleType.DepartmentHead,
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            "dept-head-1");

        Assert.Equal(2, results.Items.Count);
        Assert.Contains(results.Items, item => item.CourseCode == "CE102");
        Assert.Contains(results.Items, item => item.CourseCode == "BUS200");
    }

    [Fact]
    public async Task GetAccessibleDocumentAsync_DepartmentHeadCanOpenSubmittedSyllabusFromAnyDepartment()
    {
        await using var factory = await CreateFactoryAsync();
        await using var context = await factory.CreateDbContextAsync();
        SeedData(context);

        var otherDepartmentSubmittedDocument = await context.SyllabusDocuments.SingleAsync(document => document.CourseCode == "BUS200");
        otherDepartmentSubmittedDocument.Status = SyllabusStatus.Submitted;
        otherDepartmentSubmittedDocument.IsPublished = false;
        otherDepartmentSubmittedDocument.SubmittedAtUtc = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync();

        ISyllabusSearchService service = new SyllabusSearchService(factory);

        var document = await service.GetAccessibleDocumentAsync(
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            UserRoleType.DepartmentHead,
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            "dept-head-1");

        Assert.NotNull(document);
        Assert.Equal("BUS200", document.CourseCode);
    }

    [Fact]
    public void SyllabusAccessPolicy_EducatorCannotReviewSubmittedSyllabus()
    {
        var document = new SyllabusDocument
        {
            DepartmentId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Status = SyllabusStatus.Submitted,
            OwnerUserId = "educator-1"
        };

        Assert.False(SyllabusAccessPolicy.CanReview(document, UserRoleType.Educator, document.DepartmentId));
    }

    [Fact]
    public void SyllabusAccessPolicy_DepartmentHeadCanReviewSubmittedSyllabusFromAnyDepartment()
    {
        var document = new SyllabusDocument
        {
            DepartmentId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Status = SyllabusStatus.Submitted,
            OwnerUserId = "educator-1"
        };

        Assert.True(SyllabusAccessPolicy.CanReview(document, UserRoleType.DepartmentHead, Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")));
    }

    [Fact]
    public async Task GetAccessibleDocumentAsync_RejectsViewerAccessToDraft()
    {
        await using var factory = await CreateFactoryAsync();
        await using var context = await factory.CreateDbContextAsync();
        SeedData(context);

        ISyllabusSearchService service = new SyllabusSearchService(factory);

        var document = await service.GetAccessibleDocumentAsync(Guid.Parse("22222222-2222-2222-2222-222222222222"), UserRoleType.Student, null, "viewer-user");

        Assert.Null(document);
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
            },
            new SyllabusDocument
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                DepartmentId = department.Id,
                CourseCode = "CE103",
                CourseTitle = "Signals and Systems",
                AcademicYear = "2025-2026",
                Semester = "1st Semester",
                InstructorName = "Prof. Cruz",
                OwnerUserId = "educator-3",
                Status = SyllabusStatus.Draft,
                IsPublished = false,
                CurrentVersionNumber = 1,
                CurrentFileName = "CE103-V1.pdf",
                CurrentStoragePath = "C:\\temp\\CE103-V1.pdf"
            });

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
