using Microsoft.OpenApi.Models;
using API_PS_SOUTENANCE.Services;
using Microsoft.AspNetCore.Hosting;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddTransient<ProcedureLoader>();

// Enregistrement de XmlConfigurationService
builder.Services.AddSingleton(provider =>
{
    var env = provider.GetRequiredService<IWebHostEnvironment>();
    return new XmlConfigurationService(env);
});

// Enregistrement de Helper
builder.Services.AddSingleton<Helper>();

// Correction de l'injection de `ProcedureService`
builder.Services.AddScoped<ProcedureService>(provider =>
    new ProcedureService(builder.Configuration.GetConnectionString("base_ecoleConnection")));

// Ajouter les services nécessaires pour Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "API_PS_SOUTENANCE", Version = "v1" });
});

// Ajouter les services nécessaires pour les contrôleurs
builder.Services.AddControllers();

var app = builder.Build();

// Si en environnement de développement, activer Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "API_PS_SOUTENANCE v1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();