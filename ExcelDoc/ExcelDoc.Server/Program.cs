using ExcelDoc.Server.Data;
using ExcelDoc.Server.Options;
using Microsoft.EntityFrameworkCore;
using ExcelDoc.Server.IoC;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' não configurada.");

// Add services to the container.

builder.Services.Configure<ProcessingOptions>(builder.Configuration.GetSection(ProcessingOptions.SectionName));
builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection(StorageOptions.SectionName));
builder.Services.Configure<EncryptionOptions>(builder.Configuration.GetSection(EncryptionOptions.SectionName));

builder.Services.AddDbContext<ExcelDocDbContext>(options =>
    options.UseMySQL(connectionString));

builder.Services.AddHttpClient("sap-service-layer");
builder.Services.AddInfrastructureRepositories();

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseDefaultFiles();
app.MapStaticAssets();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapFallbackToFile("/index.html");

app.Run();
