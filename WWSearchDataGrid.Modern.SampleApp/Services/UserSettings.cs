using System;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace WWSearchDataGrid.Modern.SampleApp.Services
{
    /// <summary>
    /// Persists user-scoped application settings (e.g. last window position) to a JSON file
    /// under the user's local application data folder.
    /// </summary>
    public sealed class UserSettings
    {
        private const string FolderName = "WWSearchDataGrid.Modern.SampleApp";
        private const string FileName = "settings.json";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        public WindowPositionSetting? WindowPosition { get; set; }

        public static UserSettings Load()
        {
            try
            {
                var path = GetSettingsFilePath();
                if (!File.Exists(path))
                    return new UserSettings();

                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<UserSettings>(json, JsonOptions) ?? new UserSettings();
            }
            catch
            {
                return new UserSettings();
            }
        }

        public void Save()
        {
            try
            {
                var path = GetSettingsFilePath();
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                var json = JsonSerializer.Serialize(this, JsonOptions);
                File.WriteAllText(path, json);
            }
            catch
            {
                // Swallow — settings persistence is best-effort.
            }
        }

        private static string GetSettingsFilePath()
        {
            var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(baseDir, FolderName, FileName);
        }
    }

    public sealed class WindowPositionSetting
    {
        public double Left { get; set; }
        public double Top { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public WindowState WindowState { get; set; }
    }
}
