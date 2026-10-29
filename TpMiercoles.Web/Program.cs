using System.Globalization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TpMiercoles.Entidades;
using TpMiercoles.Repositorios;
using TpMiercoles.Servicios;
using TpMiercoles.Web.Infraestructura;
using TpMiercoles.Web.Mapeos;


var builder = WebApplication.CreateBuilder(args);
// --- CONFIGURACIÓN DE CULTURA (SOLUCIÓN PARA DECIMALES CON PUNTO/COMA) ---
var cultureInfo = new CultureInfo("es-AR");
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

// Add services to the container.
builder.Services.AddHttpContextAccessor();
builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddMaps(typeof(PerfilDeMapeo).Assembly);
    cfg.AddMaps(typeof(PerfilDeMapeoWeb).Assembly);
});
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient("ViajesApi", cliente =>
{
    cliente.BaseAddress = new Uri(
        builder.Configuration["Api:BaseUrl"]!);
});
builder.Services.AddDbContext<ViajesDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IUsuarioServicio, UsuarioServicio>();
builder.Services.AddScoped<IUsuarioRepositorio, UsuarioRepositorio>();
builder.Services.AddScoped<IViajeServicio, ViajeServicio>();
builder.Services.AddScoped<IViajeRepositorio, ViajeRepositorio>();
builder.Services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();
builder.Services.AddScoped<ISesionWeb, SesionWeb>();
builder.Services.AddScoped<IEnvioServicio, EnvioServicio>();
builder.Services.AddScoped<IEnvioRepositorio, EnvioRepositorio>();
builder.Services.AddScoped<ISeedDeDesarrollo, SeedDeDesarrollo>();

string carpetaClaves = Path.GetFullPath(
    Path.Combine(builder.Environment.ContentRootPath, "..", "shared-keys"));

builder.Services
    .AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(carpetaClaves))
    .SetApplicationName("TpMiercoles");

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Usuario/Login";
        options.Cookie.Name = ".TpMiercoles.Auth";
        options.Cookie.Path = "/";
        options.Cookie.HttpOnly = true;  //Js no puede leer la cookie
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always; //La Cookie solo se envia por HTTPS
        options.Cookie.SameSite = SameSiteMode.Lax; //Permite peticiones GET en otro navegador, no POST
        options.ExpireTimeSpan = TimeSpan.FromHours(8); //La cookie dura 8 horas
        options.SlidingExpiration = true; //Se renueva si el usuario sigue logueado

        options.Events.OnRedirectToLogin = contexto =>
        {
            if (contexto.Request.Path.StartsWithSegments("/api"))
            {
                contexto.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }

            contexto.Response.Redirect(contexto.RedirectUri);
            return Task.CompletedTask;
        };

        options.Events.OnRedirectToAccessDenied = contexto =>
        {
            if (contexto.Request.Path.StartsWithSegments("/api"))
            {
                contexto.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            }

            contexto.Response.Redirect(contexto.RedirectUri);
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization(); //Hace funcionar [Authorize]

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Datos de ejemplo para poder probar las pantallas. No corre en produccion.
    using IServiceScope ambitoDeSeed = app.Services.CreateScope();
    await ambitoDeSeed.ServiceProvider.GetRequiredService<ISeedDeDesarrollo>().SembrarAsync();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication(); // lee la cookie en cada request y genera el User en HttpContext.User
app.UseAuthorization(); // verifica si el User tiene permisos para acceder a una ruta

app.MapStaticAssets();

app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
