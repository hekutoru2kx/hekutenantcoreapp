using Hekutenantcoreapp.Application.Interfaces;
using Hekutenantcoreapp.Application.Services;
using Hekutenantcoreapp.Domain.Interfaces;
using Hekutenantcoreapp.Infrastructure.Data;
using Hekutenantcoreapp.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using Hekutenantcoreapp.Infrastructure.Email;
using Hekutenantcoreapp.Infrastructure.Repositories;
using Hekutenantcoreapp.Domain.Enums;
using Hekutenantcoreapp.Domain.Enums.Permissions;
using Hekutenantcoreapp.Domain.Catalogs;
using Hekutenantcoreapp.Domain.Entities;
using Hekutenantcoreapp.Domain.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularDev", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

//Localization
var supportedCultures = new[] { "en", "es" };
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture("en")
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);

builder.Services.AddLocalization();

// Database
builder.Services.AddDbContext<HekutenantcoreappDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<HekutenantcoreappDbContext>()
    .AddDefaultTokenProviders()
    .AddErrorDescriber<LocalizedIdentityErrorDescriber>();

//User Config
builder.Services.AddScoped<IUserService, UserService>();

//Email Service
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<EmailTemplates>();

// DI registrations
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserManagementService, UserManagementService>();
builder.Services.AddScoped<IUserManagementRepository, UserManagementRepository>();
builder.Services.AddScoped<IRoleManagementService, RoleManagementService>();
builder.Services.AddScoped<IRoleManagementRepository, RoleManagementRepository>();
builder.Services.AddScoped<IGoogleTokenValidator, GoogleTokenValidator>();

//Accessor for HttpContext to get the current user in DbContext
builder.Services.AddHttpContextAccessor();

//Geography Service and Repository
builder.Services.AddScoped<IGeographyService, GeographyService>();
builder.Services.AddScoped<IGeographyRepository, GeographyRepository>();
//Person Service and Repository
builder.Services.AddScoped<IPersonService, PersonService>();
builder.Services.AddScoped<IPersonRepository, PersonRepository>();
//Tenant Service and Repository
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<ITenantRepository, TenantRepository>();
builder.Services.AddScoped<ITenantMembershipRepository, TenantMembershipRepository>();
builder.Services.AddScoped<IUserTenantRoleRepository, UserTenantRoleRepository>();
//Employee Service and Repository
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
//Tenant Role Service and Repository (tenant-scoped role catalog)
builder.Services.AddScoped<ITenantRoleService, TenantRoleService>();
builder.Services.AddScoped<ITenantRoleRepository, TenantRoleRepository>();
//Generic content-translation repository (LocalizedText) — reusable across any entity/field
builder.Services.AddScoped<ILocalizedTextRepository, LocalizedTextRepository>();
//Multi-tenant settings (SuperAdmin-only, singleton settings row) — drives AuthService's tenant resolution
builder.Services.AddScoped<IMultiTenantSettingsService, MultiTenantSettingsService>();
builder.Services.AddScoped<IMultiTenantSettingsRepository, MultiTenantSettingsRepository>();
//Export safety cap (shared by every repository's unpaged GetAllXAsync) — null/<=0 is unlimited
builder.Services.AddSingleton(new ExportSettings(builder.Configuration.GetValue<int?>("ExportMaxRows")));

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"]!;
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

// Policy-based authorization, generated from PermissionCatalog's registered modules
builder.Services.AddAuthorization(options =>
{
    foreach (var (module, enumType) in PermissionCatalog.Modules)
        foreach (var action in Enum.GetNames(enumType))
            options.AddPolicy($"{module}.{action}", policy => policy.RequireClaim(module, action));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseRequestLocalization(localizationOptions);
app.UseCors("AllowAngularDev");
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<Hekutenantcoreapp.Api.Middleware.UserLanguageMiddleware>();
app.UseMiddleware<Hekutenantcoreapp.Api.Middleware.ActiveUserMiddleware>();
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapControllers();
// SPA fallback: any route not matched by an API controller or a static file is a
// client-side Angular route (login, register, tenant-picker, admin/*, ...) — serve
// index.html and let the Angular router take over, instead of 404ing.
app.MapFallbackToFile("index.html");

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var db = scope.ServiceProvider.GetRequiredService<HekutenantcoreappDbContext>();

    // Reportable enum lookup tables (genders, document_types, ...) — always run first since
    // Person/Tenant/TenantMembership rows below are FK'd to these.
    await EnumLookupSeeder.SeedAllAsync(db);

    // Multi-tenant settings singleton row (seeded with both flags off, no default tenant) —
    // read/updated by the SuperAdmin-only admin page and consulted by AuthService at login.
    await MultiTenantSettingsSeeder.SeedAsync(db);

    // SuperAdmin: global, platform-wide role (manages tenants themselves, role definitions,
    // system-wide user administration). Always has every registered permission, so it can
    // never lock itself out of a module after that module switches from role to policy checks.
    if (!await roleManager.RoleExistsAsync("SuperAdmin"))
        await roleManager.CreateAsync(new IdentityRole("SuperAdmin"));

    var superAdminRole = await roleManager.FindByNameAsync("SuperAdmin");
    if (superAdminRole != null)
    {
        var superAdminClaims = await roleManager.GetClaimsAsync(superAdminRole);
        foreach (var (module, enumType) in PermissionCatalog.Modules)
            foreach (var action in Enum.GetNames(enumType))
                if (!superAdminClaims.Any(c => c.Type == module && c.Value == action))
                    await roleManager.AddClaimAsync(superAdminRole, new Claim(module, action));
    }

    // Admin: repurposed as the per-tenant catalog role, assigned via UserTenantRole rather
    // than the global AspNetUserRoles. Gets every tenant-operational permission except the
    // ones that manage shared/global catalogs — those stay SuperAdmin-exclusive so a tenant
    // Admin can never reach outside its own tenant (e.g. deactivate another tenant's users).
    if (!await roleManager.RoleExistsAsync("Admin"))
        await roleManager.CreateAsync(new IdentityRole("Admin"));

    var adminRole = await roleManager.FindByNameAsync("Admin");
    if (adminRole != null)
    {
        var globalOnlyModules = new[] { nameof(TenantsPermission), nameof(RolesPermission), nameof(UserManagementPermission), nameof(MultiTenantSettingsPermission) };

        var adminClaims = await roleManager.GetClaimsAsync(adminRole);

        // Strip any global-only claims that were granted before this split existed (or added
        // manually since) — the sync below only ever adds tenant-operational claims, so stray
        // global claims would otherwise survive forever.
        foreach (var claim in adminClaims.Where(c => globalOnlyModules.Contains(c.Type)))
            await roleManager.RemoveClaimAsync(adminRole, claim);

        foreach (var (module, enumType) in PermissionCatalog.Modules)
        {
            if (globalOnlyModules.Contains(module)) continue;

            foreach (var action in Enum.GetNames(enumType))
            {
                if (!adminClaims.Any(c => c.Type == module && c.Value == action))
                    await roleManager.AddClaimAsync(adminRole, new Claim(module, action));
            }
        }
    }

    // Break-glass recovery: if SuperAdmin has no members (e.g. it was just created above,
    // or the last one was removed), restore a known bootstrap admin so the app can never
    // become unmanageable.
    var bootstrapEmail = builder.Configuration["BootstrapAdminEmail"];
    ApplicationUser? bootstrapUser = string.IsNullOrEmpty(bootstrapEmail) ? null : await userManager.FindByEmailAsync(bootstrapEmail);

    if (superAdminRole != null && bootstrapUser != null)
    {
        var superAdminUsers = await userManager.GetUsersInRoleAsync("SuperAdmin");
        if (superAdminUsers.Count == 0)
            await userManager.AddToRoleAsync(bootstrapUser, "SuperAdmin");
    }

    // Default tenant: create-if-missing, and give the bootstrap SuperAdmin a real home
    // (membership + tenant-scoped Admin role) so they land somewhere on first use instead
    // of only holding platform-wide powers with nothing to click into.
    var defaultTenantName = builder.Configuration["DefaultTenantName"];
    if (!string.IsNullOrEmpty(defaultTenantName))
    {
        var defaultTenant = await db.Tenants.FirstOrDefaultAsync(t => t.Name == defaultTenantName);
        if (defaultTenant == null)
        {
            defaultTenant = new Tenant { Name = defaultTenantName, TenantType = TenantType.Standard, IsActive = true };
            db.Tenants.Add(defaultTenant);
            await db.SaveChangesAsync();
        }

        if (bootstrapUser != null && adminRole != null)
        {
            var hasMembership = await db.TenantMemberships.IgnoreQueryFilters()
                .AnyAsync(m => m.UserId == bootstrapUser.Id && m.TenantId == defaultTenant.Id);
            if (!hasMembership)
            {
                db.TenantMemberships.Add(new TenantMembership
                {
                    UserId = bootstrapUser.Id,
                    TenantId = defaultTenant.Id,
                    Status = TenantMembershipStatus.Active
                });
                await db.SaveChangesAsync();
            }

            var hasTenantRole = await db.UserTenantRoles.IgnoreQueryFilters()
                .AnyAsync(r => r.UserId == bootstrapUser.Id && r.TenantId == defaultTenant.Id && r.RoleId == adminRole.Id);
            if (!hasTenantRole)
            {
                db.UserTenantRoles.Add(new UserTenantRole
                {
                    UserId = bootstrapUser.Id,
                    TenantId = defaultTenant.Id,
                    RoleId = adminRole.Id
                });
                await db.SaveChangesAsync();
            }
        }
    }
}

app.Run();
