namespace ZombieForge.Models
{
    public enum EntryType
    {
        Bind,
        CVar,
        Comment,
        Unknown
    }

    public class ConfigEntry
    {
        public EntryType EntryType { get; set; }

        /// <summary>For Bind: key name (e.g. MOUSE1). For CVar: variable name (e.g. cg_fov).</summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>The command or value.</summary>
        public string Value { get; set; } = string.Empty;

        /// <summary>Original text line — written verbatim for Comment/Unknown entries.</summary>
        public string RawLine { get; set; } = string.Empty;
    }
}
