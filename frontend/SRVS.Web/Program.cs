using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.StaticWebAssets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
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

// Configure Http JSON Options to serialize enums as strings in endpoints and Swagger
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "SRVS API", Version = "v1" });

    // Map enums to string schemas so Swagger shows readable values
    c.UseAllOfForInheritance();
    
    // Map each UserRoleType/UserAccountStatus/SyllabusStatus to string enums
    c.MapType<SRVS.Domain.Enums.UserRoleType>(() => new OpenApiSchema
    {
        Type = "string",
        Enum = Enum.GetNames<SRVS.Domain.Enums.UserRoleType>().Select(n => (Microsoft.OpenApi.Any.IOpenApiAny)new Microsoft.OpenApi.Any.OpenApiString(n)).ToList()
    });
    c.MapType<SRVS.Domain.Enums.UserAccountStatus>(() => new OpenApiSchema
    {
        Type = "string",
        Enum = Enum.GetNames<SRVS.Domain.Enums.UserAccountStatus>().Select(n => (Microsoft.OpenApi.Any.IOpenApiAny)new Microsoft.OpenApi.Any.OpenApiString(n)).ToList()
    });
    c.MapType<SRVS.Domain.Enums.SyllabusStatus>(() => new OpenApiSchema
    {
        Type = "string",
        Enum = Enum.GetNames<SRVS.Domain.Enums.SyllabusStatus>().Select(n => (Microsoft.OpenApi.Any.IOpenApiAny)new Microsoft.OpenApi.Any.OpenApiString(n)).ToList()
    });
    
    // Exclude reports and non-api syllabus downloads
    c.DocInclusionPredicate((docName, apiDesc) =>
        apiDesc.RelativePath != null &&
        apiDesc.RelativePath.StartsWith("api/", StringComparison.OrdinalIgnoreCase) &&
        !apiDesc.RelativePath.StartsWith("api/reports", StringComparison.OrdinalIgnoreCase));

    // Configure cookie authentication description for the UI
    c.AddSecurityDefinition("CookieAuth", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Cookie,
        Name = ".AspNetCore.Identity.Application",
        Description = "ASP.NET Core Identity Cookie Authentication. Log in via the web interface or '/api/auth/login' to authenticate your browser session."
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "CookieAuth"
                }
            },
            Array.Empty<string>()
        }
    });

    // Load XML documentation comments if file exists
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
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

// For API endpoints: return JSON 401/403 instead of cookie redirect pages
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            return context.Response.WriteAsync("{\"error\":\"Unauthorized. Please log in first.\"}");
        }
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = 403;
            context.Response.ContentType = "application/json";
            return context.Response.WriteAsync("{\"error\":\"Access denied. You do not have permission for this resource.\"}");
        }
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
{
    options.UseNpgsql(connectionString);
    if (builder.Environment.IsDevelopment())
    {
        options.ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
    }
});
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
    .AddSignInManager()
    .AddDefaultTokenProviders();

// Register our custom UserStore that queries Email/UserName directly
// (the default EF store tries to query removed NormalizedEmail/NormalizedUserName columns).
builder.Services.AddScoped<IUserStore<ApplicationUser>, ApplicationUserStore>();

// Use a pass-through normalizer since we removed NormalizedUserName / NormalizedEmail columns.
builder.Services.AddScoped<ILookupNormalizer, PassThroughLookupNormalizer>();

// Disable SecurityStamp validation since the SecurityStamp column was removed.
builder.Services.Configure<SecurityStampValidatorOptions>(o =>
    o.ValidationInterval = TimeSpan.FromDays(365 * 100));

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
// app.MapCoursesEndpoints();
// app.MapFacultyEndpoints();
// app.MapStudentEndpoints();
// app.MapProgramEndpoints();
// app.MapDepartmentEndpoints();
// app.MapUserEndpoints();
// app.MapAcademicYearEndpoints();
app.MapReportEndpoints();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.Lifetime.ApplicationStarted.Register(() =>
{
    Console.WriteLine();
    Console.WriteLine("========================================");
    Console.WriteLine("Web Application: http://localhost:5300");
    Console.WriteLine("API Swagger UI:  http://localhost:5300/swagger");
    Console.WriteLine("========================================");
    Console.WriteLine();
});

app.Run();

static async Task SeedSrvsDataAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;

    var dbContext = services.GetRequiredService<ApplicationDbContext>();

    try
    {
        await dbContext.Database.EnsureCreatedAsync();
    }
    catch
    {
        // Ignore if schema already created manually
    }

    // DIAGNOSTIC DUMP
    try
    {
        var allUsers = await dbContext.Users.ToListAsync();
        var lines = allUsers.Select(u => $"Id: {u.Id} | UserName: {u.UserName} | Email: {u.Email} | FullName: {u.FullName} | Role: {u.Role} | Status: {u.AccountStatus}");
        await System.IO.File.WriteAllLinesAsync("user_list.txt", lines);
    }
    catch (Exception dumpEx)
    {
        await System.IO.File.WriteAllTextAsync("user_list.txt", "Failed to dump: " + dumpEx.ToString());
    }

    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    var adminEmail = app.Configuration["SeedAdmin:Email"] ?? "admin@srvs.local";
    var adminPassword = app.Configuration["SeedAdmin:Password"] ?? "admin123";

    try
    {
        // 1. Clean up and migrate any existing users whose ID is currently a GUID (hash)
        var allDbUsers = await dbContext.Users.ToListAsync();
        var nextStaffId = 10005;
        var nextStudentId = 2026000003;

        foreach (var dbUser in allDbUsers)
        {
            var oldId = dbUser.Id;
            if (oldId.Contains('-') || oldId.Length == 36)
            {
                // Determine new unhashed School ID
                string newId;
                var email = dbUser.Email?.ToLowerInvariant() ?? string.Empty;

                if (email == "jane@edu.ph") newId = "10001";
                else if (email == "john@edu.ph") newId = "10002";
                else if (email == "alserge@edu.ph") newId = "10003";
                else if (email == "ralph@edu.ph") newId = "10004";
                else if (email == "giannis@edu.ph") newId = "2026000001";
                else if (email == "gian@edu.ph") newId = "2026000002";
                else
                {
                    if (dbUser.Role == UserRoleType.Student)
                    {
                        newId = nextStudentId++.ToString();
                    }
                    else
                    {
                        newId = nextStaffId++.ToString();
                    }
                }

                // Retrieve properties before deleting
                var userRole = dbUser.Role;
                var userStatus = dbUser.AccountStatus;
                var fullName = dbUser.FullName;
                var uName = dbUser.UserName;

                // Delete the GUID record
                var delRes = await userManager.DeleteAsync(dbUser);
                if (delRes.Succeeded)
                {
                    var names = fullName.Split(' ', 2);
                    var firstName = names.Length > 0 ? names[0] : string.Empty;
                    var lastName = names.Length > 1 ? names[1] : string.Empty;

                    // Create the fresh unhashed record with the new ID and "Giangwapo123?" password
                    var newUser = new ApplicationUser
                    {
                        Id = newId,
                        UserName = uName,
                        Email = email,
                        FirstName = firstName,
                        LastName = lastName,
                        FullName = fullName,
                        Role = userRole,
                        AccountStatus = userStatus,
                        EmailConfirmed = true
                    };

                    var createRes = await userManager.CreateAsync(newUser, "Giangwapo123?");
                    if (createRes.Succeeded)
                    {
                        // Update references in other tables to maintain data integrity
                        var syllabi = await dbContext.SyllabusDocuments.Where(s => s.OwnerUserId == oldId || s.ReviewedByUserId == oldId).ToListAsync();
                        foreach (var s in syllabi)
                        {
                            if (s.OwnerUserId == oldId) s.OwnerUserId = newId;
                            if (s.ReviewedByUserId == oldId) s.ReviewedByUserId = newId;
                        }

                        var assignments = await dbContext.SyllabusAssignments.Where(a => a.StudentId == oldId).ToListAsync();
                        foreach (var a in assignments)
                        {
                            a.StudentId = newId;
                        }

                        var auditLogs = await dbContext.AuditLogEntries.Where(l => l.UserId == oldId).ToListAsync();
                        foreach (var l in auditLogs)
                        {
                            l.UserId = newId;
                        }

                        await dbContext.SaveChangesAsync();
                        Console.WriteLine($"Successfully migrated user {email} from GUID ID {oldId} to School ID {newId} with password Giangwapo123?");
                    }
                    else
                    {
                        var errors = string.Join(", ", createRes.Errors.Select(e => e.Description));
                        Console.WriteLine($"Error re-creating migrated user {email}: {errors}");
                    }
                }
            }
            else if (dbUser.Id != "00000")
            {
                // If it's already an unhashed user in the database (not administrator), update their password to "Giangwapo123?"
                var hasPassword = await userManager.HasPasswordAsync(dbUser);
                if (hasPassword)
                {
                    await userManager.RemovePasswordAsync(dbUser);
                }
                var resetRes = await userManager.AddPasswordAsync(dbUser, "Giangwapo123?");
                if (!resetRes.Succeeded)
                {
                    Console.WriteLine($"Warning: failed to reset password for user {dbUser.Email}");
                }
                await userManager.UpdateAsync(dbUser);
            }
        }

        // 2. Seed/Clean up System Administrator
        var staleAdminByEmail = await userManager.FindByEmailAsync(adminEmail);
        if (staleAdminByEmail is not null && staleAdminByEmail.Id != "00000")
        {
            await userManager.DeleteAsync(staleAdminByEmail);
        }

        var staleAdminById = await userManager.FindByIdAsync("00000");
        if (staleAdminById is not null)
        {
            var hasPassword = await userManager.HasPasswordAsync(staleAdminById);
            if (hasPassword)
            {
                await userManager.RemovePasswordAsync(staleAdminById);
            }
            var resetRes = await userManager.AddPasswordAsync(staleAdminById, adminPassword);
            if (!resetRes.Succeeded)
            {
                Console.WriteLine("Warning: failed to reset System Admin password");
            }
            staleAdminById.AccountStatus = UserAccountStatus.Active;
            await userManager.UpdateAsync(staleAdminById);
        }
        else
        {
            var admin = new ApplicationUser
            {
                Id = "00000",
                UserName = adminEmail,
                Email = adminEmail,
                FirstName = "System",
                LastName = "Administrator",
                FullName = "System Administrator",
                Role = UserRoleType.Admin,
                AccountStatus = UserAccountStatus.Active,
                EmailConfirmed = true
            };
            await userManager.CreateAsync(admin, adminPassword);
        }
    }
    catch (Exception ex)
    {
        System.IO.File.WriteAllText("seeding_error.txt", ex.ToString());
        throw;
    }
}


