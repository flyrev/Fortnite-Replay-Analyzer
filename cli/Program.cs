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
        static ReplayReader reader = new ReplayReader();

        public static List<string> replayFiles = new List<string>();

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
                var read = reader.ReadReplay(path);
                PrintReplayInformationFromReplay(read);
                replayFiles.Clear();
                Console.WriteLine("Waiting for next game");
            } catch (IOException) {
                Log("Could not open replay");
            } catch (InvalidReplayException) {
                if (!replayFiles.Contains(path)) {
                    Log($"In progress: {path}");
                    replayFiles.Add(path);
                }
            }
        }
       static void PrintReplayInformationFromReplay(FortniteReplay replay) {
            var totalPlayerCount = Convert.ToInt32(replay.TeamStats.TotalPlayers);

            var realPlayers = replay.PlayerData.Where(player => player.EpicId != null && player.EpicId.Length > 0).Distinct();
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
            Console.WriteLine($"Message: {ex.Message}");
            Console.WriteLine("Stacktrace:");
            Console.WriteLine(ex.StackTrace);
            Console.WriteLine();
            PrintException(ex.InnerException);
        }
    }
}
