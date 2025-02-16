using Microsoft.OpenApi.Models;
using API_PS_SOUTENANCE.Services;  // Assurez-vous que cette ligne est présente pour le bon espace de noms

var builder = WebApplication.CreateBuilder(args);

// Correction de l'injection de `ProcedureService` avec `Scoped` (ou `Transient` si vous préférez)
builder.Services.AddScoped<ProcedureService>(provider =>
    new ProcedureService(builder.Configuration.GetConnectionString("base_ecoleConnection")));

// Ajouter les services nécessaires pour Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "API_PS_SOUTENANCE", Version = "v1" });
});

// Ajouter les services nécessaires pour les contrôleurs
builder.Services.AddControllers();  // Ajout de cette ligne pour les contrôleurs

var app = builder.Build();

// Si en environnement de développement, activer Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "API_PS_SOUTENANCE v1");
        c.RoutePrefix = string.Empty;  // Pour afficher Swagger à la racine
    });
}

app.UseHttpsRedirection();  // Assurez-vous que HTTPS est configuré
app.MapControllers();  // Cette ligne doit être après l'ajout des contrôleurs

app.Run();
