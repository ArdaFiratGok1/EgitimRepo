
/*using EgitimMaskotuApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// HttpClient'� register edin
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

// AI Agent Content Service - t�m servisleri birle�tiren ana servis
builder.Services.AddScoped<AIAgentContentService>();

// Session deste�i ekle (m�nazara ve ��retim verileri i�in)
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
var configuration = builder.Configuration; // Configuration'� ba�ta bir kere alal�m

// Add services to the container.
builder.Services.AddControllersWithViews();

// 1. HttpClient Factory'yi ekle (Bu en iyi pratiktir)
builder.Services.AddHttpClient();

// 2. Gemini servisini AddHttpClient ile kaydet (Bu zaten do�ru)
builder.Services.AddHttpClient<GeminiApiService>();
builder.Services.AddScoped<GeminiApiService>();


// 3. YouTube servisini AddSingleton olarak kaydet (Daha performansl�)
// Bu servis sadece API anahtar� ald��� i�in singleton olmas� daha mant�kl�.
builder.Services.AddSingleton<YouTubeService>(sp =>
    new YouTubeService(configuration["ApiKeys:YouTube"])
);


// 4. Core API servisini AddHttpClient ile kaydet (EN �NEML� DE����KL�K)
// Bu y�ntem, CoreApiService'e �zel bir HttpClient �rne�i verilmesini sa�lar.
builder.Services.AddHttpClient<CoreApiService>((serviceProvider, client) =>
{
    // Base URL'i de burada ayarlayabiliriz, ama servisin i�inde de kalabilir.
    // client.BaseAddress = new Uri("https://api.core.ac.uk/v3/");
});
builder.Services.AddScoped<CoreApiService>();


// 5. Ana i�erik servisini kaydet
builder.Services.AddScoped<AIAgentContentService>();


// 6. Session deste�i (Bu k�s�m zaten do�ru)
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