using Microsoft.EntityFrameworkCore;
using TaskManager.Models;

var builder = WebApplication.CreateBuilder(args);

// Lấy chuỗi kết nối CSDL SQL Server từ appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// 1. Đăng ký CSDL AppDbContext duy nhất
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// 2. Đăng ký Controllers, Views và Session
builder.Services.AddControllersWithViews();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2); // Thời gian sống của Session
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// 3. Đăng ký Swagger (cho API)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Bật Swagger UI
app.UseSwagger();
app.UseSwaggerUI();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthorization();


app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");


app.MapControllers();

app.Run();