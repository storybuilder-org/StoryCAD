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
                
                // Migrate old fields to Description field if Description is empty
                if (string.IsNullOrEmpty(element.Description))
                {
                    string migratedDescription = null;
                    
                    switch (element)
                    {
                        case OverviewModel overview when !string.IsNullOrEmpty(overview.StoryIdea):
                            migratedDescription = overview.StoryIdea;
                            overview.StoryIdea = string.Empty; // Clear old field
                            break;
                            
                        case FolderModel folder when !string.IsNullOrEmpty(folder.Notes):
                            migratedDescription = folder.Notes;
                            folder.Notes = string.Empty; // Clear old field
                            break;
                            
                        case WebModel web when web.URL != null:
                            migratedDescription = web.URL.ToString();
                            // Don't clear URL as it's still used for other purposes
                            break;
                            
                        case ProblemModel problem when !string.IsNullOrEmpty(problem.StoryQuestion):
                            migratedDescription = problem.StoryQuestion;
                            problem.StoryQuestion = string.Empty; // Clear old field
                            break;
                            
                        case CharacterModel character when !string.IsNullOrEmpty(character.CharacterSketch):
                            migratedDescription = character.CharacterSketch;
                            character.CharacterSketch = string.Empty; // Clear old field
                            break;
                            
                        case SettingModel setting when !string.IsNullOrEmpty(setting.Summary):
                            migratedDescription = setting.Summary;
                            setting.Summary = string.Empty; // Clear old field
                            break;
                            
                        case SceneModel scene when !string.IsNullOrEmpty(scene.Remarks):
                            // Migrate SceneModel's Remarks (Scene Sketch) to Description
                            migratedDescription = scene.Remarks;
                            scene.Remarks = string.Empty; // Clear old field
                            break;
                    }
                    
                    if (!string.IsNullOrEmpty(migratedDescription))
                    {
                        element.Description = migratedDescription;
                    }
                }
                
                return element;
            }

            // Deserialize into the target type
            throw new JsonException($"Unsupported Type discriminator: {typeDiscriminator}");
        }
		catch (Exception ex)
		{
			Ioc.Default.GetRequiredService<ILogService>().LogException(LogLevel.Error,ex, "");
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
			Ioc.Default.GetRequiredService<ILogService>().LogException(LogLevel.Error,ex, "Failed to write back");
			throw;
		}

	}
}
