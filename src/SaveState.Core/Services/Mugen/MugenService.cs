using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace SaveState.Core.Services.Mugen
{
    public class MugenService
    {
        private readonly string _engineRootPath;
        private readonly string _executablePath;

        public MugenService()
        {
            _engineRootPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "SaveState2", "MUGEN");
            _executablePath = Path.Combine(_engineRootPath, "Ikemen_GO.exe");
        }

        public List<MugenFighter> GetFighters()
        {
            var fighters = new List<MugenFighter>();
            var charsDir = Path.Combine(_engineRootPath, "chars");

            if (Directory.Exists(charsDir))
            {
                foreach (var dir in Directory.GetDirectories(charsDir))
                {
                    fighters.Add(new MugenFighter
                    {
                        Name = new DirectoryInfo(dir).Name,
                        Path = dir
                    });
                }
            }
            return fighters;
        }

        public List<MugenStage> GetStages()
        {
            var stages = new List<MugenStage>();
            var stagesDir = Path.Combine(_engineRootPath, "stages");

            if (Directory.Exists(stagesDir))
            {
                foreach (var file in Directory.GetFiles(stagesDir, "*.def"))
                {
                    stages.Add(new MugenStage
                    {
                        Name = Path.GetFileNameWithoutExtension(file),
                        Path = file
                    });
                }
            }
            return stages;
        }

        public List<string> GetRoster()
        {
            var roster = new List<string>();
            var selectDefPath = Path.Combine(_engineRootPath, "data", "select.def");

            if (File.Exists(selectDefPath))
            {
                var lines = File.ReadAllLines(selectDefPath);
                bool inCharacters = false;
                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith(";")) continue;

                    if (trimmed.Equals("[Characters]", StringComparison.OrdinalIgnoreCase))
                    {
                        inCharacters = true;
                        continue;
                    }
                    if (trimmed.StartsWith("[") && inCharacters)
                    {
                        inCharacters = false;
                        continue;
                    }

                    if (inCharacters)
                    {
                        var parts = trimmed.Split(',');
                        roster.Add(parts[0].Trim());
                    }
                }
            }
            return roster;
        }

        public void SaveRoster(List<string> newRoster)
        {
             var selectDefPath = Path.Combine(_engineRootPath, "data", "select.def");
             if (!File.Exists(selectDefPath)) return;

             var lines = File.ReadAllLines(selectDefPath).ToList();
             var newLines = new List<string>();
             bool replacedCharacters = false;
             
             for (int i = 0; i < lines.Count; i++)
             {
                 var line = lines[i].Trim();
                 if (!replacedCharacters && line.Equals("[Characters]", StringComparison.OrdinalIgnoreCase))
                 {
                     newLines.Add(lines[i]);
                     newLines.AddRange(newRoster.Select(c => $"{c}, random"));
                     replacedCharacters = true;
                     
                     while (i + 1 < lines.Count)
                     {
                         var nextLine = lines[i + 1].Trim();
                         if (nextLine.StartsWith("[") && !nextLine.StartsWith(";"))
                         {
                             break;
                         }
                         i++;
                     }
                 }
                 else
                 {
                     newLines.Add(lines[i]);
                 }
             }

             File.WriteAllLines(selectDefPath, newLines);
        }

        public void LaunchEngine()
        {
            if (!File.Exists(_executablePath))
            {
                throw new FileNotFoundException("Ikemen GO executable not found at " + _executablePath);
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = _executablePath,
                    WorkingDirectory = _engineRootPath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to launch engine: {ex.Message}");
            }
        }
    }
}
