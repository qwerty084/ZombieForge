namespace ZombieForge.Models
{
    /// <summary>
    /// Represents a predefined console command shown in the config UI.
    /// </summary>
    public sealed class PresetCommand
    {
        /// <summary>Gets the preset group shown to the user.</summary>
        public string Category { get; }

        /// <summary>Gets the display label for the preset.</summary>
        public string Label    { get; }

        /// <summary>Gets the console command that will be inserted or bound.</summary>
        public string Command  { get; }

        /// <summary>
        /// Initializes a new preset command definition.
        /// </summary>
        /// <param name="category">The UI category used to group the preset.</param>
        /// <param name="label">The user-facing label for the preset.</param>
        /// <param name="command">The BO1 console command text.</param>
        public PresetCommand(string category, string label, string command)
        {
            Category = category;
            Label    = label;
            Command  = command;
        }
    }
}
