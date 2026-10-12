using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NeoLauncher.Models.Services.Launcher;
using NeoLauncher.Services.Launcher;
using NuGet.Versioning;
using Velopack;
using Velopack.Logging;
using Velopack.Sources;

namespace NeoLauncher.Services.Updates;

public class NeoUpdateSource(LauncherService launcherService) : IUpdateSource
{
	private readonly HttpClient _httpClient = new HttpClient
	{
		Timeout = TimeSpan.FromMinutes(5L)
	};

	public async Task<VelopackAssetFeed> GetReleaseFeed(IVelopackLogger logger, string? releaseName, string channel, Guid? stagingId = null, VelopackAsset? latestLocalRelease = null)
	{
		VelopackAsset[] assets = (await launcherService.GetReleasesAsync()).Select((ReleaseInfo r) => new VelopackAsset
		{
			PackageId = r.PackageId,
			Version = SemanticVersion.Parse(r.Version),
			Type = ((!r.IsDelta) ? VelopackAssetType.Full : VelopackAssetType.Delta),
			FileName = r.DownloadUrl,
			SHA256 = r.Sha256,
			Size = r.SizeBytes
		}).ToArray();
		return new VelopackAssetFeed
		{
			Assets = assets
		};
	}

	public async Task DownloadReleaseEntry(IVelopackLogger logger, VelopackAsset releaseEntry, string localFile, Action<int> progress, CancellationToken cancelToken)
	{
		using HttpResponseMessage response = await _httpClient.GetAsync(releaseEntry.FileName, HttpCompletionOption.ResponseHeadersRead, cancelToken);
		response.EnsureSuccessStatusCode();
		long total = response.Content.Headers.ContentLength ?? releaseEntry.Size;
		await using Stream src = await response.Content.ReadAsStreamAsync(cancelToken);
		await using FileStream dst = File.Create(localFile);
		byte[] buffer = new byte[81920];
		long read = 0L;
		int lastPct = -1;
		while (true)
		{
			int num;
			int n = (num = await src.ReadAsync(buffer, cancelToken));
			if (num <= 0)
			{
				break;
			}
			await dst.WriteAsync(buffer.AsMemory(0, n), cancelToken);
			read += n;
			int num2 = (int)((total > 0) ? (read * 100 / total) : 0);
			if (num2 != lastPct)
			{
				progress(num2);
				lastPct = num2;
			}
		}
	}
}
