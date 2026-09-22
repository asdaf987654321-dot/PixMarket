using Microsoft.EntityFrameworkCore;
using PixMarketAPI.Data;
using System.Globalization;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler =
            ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<PixContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

builder.Services.AddCors(options =>
{
    options.AddPolicy("PermitirTodo", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// El binding de formularios usa SIEMPRE la cultura invariante,
// para que los decimales se envíen con punto ("12.50").
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(CultureInfo.InvariantCulture);
    options.SupportedCultures = new[] { CultureInfo.InvariantCulture };
    options.SupportedUICultures = new[] { CultureInfo.InvariantCulture };
});

var app = builder.Build();

// Asegurar la carpeta de imágenes subidas (wwwroot/Imagenes)
var carpetaImagenes = Path.Combine(app.Environment.ContentRootPath, "wwwroot", "Imagenes");
Directory.CreateDirectory(carpetaImagenes);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRequestLocalization();

app.UseStaticFiles();

app.UseCors("PermitirTodo");

app.MapControllers();

app.Run();