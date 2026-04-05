using ExcelDoc.Server.Data;
using ExcelDoc.Server.Security;
using ExcelDoc.Server.Options;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using ExcelDoc.Server.IoC;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' não configurada.");
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("Seção de configuração 'Jwt' não configurada.");

// Add services to the container.

builder.Services.Configure<ProcessingOptions>(builder.Configuration.GetSection(ProcessingOptions.SectionName));
builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection(StorageOptions.SectionName));
builder.Services.Configure<EncryptionOptions>(builder.Configuration.GetSection(EncryptionOptions.SectionName));
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));

builder.Services.AddDbContext<ExcelDocDbContext>(options =>
    options.UseMySQL(connectionString));

builder.Services.AddHttpClient("sap-service-layer");
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
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey))
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthRoles.Administrador, policy => policy.RequireRole(AuthRoles.Administrador));
    options.AddPolicy(AuthRoles.Usuario, policy => policy.RequireRole(AuthRoles.Usuario));
});

builder.Services.AddInfrastructureRepositories();

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

await ApplicationDbInitializer.InitializeAsync(app.Services);

app.UseDefaultFiles();
app.MapStaticAssets();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapFallbackToFile("/index.html");

app.Run();
