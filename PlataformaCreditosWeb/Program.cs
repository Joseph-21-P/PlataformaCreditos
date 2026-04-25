using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PlataformaCreditosWeb.Data;
using PlataformaCreditosWeb.Models;

var builder = WebApplication.CreateBuilder(args);


var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddControllersWithViews();

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration["Redis:ConnectionString"];
    options.InstanceName = "CreditosApp_";
});

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddHttpContextAccessor();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
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
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();


// SCRIPT DE DATOS INICIALES (Analista, Clientes y Solicitudes)
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    // 1. Crear rol Analista
    if (!await roleManager.RoleExistsAsync("Analista"))
        await roleManager.CreateAsync(new IdentityRole("Analista"));

    // 2. Crear usuario Analista
    if (await userManager.FindByEmailAsync("analista@banco.com") == null)
    {
        var analista = new IdentityUser { UserName = "analista@banco.com", Email = "analista@banco.com", EmailConfirmed = true };
        await userManager.CreateAsync(analista, "Password123!");
        await userManager.AddToRoleAsync(analista, "Analista");
    }

    // 3. Crear 2 Clientes y 2 Solicitudes (Requisito Pregunta 1)
    if (!dbContext.Clientes.Any())
    {
        // Cliente 1
        var c1User = new IdentityUser { UserName = "cliente1@banco.com", Email = "cliente1@banco.com", EmailConfirmed = true };
        await userManager.CreateAsync(c1User, "Password123!");
        var cliente1 = new Cliente { UsuarioId = c1User.Id, IngresosMensuales = 2000, Activo = true };
        dbContext.Clientes.Add(cliente1);

        // Cliente 2
        var c2User = new IdentityUser { UserName = "cliente2@banco.com", Email = "cliente2@banco.com", EmailConfirmed = true };
        await userManager.CreateAsync(c2User, "Password123!");
        var cliente2 = new Cliente { UsuarioId = c2User.Id, IngresosMensuales = 5000, Activo = true };
        dbContext.Clientes.Add(cliente2);

        await dbContext.SaveChangesAsync();

        // Solicitudes iniciales
        dbContext.SolicitudesCredito.Add(new SolicitudCredito { ClienteId = cliente1.Id, MontoSolicitado = 1500, Estado = "Pendiente", FechaSolicitud = DateTime.Now });
        dbContext.SolicitudesCredito.Add(new SolicitudCredito { ClienteId = cliente2.Id, MontoSolicitado = 10000, Estado = "Aprobado", FechaSolicitud = DateTime.Now.AddDays(-2) });

        await dbContext.SaveChangesAsync();
    }
}

app.Run();
