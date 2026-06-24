using Microsoft.EntityFrameworkCore;
using Serilog;
using Vaxtrack.Interfaces;
using Vaxtrack.Interfaces.RepositoryInterfaces;
using Vaxtrack.Interfaces.UtilityInterfaces;
using Vaxtrack.Repositories;
using Vaxtrack.Services;
using Vaxtrack.Utilities;
using Vaxtrack.Models;

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

var builder = WebApplication.CreateBuilder(args);

// Replace default logging with Serilog
builder.Host.UseSerilog();

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
builder.Services.AddScoped<IUtilityService, UtilityService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
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
    public DbSet<HospitalModel> Hospitals { get; set; }
    public DbSet<BookingModel> Bookings {get; set;}
}
