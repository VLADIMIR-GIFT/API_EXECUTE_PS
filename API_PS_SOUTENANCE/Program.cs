using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using System.Threading.Tasks;

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
        builder.Services.AddSingleton<XmlConfigurationService>();
        builder.Services.AddSingleton<Helper>();
        builder.Services.AddSingleton<StoredProcedureRegistry>();
        builder.Services.AddScoped<ProcedureLoader>();

        var app = builder.Build();

        // Initialisation de StoredProcedureRegistry
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

        // 🚀 Ajout de l'écoute sur toutes les interfaces (nécessaire pour Docker)
        app.Urls.Add("http://0.0.0.0:3000");

        app.Run();
    }
}
