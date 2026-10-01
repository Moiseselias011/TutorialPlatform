using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TutorialPlatform.Api.Data;
using TutorialPlatform.Api.Json;
using TutorialPlatform.Api.Middleware;
using TutorialPlatform.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------- EF Core + SQL Server ----------
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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

// ---------- wwwroot debe existir ANTES de Build() ----------
// Si no existe al arrancar, WebRootPath queda null y UseStaticFiles sirve 404 siempre.
Directory.CreateDirectory(Path.Combine(builder.Environment.ContentRootPath, "wwwroot", "uploads"));

var app = builder.Build();

// ---------- Manejador global de excepciones (debe ir primero) ----------
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

// ---------- Seed de datos de prueba ----------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await DbSeeder.SeedAsync(db);
}

app.Run();
