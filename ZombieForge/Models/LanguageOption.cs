namespace ZombieForge.Models
{
    /// <summary>Represents a selectable UI language in Settings.</summary>
    /// <param name="Tag">BCP-47 language tag (e.g. "en-US") or empty string for "System Default".</param>
    /// <param name="DisplayName">Always shown in the language's own native name.</param>
    public sealed record LanguageOption(string Tag, string DisplayName)
    {
        /// <summary>
        /// Gets the English language option.
        /// </summary>
        public static readonly LanguageOption English = new("en-US", "English");

        /// <summary>
        /// Gets the German language option.
        /// </summary>
        public static readonly LanguageOption German  = new("de-DE", "Deutsch");
    }
}
