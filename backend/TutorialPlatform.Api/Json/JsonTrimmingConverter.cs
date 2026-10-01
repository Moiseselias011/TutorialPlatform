using System.Text.Json;
using System.Text.Json.Serialization;

namespace TutorialPlatform.Api.Json;

/// <summary>
/// Convierte todo string del body JSON eliminando espacios al inicio y al final.
///
/// Se registra a nivel global para que el recortado ocurra durante la
/// DESERIALIZACIÓN, es decir ANTES de que ASP.NET ejecute las validaciones
/// ([Required], [MinLength], ...). Así "   " no puede colarse como un título,
/// una descripción, un comentario o el nombre de una lista: se convierte en ""
/// y [Required]/[MinLength] lo rechazan con un 400.
///
/// Esto mantiene cliente y servidor idénticos: el frontend siempre hace
/// .trim() antes de enviar, y ahora el backend hace exactamente lo mismo.
/// </summary>
public sealed class JsonTrimmingConverter : JsonConverter<string>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // JSON null -> null (los campos string? lo siguen siendo)
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        var value = reader.GetString();
        return value is null ? null : value.Trim();
    }

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
        // writer.WriteStringValue evita la recursión que causaría llamar a
        // JsonSerializer.Serialize con este mismo converter registrado.
        => writer.WriteStringValue(value);
}
