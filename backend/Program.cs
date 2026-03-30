using Microsoft.EntityFrameworkCore;
using AudioBackend.Data;
using AudioBackend.Services;

var builder = WebApplication.CreateBuilder(args);

// DbContext'i PostgreSQL ile servisler arasına ekliyoruz
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Application Services
builder.Services.AddScoped<IAudioService, AudioService>();

// Background processing queue + hosted service
builder.Services.AddSingleton<AudioProcessingQueue>();
builder.Services.AddHostedService<AudioProcessingBackgroundService>();

// HttpClientFactory — Python service ile iletişim için
builder.Services.AddHttpClient();

// CORS — Frontend'in API'ye erişimi için
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline...
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
