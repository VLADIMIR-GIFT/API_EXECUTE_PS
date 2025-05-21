using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using System.Threading.Tasks;
using API_PS_SOUTENANCE.Services;
using System; // Assurez-vous d'avoir ce using pour Exception, etc.
using System.IO; // Ajouté pour Path.Combine et Directory.GetCurrentDirectory

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Configuration des services
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "API_PS_SOUTENANCE", Version = "v1" });
        });

        // ====================================================================================
        // CORRECTION MAJEURE ICI : Comment enregistrer XmlConfigurationService
        // Le chemin vers votre fichier database-config.xml doit être correct et absolu ou relatif à l'exécution
        // Utilisation de Path.Combine pour construire un chemin sûr
        var configFilePath = Path.Combine(Directory.GetCurrentDirectory(), "database-config.xml");
        
        // Enregistrement de XmlConfigurationService en tant que Singleton
        builder.Services.AddSingleton(new XmlConfigurationService(configFilePath));
        // ====================================================================================

        // Enregistrement de Helper (si Helper est une classe utilitaire que vous utilisez)
        // Si Helper n'a pas de dépendances complexes, AddSingleton<Helper>() est suffisant.
        // Si Helper a un constructeur vide, vous pouvez le laisser comme ça.
        builder.Services.AddSingleton<Helper>(); // Assurez-vous que la classe Helper existe

        // Enregistrement de StoredProcedureRegistry
        // Il est important que XmlConfigurationService soit disponible pour son constructeur
        builder.Services.AddSingleton<StoredProcedureRegistry>();

        // Enregistrement de ProcedureService
        // Il a besoin de la chaîne de connexion et de XmlConfigurationService
        builder.Services.AddTransient<ProcedureService>(serviceProvider =>
        {
            var configService = serviceProvider.GetRequiredService<XmlConfigurationService>();
            string? defaultDatabaseAlias = configService.GetDefaultDatabaseAlias(); // Rendre nullable
            if (string.IsNullOrEmpty(defaultDatabaseAlias))
            {
                throw new Exception("L'alias de base de données par défaut est introuvable dans database-config.xml. Assurez-vous qu'il y a au moins une configuration de base de données.");
            }
            string? connectionString = configService.GetConnectionString(defaultDatabaseAlias); // Rendre nullable
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new Exception($"La chaîne de connexion pour l'alias '{defaultDatabaseAlias}' est introuvable dans database-config.xml.");
            }
            return new ProcedureService(connectionString, configService);
        });

        // Enregistrement de ProcedureLoader
        // Il a besoin de XmlConfigurationService, Helper et StoredProcedureRegistry
        builder.Services.AddScoped<ProcedureLoader>();

        var app = builder.Build();

        // Initialisation de StoredProcedureRegistry après que les services soient construits
        // Ceci permet de charger les procédures au démarrage de l'application
        using (var scope = app.Services.CreateScope())
        {
            var registry = scope.ServiceProvider.GetRequiredService<StoredProcedureRegistry>();
            await registry.InitializeProcedures();
        }

        // Configuration du pipeline HTTP
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
        app.UseAuthorization();
        app.MapControllers();

        // Ajout de l'écoute sur toutes les interfaces pour la compatibilité Docker
        app.Urls.Add("http://0.0.0.0:3000");

        app.Run();
    }
}