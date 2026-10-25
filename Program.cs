using Microsoft.EntityFrameworkCore;
using TaskManager.Models;
var builder = WebApplication.CreateBuilder(args);

var conn = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
 options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


builder.Services.AddControllersWithViews();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapControllerRoute(
    name: "products",
    pattern: "Products",
    defaults: new { controller = "Products", action = "ProductList" });

app.MapControllerRoute(
    name: "productmgr",
    pattern: "ProductMgr",
    defaults: new { controller = "Products", action = "ProductMgr" });

app.MapControllerRoute(
    name: "newproduct",
    pattern: "NewProduct",
    defaults: new { controller = "Products", action = "NewProduct" });
app.MapControllers();
app.Run();
