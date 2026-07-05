using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Vaxtrack.Interfaces;
using Vaxtrack.Interfaces.RepositoryInterfaces;
using Vaxtrack.Interfaces.UtilityInterfaces;
using Vaxtrack.Models;
using Vaxtrack.Repositories;
using Vaxtrack.Services;
using Vaxtrack.Utilities;

// Configure Serilog — new log file created on every app start
var startupTimestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", Serilog.Events.LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console(
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: $"Logs/vaxtrack-{startupTimestamp}.log",
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

// Keep JWT claim names as short names (sub, role, jti) — without this, the default
// JwtSecurityTokenHandler remaps "sub" → NameIdentifier URI before the principal is built,
// breaking User.FindFirst(JwtRegisteredClaimNames.Sub) in every controller.
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

var builder = WebApplication.CreateBuilder(args);

// Replace default logging with Serilog
builder.Host.UseSerilog();

// Bind JwtSettings and register as singleton so AuthService can inject it
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>()!;
builder.Services.AddSingleton(jwtSettings);

// Bind SmtpSettings and register as singleton so AuthService/EmailService can inject it
var smtpSettings = builder.Configuration.GetSection("Smtp").Get<SmtpSettings>() ?? new SmtpSettings();
builder.Services.AddSingleton(smtpSettings);

// Configure JWT bearer authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // The runtime token handler (JsonWebTokenHandler) ignores the legacy
        // JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear() call above — without this,
        // it silently remaps short claim names (sub, role) to long claim-type URIs, breaking
        // RoleClaimType = "role" below and any User.IsInRole("admin") / [Authorize(Roles=...)] check.
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
            ValidateIssuer           = true,
            ValidIssuer              = jwtSettings.Issuer,
            ValidateAudience         = true,
            ValidAudience            = jwtSettings.Audience,
            ValidateLifetime         = true,
            ClockSkew                = TimeSpan.Zero,
            RoleClaimType            = "role"
        };

        // Reject tokens that have been explicitly revoked via /auth/logout, or that belong to
        // an account disabled AFTER the token was issued — this is what makes disabling a user
        // take effect immediately on any already-live session, not just on their next login.
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var blacklist = context.HttpContext.RequestServices
                    .GetRequiredService<ITokenBlacklistRepository>();

                var jti = context.Principal?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;

                if (!string.IsNullOrEmpty(jti) && await blacklist.IsRevokedAsync(jti))
                {
                    context.Fail("Token has been revoked.");
                    return;
                }

                var sub = context.Principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
                if (!string.IsNullOrEmpty(sub))
                {
                    var userRepository = context.HttpContext.RequestServices
                        .GetRequiredService<IUserRepository>();
                    var user = await userRepository.GetUserDetailsByUserUidAsync(sub);

                    if (user is not null && user.Status != "Active")
                        context.Fail("Account has been disabled.");
                }
            }
        };
    });

// Add services to the container
builder.Services.AddOpenApi();
builder.Services.AddControllers();

// Add CORS if needed
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policyBuilder =>
    {
        policyBuilder.AllowAnyOrigin()
                     .AllowAnyMethod()
                     .AllowAnyHeader();
    });
});

// Add Entity Framework Core with SQL Server
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<VaxtrackDbContext>(options =>
    options.UseSqlServer(connectionString));

// Add application services (repositories, business logic)
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IHospitalRepository, HospitalRepository>();
builder.Services.AddScoped<IHospitalService, HospitalService>();
builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IBookingAuditLogRepository, BookingAuditLogRepository>();
builder.Services.AddScoped<IHospitalAuditLogRepository, HospitalAuditLogRepository>();
builder.Services.AddScoped<IUserAuditLogRepository, UserAuditLogRepository>();
builder.Services.AddScoped<IUserRequestRepository, UserRequestRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IUserRoleMappingRepository, UserRoleMappingRepository>();
builder.Services.AddScoped<IUserRoleMappingService, UserRoleMappingService>();
builder.Services.AddScoped<IUserCredentialsRepository, UserCredentialsRepository>();
builder.Services.AddScoped<ITokenBlacklistRepository, TokenBlacklistRepository>();
builder.Services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
builder.Services.AddScoped<IUtilityService, UtilityService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEmailService, EmailService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();

// serves wwwroot/uploads/profile-pictures/{userUid}.jpg at /uploads/profile-pictures/{userUid}.jpg
// Cache-Control: no-cache forces the browser to always revalidate via ETag/Last-Modified before
// reusing a cached copy — without it, re-uploading a picture to the same fixed filename can leave
// browsers showing the old image for a long time (heuristic freshness grows with file age).
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.CacheControl = "no-cache";
    }
});
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Run database migrations on startup (optional)
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<VaxtrackDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.Run();

public class VaxtrackDbContext : DbContext
{
    public VaxtrackDbContext(DbContextOptions<VaxtrackDbContext> options) : base(options) { }

    public DbSet<UserModel> Users { get; set; }
    public DbSet<UserCredentialsModel> UserCredentials { get; set; }
    public DbSet<HospitalModel> Hospitals { get; set; }
    public DbSet<BookingModel> Bookings { get; set; }
    public DbSet<UserRoleMappingModel> UserRoleMappings { get; set; }
    public DbSet<RevokedTokenModel> RevokedTokens { get; set; }
    public DbSet<PasswordResetTokenModel> PasswordResetTokens { get; set; }
    public DbSet<BookingAuditLogModel> BookingAuditLogs { get; set; }
    public DbSet<HospitalAuditLogModel> HospitalAuditLogs { get; set; }
    public DbSet<UserAuditLogModel> UserAuditLogs { get; set; }
    public DbSet<UserRequestModel> UserRequests { get; set; }
    public DbSet<NotificationModel> Notifications { get; set; }
}
