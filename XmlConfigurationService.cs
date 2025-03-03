using System.Xml.Serialization;
using System.IO;
using System;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using System.Collections.Generic;

public class XmlConfigurationService
{
    private DatabaseConfigurations _configurations;

    public XmlConfigurationService(IWebHostEnvironment env)
    {
        var filePath = Path.Combine(env.ContentRootPath, "database-config.xml");

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
    // Ajoutez d'autres propriétés si nécessaire
}