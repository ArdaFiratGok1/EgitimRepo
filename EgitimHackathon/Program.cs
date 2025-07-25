
/*using EgitimMaskotuApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// HttpClient'ý register edin
builder.Services.AddHttpClient();

// Mevcut Gemini API Service
builder.Services.AddHttpClient<GeminiApiService>();
builder.Services.AddScoped<GeminiApiService>();

// YouTube API Service'i ekle
builder.Services.AddScoped<YouTubeService>(provider =>
{
    var configuration = provider.GetRequiredService<IConfiguration>();
    var apiKey = configuration["ApiKeys:YouTube"];
    return new YouTubeService(apiKey);
});

// Core API Service'i ekle
builder.Services.AddScoped<CoreApiService>(provider =>
{
    var configuration = provider.GetRequiredService<IConfiguration>();
    var httpClient = provider.GetRequiredService<HttpClient>();
    var apiKey = configuration["ApiKeys:CoreApi"];
    return new CoreApiService(apiKey, httpClient);
});

// AI Agent Content Service - tüm servisleri birleþtiren ana servis
builder.Services.AddScoped<AIAgentContentService>();

// Session desteði ekle (münazara ve öðretim verileri için)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();
*/


using EgitimMaskotuApp.Services;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration; // Configuration'ý baþta bir kere alalým

// Add services to the container.
builder.Services.AddControllersWithViews();

// 1. HttpClient Factory'yi ekle (Bu en iyi pratiktir)
builder.Services.AddHttpClient();

// 2. Gemini servisini AddHttpClient ile kaydet (Bu zaten doðru)
builder.Services.AddHttpClient<GeminiApiService>();
builder.Services.AddScoped<GeminiApiService>();


// 3. YouTube servisini AddSingleton olarak kaydet (Daha performanslý)
// Bu servis sadece API anahtarý aldýðý için singleton olmasý daha mantýklý.
builder.Services.AddSingleton<YouTubeService>(sp =>
    new YouTubeService(configuration["ApiKeys:YouTube"])
);


// 4. Core API servisini AddHttpClient ile kaydet (EN ÖNEMLÝ DEÐÝÞÝKLÝK)
// Bu yöntem, CoreApiService'e özel bir HttpClient örneði verilmesini saðlar.
builder.Services.AddHttpClient<CoreApiService>((serviceProvider, client) =>
{
    // Base URL'i de burada ayarlayabiliriz, ama servisin içinde de kalabilir.
    // client.BaseAddress = new Uri("https://api.core.ac.uk/v3/");
});
builder.Services.AddScoped<CoreApiService>();


// 5. Ana içerik servisini kaydet
builder.Services.AddScoped<AIAgentContentService>();


// 6. Session desteði (Bu kýsým zaten doðru)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});


var app = builder.Build();


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();