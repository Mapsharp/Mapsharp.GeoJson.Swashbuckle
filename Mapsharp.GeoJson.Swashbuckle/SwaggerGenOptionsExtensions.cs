using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Mapsharp.GeoJson.Swashbuckle
{
    public static class SwaggerGenOptionsExtensions
    {
        public static void AddGeoJsonSchemas(this SwaggerGenOptions options)
        {
            options.SchemaGeneratorOptions.UseOneOfForPolymorphism = true;
            options.DocumentFilter<GeoJsonDocumentFilter>(options.SchemaGeneratorOptions);
        }
    }
}