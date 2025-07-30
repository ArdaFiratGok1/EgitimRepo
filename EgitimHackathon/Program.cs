using EgitimMaskotuApp.Services;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration; 

builder.Services.AddControllersWithViews();

builder.Services.AddHttpClient();

builder.Services.AddHttpClient<GeminiApiService>();
builder.Services.AddScoped<GeminiApiService>();




builder.Services.AddSingleton<YouTubeService>(sp =>
    new YouTubeService(configuration["ApiKeys:YouTube"])
);//Bu serviste sadece API anahtarý aldýðý için singleton olmasý daha mantýklý.



builder.Services.AddHttpClient<CoreApiService>((serviceProvider, client) =>
{
    // Base URL'i de burada ayarlayabiliriz, ama servisin içinde de kalabilir, þimdilik kalsýn burada

    // client.BaseAddress = new Uri("https://api.core.ac.uk/v3/");
});
builder.Services.AddScoped<CoreApiService>();


builder.Services.AddScoped<AIAgentContentService>();


builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});


var app = builder.Build();


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