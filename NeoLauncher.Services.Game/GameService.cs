using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NeoLauncher.Models.Services.Launcher;

namespace NeoLauncher.Services.Game;

public class GameService
{
	public List<NewsInfo> Articles { get; private set; } = new List<NewsInfo>();

	public async Task LoadAsync()
	{
		Articles = new List<NewsInfo>(2)
		{
			new NewsInfo
			{
				Title = "Season 3 Release",
				Description = "New season and improvements are now live!",
				ImageUrl = "https://gamespot.com/a/uploads/scale_super/123/1239113/3355863-fn.jpg",
				PublishedDate = DateTime.Now.AddHours(-2.0)
			},
			new NewsInfo
			{
				Title = "Milxnor comes out",
				Description = "Join us for Milxnor's special announcement!",
				ImageUrl = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcRYVjwWcucjURU7AiWxIaK5KkqLDSH9zgO9gg&s",
				PublishedDate = DateTime.Now.AddDays(-1.0)
			}
		};
	}
}
