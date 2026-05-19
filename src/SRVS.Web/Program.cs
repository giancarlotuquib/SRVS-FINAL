using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.StaticWebAssets;
using Microsoft.EntityFrameworkCore;
using SRVS.Application.Abstractions;
using SRVS.Application.Models;
using SRVS.Domain.Entities;
using SRVS.Domain.Enums;
using SRVS.Web.Components;
using SRVS.Web.Components.Account;
using SRVS.Web.Data;
using SRVS.Infrastructure.Services;
using SRVS.Application.Services;
using SRVS.Web.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddScoped<ApplicationDbContext>(p => p.GetRequiredService<IDbContextFactory<ApplicationDbContext>>().CreateDbContext());
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddScoped<ISyllabusFileStorage, LocalSyllabusFileStorage>();
builder.Services.AddScoped<ISyllabusWorkflowService, SyllabusWorkflowService>();
builder.Services.AddScoped<IRegistrationApprovalService, RegistrationApprovalService>();
builder.Services.AddScoped<ISyllabusSearchService, SyllabusSearchService>();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, ApplicationUserClaimsPrincipalFactory>();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

var app = builder.Build();

await SeedSrvsDataAsync(app);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();



app.UseStaticFiles();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapGet("/syllabi/{syllabusDocumentId:guid}/download", async (
    Guid syllabusDocumentId,
    HttpContext httpContext,
    ISyllabusSearchService syllabusSearchService,
    ISyllabusFileStorage syllabusFileStorage,
    UserManager<ApplicationUser> userManager,
    CancellationToken cancellationToken) =>
{
    var user = await userManager.GetUserAsync(httpContext.User);
    if (user is null)
    {
        return Results.Unauthorized();
    }

    var document = await syllabusSearchService.GetAccessibleDocumentAsync(syllabusDocumentId, user.Role, user.DepartmentId, user.Id, cancellationToken);
    if (document is null)
    {
        return Results.NotFound();
    }

    if (!await syllabusFileStorage.ExistsAsync(document.CurrentStoragePath, cancellationToken))
    {
        return Results.NotFound();
    }

    await using var stream = await syllabusFileStorage.OpenReadAsync(document.CurrentStoragePath, cancellationToken);
    var contentType = Path.GetExtension(document.CurrentFileName).ToLowerInvariant() switch
    {
        ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        ".pdf" => "application/pdf",
        _ => "application/octet-stream"
    };

    return Results.File(stream, contentType, document.CurrentFileName);
}).RequireAuthorization();

app.MapGet("/syllabi/versions/{versionId:guid}/download", async (
    Guid versionId,
    HttpContext httpContext,
    ApplicationDbContext dbContext,
    ISyllabusFileStorage syllabusFileStorage,
    UserManager<ApplicationUser> userManager,
    CancellationToken cancellationToken) =>
{
    var user = await userManager.GetUserAsync(httpContext.User);
    if (user is null) return Results.Unauthorized();

    var version = await dbContext.SyllabusVersions
        .Include(v => v.SyllabusDocument)
        .FirstOrDefaultAsync(v => v.Id == versionId, cancellationToken);
        
    if (version is null || version.SyllabusDocument is null) return Results.NotFound();

    // Basic permission check - admin, dept head of same dept, or owner
    var hasAccess = SyllabusAccessPolicy.CanDownload(version.SyllabusDocument, user.Role, user.DepartmentId, user.Id);

    if (!hasAccess) return Results.Forbid();

    if (!await syllabusFileStorage.ExistsAsync(version.StoragePath, cancellationToken))
    {
        return Results.NotFound();
    }

    var stream = await syllabusFileStorage.OpenReadAsync(version.StoragePath, cancellationToken);
    var contentType = Path.GetExtension(version.FileName).ToLowerInvariant() switch
    {
        ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        ".pdf" => "application/pdf",
        _ => "application/octet-stream"
    };

    return Results.File(stream, contentType, version.FileName);
}).RequireAuthorization();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

var api = app.MapGroup("/api");

api.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    timestampUtc = DateTimeOffset.UtcNow
}))
.AllowAnonymous()
.WithName("GetHealth");

api.MapGet("/syllabi/search", async (
    string? term,
    int maxResults,
    HttpContext httpContext,
    UserManager<ApplicationUser> userManager,
    ISyllabusSearchService syllabusSearchService,
    CancellationToken cancellationToken) =>
{
    var user = await userManager.GetUserAsync(httpContext.User);
    if (user is null)
    {
        return Results.Unauthorized();
    }

    var results = await syllabusSearchService.SearchAsync(new SyllabusSearchRequest(term, null, maxResults <= 0 ? 100 : maxResults), user.Role, user.DepartmentId, user.Id, cancellationToken);
    return Results.Ok(results);
})
.RequireAuthorization()
.WithName("SearchSyllabi");

api.MapGet("/registrations", async (
    string? search,
    IRegistrationApprovalService registrationApprovalService,
    CancellationToken cancellationToken) =>
{
    var queue = await registrationApprovalService.GetQueueAsync(search, cancellationToken);
    return Results.Ok(queue);
})
.RequireAuthorization()
.WithName("GetRegistrationQueue");

api.MapPost("/registrations/{registrationRequestId:guid}/approve", async (
    Guid registrationRequestId,
    HttpContext httpContext,
    UserManager<ApplicationUser> userManager,
    IRegistrationApprovalService registrationApprovalService,
    CancellationToken cancellationToken) =>
{
    var user = await userManager.GetUserAsync(httpContext.User);
    if (user is null)
    {
        return Results.Unauthorized();
    }

    await registrationApprovalService.ApproveAsync(registrationRequestId, user.Id, user.FullName, cancellationToken);
    return Results.NoContent();
})
.RequireAuthorization()
.WithName("ApproveRegistration");

api.MapPost("/registrations/{registrationRequestId:guid}/reject", async (
    Guid registrationRequestId,
    RejectRegistrationRequest request,
    HttpContext httpContext,
    UserManager<ApplicationUser> userManager,
    IRegistrationApprovalService registrationApprovalService,
    CancellationToken cancellationToken) =>
{
    var user = await userManager.GetUserAsync(httpContext.User);
    if (user is null)
    {
        return Results.Unauthorized();
    }

    await registrationApprovalService.RejectAsync(registrationRequestId, user.Id, user.FullName, request.ReviewRemarks ?? string.Empty, cancellationToken);
    return Results.NoContent();
})
.RequireAuthorization()
.WithName("RejectRegistration");

app.Run();

static async Task SeedSrvsDataAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;

    var dbContext = services.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.EnsureCreatedAsync();

    if (!await dbContext.Departments.AnyAsync())
    {
        // Create only Computer Engineering department
        var ceDept = new Department 
        { 
            Code = "CE", 
            Name = "Computer Engineering",
            IsActive = true
        };
        dbContext.Departments.Add(ceDept);
        await dbContext.SaveChangesAsync();

        // Add sample Computer Engineering courses
        var courses = new[]
        {
            new CourseAssignment 
            { 
                DepartmentId = ceDept.Id, 
                CourseCode = "CE101", 
                CourseTitle = "Introduction to Computer Engineering",
                InstructorName = "Faculty Member",
                IsActive = true
            },
            new CourseAssignment 
            { 
                DepartmentId = ceDept.Id, 
                CourseCode = "CE201", 
                CourseTitle = "Digital Logic Design",
                InstructorName = "Faculty Member",
                IsActive = true
            },
            new CourseAssignment 
            { 
                DepartmentId = ceDept.Id, 
                CourseCode = "CE301", 
                CourseTitle = "Computer Architecture",
                InstructorName = "Faculty Member",
                IsActive = true
            },
            new CourseAssignment 
            { 
                DepartmentId = ceDept.Id, 
                CourseCode = "CE401", 
                CourseTitle = "Senior Design Project",
                InstructorName = "Faculty Member",
                IsActive = true
            }
        };
        dbContext.CourseAssignments.AddRange(courses);
        await dbContext.SaveChangesAsync();
    }

    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    var adminEmail = app.Configuration["SeedAdmin:Email"] ?? "admin@srvs.local";
    var adminPassword = app.Configuration["SeedAdmin:Password"] ?? "Admin123!";

    var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
    if (existingAdmin is null)
    {
        var admin = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FullName = "System Administrator",
            InstitutionalId = "00000",
            Role = UserRoleType.Admin,
            AccountStatus = UserAccountStatus.Active,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(admin, adminPassword);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Unable to seed SRVS admin account: {string.Join(", ", result.Errors.Select(error => error.Description))}");
        }
    }

    var deptHeadEmail = "depthead@srvs.local";
    if (await userManager.FindByEmailAsync(deptHeadEmail) is null)
    {
        var dept = await dbContext.Departments.FirstOrDefaultAsync(d => d.Code == "CE") 
                   ?? await dbContext.Departments.FirstOrDefaultAsync();
        if (dept is not null)
        {
            var user = new ApplicationUser
            {
                UserName = deptHeadEmail,
                Email = deptHeadEmail,
                FullName = "Department Head User",
                InstitutionalId = "11111",
                Role = UserRoleType.DepartmentHead,
                DepartmentId = dept.Id,
                AccountStatus = UserAccountStatus.Active,
                EmailConfirmed = true
            };
            await userManager.CreateAsync(user, adminPassword);
        }
    }

    var educatorEmail = "educator@srvs.local";
    if (await userManager.FindByEmailAsync(educatorEmail) is null)
    {
        var dept = await dbContext.Departments.FirstOrDefaultAsync(d => d.Code == "CE") 
                   ?? await dbContext.Departments.FirstOrDefaultAsync();
        if (dept is not null)
        {
            var user = new ApplicationUser
            {
                UserName = educatorEmail,
                Email = educatorEmail,
                FullName = "Educator User",
                InstitutionalId = "22222",
                Role = UserRoleType.Educator,
                DepartmentId = dept.Id,
                AccountStatus = UserAccountStatus.Active,
                EmailConfirmed = true
            };
            await userManager.CreateAsync(user, adminPassword);
        }
    }
}
