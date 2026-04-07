using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using ZombieForge.Models;

namespace ZombieForge.Services
{
    public class ConfigService
    {
        private static readonly string DefaultConfigPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Activision", "CoDBlackOps", "players", "config.cfg");

        // bind <KEY> "<command>"
        private static readonly Regex BindRegex = new(
            @"^bind\s+(\S+)\s+""(.*)""\s*$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // seta <name> "<value>"
        private static readonly Regex SetaRegex = new(
            @"^seta\s+(\S+)\s+""(.*)""\s*$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public string? FindConfigPath()
            => File.Exists(DefaultConfigPath) ? DefaultConfigPath : null;

        public BO1Config Load(string path)
        {
            var lines = File.ReadAllLines(path);
            var entries = new List<ConfigEntry>(lines.Length);

            foreach (var line in lines)
            {
                var trimmed = line.Trim();

                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("//", StringComparison.Ordinal))
                {
                    entries.Add(new ConfigEntry
                    {
                        EntryType = EntryType.Comment,
                        RawLine = line
                    });
                    continue;
                }

                var bindMatch = BindRegex.Match(trimmed);
                if (bindMatch.Success)
                {
                    entries.Add(new ConfigEntry
                    {
                        EntryType = EntryType.Bind,
                        Key = bindMatch.Groups[1].Value,
                        Value = bindMatch.Groups[2].Value,
                        RawLine = line
                    });
                    continue;
                }

                var setaMatch = SetaRegex.Match(trimmed);
                if (setaMatch.Success)
                {
                    entries.Add(new ConfigEntry
                    {
                        EntryType = EntryType.CVar,
                        Key = setaMatch.Groups[1].Value,
                        Value = setaMatch.Groups[2].Value,
                        RawLine = line
                    });
                    continue;
                }

                entries.Add(new ConfigEntry
                {
                    EntryType = EntryType.Unknown,
                    RawLine = line
                });
            }

            return new BO1Config(entries);
        }

        public void Save(string path, BO1Config config)
        {
            var lines = new List<string>(config.Entries.Count);
            foreach (var entry in config.Entries)
                lines.Add(entry.RawLine);

            var tempPath = path + ".tmp";
            File.WriteAllLines(tempPath, lines);
            File.Replace(tempPath, path, null);
        }
    }
}
