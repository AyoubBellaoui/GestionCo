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
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IReapproService, ReapproService>();
builder.Services.AddHostedService<RecurringChargesJob>();
builder.Services.AddHostedService<VenteEcheanceJob>();

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

        // Tables ajoutées après la création initiale (CREATE TABLE manuel)
        await ApplyManualTablesAsync(db, logger);

        // Colonnes ajoutées après la création initiale (ALTER TABLE manuel)
        await ApplyManualColumnsAsync(db, logger);

        // Seed par défaut pour parametres_facturation
        try
        {
            if (!await db.ParametresFacturation.AnyAsync())
            {
                db.ParametresFacturation.Add(new GestionCo.Api.Domain.Entities.ParametresFacturation());
                await db.SaveChangesAsync();
                logger.LogInformation("✅ Paramètres facturation initialisés par défaut");
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning("⚠️  Impossible d'initialiser parametres_facturation : {Msg}", ex.Message);
        }

        // Seed par défaut pour parametres_entreprise
        try
        {
            if (!await db.ParametresEntreprise.AnyAsync())
            {
                db.ParametresEntreprise.Add(new GestionCo.Api.Domain.Entities.ParametresEntreprise());
                await db.SaveChangesAsync();
                logger.LogInformation("✅ Paramètres entreprise initialisés par défaut");
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning("⚠️  Impossible d'initialiser parametres_entreprise : {Msg}", ex.Message);
        }

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

// Crée les tables ajoutées après la création initiale de la DB
static async Task ApplyManualTablesAsync(AppDbContext db, ILogger logger)
{
    var tables = new (string Name, string Sql)[]
    {
        ("categories_charge", """
            CREATE TABLE [categories_charge] (
                [Id]    INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                [Nom]   NVARCHAR(100) NOT NULL,
                [Icone] NVARCHAR(10)  NULL,
                CONSTRAINT [UQ_categories_charge_Nom] UNIQUE ([Nom])
            )
            """),

        ("charges", """
            CREATE TABLE [charges] (
                [Id]               INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                [Reference]        NVARCHAR(30)   NOT NULL,
                [Titre]            NVARCHAR(200)  NOT NULL,
                [Description]      NVARCHAR(1000) NULL,
                [Montant]          DECIMAL(18,2)  NOT NULL,
                [MontantPaye]      DECIMAL(18,2)  NOT NULL DEFAULT 0,
                [Justificatif]     NVARCHAR(500)  NULL,
                [Statut]           INT            NOT NULL DEFAULT 1,
                [DateCharge]       DATETIME2      NOT NULL,
                [CategorieChargeId] INT           NOT NULL,
                [UtilisateurId]    INT            NOT NULL,
                [FournisseurId]    INT            NULL,
                [CreatedAt]        DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
                [UpdatedAt]        DATETIME2      NULL,
                [CreatedBy]        INT            NULL,
                [UpdatedBy]        INT            NULL,
                CONSTRAINT [UQ_charges_Reference] UNIQUE ([Reference]),
                CONSTRAINT [FK_charges_categories_charge] FOREIGN KEY ([CategorieChargeId]) REFERENCES [categories_charge]([Id]),
                CONSTRAINT [FK_charges_utilisateurs]      FOREIGN KEY ([UtilisateurId])     REFERENCES [utilisateurs]([Id]),
                CONSTRAINT [FK_charges_fournisseurs]      FOREIGN KEY ([FournisseurId])     REFERENCES [fournisseurs]([Id])
            )
            """),

        ("paiements_charge", """
            CREATE TABLE [paiements_charge] (
                [Id]           INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                [ChargeId]     INT           NOT NULL,
                [Montant]      DECIMAL(18,2) NOT NULL,
                [Methode]      INT           NOT NULL,
                [Statut]       INT           NOT NULL DEFAULT 2,
                [DatePaiement] DATETIME2     NOT NULL,
                [Reference]    NVARCHAR(100) NULL,
                [Notes]        NVARCHAR(500) NULL,
                [CreatedAt]    DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
                [UpdatedAt]    DATETIME2     NULL,
                [CreatedBy]    INT           NULL,
                [UpdatedBy]    INT           NULL,
                CONSTRAINT [FK_paiements_charge_charges] FOREIGN KEY ([ChargeId]) REFERENCES [charges]([Id]) ON DELETE CASCADE
            )
            """),

        ("notifications", """
            CREATE TABLE [notifications] (
                [Id]              INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                [Titre]           NVARCHAR(200) NOT NULL,
                [Message]         NVARCHAR(500) NOT NULL,
                [Type]            INT           NOT NULL DEFAULT 1,
                [Categorie]       INT           NOT NULL DEFAULT 7,
                [IsRead]          BIT           NOT NULL DEFAULT 0,
                [EntiteId]        INT           NULL,
                [EntiteReference] NVARCHAR(30)  NULL,
                [LienUrl]         NVARCHAR(200) NULL,
                [CreatedAt]       DATETIME2     NOT NULL DEFAULT GETUTCDATE()
            )
            """),

        ("devis", """
            CREATE TABLE [devis] (
                [Id]             INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                [Reference]      NVARCHAR(30)   NOT NULL,
                [ClientId]       INT            NOT NULL,
                [UtilisateurId]  INT            NOT NULL,
                [DateDevis]      DATETIME2      NOT NULL,
                [DateValidite]   DATETIME2      NULL,
                [MontantTotalHT] DECIMAL(18,2)  NOT NULL DEFAULT 0,
                [MontantTVA]     DECIMAL(18,2)  NOT NULL DEFAULT 0,
                [MontantTotal]   DECIMAL(18,2)  NOT NULL DEFAULT 0,
                [Statut]         INT            NOT NULL DEFAULT 1,
                [Notes]          NVARCHAR(1000) NULL,
                [VenteId]        INT            NULL,
                [CreatedAt]      DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
                [UpdatedAt]      DATETIME2      NULL,
                [CreatedBy]      INT            NULL,
                [UpdatedBy]      INT            NULL,
                CONSTRAINT [UQ_devis_Reference] UNIQUE ([Reference]),
                CONSTRAINT [FK_devis_clients]      FOREIGN KEY ([ClientId])     REFERENCES [clients]([Id]),
                CONSTRAINT [FK_devis_utilisateurs] FOREIGN KEY ([UtilisateurId]) REFERENCES [utilisateurs]([Id]),
                CONSTRAINT [FK_devis_ventes]       FOREIGN KEY ([VenteId])      REFERENCES [ventes]([Id])
            )
            """),

        ("lignes_devis", """
            CREATE TABLE [lignes_devis] (
                [Id]           INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                [DevisId]      INT           NOT NULL,
                [ProduitId]    INT           NOT NULL,
                [Quantite]     INT           NOT NULL,
                [PrixUnitaire] DECIMAL(18,2) NOT NULL,
                [Tva]          DECIMAL(5,2)  NOT NULL DEFAULT 20,
                CONSTRAINT [FK_lignes_devis_devis]    FOREIGN KEY ([DevisId])   REFERENCES [devis]([Id]) ON DELETE CASCADE,
                CONSTRAINT [FK_lignes_devis_produits] FOREIGN KEY ([ProduitId]) REFERENCES [produits]([Id])
            )
            """),

        ("parametres_facturation", """
            CREATE TABLE [parametres_facturation] (
                [Id]             INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                [PrefixeFacture] NVARCHAR(10) NOT NULL DEFAULT 'FAC',
                [PrefixeVente]   NVARCHAR(10) NOT NULL DEFAULT 'VNT',
                [PrefixeAchat]   NVARCHAR(10) NOT NULL DEFAULT 'ACH',
                [PrefixeProduit] NVARCHAR(10) NOT NULL DEFAULT 'PRD',
                [TvaParDefaut]   INT NOT NULL DEFAULT 20,
                [DelaiPaiement]  INT NOT NULL DEFAULT 30,
                [UpdatedAt]      DATETIME2 NOT NULL DEFAULT GETUTCDATE()
            )
            INSERT INTO [parametres_facturation] ([PrefixeFacture],[PrefixeVente],[PrefixeAchat],[PrefixeProduit],[TvaParDefaut],[DelaiPaiement],[UpdatedAt])
            VALUES ('FAC','VNT','ACH','PRD',20,30,GETUTCDATE())
            """),

        ("parametres_entreprise", """
            CREATE TABLE [parametres_entreprise] (
                [Id]           INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                [RaisonSociale] NVARCHAR(200) NOT NULL DEFAULT 'GestionCo. SARL',
                [Adresse]      NVARCHAR(500) NULL,
                [Telephone]    NVARCHAR(50)  NULL,
                [Email]        NVARCHAR(200) NULL,
                [Ice]          NVARCHAR(50)  NULL,
                [Rc]           NVARCHAR(100) NULL,
                [If]           NVARCHAR(100) NULL,
                [Patente]      NVARCHAR(100) NULL,
                [Cnss]         NVARCHAR(100) NULL,
                [Capital]      NVARCHAR(100) NULL,
                [Rib]          NVARCHAR(100) NULL,
                [Banque]       NVARCHAR(200) NULL,
                [Swift]        NVARCHAR(20)  NULL,
                [Logo]         NVARCHAR(2000) NULL,
                [UpdatedAt]    DATETIME2 NOT NULL DEFAULT GETUTCDATE()
            )
            INSERT INTO [parametres_entreprise] ([RaisonSociale],[UpdatedAt]) VALUES ('GestionCo. SARL',GETUTCDATE())
            """),
    };

    foreach (var (name, sql) in tables)
    {
        try
        {
            await db.Database.ExecuteSqlRawAsync($"""
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{name}')
                BEGIN
                    {sql}
                END
                """);
            logger.LogInformation("✅ Table '{Table}' vérifiée/créée", name);
        }
        catch (Exception ex)
        {
            logger.LogWarning("⚠️  Impossible de créer la table '{Table}' : {Msg}", name, ex.Message);
        }
    }
}

// Ajoute les colonnes manquantes sans migrations EF formelles
static async Task ApplyManualColumnsAsync(AppDbContext db, ILogger logger)
{
    var columns = new[]
    {
        ("Clients",       "SourceAcquisition", "NVARCHAR(100) NULL"),
        ("Clients",       "DelaiPaiement",     "INT NULL"),
        ("lignes_vente",  "Remise",             "DECIMAL(5,2) NOT NULL DEFAULT 0"),
        ("lignes_achat",  "Remise",             "DECIMAL(5,2) NOT NULL DEFAULT 0"),
        ("lignes_devis",  "Remise",             "DECIMAL(5,2) NOT NULL DEFAULT 0"),
        ("Produits",      "QuantiteReappro",    "INT NOT NULL DEFAULT 0"),
    };

    foreach (var (table, column, definition) in columns)
    {
        try
        {
            await db.Database.ExecuteSqlRawAsync($"""
                IF NOT EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = '{table}' AND COLUMN_NAME = '{column}'
                )
                ALTER TABLE [{table}] ADD [{column}] {definition}
                """);
        }
        catch (Exception ex)
        {
            logger.LogWarning("Impossible d'ajouter la colonne {Column} sur {Table} : {Msg}", column, table, ex.Message);
        }
    }
}
