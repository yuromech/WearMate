using DotNetEnv;
using WearMate.Shared.Helpers;
using WearMate.Web.ApiClients;
using WearMate.Web.Services;

var envPath = Path.Combine(Directory.GetCurrentDirectory(), "..", ".env");
if (File.Exists(envPath))
{
    Env.Load(envPath);
    Console.WriteLine($"Loaded .env from: {envPath}");
}

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

var port = Environment.GetEnvironmentVariable("MAIN_WEB") ?? "http://localhost:5050";
builder.WebHost.UseUrls(port);

Console.WriteLine($"🚀 WearMate.Web will run on: {port}");

builder.Services.AddControllersWithViews();
builder.Services.Configure<Microsoft.AspNetCore.HttpsPolicy.HttpsRedirectionOptions>(options =>
{
    options.HttpsPort = 443;
});

// AuthService
builder.Services.AddScoped<AuthService>();

// Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Initialize ImageHelper with defaults
var defaultProductImage = Environment.GetEnvironmentVariable("DEFAULT_PRODUCT_IMAGE") ?? string.Empty;
var defaultAvatarImage = Environment.GetEnvironmentVariable("DEFAULT_AVATAR_IMAGE") ?? string.Empty;
var bucket = Environment.GetEnvironmentVariable("SUPABASE_STORAGE_BUCKET") ?? "public";
var storageUrl = Environment.GetEnvironmentVariable("SUPABASE_URL") ?? throw new Exception("SUPABASE_STORAGE_URL not found");
ImageHelper.SetDefaults(storageUrl, bucket, defaultProductImage, defaultAvatarImage);

// HttpClients for APIs
var productApiUrl = Environment.GetEnvironmentVariable("MS_PRODUCT") ?? "http://localhost:6001";
var orderApiUrl = Environment.GetEnvironmentVariable("MS_ORDER") ?? "http://localhost:6003";
var chatApiUrl = Environment.GetEnvironmentVariable("MS_CHAT") ?? "http://localhost:6004";
var inventoryApiUrl = Environment.GetEnvironmentVariable("MS_INVENTORY") ?? "http://localhost:6002";

builder.Services.AddHttpClient<ProductApiClient>(client =>
{
    client.BaseAddress = new Uri(productApiUrl);
});

// Named client for compatibility with CreateClient("ProductAPI")
builder.Services.AddHttpClient("ProductAPI", client =>
{
    client.BaseAddress = new Uri(productApiUrl);
});

builder.Services.AddHttpClient<OrderApiClient>(client =>
{
    client.BaseAddress = new Uri(orderApiUrl);
});

// Named client for compatibility with CreateClient("OrderAPI")
builder.Services.AddHttpClient("OrderAPI", client =>
{
    client.BaseAddress = new Uri(orderApiUrl);
});

builder.Services.AddHttpClient<ChatApiClient>(client =>
{
    client.BaseAddress = new Uri(chatApiUrl);
});

// Named client for compatibility with CreateClient("ChatAPI")
builder.Services.AddHttpClient("ChatAPI", client =>
{
    client.BaseAddress = new Uri(chatApiUrl);
});

builder.Services.AddHttpClient<InventoryApiClient>(client =>
{
    client.BaseAddress = new Uri(inventoryApiUrl);
});

// Named client for compatibility with CreateClient("InventoryAPI")
builder.Services.AddHttpClient("InventoryAPI", client =>
{
    client.BaseAddress = new Uri(inventoryApiUrl);
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var app = builder.Build();
app.Use((context, next) =>
{
    // Cloudflare sẽ gửi header này nếu request là HTTPS
    if (context.Request.Headers.ContainsKey("CF-Visitor"))
    {
        context.Request.Scheme = "https";
    }
    return next();
});
app.Use(async (context, next) =>
{
    Console.WriteLine("=== MIDDLEWARE TEST ===");
    await next();
});

app.UseCors("AllowAll");
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

// Attribute-routed APIs (e.g., product variant proxy)
app.MapControllers();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

Console.WriteLine("✅ WearMate.Web started!");
Console.WriteLine($"📍 URL: {port}");

app.Run();
