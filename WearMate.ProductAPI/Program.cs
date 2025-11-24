using DotNetEnv;
using WearMate.ProductAPI.Data;
using WearMate.ProductAPI.Services;
using WearMate.Shared.Helpers;

// Load .env from solution root
var envPath = Path.Combine(Directory.GetCurrentDirectory(), "..", ".env");
if (File.Exists(envPath))
{
    Env.Load(envPath);
    Console.WriteLine($"Loaded .env from: {envPath}");
}
else
{
    Console.WriteLine($".env not found at: {envPath}");
}

var builder = WebApplication.CreateBuilder(args);

// Add environment variables to configuration
builder.Configuration.AddEnvironmentVariables();

// Get port from env
var port = Environment.GetEnvironmentVariable("MS_PRODUCT") ?? "http://localhost:6001";
builder.WebHost.UseUrls(port);

Console.WriteLine($"ProductAPI will run on: {port}");

// Add services
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register SupabaseClient
builder.Services.AddSingleton<SupabaseClient>();

// Register Services
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<BrandService>();

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

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthorization();
app.MapControllers();

// Health check
app.MapGet("/health", () => new
{
    status = "healthy",
    service = "ProductAPI",
    timestamp = DateTime.UtcNow
});
Console.WriteLine("ProductAPI started successfully!");
Console.WriteLine($"Swagger UI: {port}/swagger");
Console.WriteLine($"Health check: {port}/health");

app.Run();