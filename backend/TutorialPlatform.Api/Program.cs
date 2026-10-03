using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TutorialPlatform.Api.Data;
using TutorialPlatform.Api.Json;
using TutorialPlatform.Api.Middleware;
using TutorialPlatform.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------- EF Core + proveedor de base de datos ----------
// Local: SQL Server, tal y como lo define el §6 del PROMPT.
// Nube: SQLite, elegido con el ajuste "Database:Provider" de la App Service.
// De este modo el desarrollo local conserva SQL Server, sus migraciones y sus datos,
// y solo la instancia desplegada en la nube cambia de motor.
var dbProvider = builder.Configuration["Database:Provider"] ?? "SqlServer";
var usarSqlite = dbProvider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase);

if (usarSqlite)
{
    var sqlitePath = builder.Configuration["Database:SqlitePath"] ?? "tutorialplatform.db";
    var sqliteDir = Path.GetDirectoryName(Path.GetFullPath(sqlitePath));
    if (!string.IsNullOrEmpty(sqliteDir)) Directory.CreateDirectory(sqliteDir);

    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite($"Data Source={sqlitePath}"));
}
else
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
}

builder.Services.AddControllers().AddJsonOptions(o =>
{
    // Recorta los strings del body ANTES de validar (ver JsonTrimmingConverter)
    o.JsonSerializerOptions.Converters.Add(new JsonTrimmingConverter());
});
builder.Services.AddOpenApi();

// ---------- Auth: JWT ----------
var jwtSecret = builder.Configuration["Jwt:SecretKey"]
    ?? throw new InvalidOperationException("Falta Jwt:SecretKey en appsettings.json");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddScoped<ITutorialMapper, TutorialMapper>();

// ---------- CORS (para el frontend Vue en dev) ----------
var vueOrigin = builder.Configuration["Cors:Frontend"] ?? "http://localhost:5173";
builder.Services.AddCors(options =>
    options.AddPolicy("Frontend", p => p.WithOrigins(vueOrigin).AllowAnyHeader().AllowAnyMethod()));

// ---------- Cabeceras de proxy inverso ----------
// Azure App Service termina el TLS y entrega la petición al proceso como HTTP
// junto con X-Forwarded-Proto: https. Sin procesarlas, UseHttpsRedirection
// vería siempre «http» y redirigiría a un destino incorrecto.
// En local no llegan cabeceras de reenvío, así que el comportamiento no cambia.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// ---------- wwwroot debe existir ANTES de Build() ----------
// Si no existe al arrancar, WebRootPath queda null y UseStaticFiles sirve 404 siempre.
Directory.CreateDirectory(Path.Combine(builder.Environment.ContentRootPath, "wwwroot", "uploads"));

var app = builder.Build();

// ---------- Cabeceras del proxy inverso: lo primero que se procesa ----------
app.UseForwardedHeaders();

// ---------- Manejador global de excepciones ----------
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// ---------- Archivos estáticos: /uploads/... y / (wwwroot) ----------
app.UseStaticFiles();

app.UseCors("Frontend");
app.UseAuthentication(); // ← primero autenticar...
app.UseAuthorization();  // ← ...luego autorizar
app.MapControllers();

// ---------- SPA: rutas limpias del frontend Vue ----------
// El router usa createWebHistory(), así que genera URLs como /tutorials/5 que,
// sin este fallback, devolverían 404 al recargar la página.
// Solo se registra si index.html está compilado en wwwroot: en desarrollo el
// frontend lo sirve Vite en el 5173 y aquí no hay build, así que no interfiere.
var indexSpa = Path.Combine(builder.Environment.ContentRootPath, "wwwroot", "index.html");
if (File.Exists(indexSpa))
{
    app.MapFallbackToFile("index.html");
}

// ---------- Seed de datos de prueba ----------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    if (usarSqlite)
    {
        // Las migraciones del proyecto están generadas para SQL Server
        // (anotación SqlServer:Identity, tipos nvarchar y datetime2) y no pueden
        // ejecutarse en SQLite. Para la nube se construye el esquema directamente
        // a partir del modelo; SQLite crea el fichero si no existe.
        await db.Database.EnsureCreatedAsync();
    }
    else
    {
        await db.Database.MigrateAsync();
    }

    await DbSeeder.SeedAsync(db);
}

app.Run();
