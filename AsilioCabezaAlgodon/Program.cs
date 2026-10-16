
using AsilioCabezaAlgodon.Datos;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Base de datos del asilo
builder.Services.AddDbContext<AsiloContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("ConexionSQL")));

// Base de datos para usuarios y roles
builder.Services.AddDbContext<IdentityContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("ConexionSQL")));

// Configuración de Identity
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 8;

    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<IdentityContext>()
.AddDefaultTokenProviders();

// Configuración del inicio de sesión
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Cuenta/Login";
    options.AccessDeniedPath = "/Cuenta/AccesoDenegado";
});

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configuración de errores
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

// Autenticación y autorización
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider
        .GetRequiredService<RoleManager<IdentityRole>>();

    string[] roles =
    {
        "Administrador",
        "Recepcionista",
        "Medico",
        "Enfermero",
        "Laboratorio",
        "Farmacia",
        "Fundacion"
    };

    foreach (string rol in roles)
    {
        if (!await roleManager.RoleExistsAsync(rol))
        {
            var resultado = await roleManager.CreateAsync(
                new IdentityRole(rol));

            if (!resultado.Succeeded)
            {
                throw new Exception(
                    "No se pudo crear el rol: " + rol);
            }
        }
    }
}
// Crear el usuario administrador inicial
using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider
        .GetRequiredService<UserManager<IdentityUser>>();

    string correo = "admin@asilo.com";

    var administrador = await userManager.FindByEmailAsync(correo);

    if (administrador == null)
    {
        administrador = new IdentityUser
        {
            UserName = correo,
            Email = correo,
            EmailConfirmed = true
        };

        string contrasena = Environment.GetEnvironmentVariable(
            "ASILO_ADMIN_PASSWORD") ?? "";

        if (string.IsNullOrWhiteSpace(contrasena))
        {
            throw new Exception(
                "Configura ASILO_ADMIN_PASSWORD antes de iniciar.");
        }

        var resultado = await userManager.CreateAsync(
            administrador, contrasena);

        if (!resultado.Succeeded)
        {
            throw new Exception(string.Join("; ",
                resultado.Errors.Select(e => e.Description)));
        }
    }

    if (!await userManager.IsInRoleAsync(administrador, "Administrador"))
    {
        var resultadoRol = await userManager.AddToRoleAsync(
            administrador, "Administrador");

        if (!resultadoRol.Succeeded)
        {
            throw new Exception(string.Join("; ",
                resultadoRol.Errors.Select(e => e.Description)));
        }
    }
}

app.Run();
