using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Mapsharp.GeoJson.Core.Geometries;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Data;

namespace Mapsharp.GeoJson.Swashbuckle
{
    public class GeoJsonDocumentFilter : IDocumentFilter
    {
        private const string COORDINATES = nameof(COORDINATES);
        private const string GEOMETRIES = nameof(GEOMETRIES);
        private const string TYPE = nameof(TYPE);

        private readonly SchemaGeneratorOptions _schemaGeneratorOptions;

        public GeoJsonDocumentFilter(SchemaGeneratorOptions schemaGeneratorOptions)
        {
            _schemaGeneratorOptions = schemaGeneratorOptions;
        }

        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            if (!context.SchemaRepository.TryLookupByType(typeof(Position), out var positionSchema))
            {
                positionSchema = context.SchemaGenerator.GenerateSchema(typeof(Position), context.SchemaRepository);
            }
            positionSchema.MinItems = 2;
            positionSchema.MaxItems = 3;

            EnrichGeometryBaseSchema(context);
            EnrichPointSchema(context, positionSchema);
            EnrichMultiPointSchema(context, positionSchema);
            EnrichLineStringSchema(context, positionSchema);
            EnrichMultiLineStringSchema(context, positionSchema);
            EnrichPolygonSchema(context, positionSchema);
            EnrichMultiPolygonSchema(context, positionSchema);
            EnrichGeometryCollectionSchema(context);

        }

        private void EnrichGeometryBaseSchema(DocumentFilterContext context)
        {
            if (context.SchemaRepository.TryLookupByType(typeof(GeometryBase), out var reference))
            {
                var schema = context.SchemaRepository.Schemas[reference.Reference.Id];
                string typePropertyName = schema.Properties.Keys.Where(k => k.ToUpper() == TYPE).First();
                var typeSchema = schema.Properties[typePropertyName];
                typeSchema.Nullable = false;
                typeSchema.Pattern = $"{nameof(Point)}|{nameof(MultiPoint)}|{nameof(LineString)}|{nameof(MultiLineString)}|{nameof(Polygon)}|{nameof(MultiPolygon)}|{nameof(GeometryCollection)}";
            }
        }

        private void EnrichPointSchema(DocumentFilterContext context, OpenApiSchema positionSchema)
        {
            EnrichCoordinateGeoJsonSchema<Point>(context, coordinates =>
            {
                coordinates.MinItems = positionSchema.MinItems;
                coordinates.MaxItems = positionSchema.MaxItems;
            });
        }

        private void EnrichMultiPointSchema(DocumentFilterContext context, OpenApiSchema positionSchema)
        {
            EnrichCoordinateGeoJsonSchema<MultiPoint>(context, coordinates =>
            {
                coordinates.Items = positionSchema;
            });
        }

        private void EnrichLineStringSchema(DocumentFilterContext context, OpenApiSchema positionSchema)
        {
            EnrichCoordinateGeoJsonSchema<LineString>(context, coordinates =>
            {
                coordinates.MinItems = 2;
                coordinates.Items = positionSchema;
            });
        }

        private void EnrichMultiLineStringSchema(DocumentFilterContext context, OpenApiSchema positionSchema)
        {
            EnrichCoordinateGeoJsonSchema<MultiLineString>(context, coordinates =>
            {
                coordinates.MinItems = 1;
                coordinates.Items.MinItems = 2;
                coordinates.Items.Items = positionSchema;
            });
        }

        private void EnrichPolygonSchema(DocumentFilterContext context, OpenApiSchema positionSchema)
        {
            EnrichCoordinateGeoJsonSchema<Polygon>(context, coordinates =>
            {
                coordinates.MinItems = 1;
                coordinates.Items.MinItems = 1;
                coordinates.Items.Items.MinItems = 4;
                coordinates.Items.Items.Items = positionSchema;
            });
        }

        private void EnrichMultiPolygonSchema(DocumentFilterContext context, OpenApiSchema positionSchema)
        {
            EnrichCoordinateGeoJsonSchema<MultiPolygon>(context, coordinates =>
            {
                coordinates.MinItems = 1;
                coordinates.Items.MinItems = 1;
                coordinates.Items.Items.MinItems = 1;
                coordinates.Items.Items.Items.MinItems = 4;
                coordinates.Items.Items.Items.Items = positionSchema;
            });
        }

        private void EnrichCoordinateGeoJsonSchema<T>(DocumentFilterContext context, Action<OpenApiSchema> enrichCoordinates)
        {
            if (context.SchemaRepository.TryLookupByType(typeof(T), out var reference))
            {
                var schema = context.SchemaRepository.Schemas[reference.Reference.Id];

                EnrichTypeProperty<T>(context, schema);

                string coordinatesPropertyName = schema.Properties.Keys.Where(k => k.ToUpper() == COORDINATES).First();
                var coordinatesSchema = schema.Properties[coordinatesPropertyName];
                coordinatesSchema.Nullable = false;

                enrichCoordinates(coordinatesSchema);
            }
        }

        private void EnrichTypeProperty<T>(DocumentFilterContext context, OpenApiSchema schema)
        {
            string typePropertyName;

            if (_schemaGeneratorOptions.UseAllOfForInheritance || _schemaGeneratorOptions.UseOneOfForPolymorphism)
            {
                typePropertyName = context.SchemaRepository.Schemas[schema.AllOf[0].Reference.Id]
                                                           .Properties.Keys.Where(k => k.ToUpper() == TYPE).First();
                schema.AllOf.Clear();
                var newTypeSchema = new OpenApiSchema();
                newTypeSchema.Type = "string";
                schema.Properties.Add(typePropertyName, newTypeSchema);
            }

            typePropertyName = schema.Properties.Keys.Where(k => k.ToUpper() == TYPE).First();

            var typeSchema = schema.Properties[typePropertyName];
            typeSchema.Pattern = typeof(T).Name;
            typeSchema.Example = new OpenApiString(typeSchema.Pattern);
            typeSchema.Nullable = false;
        }

        private void EnrichGeometryCollectionSchema(DocumentFilterContext context)
        {
            if (context.SchemaRepository.TryLookupByType(typeof(GeometryCollection), out var reference))
            {
                var schema = context.SchemaRepository.Schemas[reference.Reference.Id];
                EnrichTypeProperty<GeometryCollection>(context, schema);

                var geometriesPropertyName = schema.Properties.Keys.Where(k => k.ToUpper() == GEOMETRIES).First();
                var geometriesProperty = schema.Properties[geometriesPropertyName];

                geometriesProperty.Nullable = false;

                var selfReference = geometriesProperty.Items.OneOf.Where(r => r.Reference.Id == reference.Reference.Id).First();
                geometriesProperty.Items.OneOf.Remove(selfReference); // remove self reference... swashbuckle does not approve.           
            }
        }
    }

}