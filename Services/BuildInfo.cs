using System;

namespace NotepadByFerryPot.Services
{
    public static class BuildInfo
    {
#if CHANNEL_FEATURES
        public const string Channel = "Features";
        public const string TitleSuffix = " [Features]";
        public const string AppDataFolderName = "NotepadByFerryPot-features";
        public const string VersionSuffix = "features";
#elif CHANNEL_AI
        public const string Channel = "AI";
        public const string TitleSuffix = " [AI]";
        public const string AppDataFolderName = "NotepadByFerryPot-ai";
        public const string VersionSuffix = "ai";
#else
        public const string Channel = "Stable";
        public const string TitleSuffix = "";
        public const string AppDataFolderName = "NotepadByFerryPot";
        public const string VersionSuffix = "";
#endif
    }
}
