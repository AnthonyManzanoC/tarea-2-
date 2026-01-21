using CoronelExpress.Data;
using CoronelExpress.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;



var builder = WebApplication.CreateBuilder(args);

// 1️⃣ Configurar EF Core con SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("cn")));

// 2️⃣ Configurar Identity con soporte de Roles
builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// 3️⃣ Configurar cookies (Login y acceso denegado)
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    options.SlidingExpiration = true;
});

// 4️⃣ Registrar servicios personalizados (por ejemplo, ProductService)
builder.Services.AddScoped<IProductService, ProductService>();

builder.Services.AddHttpClient();

// 5️⃣ Habilitar sesiones en la app
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddScoped<InventoryService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();

builder.Services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
builder.Services.AddScoped<IPromotionRepository, PromotionRepository>();
// 6️⃣ Habilitar Razor Pages (si usas vistas de Identity)
builder.Services.AddRazorPages();

// 7️⃣ Habilitar controladores y aplicar autenticación globalmente
builder.Services.AddControllersWithViews(options =>
{
    var policy = new AuthorizationPolicyBuilder()
                     .RequireAuthenticatedUser()
                     .Build();
    options.Filters.Add(new AuthorizeFilter(policy));
});

var app = builder.Build();

// 🔧 Seeding de roles: aseguramos que existan "Admin" y "User"
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    string[] roleNames = { "Admin", "User" };

    foreach (var roleName in roleNames)
    {
        if (!roleManager.RoleExistsAsync(roleName).GetAwaiter().GetResult())
        {
            roleManager.CreateAsync(new IdentityRole(roleName)).GetAwaiter().GetResult();
        }
    }
}

// 🛠️ Middlewares esenciales
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}"
);

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();
