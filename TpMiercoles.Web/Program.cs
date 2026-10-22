using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TpMiercoles.Entidades;
using TpMiercoles.Repositorios;
using TpMiercoles.Servicios;
using TpMiercoles.Web.Infraestructura;
using TpMiercoles.Web.Mapeos;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddHttpContextAccessor();
builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddMaps(typeof(PerfilDeMapeo).Assembly);
    cfg.AddMaps(typeof(PerfilDeMapeoWeb).Assembly);
});
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<ViajesDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IUsuarioServicio, UsuarioServicio>();
builder.Services.AddScoped<IUsuarioRepositorio, UsuarioRepositorio>();
builder.Services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();
builder.Services.AddScoped<ISesionWeb, SesionWeb>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Usuario/Login";
        options.Cookie.HttpOnly = true;  //Js no puede leer la cookie
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always; //La Cookie solo se envia por HTTPS
        options.Cookie.SameSite = SameSiteMode.Lax; //Permite peticiones GET en otro navegador, no POST
        options.ExpireTimeSpan = TimeSpan.FromHours(8); //La cookie dura 8 horas
        options.SlidingExpiration = true; //Se renueva si el usuario sigue logueado
    });

builder.Services.AddAuthorization(); //Hace funcionar [Authorize]

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
