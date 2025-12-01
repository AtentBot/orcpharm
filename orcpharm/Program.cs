using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;
using Data;
using Middleware;
using Models.Pharmacy;
using Models;
using Microsoft.EntityFrameworkCore.Diagnostics;
using FluentValidation;
using FluentValidation.AspNetCore;
using Service.Purchasing;
using Service.Auth;
using Service.Notifications;
using Service;
using Service.Formulas;
using Validators.Formulas;
using Service.BatchQuality;
using Microsoft.AspNetCore.Authentication.Cookies;
using Extensions;
using Isopoh.Cryptography.Argon2;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddControllers();
builder.Services.AddScoped<FormulaService>();
builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<PrescriptionService>();
builder.Services.AddScoped<SaleService>();
builder.Services.AddScoped<SngpcService>();
builder.Services.AddScoped<LabelService>();
builder.Services.AddManipulationServices();
builder.Services.AddHttpContextAccessor();

// ADICIONAR DbContext para PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorCodesToAdd: null
        );

        npgsqlOptions.CommandTimeout(60);
        npgsqlOptions.MigrationsAssembly("orcpharm");
    });

    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }

    options.ConfigureWarnings(w =>
        w.Ignore(RelationalEventId.PendingModelChangesWarning));
});

// HttpClient
builder.Services.AddHttpClient<WhatsAppService>();

// Services
builder.Services.AddScoped<PurchaseOrderService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<WhatsAppService>();
builder.Services.AddScoped<BatchQualityService>();
builder.Services.AddManipulationServices();
builder.Services.AddScoped<Service.Prescriptions.PrescriptionQuoteService>();
builder.Services.AddScoped<Service.Prescriptions.QuoteWhatsAppService>();
builder.Services.AddScoped<Service.Prescriptions.OpenAIPrescriptionParserService>();
builder.Services.AddScoped<Service.Prescriptions.PrescriptionWorkflowService>();
builder.Services.AddScoped<Service.IngredientMatcherService>();

// FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Health Checks
builder.Services.AddHealthChecks();
builder.Services.AddHttpClient();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = "OrcPharm.Session";
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Version = "v1",
        Title = "OrcPharm API",
        Description = "Documentação da API do projeto OrcPharm",
    });

    c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Name = "X-API-KEY",
        Description = "Cole aqui sua X-API-KEY"
    });

    c.AddSecurityDefinition("SessionToken", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Name = "X-SESSION-TOKEN",
        Description = "Token de sessão retornado no login"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "ApiKey" }
            },
            Array.Empty<string>()
        },
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "SessionToken" }
            },
            Array.Empty<string>()
        }
    });

    c.DocInclusionPredicate((docName, apiDesc) =>
    {
        return apiDesc.RelativePath?.StartsWith("api/", StringComparison.OrdinalIgnoreCase) == true;
    });
});

var app = builder.Build();

// Seed data
try
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (await db.Database.CanConnectAsync())
        {
            // ══════════════════════════════════════════════════════════
            // SEED ACCESS LEVEL (para Establishments)
            // ══════════════════════════════════════════════════════════
            if (!await db.AccessLevels.AnyAsync())
            {
                db.AccessLevels.Add(new AccessLevel
                {
                    Id = Guid.NewGuid(),
                    Code = "FARM",
                    Name = "Farmácia",
                    Description = "Nível de acesso para farmácias de manipulação",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
                await db.SaveChangesAsync();
                Console.WriteLine("✅ AccessLevel 'FARM' criado");
            }

            // ══════════════════════════════════════════════════════════
            // SEED SAAS ADMIN (Primeiro Administrador)
            // ══════════════════════════════════════════════════════════
            if (!await db.SaasAdmins.AnyAsync())
            {
                var senhaAdmin = "OrcPharm@2024"; // ⚠️ TROQUE EM PRODUÇÃO!
                var hashSenha = Argon2.Hash(senhaAdmin);

                db.SaasAdmins.Add(new SaasAdmin
                {
                    Id = Guid.Parse("a0000000-0000-0000-0000-000000000001"),
                    FullName = "Douglas - Administrador",
                    Email = "admin@orcpharm.com.br",
                    PasswordHash = hashSenha,
                    PasswordAlgorithm = "argon2id-v1",
                    Role = "SUPER_ADMIN",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
                await db.SaveChangesAsync();

                Console.WriteLine("═══════════════════════════════════════════════════════════");
                Console.WriteLine("✅ SAAS ADMIN CRIADO!");
                Console.WriteLine("═══════════════════════════════════════════════════════════");
                Console.WriteLine($"   Email: admin@orcpharm.com.br");
                Console.WriteLine($"   Senha: {senhaAdmin}");
                Console.WriteLine($"   URL:   /admin/login");
                Console.WriteLine("═══════════════════════════════════════════════════════════");
                Console.WriteLine("   ⚠️  TROQUE A SENHA APÓS O PRIMEIRO LOGIN!");
                Console.WriteLine("═══════════════════════════════════════════════════════════");
            }
        }
        else
        {
            Console.WriteLine("⚠️ Banco de dados não disponível. Continuando sem seed data.");
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"⚠️ Erro ao criar seed data: {ex.Message}");
}

// Configure pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

// Health Check
app.MapHealthChecks("/health");

// Swagger
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "OrcPharm API v1");
    options.RoutePrefix = "swagger";
});

app.UseCors();
app.UseRouting();
app.UseSession();

app.UseAuthentication();
app.UseAdminAuth();           
app.UseEmployeeAuth();
app.UseSubscriptionLimits();  
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapControllerRoute(
    name: "manipulacoes",
    pattern: "Manipulacoes/{action=Index}/{id?}",
    defaults: new { controller = "Manipulacoes" });

app.Run();