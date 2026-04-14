namespace CodexTldSaveEditor.App.Models
{
    public class SaveFileEntry
    {
        public string DisplayName { get; set; }
        public string FileName { get; set; }
        public string FullPath { get; set; }
        public string DirectoryPath { get; set; }
        public string Timestamp { get; set; }
        public string GameMode { get; set; }
        public string Kind { get; set; }
        public string InternalName { get; set; }
        public int Version { get; set; }
        public int Changelist { get; set; }
        public int GameId { get; set; }
        public int SectionCount { get; set; }

        public string PickerLabel =>
            string.IsNullOrWhiteSpace(DisplayName)
                ? FileName
                : $"{DisplayName} ({FileName})";

        public string Summary =>
            string.IsNullOrWhiteSpace(GameMode)
                ? FileName
                : $"{DisplayName} [{GameMode}]";
    }
}
