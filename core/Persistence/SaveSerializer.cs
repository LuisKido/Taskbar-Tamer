using System.Text.Json;
using System.Text.Json.Serialization;

namespace TaskbarTamer.Core.Persistence;

/// <summary>
/// Serializa/deserializa <see cref="SaveData"/> a JSON. Las enumeraciones se escriben
/// como texto (legible y estable ante reordenaciones del enum).
/// </summary>
public static class SaveSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public static string Serialize(SaveData data) => JsonSerializer.Serialize(data, Options);

    public static SaveData Deserialize(string json)
    {
        SaveData? data = JsonSerializer.Deserialize<SaveData>(json, Options);
        return data ?? throw new InvalidDataException("El contenido del save es nulo o inválido.");
    }
}
