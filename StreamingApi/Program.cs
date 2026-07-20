using Microsoft.EntityFrameworkCore;
using StreamingApi.Data;
using StreamingApi.Middleware;
using StreamingApi.Service.Interface;
using StreamingApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IMovieService, MovieService>();
builder.Services.AddScoped<IGoogleDriveService, GoogleDriveService>();
builder.Services.AddHttpClient<IGoogleDriveService, GoogleDriveService>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowReactApp", policy =>
        {
            policy.WithOrigins("http://localhost:3000")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
    });
}

var app = builder.Build();

// Global exception handler - harus didaftarkan paling awal di pipeline
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (app.Environment.IsDevelopment())
{
    app.UseCors("AllowReactApp");
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
    app.UseDefaultFiles();
    app.UseStaticFiles();
}

app.MapControllers();

app.MapFallbackToFile("index.html");

app.Run();
