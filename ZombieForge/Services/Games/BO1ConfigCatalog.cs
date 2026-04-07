using System.Collections.Generic;

namespace ZombieForge.Services.Games
{
    public enum CvarEditorType
    {
        TextBox,
        NumberBox,
        Slider,
        ComboBox,
        Toggle
    }

    public class BindDefinition
    {
        public required string FriendlyName { get; init; }
        public required string Command { get; init; }
        public required string Category { get; init; }
        public required string Description { get; init; }
    }

    public class CvarDefinition
    {
        public required string FriendlyName { get; init; }
        public required string CvarName { get; init; }
        public required string Category { get; init; }
        public required string Description { get; init; }
        public required CvarEditorType EditorType { get; init; }
        public double Min { get; init; }
        public double Max { get; init; }
        public double Step { get; init; } = 1;
        public IReadOnlyList<string>? AllowedValues { get; init; }
        public string? DefaultValue { get; init; }
    }

    public static class BO1ConfigCatalog
    {
        public static IReadOnlyList<BindDefinition> Binds { get; } = new List<BindDefinition>
        {
            // Movement
            new() { FriendlyName = "Move Forward",    Command = "+forward",    Category = "Movement",      Description = "Move the player forward." },
            new() { FriendlyName = "Move Backward",   Command = "+back",       Category = "Movement",      Description = "Move the player backward." },
            new() { FriendlyName = "Move Left",       Command = "+moveleft",   Category = "Movement",      Description = "Strafe left." },
            new() { FriendlyName = "Move Right",      Command = "+moveright",  Category = "Movement",      Description = "Strafe right." },
            new() { FriendlyName = "Sprint",          Command = "+speed",      Category = "Movement",      Description = "Hold to sprint." },
            new() { FriendlyName = "Jump / Stand Up", Command = "+gostand",    Category = "Movement",      Description = "Jump, and also stand up from crouch or prone." },
            new() { FriendlyName = "Toggle Crouch",   Command = "togglecrouch",Category = "Movement",      Description = "Toggle crouch stance." },
            new() { FriendlyName = "Toggle Prone",    Command = "+prone",      Category = "Movement",      Description = "Toggle prone stance." },
            new() { FriendlyName = "Lean Left",       Command = "+leanleft",   Category = "Movement",      Description = "Lean left around cover." },
            new() { FriendlyName = "Lean Right",      Command = "+leanright",  Category = "Movement",      Description = "Lean right around cover." },

            // Combat
            new() { FriendlyName = "Shoot",           Command = "+attack",     Category = "Combat",        Description = "Fire the current weapon." },
            new() { FriendlyName = "Aim Down Sights", Command = "+ads",        Category = "Combat",        Description = "Aim down sights / scope." },
            new() { FriendlyName = "Reload",          Command = "+reload",     Category = "Combat",        Description = "Reload the current weapon." },
            new() { FriendlyName = "Melee / Knife",   Command = "+melee",      Category = "Combat",        Description = "Perform a melee attack." },
            new() { FriendlyName = "Frag Grenade",    Command = "+frag",       Category = "Combat",        Description = "Throw a frag grenade." },
            new() { FriendlyName = "Special Grenade", Command = "+smoke",      Category = "Combat",        Description = "Throw a special grenade (e.g. Semtex, Concussion)." },

            // Interaction
            new() { FriendlyName = "Use / Interact",  Command = "+activate",   Category = "Interaction",   Description = "Buy weapons, open doors, and interact with objects." },
            new() { FriendlyName = "Drop Weapon",     Command = "dropweapon",  Category = "Interaction",   Description = "Drop the currently held weapon." },
            new() { FriendlyName = "Next Weapon",     Command = "weapnext",    Category = "Interaction",   Description = "Switch to the next weapon." },
            new() { FriendlyName = "Previous Weapon", Command = "weapback",    Category = "Interaction",   Description = "Switch to the previous weapon." },
            new() { FriendlyName = "Quick Scope",     Command = "+speed_throw",Category = "Interaction",   Description = "Hold to quick-scope (ADS then fire immediately)." },

            // Communication
            new() { FriendlyName = "Say (All Chat)",  Command = "say",         Category = "Communication", Description = "Open the all-chat text input." },
            new() { FriendlyName = "Team Say",        Command = "sayteam",     Category = "Communication", Description = "Open the team-chat text input." },

            // Utility
            new() { FriendlyName = "Open Console",    Command = "toggleconsole",Category = "Utility",      Description = "Toggle the developer console. Requires 'con_restricted' set to 0." },
            new() { FriendlyName = "Scoreboard",      Command = "+scores",     Category = "Utility",       Description = "Hold to show the scoreboard." },
            new() { FriendlyName = "Pause Menu",      Command = "togglemenu",  Category = "Utility",       Description = "Open or close the pause menu." },
        };

        public static IReadOnlyList<CvarDefinition> CVars { get; } = new List<CvarDefinition>
        {
            // Performance
            new()
            {
                FriendlyName = "Max FPS",
                CvarName     = "com_maxfps",
                Category     = "Performance",
                Description  = "Maximum frames per second. Values above 85 may cause physics bugs in BO1 (e.g. jumping issues). Use 0 for unlimited.",
                EditorType   = CvarEditorType.ComboBox,
                AllowedValues = new[] { "30", "60", "85", "125", "250", "0" },
                DefaultValue = "85"
            },

            // Graphics
            new()
            {
                FriendlyName = "Field of View",
                CvarName     = "cg_fov",
                Category     = "Graphics",
                Description  = "Horizontal field of view in degrees. Default is 65; higher values give a wider perspective.",
                EditorType   = CvarEditorType.Slider,
                Min = 65, Max = 90, Step = 1,
                DefaultValue = "65"
            },
            new()
            {
                FriendlyName = "Fullscreen",
                CvarName     = "r_fullscreen",
                Category     = "Graphics",
                Description  = "1 = fullscreen, 0 = windowed mode.",
                EditorType   = CvarEditorType.Toggle,
                DefaultValue = "1"
            },
            new()
            {
                FriendlyName = "Brightness",
                CvarName     = "r_filmTweakBrightness",
                Category     = "Graphics",
                Description  = "Screen brightness adjustment. Range -1 (dark) to 1 (bright), default 0.",
                EditorType   = CvarEditorType.Slider,
                Min = -1, Max = 1, Step = 0.05,
                DefaultValue = "0"
            },

            // Mouse
            new()
            {
                FriendlyName = "Mouse Sensitivity",
                CvarName     = "sensitivity",
                Category     = "Mouse",
                Description  = "Mouse sensitivity. Higher = faster cursor movement.",
                EditorType   = CvarEditorType.NumberBox,
                Min = 0.1, Max = 100, Step = 0.1,
                DefaultValue = "5"
            },
            new()
            {
                FriendlyName = "Mouse Acceleration",
                CvarName     = "cl_mouseaccel",
                Category     = "Mouse",
                Description  = "Mouse acceleration. Strongly recommended to keep this off (0) for consistent aim.",
                EditorType   = CvarEditorType.Toggle,
                DefaultValue = "0"
            },
            new()
            {
                FriendlyName = "Invert Y Axis",
                CvarName     = "m_invert",
                Category     = "Mouse",
                Description  = "Invert the vertical mouse axis. 1 = inverted, 0 = normal.",
                EditorType   = CvarEditorType.Toggle,
                DefaultValue = "0"
            },

            // Audio
            new()
            {
                FriendlyName = "Master Volume",
                CvarName     = "volume",
                Category     = "Audio",
                Description  = "Overall game volume. Range 0 (silent) to 1 (full).",
                EditorType   = CvarEditorType.Slider,
                Min = 0, Max = 1, Step = 0.05,
                DefaultValue = "1"
            },
            new()
            {
                FriendlyName = "Music Volume",
                CvarName     = "s_musicvolume",
                Category     = "Audio",
                Description  = "In-game music volume. Range 0–1.",
                EditorType   = CvarEditorType.Slider,
                Min = 0, Max = 1, Step = 0.05,
                DefaultValue = "1"
            },
            new()
            {
                FriendlyName = "Voice Volume",
                CvarName     = "s_voiceVolume",
                Category     = "Audio",
                Description  = "Player voice/communication volume. Range 0–1.",
                EditorType   = CvarEditorType.Slider,
                Min = 0, Max = 1, Step = 0.05,
                DefaultValue = "1"
            },

            // HUD
            new()
            {
                FriendlyName = "Show FPS",
                CvarName     = "cg_drawFPS",
                Category     = "HUD",
                Description  = "Display an FPS counter on screen. 0 = off, 1 = FPS only, 2 = full performance stats.",
                EditorType   = CvarEditorType.ComboBox,
                AllowedValues = new[] { "0", "1", "2" },
                DefaultValue = "0"
            },

            // Console
            new()
            {
                FriendlyName = "Enable Console",
                CvarName     = "con_restricted",
                Category     = "Console",
                Description  = "Set to 0 to enable the developer console and console-bound commands. Required for toggleconsole bind to work.",
                EditorType   = CvarEditorType.Toggle,
                DefaultValue = "1"
            },
        };

        /// <summary>All valid BO1 key names for bind pickers.</summary>
        public static IReadOnlyList<string> KeyNames { get; } = new List<string>
        {
            // Letters
            "a","b","c","d","e","f","g","h","i","j","k","l","m",
            "n","o","p","q","r","s","t","u","v","w","x","y","z",
            // Numbers
            "1","2","3","4","5","6","7","8","9","0",
            // Special keys
            "SPACE","ENTER","ESCAPE","TAB","BACKSPACE","DEL","HOME","END","PGUP","PGDN","INS",
            // Function keys
            "F1","F2","F3","F4","F5","F6","F7","F8","F9","F10","F11","F12",
            // Numpad
            "KP_0","KP_1","KP_2","KP_3","KP_4","KP_5","KP_6","KP_7","KP_8","KP_9",
            "KP_ENTER","KP_PLUS","KP_MINUS","KP_SLASH","KP_STAR","KP_DEL",
            // Mouse
            "MOUSE1","MOUSE2","MOUSE3","MOUSE4","MOUSE5","MWHEELUP","MWHEELDOWN",
            // Symbols
            "SEMICOLON","EQUALS","COMMA","MINUS","PERIOD","SLASH","BACKSLASH",
            "APOSTROPHE","LBRACKET","RBRACKET",
            // Modifier keys
            "SHIFT","CTRL","ALT","LSHIFT","RSHIFT","LCTRL","RCTRL","LALT","RALT",
        };
    }
}
