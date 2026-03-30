using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FortniteReplayReader;
using FortniteReplayReader.Models;
using Unreal.Core.Models.Enums;
using Unreal.Core.Exceptions;

namespace cli
{
    class Program
    {
        static readonly ReplayReader Reader = new ReplayReader();

        static readonly List<string> ReplayFiles = new List<string>();

        static void Main(string[] args)
        {
            var appDataReplayFolder = @"%LOCALAPPDATA%\FortniteGame\Saved\Demos";
            var replayFolder = Environment.ExpandEnvironmentVariables(appDataReplayFolder);
            Log($"Replay folder: {replayFolder}");

            var replayFile = GetNewestFile(replayFolder);
            PrintReplayInformationFromReplayFile(replayFile);

            using var watcher = new FileSystemWatcher(replayFolder);

            watcher.NotifyFilter = NotifyFilters.Attributes
                                 | NotifyFilters.CreationTime
                                 | NotifyFilters.DirectoryName
                                 | NotifyFilters.FileName
                                 | NotifyFilters.LastAccess
                                 | NotifyFilters.LastWrite
                                 | NotifyFilters.Security
                                 | NotifyFilters.Size;

            watcher.Changed += OnChanged;
            watcher.Error += OnError;

            watcher.Filter = "*.replay";
            watcher.EnableRaisingEvents = true;

            Console.ReadLine();
        }
        
        static void Log(Object o) {
            Console.WriteLine(o);
        }

        private static string GetNewestFile(string path)
       {
           var file = new DirectoryInfo(path).GetFiles().OrderByDescending(o => o.LastWriteTime).FirstOrDefault();
           return Path.Join(path, file.Name);
       }

        static void PrintReplayInformationFromReplayFile(string path) {
            try {
                var read = Reader.ReadReplay(path);
                PrintReplayInformationFromReplay(read);
                ReplayFiles.Clear();
                Console.WriteLine("Waiting for next game");
            } catch (IOException) {
                Log("Could not open replay");
            } catch (InvalidReplayException) {
                if (!ReplayFiles.Contains(path)) {
                    Log($"In progress: {path}");
                    ReplayFiles.Add(path);
                }
            }
        }
       static void PrintReplayInformationFromReplay(FortniteReplay replay) {
            var totalPlayerCount = Convert.ToInt32(replay.TeamStats.TotalPlayers);

            var realPlayers = replay.PlayerData
                .Where(player => !string.IsNullOrWhiteSpace(player.EpicId))
                .GroupBy(player => player.EpicId)
                .Select(group => group.First());
            var realPlayerCount = realPlayers.Count();
            var botCount = totalPlayerCount-realPlayerCount;

            Log($"Bots: {botCount}");
            Log($"Players: {realPlayerCount}");
            Log($"Total: {totalPlayerCount}");
       }
        private static void OnChanged(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType != WatcherChangeTypes.Changed)
            {
                return;
            }
            PrintReplayInformationFromReplayFile(e.FullPath);
        }

        private static void OnError(object sender, ErrorEventArgs e) =>
            PrintException(e.GetException());

        private static void PrintException(Exception ex)
        {
            if (ex == null)
            {
                return;
            }

            Console.WriteLine($"Message: {ex.Message}");
            Console.WriteLine("Stacktrace:");
            Console.WriteLine(ex.StackTrace);
            Console.WriteLine();
            PrintException(ex.InnerException);
        }
    }
}
