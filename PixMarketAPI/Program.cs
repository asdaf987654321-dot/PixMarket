using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using PixMarketAPI.Data;
using System.Globalization;
using System.Reflection;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler =
            ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "PixMarket API",
        Version = "v1",
        Description = "API REST de Block du Booster: catálogo de cartas, usuarios, ventas e imágenes. " +
            "Interfaz interactiva (Try it out) para probar cada endpoint.",
        Contact = new OpenApiContact
        {
            Name = "Block du Booster",
            Url = new Uri("http://localhost:5029")
        },
        License = new OpenApiLicense
        {
            Name = "Uso interno / académico"
        }
    });

    // Incluye los comentarios XML de los controladores como descripciones.
    var archivoXml = Path.Combine(
        AppContext.BaseDirectory,
        $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");

    if (System.IO.File.Exists(archivoXml))
    {
        c.IncludeXmlComments(archivoXml);
    }
});

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

// Swagger habilitado SIEMPRE (no solo en Development):
// la API es de uso interno (sin autenticación) y la interfaz
// "Try it out" se usa para probar los endpoints.
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "PixMarket API v1");
    c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
    c.DisplayRequestDuration();
    c.EnableTryItOutByDefault();
});

app.UseRequestLocalization();

app.UseStaticFiles();

app.UseCors("PermitirTodo");

app.MapControllers();

app.Run();