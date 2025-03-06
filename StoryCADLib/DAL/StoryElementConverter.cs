using System.Text.Json;
using System.Text.Json.Serialization;
using LogLevel = StoryCAD.Services.Logging.LogLevel;

namespace StoryCAD.DAL;

/// <summary>
/// Custom converter for StoryElement with Type discriminator.
/// </summary>
public class StoryElementConverter : JsonConverter<StoryElement>
{
	public override StoryElement Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		try
		{
			// Load the JSON document
			using var jsonDoc = JsonDocument.ParseValue(ref reader);
			var jsonObject = jsonDoc.RootElement;

			// Check if the Type property exists
			if (!jsonObject.TryGetProperty("Type", out var typeProperty))
			{
				throw new JsonException("Missing Type discriminator.");
			}

			// Get the Type discriminator value
			string typeDiscriminator = typeProperty.GetString();
			if (string.IsNullOrEmpty(typeDiscriminator))
			{
				throw new JsonException("Invalid or empty Type discriminator.");
			}

			// Determine the target type based on the discriminator
			Type targetType = typeDiscriminator switch
			{
				"StoryOverview" => typeof(OverviewModel),
				"Problem" => typeof(ProblemModel),
				"Character" => typeof(CharacterModel),
				"Setting" => typeof(SettingModel),
				"Scene" => typeof(SceneModel),
				"Folder" => typeof(FolderModel),
				"Section" => typeof(FolderModel),
				"Web" => typeof(WebModel),
				"Notes" => typeof(FolderModel),
				"TrashCan" => typeof(TrashCanModel),
				_ => null // Handle unknown or unsupported types
			};

            if (targetType != null)
            {
                var element = (StoryElement)jsonObject.Deserialize(targetType, options);
                return element;
            }

            // Deserialize into the target type
            throw new JsonException($"Unsupported Type discriminator: {typeDiscriminator}");
        }
		catch (Exception ex)
		{
			Ioc.Default.GetRequiredService<LogService>().LogException(LogLevel.Error,ex, "");
			throw;
		}
	}
	public override void Write(Utf8JsonWriter writer, StoryElement value, JsonSerializerOptions options)
	{
		try
		{
			// Determine the Type discriminator based on the runtime type
			string typeDiscriminator = value switch
			{
				OverviewModel _ => "StoryOverview",
				ProblemModel _ => "Problem",
				CharacterModel _ => "Character",
				SettingModel _ => "Setting",
				SceneModel _ => "Scene",
				FolderModel { ElementType: StoryItemType.Notes } => "Notes",
				FolderModel { ElementType: StoryItemType.Folder } => "Folder",
				FolderModel { ElementType: StoryItemType.Section } => "Section",
				WebModel _ => "Web",
				TrashCanModel _ => "TrashCan",
				_ => "Unknown"
			};

			// Create a copy of the object to include the Type discriminator
            using var jsonDoc = JsonDocument.Parse(JsonSerializer.Serialize(value, value.GetType(), options));
            writer.WriteStartObject();

            // Write the Type discriminator
            writer.WriteString("Type", typeDiscriminator);

            // Write all other properties
            foreach (var property in jsonDoc.RootElement.EnumerateObject())
            {
                if (property.NameEquals("Type"))
                    continue; // Skip existing Type property if any

                property.WriteTo(writer);
            }

            writer.WriteEndObject();
        }
		catch (Exception ex)
		{
			Ioc.Default.GetRequiredService<LogService>().LogException(LogLevel.Error,ex, "Failed to write back");
			throw;
		}

	}
}
