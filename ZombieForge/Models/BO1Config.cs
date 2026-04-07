using System.Collections.Generic;
using System.Linq;

namespace ZombieForge.Models
{
    public class BO1Config
    {
        private readonly List<ConfigEntry> _entries;

        public BO1Config(IEnumerable<ConfigEntry> entries)
        {
            _entries = entries.ToList();
        }

        public IReadOnlyList<ConfigEntry> Entries => _entries;

        public string? GetBind(string key)
            => _entries.FirstOrDefault(e => e.EntryType == EntryType.Bind &&
               string.Equals(e.Key, key, System.StringComparison.OrdinalIgnoreCase))?.Value;

        public void SetBind(string key, string command)
        {
            var existing = _entries.FirstOrDefault(e => e.EntryType == EntryType.Bind &&
                string.Equals(e.Key, key, System.StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                existing.Value = command;
                existing.RawLine = $"bind {existing.Key} \"{command}\"";
            }
            else
            {
                _entries.Add(new ConfigEntry
                {
                    EntryType = EntryType.Bind,
                    Key = key,
                    Value = command,
                    RawLine = $"bind {key} \"{command}\""
                });
            }
        }

        public void RemoveBind(string key)
        {
            var existing = _entries.FirstOrDefault(e => e.EntryType == EntryType.Bind &&
                string.Equals(e.Key, key, System.StringComparison.OrdinalIgnoreCase));
            if (existing != null)
                _entries.Remove(existing);
        }

        public string? GetCVar(string name)
            => _entries.FirstOrDefault(e => e.EntryType == EntryType.CVar &&
               string.Equals(e.Key, name, System.StringComparison.OrdinalIgnoreCase))?.Value;

        public void SetCVar(string name, string value)
        {
            var existing = _entries.FirstOrDefault(e => e.EntryType == EntryType.CVar &&
                string.Equals(e.Key, name, System.StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                existing.Value = value;
                existing.RawLine = $"seta {existing.Key} \"{value}\"";
            }
            else
            {
                _entries.Add(new ConfigEntry
                {
                    EntryType = EntryType.CVar,
                    Key = name,
                    Value = value,
                    RawLine = $"seta {name} \"{value}\""
                });
            }
        }
    }
}
