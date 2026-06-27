using Microsoft.Extensions.Configuration;

namespace IntegrationTests.Configuration;

public static class ConfigurationExtensions
{
    public static T BindRequiredSection<T>(this IConfiguration configuration, string sectionName)
        where T : class, new()
    {
        var section = configuration.GetSection(sectionName);
        if (!section.Exists())
        {
            throw new InvalidOperationException($"Missing required configuration section: {sectionName}");
        }

        var instance = new T();
        section.Bind(instance);
        return instance;
    }
}