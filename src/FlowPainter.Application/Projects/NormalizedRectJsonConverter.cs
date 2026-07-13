using System.Text.Json;
using System.Text.Json.Serialization;
using FlowPainter.Domain.Geometry;

namespace FlowPainter.Application.Projects;

internal sealed class NormalizedRectJsonConverter : JsonConverter<NormalizedRect>
{
    public override NormalizedRect Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("A normalized rectangle must be represented by a JSON object.");
        }

        double? left = null;
        double? top = null;
        double? right = null;
        double? bottom = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("A normalized rectangle contains an invalid JSON token.");
            }

            string propertyName = reader.GetString()
                ?? throw new JsonException("A normalized rectangle contains an unnamed property.");

            if (!reader.Read())
            {
                throw new JsonException("A normalized rectangle property has no value.");
            }

            switch (propertyName)
            {
                case "left":
                    left = reader.GetDouble();
                    break;
                case "top":
                    top = reader.GetDouble();
                    break;
                case "right":
                    right = reader.GetDouble();
                    break;
                case "bottom":
                    bottom = reader.GetDouble();
                    break;
                default:
                    reader.Skip();
                    break;
            }
        }

        if (left is null || top is null || right is null || bottom is null)
        {
            throw new JsonException("A normalized rectangle must define left, top, right and bottom.");
        }

        try
        {
            return new NormalizedRect(left.Value, top.Value, right.Value, bottom.Value);
        }
        catch (ArgumentException exception)
        {
            throw new JsonException("The normalized rectangle bounds are invalid.", exception);
        }
    }

    public override void Write(
        Utf8JsonWriter writer,
        NormalizedRect value,
        JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("left", value.Left);
        writer.WriteNumber("top", value.Top);
        writer.WriteNumber("right", value.Right);
        writer.WriteNumber("bottom", value.Bottom);
        writer.WriteEndObject();
    }
}
