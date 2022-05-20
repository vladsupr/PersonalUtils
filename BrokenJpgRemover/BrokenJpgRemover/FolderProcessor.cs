using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrokenJpgRemover
{
	internal class FolderProcessor
	{
		Configuration Configuration { get;init;}
		public FolderProcessor(Configuration configuration)
		{
			Configuration = configuration;
		}

		public record FolderVerificationTask(string folder);
		public void Process()
		{
			var processedFolder = new HashSet<string>();
			var verificationTasks = new Queue<FolderVerificationTask>();
			verificationTasks.Enqueue(new FolderVerificationTask(Configuration.Folder));

			while (verificationTasks.Count > 0)
			{
				var currentTask = verificationTasks.Dequeue();

				var folder = currentTask.folder;
				var fullPathFolder = Path.GetFullPath(folder);
				if (processedFolder.TryGetValue(fullPathFolder, out var actualFolder) || !Directory.Exists(fullPathFolder)) continue;

				processedFolder.Add(fullPathFolder);

				WriteConsole(@$"Processing: {folder}");

				if (Configuration.IsRecursive)
				{
					var folders = Directory.GetDirectories(fullPathFolder, "*", SearchOption.TopDirectoryOnly);
					foreach (var subFolder in folders)
						verificationTasks.Enqueue(new FolderVerificationTask(subFolder));
				}

				var files = Directory
					.GetFiles(fullPathFolder, "*", SearchOption.TopDirectoryOnly)
					.Where(f => 
							Configuration.Extensions.Any(extention => Path.GetExtension(f).ToLowerInvariant().EndsWith(extention)) &&
							File.Exists(f))
					.ToList();

				int index = 0;
				foreach (var file in files)
				{
					index++;
					try
					{
						WriteConsole(@$"Processing: {folder}: ({index} from {files.Count})");
						using var image = Image.FromFile(file);
					}
					catch (OutOfMemoryException)
					{
						if (Configuration.IsAutoDelete)
						{
							File.Delete(file);
							WriteConsole($"Deleted: {file}\n");
						}
						else
							WriteConsole($"Found: {file}\n");
					}
					
				}
			}
		}

		private void WriteConsole(string message)
		{
			var spaces = Console.WindowWidth - 1 - message.Length;
			if (spaces < 0) spaces = 0;

			Console.Write("\r" + message);
			var currentX = Console.CursorLeft;
			Console.Write(new string(' ', spaces));
			Console.CursorLeft = currentX;
		}
	}
}
