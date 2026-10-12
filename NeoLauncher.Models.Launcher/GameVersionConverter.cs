using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NeoLauncher.Models.Launcher;

public class GameVersionConverter : JsonConverter<GameVersion>
{
	public override GameVersion? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		string text = reader.GetString();
		if (text != null)
		{
			return new GameVersion(text);
		}
		return null;
	}

	public override void Write(Utf8JsonWriter writer, GameVersion value, JsonSerializerOptions options)
	{
		writer.WriteStringValue(value.ToString());
	}
}
