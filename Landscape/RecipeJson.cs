using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Landscape;

// The file a recipe is kept in, its format's number before its size and its steps, so a reader of a
// later format refuses a file it cannot read rather than misreading it.
internal sealed record RecipeFile(int Format, int Columns, int Rows, float Spacing, IReadOnlyList<LandscapeStep> Steps);

// The recipe's JSON, written by the source generator so an engine that trims or compiles ahead of
// time reads a recipe with no reflection, names in camel case, enumerations by name, a vector as an
// array of its two numbers, and a property no step has refused.
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true,
    WriteIndented = true,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    Converters = [typeof(Vector2Json)])]
[JsonSerializable(typeof(RecipeFile))]
internal sealed partial class RecipeJson : JsonSerializerContext;

// A vector as [x, y], each number written as the shortest text that reads back to the same float.
internal sealed class Vector2Json : JsonConverter<Vector2>
{
    public override Vector2 Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray) throw Refused();
        var x = Number(ref reader);
        var y = Number(ref reader);
        if (!reader.Read() || reader.TokenType != JsonTokenType.EndArray) throw Refused();
        return new Vector2(x, y);

        static float Number(ref Utf8JsonReader reader) =>
            reader.Read() && reader.TokenType == JsonTokenType.Number ? reader.GetSingle() : throw Refused();

        static JsonException Refused() => new("A vector is an array of two numbers.");
    }

    public override void Write(Utf8JsonWriter writer, Vector2 value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteNumberValue(value.X);
        writer.WriteNumberValue(value.Y);
        writer.WriteEndArray();
    }
}
