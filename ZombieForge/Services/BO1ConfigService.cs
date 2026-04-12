using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;
using ZombieForge.Models;

namespace ZombieForge.Services
{
    /// <summary>Reads and writes the BO1 config.cfg file.</summary>
    public static class BO1ConfigService
    {
        private static readonly string[] _candidatePaths =
        [
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                @"Steam\steamapps\common\Call of Duty Black Ops\players\config.cfg"),
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                @"Steam\steamapps\common\Call of Duty Black Ops\players\config.cfg"),
        ];

        public static bool TryFindConfigPath(out string path)
        {
            foreach (var candidate in _candidatePaths)
            {
                if (File.Exists(candidate))
                {
                    path = candidate;
                    return true;
                }
            }

            // Check Steam registry for custom install path
            var steamPath = TryGetSteamPathFromRegistry();
            if (steamPath is not null)
            {
                var registryCandidate = Path.Combine(
                    steamPath, @"steamapps\common\Call of Duty Black Ops\players\config.cfg");
                if (File.Exists(registryCandidate))
                {
                    path = registryCandidate;
                    return true;
                }
            }

            path = _candidatePaths[0];
            return false;
        }

        private static readonly System.Text.Encoding _configEncoding =
            new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        public static ConfigData Load(string filePath)
        {
            var lines = File.ReadAllLines(filePath, _configEncoding);
            var dvars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var binds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var line in lines)
            {
                var trimmed = line.TrimStart();

                // seta <key> "<value>"
                if (trimmed.StartsWith("seta ", StringComparison.OrdinalIgnoreCase))
                {
                    var rest = trimmed[5..].TrimStart();
                    var spaceIdx = rest.IndexOf(' ');
                    if (spaceIdx > 0)
                    {
                        var key   = rest[..spaceIdx];
                        var value = Unquote(rest[(spaceIdx + 1)..].TrimStart());
                        dvars[key] = value;
                    }
                    continue;
                }

                // bind <KEY> "<command>"
                if (trimmed.StartsWith("bind ", StringComparison.OrdinalIgnoreCase))
                {
                    var rest = trimmed[5..].TrimStart();
                    var spaceIdx = rest.IndexOf(' ');
                    if (spaceIdx > 0)
                    {
                        var key     = rest[..spaceIdx].ToUpperInvariant();
                        var command = Unquote(rest[(spaceIdx + 1)..].TrimStart());
                        if (!string.IsNullOrWhiteSpace(command))
                            binds[key] = command;
                    }
                }
            }

            return new ConfigData(dvars, binds, lines);
        }

        public static void Save(string filePath, ConfigData data)
        {
            var lines = new List<string>(data.OriginalLines);

            // Track which dvars/binds we've already updated in-place
            var updatedDvars = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var updatedBinds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < lines.Count; i++)
            {
                var trimmed = lines[i].TrimStart();

                if (trimmed.StartsWith("seta ", StringComparison.OrdinalIgnoreCase))
                {
                    var rest     = trimmed[5..].TrimStart();
                    var spaceIdx = rest.IndexOf(' ');
                    if (spaceIdx > 0)
                    {
                        var key = rest[..spaceIdx];
                        if (data.Dvars.TryGetValue(key, out var newValue))
                        {
                            lines[i] = $"seta {key} \"{newValue}\"";
                            updatedDvars.Add(key);
                        }
                    }
                    continue;
                }

                if (trimmed.StartsWith("bind ", StringComparison.OrdinalIgnoreCase))
                {
                    var rest     = trimmed[5..].TrimStart();
                    var spaceIdx = rest.IndexOf(' ');
                    if (spaceIdx > 0)
                    {
                        var key = rest[..spaceIdx].ToUpperInvariant();
                        if (data.RemovedBindKeys.Contains(key))
                        {
                            lines[i] = string.Empty;
                            updatedBinds.Add(key);
                            continue;
                        }
                        if (data.Binds.TryGetValue(key, out var newCmd))
                        {
                            lines[i] = $"bind {key} \"{newCmd}\"";
                            updatedBinds.Add(key);
                        }
                    }
                }
            }

            // Append new dvars not found in original file (rare, but handle gracefully)
            foreach (var kv in data.Dvars)
            {
                if (!updatedDvars.Contains(kv.Key))
                    lines.Add($"seta {kv.Key} \"{kv.Value}\"");
            }

            // Append new binds
            foreach (var kv in data.Binds)
            {
                if (!updatedBinds.Contains(kv.Key))
                    lines.Add($"bind {kv.Key} \"{kv.Value}\"");
            }

            File.WriteAllLines(filePath, lines, _configEncoding);
        }

        private static string Unquote(string s)
        {
            if (s.Length >= 2 && s[0] == '"' && s[^1] == '"')
                return s[1..^1];
            return s;
        }

        private static string? TryGetSteamPathFromRegistry()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
                return key?.GetValue("SteamPath") as string;
            }
            catch
            {
                return null;
            }
        }
    }

    public sealed class ConfigData
    {
        public Dictionary<string, string> Dvars        { get; }
        public Dictionary<string, string> Binds        { get; }
        public HashSet<string>            RemovedBindKeys { get; } = new(StringComparer.OrdinalIgnoreCase);
        public string[]                   OriginalLines { get; }

        public ConfigData(
            Dictionary<string, string> dvars,
            Dictionary<string, string> binds,
            string[] originalLines)
        {
            Dvars         = dvars;
            Binds         = binds;
            OriginalLines = originalLines;
        }
    }
}
