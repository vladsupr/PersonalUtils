﻿using Microsoft.VisualBasic.FileIO;
using System.Drawing;
using System.Security.Cryptography;

namespace BrokenJpgRemover
{
	internal class FolderProcessor
	{
		Configuration Configuration { get; init; }
		public FolderProcessor(Configuration configuration)
		{
			Configuration = configuration;
		}

		public record FolderVerificationTask(string folder, int level);

		public void Process()
		{
			switch (Configuration.Action)
			{
				case Action.Broken:
					ProcessBroken();
					break;
				case Action.Info:
					ProcessInfo();
					break;
				case Action.Duplicates:
					ProcessDublicates();
					break;
				case Action.Squeeze:
					ProcessSqueeze();
					break;
			}
		}

		public int ProcessSubFolders(string rootFolder, Action<string, string, int> processFolderAction)
		{
			var foldersCount = 0;

			var processedFolder = new HashSet<string>();
			var verificationTasks = new Queue<FolderVerificationTask>();
			verificationTasks.Enqueue(new FolderVerificationTask(rootFolder, 1));

			while (verificationTasks.Count > 0)
			{
				var currentTask = verificationTasks.Dequeue();

				var folder = currentTask.folder;
				var fullPathFolder = Path.GetFullPath(folder);
				if (processedFolder.TryGetValue(fullPathFolder, out _) || !Directory.Exists(fullPathFolder)) continue;

				processedFolder.Add(fullPathFolder);
				foldersCount++;

				WriteConsole(@$"Processing: {folder}");

				if (Configuration.IsRecursive)
				{
					var folders = Directory.GetDirectories(fullPathFolder, "*", System.IO.SearchOption.TopDirectoryOnly);
					foreach (var subFolder in folders)
						verificationTasks.Enqueue(new FolderVerificationTask(subFolder, currentTask.level + 1));
				}

				processFolderAction(folder, fullPathFolder, currentTask.level);
			}

			return foldersCount;
		}

		public void ProcessSqueeze()
		{
			var rootFolder = Path.GetFullPath(Configuration.Folder);

			int folderCount = 0;
			int filesCount = 0;
			long totalSize = 0;
			var foldersCount = ProcessSubFolders(Configuration.Folder, (folder, fullPathFolder, level) =>
			{
				var fileNames = FileSystem.GetFiles(fullPathFolder).OrderBy(p => p).ToList();
				if (fileNames.Count > Configuration.Count)
				{
					var filesToLeave = new List<string>(Configuration.Count);
					for (int i = 0; i < Configuration.Count; i++)
					{
						var index = i * fileNames.Count / Configuration.Count;
						filesToLeave.Add(fileNames[index]);
					}

					int deletedFilesCount = 0;
					for (int i = 0; i < fileNames.Count; i++)
					{
						var fileName = fileNames[i];
						if (filesToLeave.IndexOf(fileName) < 0)
						{
							WriteConsole($"Deleteting [{i * 100 / fileNames.Count}%]: {fileName}");

							totalSize += FileSystem.GetFileInfo(fileName).Length;
							FileSystem.DeleteFile(fileName, showUI: UIOption.OnlyErrorDialogs, recycle: RecycleOption.SendToRecycleBin);
							deletedFilesCount++;
							filesCount++;
						}
					}

					folderCount++;
					WriteConsole($"Cleared {deletedFilesCount} files in {folder}");
					Console.WriteLine();
				}
			});

			WriteConsole(string.Empty);
			Console.WriteLine($"Removed files: {filesCount}");
			Console.WriteLine($"In folders: {folderCount}");
			Console.WriteLine($"Saves spaces: {(double)totalSize / 1024 / 1024:0.00} MB");
		}

		public record FileInfo(string fileName, string fullFileName, long fileSize);
		public record FileDublicateCandidate(long FileSize, IList<FileInfo> Files);

		public void ProcessDublicates()
		{
			var allFiles = new List<FileInfo>();

			var rootFolder = Path.GetFullPath(Configuration.Folder);
			var foldersCount = ProcessSubFolders(Configuration.Folder, (folder, fullPathFolder, level) =>
			{
				var fileNames = FileSystem.GetFiles(fullPathFolder).ToList();
				foreach (var fileName in fileNames)
				{
					var fullFileName = Path.GetFullPath(fileName);
					var fileInfo = FileSystem.GetFileInfo(fullFileName);

					var shortFileName = fullFileName.StartsWith(rootFolder)
						? fullFileName[rootFolder.Length..]
						: fileName;

					var fileRecord = new FileInfo(shortFileName, fullFileName, fileInfo.Length);
					allFiles.Add(fileRecord);
				}
			});
			WriteConsole($"Found {allFiles.Count} files in {foldersCount} folders");
			Console.WriteLine();

			WriteConsole($"Searching for candidates by size...");
			var duplicatesCandidates = allFiles
				.GroupBy(p => p.fileSize)
				.Where(p => p.Count() > 1)
				.Select(p => new FileDublicateCandidate(p.Key, p.ToList())).ToList()
				.OrderBy(p => p.Files.OrderBy(f => f.fullFileName).First().fileName).ToList();
			WriteConsole($"Found {duplicatesCandidates.Count()} candidates");
			Console.WriteLine();

			var duplicates = new List<FileDublicateCandidate>();

			long sizeSaved = 0;
			int filesDeleted = 0;

			using var sha = SHA512.Create();
			var buffer = new byte[10 * 1024 * 1024];

			for (var candidateIndex = 0; candidateIndex < duplicatesCandidates.Count; candidateIndex++)
			{
				var candidate = duplicatesCandidates[candidateIndex];

				long fileIndex = 0;
				long fileSize = candidate.FileSize;
				bool isEqual = true;

				var streams = candidate.Files.Select(fileInfo => File.OpenRead(fileInfo.fullFileName)).ToList();

				try
				{
					if (fileSize != 0)
						while (fileIndex < fileSize && isEqual)
						{
							int leftToRead = (fileSize - fileIndex) > buffer.Length ? buffer.Length : (int)(fileSize - fileIndex);

							WriteConsole($"Processing [{(double)candidateIndex * 100 / duplicatesCandidates.Count: 0.0} %]: {candidate.Files.First().fullFileName} [{(double)fileIndex * 100 / fileSize: 0.0}%]");
							var lastSha = new byte[0];
							foreach (var stream in streams)
							{
								stream.Read(buffer, 0, leftToRead);
								var shaSum = sha.ComputeHash(buffer, 0, leftToRead);

								if (lastSha.Length != 0)
								{
									if (!shaSum.SequenceEqual(lastSha))
									{
										isEqual = false;
										break;
									}
								}
								lastSha = shaSum;
							}

							fileIndex += leftToRead;
						}
				}
				finally
				{
					streams.ForEach(stream => stream.Close());
				}

				duplicates.Add(candidate);

				if (isEqual)
				{
					if (Configuration.IsAutoDelete)
					{
						foreach (var file in candidate.Files.Skip(1))
						{
							filesDeleted++;
							sizeSaved += file.fileSize;

							FileSystem.DeleteFile(file.fullFileName, showUI: UIOption.OnlyErrorDialogs, recycle: RecycleOption.SendToRecycleBin);
							WriteConsole($"Deleted: {file.fileName}");
							Console.WriteLine();
						}
					}
					else
					{
						WriteConsole($"Found dublicate: {candidate.Files.First().fullFileName}, size: {candidate.FileSize}, files: {candidate.Files.Count()}");
						Console.WriteLine();
					}
				}
			}

			WriteConsole(string.Empty);
			Console.WriteLine($"Folders: {foldersCount}");
			Console.WriteLine($"Files: {allFiles.Count}");
			Console.WriteLine($"Dublicates found: {duplicates.Count}");

			if (Configuration.IsAutoDelete)
			{
				WriteConsole(string.Empty);
				Console.WriteLine($"Files deleted: {filesDeleted}");
				Console.WriteLine($"Spaces saves: {(double)sizeSaved / 1024 / 1024: 0.0} MB");
			}

			if (duplicates.Count > 0 && !string.IsNullOrWhiteSpace(Configuration.OutputFile))
			{
				WriteConsole($"Writing report to: {Configuration.OutputFile}");
				using var stream = File.OpenWrite(Configuration.OutputFile);
				using var output = new StreamWriter(stream);

				foreach (var duplicate in duplicates)
				{
					output.Write(duplicate.Files.First().fileName);
					foreach (var file in duplicate.Files.Skip(1))
						output.Write($"\t{file.fileName}");
					output.WriteLine();
				}

				WriteConsole($"Report saved to: {Configuration.OutputFile}");
				Console.WriteLine();
			}
		}

		record FolderInfo(string folderName, int level, int files, int folders, double sizeInMB);
		public void ProcessInfo()
		{
			var folders = new List<FolderInfo>();
			var rootFullPath = Path.GetFullPath(Configuration.Folder);

			ProcessSubFolders(Configuration.Folder, (folder, fullPathFolder, level) =>
			{
				var foldersCount = FileSystem.GetDirectories(fullPathFolder).Count;
				var files = FileSystem.GetFiles(fullPathFolder);

				var filesCount = files.Count;
				var sizeInMB = files.Sum(file => (double)FileSystem.GetFileInfo(file).Length / 1024 / 1024);

				var folderName = fullPathFolder;
				if (folderName.StartsWith(rootFullPath))
					folderName = "~" + folderName[rootFullPath.Length..];

				folders.Add(new FolderInfo(folderName, level, filesCount, foldersCount, sizeInMB));
			});

			using TextWriter writer = (string.IsNullOrEmpty(Configuration.OutputFile))
				? Console.Out
				: new StreamWriter(Configuration.OutputFile);

			writer.WriteLine("Level\tFolder Name\tFiles\tFolders\tSize (mb)\tAverage (mb)");
			foreach (var folder in folders.OrderBy(folder => folder.folderName))
			{
				var averageSize = folder.files > 0 ? (folder.sizeInMB / folder.files) : 0;
				writer.WriteLine($"{folder.level}\t{folder.folderName}\t{folder.files}\t{folder.folders}\t{folder.sizeInMB}\t{averageSize}");
			}
		}

		public void ProcessBroken()
		{
			var totalCount = 0;
			var foundCount = 0;

			var lockObject = new object();

			var foldersCount = ProcessSubFolders(Configuration.Folder, (folder, fullPathFolder, level) =>
			{
				var files = Directory
					.GetFiles(fullPathFolder, "*", System.IO.SearchOption.TopDirectoryOnly)
					.Where(f =>
							Configuration.Extensions.Any(extention => Path.GetExtension(f).ToLowerInvariant().EndsWith(extention)) &&
							File.Exists(f))
					.ToList();

				int index = 0;
				Parallel.ForEach(files, file =>
				{
					Interlocked.Increment(ref index);
					Interlocked.Increment(ref totalCount);
					try
					{
						lock (lockObject)
							WriteConsole(@$"Processing: {folder}: ({index} from {files.Count})");

						using var image = Image.FromFile(file);
					}
					catch (OutOfMemoryException)
					{
						if (Configuration.IsAutoDelete)
							FileSystem.DeleteFile(file, showUI: UIOption.OnlyErrorDialogs, recycle: RecycleOption.SendToRecycleBin);
						Interlocked.Increment(ref foundCount);

						lock (lockObject)
						{
							if (Configuration.IsAutoDelete)
								WriteConsole($"Deleted: {file}");
							else
								WriteConsole($"Found: {file}");

							Console.WriteLine();
						}
					}
				});
			});

			WriteConsole(string.Empty);
			Console.WriteLine();

			Console.WriteLine($"Folders scanned: {foldersCount}");
			var deletePhrase = Configuration.IsAutoDelete ? "deleted" : "found";
			Console.WriteLine($"Files scanned: {totalCount}");
			Console.WriteLine($"Files {deletePhrase}: {foundCount}");
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
