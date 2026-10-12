using System;
using System.Text.Json.Serialization;

namespace NeoLauncher.Models.Services.Prism;

public class AssetInfo
{
	[JsonPropertyName("filename")]
	public required string Filename { get; set; }

	[JsonPropertyName("hash256")]
	public required string Hash256 { get; set; }

	[JsonPropertyName("size")]
	public long Size { get; set; }

	[JsonPropertyName("contentType")]
	public required string ContentType { get; set; }

	[JsonPropertyName("uploadedAt")]
	public DateTime UploadedAt { get; set; }

	[JsonPropertyName("url")]
	public required string Url { get; set; }
}
