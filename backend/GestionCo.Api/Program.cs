using FluentValidation;
using GestionCo.Api.Api.Middlewares;
using GestionCo.Api.Application.Common.Behaviors;
using GestionCo.Api.Application.Common.Interfaces;
using GestionCo.Api.Domain.Enums;
using GestionCo.Api.Infrastructure.Logging;
using GestionCo.Api.Infrastructure.Persistence;
using GestionCo.Api.Infrastructure.Persistence.Seeds;
using GestionCo.Api.Infrastructure.Services;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// ============ DATABASE ============
// Connection string depuis appsettings.json (clé "Default")
// Tu peux override via variable d'env : ConnectionStrings__Default
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? builder.Configuration.GetConnectionString("DefaultConnection") // fallback ancien nom
    ?? "Server=localhost;Database=StockVenteDb;Trusted_Connection=True;TrustServerCertificate=True;";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString, sql => sql.CommandTimeout(60)));

builder.Services.AddScoped<IAppDbContext>(p => p.GetRequiredService<AppDbContext>());

// ============ MediatR + FluentValidation ============
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
});

builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

// ============ SERVICES ============
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IReferenceGenerator, ReferenceGenerator>();
builder.Services.AddScoped<IAuditLogger, AuditLogger>();
builder.Services.AddScoped<IPdfService, PdfService>();

// ============ JWT AUTH ============
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>()
    ?? throw new InvalidOperationException("JwtSettings manquant");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", p => p.RequireRole(RoleUtilisateur.Admin.ToString()));
    options.AddPolicy("AdminOrManager", p => p.RequireRole(
        RoleUtilisateur.Admin.ToString(),
        RoleUtilisateur.Gestionnaire.ToString()));
    options.AddPolicy("ClientOnly", p => p.RequireRole(RoleUtilisateur.Client.ToString()));
});

// ============ CORS ============
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", p =>
    {
        // Accepte tous les ports localhost en dev (frontend Vite, Angular, etc.)
        p.SetIsOriginAllowed(origin =>
            new Uri(origin).Host == "localhost" || new Uri(origin).Host == "127.0.0.1")
         .AllowAnyMethod()
         .AllowAnyHeader()
         .AllowCredentials();
    });
});

// ============ CONTROLLERS + JSON ============
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// ============ SWAGGER ============
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "GestionCo. API",
        Version = "v1",
        Description = "API de gestion commerciale pour GestionCo. SARL"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authentication — Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ============ AUTO INIT DB + SEED ============
// Au démarrage : crée la DB si elle n'existe pas + ajoute les données de démo
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

    try
    {
        logger.LogInformation("");
        logger.LogInformation("================================================");
        logger.LogInformation("🔄 INITIALISATION DE LA BASE DE DONNÉES");
        logger.LogInformation("================================================");
        logger.LogInformation("📍 Connection: {ConnectionString}",
            connectionString.Substring(0, Math.Min(80, connectionString.Length)) + "...");

        // Tester la connexion
        var canConnect = await db.Database.CanConnectAsync();
        if (!canConnect)
        {
            logger.LogInformation("⚙️  La base de données n'existe pas encore, création en cours...");
        }

        // EnsureCreated = crée la DB + toutes les tables si pas existantes
        var created = await db.Database.EnsureCreatedAsync();

        if (created)
            logger.LogInformation("✅ Base de données 'StockVenteDb' créée avec succès !");
        else
            logger.LogInformation("✅ Base de données 'StockVenteDb' déjà existante");

        // Seed des données de démo
        logger.LogInformation("🌱 Insertion des données de démo...");
        await SeedData.SeedAsync(db, hasher);
        logger.LogInformation("✅ Données de démo insérées avec succès");

        logger.LogInformation("================================================");
        logger.LogInformation("🚀 Backend prêt — http://localhost:5000");
        logger.LogInformation("📚 Swagger UI — http://localhost:5000/swagger");
        logger.LogInformation("================================================");
        logger.LogInformation("");
        logger.LogInformation("👤 Comptes de démo :");
        logger.LogInformation("   🔐 Admin    : admin@gestionco.ma / Admin123!");
        logger.LogInformation("   📋 Manager  : s.fassi@gestionco.ma / Manager123!");
        logger.LogInformation("   👤 Client   : direction@atlas.ma / Client123!");
        logger.LogInformation("");
    }
    catch (Exception ex)
    {
        logger.LogError("");
        logger.LogError("================================================");
        logger.LogError("❌ ERREUR DE CONNEXION SQL SERVER");
        logger.LogError("================================================");
        logger.LogError(ex, "Détails de l'erreur :");
        logger.LogError("");
        logger.LogError("💡 SOLUTIONS POSSIBLES :");
        logger.LogError("");
        logger.LogError("   1️⃣  Vérifie que SQL Server est démarré :");
        logger.LogError("       Get-Service | Where-Object {{ $_.Name -like '*SQL*' }}");
        logger.LogError("");
        logger.LogError("   2️⃣  Modifie 'Default' dans appsettings.json selon ton setup :");
        logger.LogError("       • SQL Server local       : Server=localhost;...");
        logger.LogError("       • SQL Server Express     : Server=.\\SQLEXPRESS;...");
        logger.LogError("       • SQL Server LocalDB     : Server=(localdb)\\MSSQLLocalDB;...");
        logger.LogError("       • Avec authentification  : Server=localhost;User Id=sa;Password=xxx;...");
        logger.LogError("");
        logger.LogError("   3️⃣  Active TCP/IP dans SQL Server Configuration Manager");
        logger.LogError("");
        logger.LogError("================================================");
    }
}

// ============ PIPELINE ============
app.UseCors("AllowFrontend");
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "GestionCo API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();
