using iMMMporter.Services;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;

// Create the WebApplicationBuilder
var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel for larger request sizes
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = int.MaxValue; // Unlimited or very large limit
});

// Configure form options
builder.Services.Configure<FormOptions>(options =>
{
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartBodyLengthLimit = long.MaxValue; // Set to a large value
    options.MultipartHeadersLengthLimit = int.MaxValue;
});

// Configure IIS options
builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = int.MaxValue; // Set to a large value or null for unlimited
});

// Add services to the container
builder.Services.AddControllersWithViews();

// Register HttpClient
builder.Services.AddHttpClient<DynamicsService>();

// Register application services
builder.Services.AddScoped<CsvParserService>();
builder.Services.AddScoped<DynamicsService>();

// Build the app
var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Run the app
app.Run();