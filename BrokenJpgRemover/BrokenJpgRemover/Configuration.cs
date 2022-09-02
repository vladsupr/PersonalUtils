namespace BrokenJpgRemover
{
	public enum Action
	{
		Broken,
		Info,
		Dublicates
	}

	internal record Configuration
	{
		public Action Action { get; init; } = Action.Broken;
		public string Folder { get; init; } = ".";
		public string OutputFile { get; init; } = string.Empty;
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

				if (arg.StartsWith("--"))
				{
					var action = arg[2..].ToLowerInvariant();
					if (action == "info")
						configuration = configuration with { Action = Action.Info };
					else if (action == "broken")
						configuration = configuration with { Action = Action.Broken };
					else if (action == "dublicates")
						configuration = configuration with { Action = Action.Dublicates };
					else
						return null;

					continue;
				}
				else if (arg.StartsWith("("))
				{
					var lastIndex = arg.IndexOf(")");
					if (lastIndex <= 1) return null;

					var outputFile = arg[1..lastIndex];
					configuration = configuration with { OutputFile = outputFile };
				}
				else if (arg.StartsWith("-"))
				{
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
							if (boolSign != ":") return null;
							if (arg.Length == 3) return null;

							configuration = configuration with
							{
								Extensions = arg[3..].Split(",").Select(p => p.Trim().ToLowerInvariant()).ToList()
							};

							break;

						default:
							return null;
					}
				}
				else
				{
					configuration = configuration with { Folder = arg };
					continue;
				}
			}

			return configuration;
		}

		public static string GetUsageString()
		{
			return "Usage: ... [--info|--broken] [<initial folder>] [(<output file>)] [-r+|-] [-d+|-] [-e:<extentions>]\n\t-r - recursive (default: +)\n\t-d - auto delete (default: +)\n\t-e - (default jpg,jpeg,gif,bpm,png)";
		}
	}
}
