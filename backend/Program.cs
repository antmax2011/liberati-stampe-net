using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Sessioni in memoria (per ora)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(24);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

// CORS per Shopify
builder.Services.AddCors(options =>
{
    options.AddPolicy("ShopifyPolicy", policy =>
    {
        policy.WithOrigins(
                "https://admin.shopify.com",
                "http://localhost:4200",
                "https://think-depress-candied.ngrok-free.dev"
                )
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });

});

builder.Services.AddControllers();
builder.Services.AddHttpClient();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseHttpsRedirection();
app.UseCors("ShopifyPolicy");
app.UseSession();
app.UseAuthorization();
app.MapControllers();



app.Run();
