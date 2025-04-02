using System;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;

public class DatabaseSettings
{
    private readonly XmlConfigurationService _configService;

    public DatabaseSettings(XmlConfigurationService configService)
    {
        _configService = configService ?? throw new ArgumentNullException(nameof(configService));
    }

    public string ConnectionString { get; private set; }

    public void SetConnectionString(EnumTypeDatabase dbType)
    {
        // Utilise directement la valeur de l'énumération comme alias
        string databaseAlias = dbType.ToString();

        // Récupérer la configuration de la base de données à partir du service XML
        var dbConfig = _configService.GetDatabaseConfiguration(databaseAlias);

        // Vérifier si la configuration a été trouvée
        if (dbConfig == null)
        {
            throw new Exception($"Configuration de base de données introuvable pour le type {dbType}.");
        }

        // Définir la chaîne de connexion
        ConnectionString = dbConfig.ConnectionString;
    }
}