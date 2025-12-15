using HealthCart.Data;
using HealthCart.Interfaces;
using HealthCart.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSession();
builder.Services.AddDistributedMemoryCache();

// dependency injection
builder.Services.AddSingleton<ITokenService , TokenService>();
builder.Services.AddSingleton<IMailService, EmailService>();
builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();

// service injection 
builder.Services.AddDbContext<SqlDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("main")));

var app = builder.Build();


if (app.Environment.IsProduction())
{
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}



app.UseExceptionHandler("/Error");

app.UseSession();

app.UseHttpsRedirection();

app.UseStaticFiles();   // use static files present in wwwwroot 

app.UseRouting();

// app.UseAuthentication(); // before UseAuthorization

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();