using System.Reflection;
using Mapster;

namespace Keyloop.Application.Mappings;

/// <summary>
/// Mapping convention:
/// 1. Simple 1:1 DTO maps — implement Mapster <see cref="IMapFrom{TSource}"/> on the DTO.
/// 2. Custom members — override ConfigureMapping(TypeAdapterConfig).
/// 3. Complex / multi-source maps — add an <see cref="IRegister"/> in Features/{Feature}/Mappings.
/// Never map in controllers or repositories; inject <see cref="MapsterMapper.IMapper"/> in the application layer.
/// </summary>
public static class MappingConfig
{
    public static TypeAdapterConfig Create(params Assembly[] assemblies)
    {
        var config = new TypeAdapterConfig();
        config.Default.NameMatchingStrategy(NameMatchingStrategy.Flexible);
        config.Default.PreserveReference(true);
        config.Default.IgnoreNullValues(true);

        var targetAssemblies = assemblies.Length > 0
            ? assemblies
            : [typeof(MappingConfig).Assembly];

        foreach (var assembly in targetAssemblies)
        {
            ApplyMapFrom(config, assembly);
            config.Scan(assembly);
        }

        return config;
    }

    private static void ApplyMapFrom(TypeAdapterConfig config, Assembly assembly)
    {
        var mapFromTypes = assembly.GetTypes()
            .Where(type => type is { IsAbstract: false, IsInterface: false })
            .Select(type => new
            {
                Type = type,
                Interfaces = type.GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IMapFrom<>))
            })
            .Where(x => x.Interfaces.Any());

        foreach (var mapFrom in mapFromTypes)
        {
            var instance = Activator.CreateInstance(mapFrom.Type);
            if (instance is null)
                continue;

            foreach (var @interface in mapFrom.Interfaces)
            {
                var method = @interface.GetMethod("ConfigureMapping") ?? @interface.GetMethod("Mapping");
                method?.Invoke(instance, [config]);
            }
        }
    }
}
