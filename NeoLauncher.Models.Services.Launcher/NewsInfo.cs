using System;
using System.Text.Json.Serialization;

namespace NeoLauncher.Models.Services.Launcher;

public class NewsInfo
{
	[JsonPropertyName("title")]
	public required string Title { get; init; }

	[JsonPropertyName("description")]
	public required string Description { get; init; }

	[JsonPropertyName("imageUrl")]
	public required string ImageUrl { get; init; }

	[JsonPropertyName("publishedDate")]
	public required DateTime PublishedDate { get; init; }
}
