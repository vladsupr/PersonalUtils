using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrokenJpgRemover
{
	internal record Configuration
	{
		public string Folder { get; init; } = ".";
		public IReadOnlyList<string> Extensions { get; init; } = new[] { "jpg", "jpeg", "png", "gif", "bpm" };
		public Boolean IsRecursive { get; init; } = true;
		public Boolean IsAutoDelete { get; init; } = true;

	}

	internal static class ConfigurationExtensions
	{
		public static Configuration? ReadConfiguration(this string[] args)
		{
			var configuration = new Configuration();

			for (int i = 0; i < args.Length; i++)
			{
				var arg = args[i];

				if (!arg.StartsWith("-"))
				{
					if (i > 0) return null;

					configuration = configuration with { Folder = arg };
					continue;
				}

				if (arg.Length < 3) return null;
				var option = arg.Substring(1, 1).ToLowerInvariant();
				var boolSign = arg.Substring(2, 1).ToLowerInvariant();

				switch (option)
				{
					case "d":
						if (boolSign == "+")
							configuration = configuration with { IsAutoDelete = true };
						else if (boolSign == "-")
							configuration = configuration with { IsAutoDelete = false };
						else return null;

						break;

					case "r":
						if (boolSign == "+")
							configuration = configuration with { IsRecursive = true };
						else if (boolSign == "-")
							configuration = configuration with { IsRecursive = false };
						else return null;

						break;

					case "e":
						if (boolSign!= ":") return null;
						if (arg.Length == 3) return null;

						configuration = configuration with
						{
							Extensions = arg.Substring(3).Split(",").Select(p => p.Trim().ToLowerInvariant()).ToList()
						};

						break;

					default:
						return null;
				}
			}

			return configuration;
		}

		public static string GetUsageString()
		{
			return "Usage: ... [<initial folder>] [-r+|-] [-d+|-] [-e:<extentions>]\n\t-r - recursive (default: +)\n\t-d - auto delete (default: +)\n\t-e - (default jpg,jpeg,gif,bpm,png)";
		}
	}
}
