using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using TpMiercoles.Entidades;
using TpMiercoles.Repositorios;
using TpMiercoles.Servicios;
using TpMiercoles.Api.Infraestructura;
using TpMiercoles.Servicios.Rutas;
using TpMiercoles.Servicios.Rutas.OpenRouteService;
using TpMiercoles.Servicios.Viajes;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
// Genera la especificación OpenAPI (/openapi/v1.json) que después dibuja Swagger.
builder.Services.AddOpenApi(opciones => {
    opciones.AddDocumentTransformer((documento, contexto, ct) => {
        documento.Info.Title = "RuteaYa API";
        documento.Info.Version = "v1";
        documento.Info.Description =
            "API REST de RuteaYa: viajes, oportunidades, transportes e historial. " +
            "Requiere iniciar sesión: entrá primero en la aplicación web y después probá los endpoints desde acá.";
        return Task.CompletedTask;
    });
});

builder.Services.AddDbContext<ViajesDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IViajeServicio, ViajeServicio>();
builder.Services.AddScoped<IViajeRepositorio, ViajeRepositorio>();
builder.Services.AddScoped<IRutaRepositorio, RutaRepositorio>();
builder.Services.AddScoped<IHistorialServicio, HistorialServicio>();
builder.Services.AddScoped<IHistorialRepositorio, HistorialRepositorio>();
builder.Services.AddScoped<ITransporteServicio, TransporteServicio>();
builder.Services.AddScoped<ITransporteRepositorio, TransporteRepositorio>();
builder.Services.AddScoped<IEnvioRepositorio, EnvioRepositorio>();
builder.Services.AddScoped<IOportunidadServicio, OportunidadServicio>();
builder.Services.AddScoped<IMaterializadorDeViajes, MaterializadorDeViajes>();
builder.Services.AddHostedService<MaterializacionDeViajesHostedService>();

// con API key usa OpenRouteService, sin key usa el mock
var rutasOpts = builder.Configuration.GetSection(RutasOptions.Seccion).Get<RutasOptions>() ?? new();
builder.Services.AddSingleton(rutasOpts);

if (rutasOpts.UsarOpenRouteService) {
    builder.Services.AddHttpClient<OpenRouteServiceRutas>(c =>
        c.BaseAddress = new Uri(rutasOpts.OrsBaseUrl));

    // Plan B: si ORS agota la cuota (403) o se pasa del límite por minuto (429), se calcula con el mock.
    builder.Services.AddSingleton<MockServicioRutas>();
    builder.Services.AddSingleton<EstadoCuotaRutas>();
    builder.Services.AddTransient<IServicioRutas>(sp => new ServicioRutasConRespaldo(
        sp.GetRequiredService<OpenRouteServiceRutas>(),
        sp.GetRequiredService<MockServicioRutas>(),
        sp.GetRequiredService<EstadoCuotaRutas>(),
        sp.GetRequiredService<TimeProvider>(),
        sp.GetRequiredService<ILogger<ServicioRutasConRespaldo>>()));
} else{
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
if (app.Environment.IsDevelopment()) {
    app.MapOpenApi();

    // Página de Swagger en /swagger, armada a partir de /openapi/v1.json.
    app.UseSwaggerUI(opciones => {
        opciones.SwaggerEndpoint("/openapi/v1.json", "RuteaYa API v1");
        opciones.DocumentTitle = "RuteaYa API";
    });
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
