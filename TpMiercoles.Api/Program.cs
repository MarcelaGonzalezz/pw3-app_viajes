using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using TpMiercoles.Entidades;
using TpMiercoles.Repositorios;
using TpMiercoles.Servicios;
using TpMiercoles.Servicios.Rutas;
using TpMiercoles.Servicios.Rutas.OpenRouteService;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<ViajesDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IViajeServicio, ViajeServicio>();
builder.Services.AddScoped<IViajeRepositorio, ViajeRepositorio>();
builder.Services.AddScoped<IHistorialServicio, HistorialServicio>();
builder.Services.AddScoped<IHistorialRepositorio, HistorialRepositorio>();
builder.Services.AddScoped<ITransporteServicio, TransporteServicio>();
builder.Services.AddScoped<ITransporteRepositorio, TransporteRepositorio>();
builder.Services.AddScoped<IEnvioRepositorio, EnvioRepositorio>();
builder.Services.AddScoped<IOportunidadServicio, OportunidadServicio>();

// con API key usa OpenRouteService, sin key usa el mock
var rutasOpts = builder.Configuration.GetSection(RutasOptions.Seccion).Get<RutasOptions>() ?? new();
builder.Services.AddSingleton(rutasOpts);

if (rutasOpts.UsarOpenRouteService)
{
    builder.Services.AddHttpClient<IServicioRutas, OpenRouteServiceRutas>(c =>
        c.BaseAddress = new Uri(rutasOpts.OrsBaseUrl));

}
else
{
    // en modo mock, sin API key, para poder probar la app sin gastar llamadas a ORS
    builder.Services.AddSingleton<IServicioRutas, MockServicioRutas>();
}

builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddAutoMapper(cfg => cfg.AddMaps(typeof(PerfilDeMapeo).Assembly));

string carpetaClaves = Path.GetFullPath(
    Path.Combine(builder.Environment.ContentRootPath, "..", "shared-keys"));

builder.Services
    .AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(carpetaClaves))
    .SetApplicationName("TpMiercoles");

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = ".TpMiercoles.Auth";
        options.Cookie.Path = "/";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;

        options.Events.OnRedirectToLogin = contexto =>
        {
            contexto.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };

        options.Events.OnRedirectToAccessDenied = contexto =>
        {
            contexto.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
