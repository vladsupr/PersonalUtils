using System.Text.Json;

namespace BrokenJpgRemover
{
	public static class Program
	{
		public static void Main(string[] args)
		{
			var configuration = args.ReadConfiguration();
			if (configuration == null)
			{
				Console.WriteLine(ConfigurationExtensions.GetUsageString());
				return;
			}

			Console.WriteLine(JsonSerializer.Serialize(configuration, new JsonSerializerOptions { WriteIndented = true }));

			var folderProcessor = new FolderProcessor(configuration);
			folderProcessor.Process();
		}
	}
}
