namespace ZombieForge.Models
{
    public sealed class PresetCommand
    {
        public string Category { get; }
        public string Label    { get; }
        public string Command  { get; }

        public PresetCommand(string category, string label, string command)
        {
            Category = category;
            Label    = label;
            Command  = command;
        }
    }
}
