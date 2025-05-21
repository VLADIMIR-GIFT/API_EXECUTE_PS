using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq; // <-- Ceci est ce qui est nécessaire pour XDocument
using Microsoft.Data.SqlClient;

public class XmlConfigurationService
{
    private XDocument _document;
    private string _configFilePath;

    public XmlConfigurationService(string configFilePath)
    {
        _configFilePath = configFilePath;
        LoadConfiguration();
    }

    private void LoadConfiguration()
    {
        _document = XDocument.Load(_configFilePath);
    }

    public DatabaseConfiguration GetDatabaseConfiguration(string databaseAlias)
    {
        var configElement = _document.Descendants("DatabaseConfiguration")
                                     .FirstOrDefault(e => e.Element("DatabaseAlias")?.Value == databaseAlias);

        if (configElement != null)
        {
            return new DatabaseConfiguration
            {
                DatabaseAlias = configElement.Element("DatabaseAlias")?.Value,
                DatabaseType = configElement.Element("DatabaseType")?.Value,
                ConnectionString = configElement.Element("ConnectionString")?.Value
            };
        }
        return null;
    }

    // NOUVELLE MÉTHODE À AJOUTER
    public string GetConnectionString(string databaseAlias)
    {
        return GetDatabaseConfiguration(databaseAlias)?.ConnectionString;
    }

    // NOUVELLE MÉTHODE À AJOUTER
    public string GetDatabaseNameFromConnectionString(string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            return null;
        }

        try
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            return builder.InitialCatalog; // C'est la propriété 'Database' dans la chaîne de connexion
        }
        catch (ArgumentException)
        {
            // Gérer les chaînes de connexion invalides si nécessaire
            return null;
        }
    }

    // NOUVELLE MÉTHODE À AJOUTER
    public string GetDefaultDatabaseAlias()
    {
        // Supposons que le premier alias est le défaut si non spécifié
        return _document.Descendants("DatabaseConfiguration")
                       .FirstOrDefault()
                       ?.Element("DatabaseAlias")?.Value;
    }
}

// Assurez-vous que cette classe existe. Si elle est déjà définie ailleurs, ne la copiez pas deux fois.
public class DatabaseConfiguration
{
    public string DatabaseAlias { get; set; }
    public string DatabaseType { get; set; }
    public string ConnectionString { get; set; }
}