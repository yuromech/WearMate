using DotNetEnv;
using WearMate.OrderAPI.Data;
using WearMate.OrderAPI.Services;

var envPath = Path.Combine(Directory.GetCurrentDirectory(), "..", ".env");
if (File.Exists(envPath))
{
    Env.Load(envPath);
    Console.WriteLine($"Loaded .env from: {envPath}");
}

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

var port = Environment.GetEnvironmentVariable("MS_ORDER") ?? "http://localhost:6003";
builder.WebHost.UseUrls(port);

Console.WriteLine($"OrderAPI will run on: {port}");

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<SupabaseClient>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<PaymentService>();

// HttpClient for other APIs
builder.Services.AddHttpClient("ProductAPI", client =>
{
    var productUrl = Environment.GetEnvironmentVariable("MS_PRODUCT") ?? "http://localhost:6001";
    client.BaseAddress = new Uri(productUrl);
});

builder.Services.AddHttpClient("InventoryAPI", client =>
{
    var inventoryUrl = Environment.GetEnvironmentVariable("MS_INVENTORY") ?? "http://localhost:6002";
    client.BaseAddress = new Uri(inventoryUrl);
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/health", () => new
{
    status = "healthy",
    service = "OrderAPI",
    timestamp = DateTime.UtcNow
});

Console.WriteLine("OrderAPI started!");
Console.WriteLine($"Swagger: {port}/swagger");

app.Run();
