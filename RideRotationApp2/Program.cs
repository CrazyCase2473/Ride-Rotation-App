using RideRotationApp2.Components;
using RideRotationApp2.Services;
using Microsoft.EntityFrameworkCore;
using RideRotationApp2.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddSingleton<CsvService>();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=Data/ride_rotation.db"));
builder.Services.AddScoped<CertificationService>();
builder.Services.AddScoped<RotationService>();
builder.Services.AddScoped<NextShiftService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
