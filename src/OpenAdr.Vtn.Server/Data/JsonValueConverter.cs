using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OpenAdr.Vtn.Core;

namespace OpenAdr.Vtn.Server.Data;

/// <summary>Stores a complex DTO property (lists, nested objects) as a JSON text column.
/// SQLite has no native JSON column type and this demo has no need for querying inside
/// these columns, so a plain serialized string is the simplest storage for them.</summary>
public static class JsonValueConverter
{
    public static ValueConverter<T, string> For<T>() => new(
        v => JsonSerializer.Serialize(v, OpenAdrJson.Options),
        v => JsonSerializer.Deserialize<T>(v, OpenAdrJson.Options)!);

    public static ValueComparer<T> ComparerFor<T>() => new(
        (a, b) => JsonSerializer.Serialize(a, OpenAdrJson.Options) == JsonSerializer.Serialize(b, OpenAdrJson.Options),
        v => JsonSerializer.Serialize(v, OpenAdrJson.Options).GetHashCode(),
        v => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(v, OpenAdrJson.Options), OpenAdrJson.Options)!);

    /// <summary>Applies the JSON converter + comparer to one property in a Fluent API model builder call.</summary>
    public static PropertyBuilder<T> HasJsonConversion<T>(this PropertyBuilder<T> builder)
    {
        builder.HasConversion(For<T>());
        builder.Metadata.SetValueComparer(ComparerFor<T>());
        return builder;
    }
}
