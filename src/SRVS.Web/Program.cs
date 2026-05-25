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
using SRVS.Web.Components.Admin.Services;
using SRVS.Web.Data;
using SRVS.Infrastructure.Services;
using SRVS.Application.Services;
using SRVS.Web.Components.Admin.Models;
using SRVS.Web.Endpoints;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "SRVS API", Version = "v1" });
    c.DocInclusionPredicate((docName, apiDesc) =>
        apiDesc.RelativePath != null &&
        apiDesc.RelativePath.StartsWith("api/"));
});

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
builder.Services.AddScoped<UserActionService>();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
        // Relax password requirements for seed admin
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 6;
        options.Password.RequireDigit = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, ApplicationUserClaimsPrincipalFactory>();

builder.Services.AddScoped(sp => 
{
    var navManager = sp.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>();
    return new HttpClient { BaseAddress = new Uri(navManager.BaseUri) };
});

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

app.Use(async (context, next) =>
{
    if (context.Request.Path == "/")
    {
        context.Response.Redirect("/Account/Login");
        return;
    }

    await next();
});



app.UseStaticFiles();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapHealthEndpoints();
app.MapAuthEndpoints();
app.MapAdminEndpoints();
app.MapSyllabusEndpoints();
app.MapDeptHeadEndpoints();
app.MapCoursesEndpoints();
app.MapFacultyEndpoints();
app.MapStudentEndpoints();
// app.MapProgramEndpoints();
// app.MapDepartmentEndpoints();
// app.MapUserEndpoints();
// app.MapAcademicYearEndpoints();
app.MapReportEndpoints();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

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
    var adminPassword = app.Configuration["SeedAdmin:Password"] ?? "admin123";

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


