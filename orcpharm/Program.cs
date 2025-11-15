using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;
using Data;
using Middleware;
using Models;
using Microsoft.EntityFrameworkCore.Diagnostics;
using FluentValidation;
using FluentValidation.AspNetCore;
using Service.Purchasing;
using Service.Auth;
using Service.Notifications;
using Service.BatchQuality;

var builder = WebApplication.CreateBuilder(args);

//// Configurar Kestrel apenas para HTTP em desenvolvimento
//if (builder.Environment.IsDevelopment())
//{
//    builder.WebHost.ConfigureKestrel(serverOptions =>
//    {
//        serverOptions.ListenAnyIP(8080); // Apenas HTTP
//    });
//}

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddControllers();

// ADICIONAR DbContext para PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options
        .UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
        .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)));

// HttpClient
builder.Services.AddHttpClient<WhatsAppService>();

// Services
builder.Services.AddScoped<PurchaseOrderService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<WhatsAppService>();
builder.Services.AddScoped<BatchQualityService>();

// FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

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

// Adicionar Health Checks
builder.Services.AddHealthChecks();
builder.Services.AddHttpClient();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = "OrcPharm.Session";
});

// === Swagger ===
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Version = "v1",
        Title = "OrcPharm API",
        Description = "Documentação da API do projeto OrcPharm",
    });

    // Header: X-API-KEY
    c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Name = "X-API-KEY",
        Description = "Cole aqui sua X-API-KEY"
    });

    // Header: X-SESSION-TOKEN
    c.AddSecurityDefinition("SessionToken", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Name = "X-SESSION-TOKEN",
        Description = "Token de sessão retornado no login"
    });

    // Exigir ambos por padrão (para rotas /api)
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
});

var app = builder.Build();

// Seed data - Criar AccessLevel padrão com proteção
try
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Testar conexão primeiro
        if (await db.Database.CanConnectAsync())
        {
            if (!await db.AccessLevels.AnyAsync())
            {
                var accessLevel = new AccessLevel
                {
                    Id = Guid.NewGuid(),
                    Name = "Farmácia",
                    Description = "Nível de acesso para farmácias de manipulação",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                db.AccessLevels.Add(accessLevel);
                await db.SaveChangesAsync();
                Console.WriteLine($"✅ AccessLevel criado: {accessLevel.Id}");
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
    // Continua a execução mesmo se o seed falhar
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection(); // Apenas em produção
}

// Health Check Endpoint - IMPORTANTE para o Traefik!
app.MapHealthChecks("/health");

// Habilitar Swagger
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "OrcPharm API v1");
    options.RoutePrefix = "swagger";
});

app.UseCors();
app.UseRouting();
app.UseSession();

app.UseEmployeeAuth();      
app.UseAuthorization();    

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();