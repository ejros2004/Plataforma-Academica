using Control_De_Tareas.Data;
using Control_De_Tareas.Data.Entitys;
using Microsoft.EntityFrameworkCore;
using Control_De_Tareas.Services;
using Control_De_Tareas;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// 1. Servicios del contenedor
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IFileStorageService, FileStorageService>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddLogging();
// Configuración de Mapster
MapsterConfig.Configure();

// Configuración de la Base de Datos
builder.Services.AddDbContext<ContextDB>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Configuración de Autenticación
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Home/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(2);
        options.SlidingExpiration = true;
    });

// 3. Configuración de Autorización (Políticas)
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy =>
        policy.RequireRole("Administrador"));
    options.AddPolicy("Profesor", policy =>
        policy.RequireRole("Profesor"));
    options.AddPolicy("Estudiante", policy =>
        policy.RequireRole("Estudiante"));
    options.AddPolicy("ProfesorOAdmin", policy =>
        policy.RequireRole("Profesor", "Administrador"));
});

// 4. Configuración de Sesión
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Configuración del pipeline de solicitudes HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // IMPORTANTE: Comentado para evitar errores en servidores sin SSL (http)
    // app.UseHsts(); 
}

// IMPORTANTE: Comentado para evitar el bucle de redirección a HTTPS (puerto 443)
// que causa el ERR_CONNECTION_RESET en dominios temporales.
// app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

// --- ZONA CRÍTICA: EL ORDEN IMPORTA ---

// 1º: Sesión 
app.UseSession();

// 2º: Autenticación
app.UseAuthentication();

// 3º: Autorización
app.UseAuthorization();

// --------------------------------------

app.UseStatusCodePagesWithReExecute("/Home/Error/{0}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();