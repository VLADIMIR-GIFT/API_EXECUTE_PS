using System;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Hosting;
using System.Collections.Generic;

public class XmlConfigurationService
{
    private DatabaseConfigurations _configurations;

    public XmlConfigurationService(IWebHostEnvironment env)
    {
        var filePath = Path.Combine(env.ContentRootPath, "database-config.xml");

        // Vérifie si le fichier existe avant de le charger
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Le fichier de configuration XML est introuvable à l'emplacement : {filePath}");
        }

        try
        {
            XmlSerializer serializer = new XmlSerializer(typeof(DatabaseConfigurations));
            using (FileStream fileStream = new FileStream(filePath, FileMode.Open))
            {
                _configurations = (DatabaseConfigurations)serializer.Deserialize(fileStream);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors du chargement de la configuration XML : {ex.Message}");
            _configurations = new DatabaseConfigurations();
        }
    }

    public DatabaseConfiguration GetDatabaseConfiguration(string databaseAlias)
    {
        return _configurations.DatabaseList.FirstOrDefault(c => c.DatabaseAlias == databaseAlias);
    }

    public string GetConnectionString(string databaseAlias)
    {
        var config = GetDatabaseConfiguration(databaseAlias);
        return config?.ConnectionString;
    }

    public string GetDefaultDatabaseAlias()
    {
        if (_configurations.DatabaseList != null && _configurations.DatabaseList.Any())
        {
            return _configurations.DatabaseList.FirstOrDefault()?.DatabaseAlias;
        }
        else
        {
            return null;
        }
    }

    public List<DatabaseConfiguration> GetDatabaseConfigurations()
    {
        return _configurations.DatabaseList;
    }
}

[XmlRoot("DatabaseConfigurations")]
public class DatabaseConfigurations
{
    [XmlArray("DatabaseList")]
    [XmlArrayItem("DatabaseConfiguration")]
    public List<DatabaseConfiguration> DatabaseList { get; set; } = new List<DatabaseConfiguration>();
}

public class DatabaseConfiguration
{
    public string DatabaseAlias { get; set; }
    public string DatabaseType { get; set; }
    public string ConnectionString { get; set; }
}
