using System.Text.Json;
using System.Text.Json.Serialization;

namespace NcPasswords.Core.Api;

public sealed record Tag
{
    [JsonPropertyName("id")] public string Id { get; init; } = "";
    [JsonPropertyName("label")] public string Label { get; init; } = "";
    [JsonPropertyName("color")] public string Color { get; init; } = "";
    [JsonPropertyName("favorite")] [JsonConverter(typeof(LenientBoolConverter))] public bool Favorite { get; init; }
}

/// <summary>
/// The API returns either an array of tag ids (default) or an array of full tag objects
/// (when "tags" is included in the requested "details"). We always request the expanded
/// form, but tolerate the plain-id form too.
/// </summary>
public sealed class TagListConverter : JsonConverter<List<Tag>>
{
    public override List<Tag> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var result = new List<Tag>();
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            reader.Skip();
            return result;
        }

        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                result.Add(new Tag { Id = reader.GetString() ?? "" });
            }
            else
            {
                var tag = JsonSerializer.Deserialize<Tag>(ref reader, options);
                if (tag is not null)
                {
                    result.Add(tag);
                }
            }
        }

        return result;
    }

    public override void Write(Utf8JsonWriter writer, List<Tag> value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, options);
    }
}

/// <summary>
/// The API returns either a plain folder id (default) or a full nested folder object
/// (when "folder" is included in the requested "details"). We only ever need the id here
/// - the full folder objects come from the separate folder/list call - but tolerate both shapes.
/// </summary>
public sealed class FolderIdConverter : JsonConverter<string>
{
    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            return reader.GetString() ?? "";
        }

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            return doc.RootElement.TryGetProperty("id", out var idProp) ? idProp.GetString() ?? "" : "";
        }

        reader.Skip();
        return "";
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value);
}

/// <summary>
/// customFields is normally a JSON-encoded string, but defensively accept an already-parsed
/// object/array too (captured as raw text) so a server-side shape change doesn't blow up the
/// whole password list.
/// </summary>
public sealed class RawJsonTextConverter : JsonConverter<string>
{
    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            return reader.GetString() ?? "";
        }

        using var doc = JsonDocument.ParseValue(ref reader);
        return doc.RootElement.GetRawText();
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value);
}

/// <summary>
/// Nextcloud's OCS-based APIs are known to serialize numbers and booleans as JSON strings in
/// some responses. Accept either shape instead of throwing on a plain type mismatch.
/// </summary>
public sealed class LenientLongConverter : JsonConverter<long>
{
    public override long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.String
            ? long.TryParse(reader.GetString(), out var parsed) ? parsed : 0
            : reader.GetInt64();

    public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options) =>
        writer.WriteNumberValue(value);
}

public sealed class LenientBoolConverter : JsonConverter<bool>
{
    public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType switch
        {
            JsonTokenType.True => true,
            JsonTokenType.False => false,
            JsonTokenType.Number => reader.GetInt32() != 0,
            JsonTokenType.String => reader.GetString() is "1" or "true",
            _ => false,
        };

    public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options) =>
        writer.WriteBooleanValue(value);
}

public sealed record PasswordEntry
{
    [JsonPropertyName("id")] public string Id { get; init; } = "";
    [JsonPropertyName("label")] public string Label { get; init; } = "";
    [JsonPropertyName("username")] public string Username { get; init; } = "";
    [JsonPropertyName("password")] public string Password { get; init; } = "";
    [JsonPropertyName("url")] public string Url { get; init; } = "";
    [JsonPropertyName("notes")] public string Notes { get; init; } = "";

    [JsonPropertyName("customFields")]
    [JsonConverter(typeof(RawJsonTextConverter))]
    public string CustomFields { get; init; } = "";

    [JsonPropertyName("folder")]
    [JsonConverter(typeof(FolderIdConverter))]
    public string Folder { get; init; } = "";

    [JsonPropertyName("tags")]
    [JsonConverter(typeof(TagListConverter))]
    public List<Tag> Tags { get; init; } = new();

    [JsonPropertyName("favorite")] [JsonConverter(typeof(LenientBoolConverter))] public bool Favorite { get; init; }
    [JsonPropertyName("trashed")] [JsonConverter(typeof(LenientBoolConverter))] public bool Trashed { get; init; }
    [JsonPropertyName("hidden")] [JsonConverter(typeof(LenientBoolConverter))] public bool Hidden { get; init; }
    [JsonPropertyName("created")] [JsonConverter(typeof(LenientLongConverter))] public long Created { get; init; }
    [JsonPropertyName("updated")] [JsonConverter(typeof(LenientLongConverter))] public long Updated { get; init; }
    [JsonPropertyName("edited")] [JsonConverter(typeof(LenientLongConverter))] public long Edited { get; init; }

    public DateTimeOffset CreatedAt => DateTimeOffset.FromUnixTimeSeconds(Created);
    public DateTimeOffset UpdatedAt => DateTimeOffset.FromUnixTimeSeconds(Updated);
    public DateTimeOffset EditedAt => DateTimeOffset.FromUnixTimeSeconds(Edited);
}

public sealed record Folder
{
    public const string RootId = "00000000-0000-0000-0000-000000000000";

    [JsonPropertyName("id")] public string Id { get; init; } = "";
    [JsonPropertyName("label")] public string Label { get; init; } = "";
    [JsonPropertyName("parent")] public string Parent { get; init; } = RootId;
    [JsonPropertyName("favorite")] [JsonConverter(typeof(LenientBoolConverter))] public bool Favorite { get; init; }
    [JsonPropertyName("trashed")] [JsonConverter(typeof(LenientBoolConverter))] public bool Trashed { get; init; }
    [JsonPropertyName("created")] [JsonConverter(typeof(LenientLongConverter))] public long Created { get; init; }
    [JsonPropertyName("updated")] [JsonConverter(typeof(LenientLongConverter))] public long Updated { get; init; }
}

/// <summary>
/// A single custom field parsed out of <see cref="PasswordEntry.CustomFields"/>, which the
/// API transmits as a JSON-encoded string rather than a nested object.
/// </summary>
public sealed record CustomField(string Label, string Type, string Value);

public static class CustomFieldParser
{
    public static IReadOnlyList<CustomField> Parse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return [];
        }

        try
        {
            using var doc = JsonDocument.Parse(raw);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var fields = new List<CustomField>();
            foreach (var element in doc.RootElement.EnumerateArray())
            {
                var label = element.TryGetProperty("label", out var l) ? l.GetString() ?? "" : "";
                var type = element.TryGetProperty("type", out var t) ? t.GetString() ?? "" : "";
                var value = element.TryGetProperty("value", out var v) ? v.GetString() ?? "" : "";
                fields.Add(new CustomField(label, type, value));
            }

            return fields;
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
