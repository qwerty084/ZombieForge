using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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

        /// <summary>
        /// Tries to locate the BO1 <c>config.cfg</c> file.
        /// </summary>
        /// <param name="path">When this method returns, contains the discovered path, or the default candidate path when no file is found. This parameter is treated as uninitialized.</param>
        /// <returns><see langword="true" /> if a config file exists at a discovered location; otherwise, <see langword="false" />.</returns>
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

            // Check Steam registry for custom install path, then enumerate library folders from VDF
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

                // Parse libraryfolders.vdf to find non-default Steam library paths
                foreach (var libraryRoot in GetSteamLibraryPaths(steamPath))
                {
                    var libraryCandidate = Path.Combine(
                        libraryRoot, @"steamapps\common\Call of Duty Black Ops\players\config.cfg");
                    if (File.Exists(libraryCandidate))
                    {
                        path = libraryCandidate;
                        return true;
                    }
                }
            }

            path = _candidatePaths[0];
            return false;
        }

        private static readonly Encoding _configEncoding =
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        /// <summary>
        /// Loads BO1 config data from a file.
        /// </summary>
        /// <param name="filePath">The path to the config file.</param>
        /// <returns>A parsed config data model.</returns>
        public static ConfigData Load(string filePath)
        {
            var text = File.ReadAllText(filePath, _configEncoding);
            var lines = SplitLines(text);
            var dvars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var binds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var line in lines)
            {
                if (!TryParseDirective(line, out var directive, out var key, out var value))
                    continue;

                if (directive.Equals("seta", StringComparison.OrdinalIgnoreCase))
                {
                    dvars[key] = value;
                    continue;
                }

                if (directive.Equals("bind", StringComparison.OrdinalIgnoreCase))
                {
                    var bindKey = key.ToUpperInvariant();
                    if (!string.IsNullOrWhiteSpace(value))
                        binds[bindKey] = value;
                }
            }

            return new ConfigData(
                dvars,
                binds,
                lines,
                DetectLineEnding(text),
                HasTrailingLineEnding(text));
        }

        /// <summary>
        /// Saves config data back to a BO1 config file while preserving original line and newline style when possible.
        /// </summary>
        /// <param name="filePath">The path of the config file to write.</param>
        /// <param name="data">The config data to persist.</param>
        public static void Save(string filePath, ConfigData data)
        {
            var lines = new List<string>(data.OriginalLines);

            // Track which dvars/binds we've already updated in-place
            var updatedDvars = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var updatedBinds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < lines.Count; i++)
            {
                if (!TryParseDirective(lines[i], out var directive, out var key, out _))
                    continue;

                if (directive.Equals("seta", StringComparison.OrdinalIgnoreCase))
                {
                    if (data.Dvars.TryGetValue(key, out var newValue))
                    {
                        lines[i] = $"seta {key} \"{EscapeValue(newValue)}\"";
                        updatedDvars.Add(key);
                    }
                    continue;
                }

                if (directive.Equals("bind", StringComparison.OrdinalIgnoreCase))
                {
                    var bindKey = key.ToUpperInvariant();
                    if (data.RemovedBindKeys.Contains(bindKey))
                    {
                        lines.RemoveAt(i--);
                        updatedBinds.Add(bindKey);
                        continue;
                    }

                    if (data.Binds.TryGetValue(bindKey, out var newCmd))
                    {
                        lines[i] = $"bind {bindKey} \"{EscapeValue(newCmd)}\"";
                        updatedBinds.Add(bindKey);
                    }
                }
            }

            // Append new dvars not found in original file (rare, but handle gracefully)
            foreach (var kv in data.Dvars)
            {
                if (!updatedDvars.Contains(kv.Key))
                    lines.Add($"seta {kv.Key} \"{EscapeValue(kv.Value)}\"");
            }

            // Append new binds
            foreach (var kv in data.Binds)
            {
                if (!updatedBinds.Contains(kv.Key))
                    lines.Add($"bind {kv.Key} \"{EscapeValue(kv.Value)}\"");
            }

            var lineEnding = string.IsNullOrEmpty(data.LineEnding) ? Environment.NewLine : data.LineEnding;
            var output = string.Join(lineEnding, lines);

            if (data.HadTrailingLineEnding)
                output += lineEnding;

            File.WriteAllText(filePath, output, _configEncoding);
        }

        private static bool TryParseDirective(string line, out string directive, out string key, out string value)
        {
            directive = string.Empty;
            key = string.Empty;
            value = string.Empty;

            var trimmed = line.TrimStart();
            if (trimmed.Length == 0 || trimmed[0] == '/')
                return false;

            var index = 0;
            if (!TryReadToken(trimmed, ref index, out directive))
                return false;

            if (!directive.Equals("seta", StringComparison.OrdinalIgnoreCase) &&
                !directive.Equals("bind", StringComparison.OrdinalIgnoreCase))
                return false;

            if (!TryReadToken(trimmed, ref index, out key) || string.IsNullOrWhiteSpace(key))
                return false;

            SkipWhitespace(trimmed, ref index);
            if (index >= trimmed.Length)
                return false;

            if (trimmed[index] == '"')
                return TryReadToken(trimmed, ref index, out value);

            value = trimmed[index..].TrimEnd();
            return value.Length > 0;
        }

        private static bool TryReadToken(string text, ref int index, out string token)
        {
            token = string.Empty;
            SkipWhitespace(text, ref index);

            if (index >= text.Length)
                return false;

            if (text[index] == '"')
            {
                index++; // opening quote
                var sb = new StringBuilder();
                while (index < text.Length)
                {
                    char c = text[index++];
                    if (c == '\\' && index < text.Length)
                    {
                        char escaped = text[index];
                        if (escaped == '"' || escaped == '\\')
                        {
                            sb.Append(escaped);
                            index++;
                            continue;
                        }
                    }

                    if (c == '"')
                    {
                        token = sb.ToString();
                        return true;
                    }

                    sb.Append(c);
                }

                token = string.Empty;
                return false;
            }

            int start = index;
            while (index < text.Length && !char.IsWhiteSpace(text[index]))
                index++;

            token = text[start..index];
            return token.Length > 0;
        }

        private static void SkipWhitespace(string text, ref int index)
        {
            while (index < text.Length && char.IsWhiteSpace(text[index]))
                index++;
        }

        private static string[] SplitLines(string text)
        {
            if (text.Length == 0)
                return [];

            var lines = new List<string>();
            int start = 0;

            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] != '\r' && text[i] != '\n')
                    continue;

                lines.Add(text[start..i]);
                if (text[i] == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                    i++;

                start = i + 1;
            }

            if (start < text.Length)
                lines.Add(text[start..]);

            return [.. lines];
        }

        private static string DetectLineEnding(string text)
        {
            for (var i = 0; i < text.Length; i++)
            {
                if (text[i] == '\r')
                    return i + 1 < text.Length && text[i + 1] == '\n' ? "\r\n" : "\r";

                if (text[i] == '\n')
                    return "\n";
            }

            return Environment.NewLine;
        }

        private static bool HasTrailingLineEnding(string text)
        {
            if (text.Length == 0)
                return false;

            char last = text[^1];
            return last == '\r' || last == '\n';
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

        /// <summary>
        /// Parses Steam's libraryfolders.vdf to enumerate all library root paths.
        /// </summary>
        private static IEnumerable<string> GetSteamLibraryPaths(string steamRoot)
        {
            var vdfPath = Path.Combine(steamRoot, @"steamapps\libraryfolders.vdf");
            if (!File.Exists(vdfPath))
                yield break;

            foreach (var line in File.ReadAllLines(vdfPath))
            {
                var trimmed = line.Trim();
                // Match lines like:   "path"    "D:\\SteamLibrary"
                if (!trimmed.StartsWith("\"path\"", StringComparison.OrdinalIgnoreCase))
                    continue;

                var rest = trimmed[6..].TrimStart();
                if (rest.Length >= 2 && rest[0] == '"')
                {
                    var end = rest.IndexOf('"', 1);
                    if (end > 1)
                        yield return rest[1..end].Replace("\\\\", "\\");
                }
            }
        }

        private static string EscapeValue(string value)
            => value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    /// <summary>
    /// Represents parsed BO1 config directives and original formatting metadata.
    /// </summary>
    public sealed class ConfigData
    {
        /// <summary>
        /// Gets the dvar assignments keyed by dvar name.
        /// </summary>
        public Dictionary<string, string> Dvars        { get; }

        /// <summary>
        /// Gets the keybind assignments keyed by uppercase key name.
        /// </summary>
        public Dictionary<string, string> Binds        { get; }

        /// <summary>
        /// Gets the set of bind keys that should be removed on save.
        /// </summary>
        public HashSet<string>            RemovedBindKeys { get; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gets the original file lines as loaded from disk.
        /// </summary>
        public string[]                   OriginalLines { get; }

        /// <summary>
        /// Gets the line-ending sequence detected in the original file.
        /// </summary>
        public string                     LineEnding { get; }

        /// <summary>
        /// Gets a value that indicates whether the original file ended with a trailing line ending.
        /// </summary>
        public bool                       HadTrailingLineEnding { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigData"/> class.
        /// </summary>
        /// <param name="dvars">A dictionary of parsed dvar values.</param>
        /// <param name="binds">A dictionary of parsed bind values.</param>
        /// <param name="originalLines">The original file lines.</param>
        /// <param name="lineEnding">The original line-ending sequence.</param>
        /// <param name="hadTrailingLineEnding"><see langword="true" /> if the original text ended with a line ending; otherwise, <see langword="false" />.</param>
        public ConfigData(
            Dictionary<string, string> dvars,
            Dictionary<string, string> binds,
            string[] originalLines,
            string lineEnding,
            bool hadTrailingLineEnding)
        {
            Dvars         = dvars;
            Binds         = binds;
            OriginalLines = originalLines;
            LineEnding = lineEnding;
            HadTrailingLineEnding = hadTrailingLineEnding;
        }
    }
}
