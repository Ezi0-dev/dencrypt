using System;
using System.IO;
using System.Text.Json;

namespace DencryptCore
{
    public class AppSettings
    {
        public string DefaultVaultPath { get; set; } = "";
        public bool RemoveOriginalFiles { get; set; } = true;

    }
    public static class SettingsManager
    {
        private static readonly string settingsFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Dencrypt",
        "settings.json"
        );

        public static AppSettings Current { get; private set; } = new AppSettings();

        static SettingsManager()
        {
            Load();
        }

        public static void Load()
        {
            try
            {
                if (File.Exists(settingsFile))
                {
                    string json = File.ReadAllText(settingsFile);
                    Current = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
                else
                {
                    Save(); // First time
                }
            }
            catch
            {
                Current = new AppSettings();
            }
        }

        public static void Save()
        {
            try
            {
                string directory = Path.GetDirectoryName(settingsFile)!;
                Directory.CreateDirectory(directory);

                string json = JsonSerializer.Serialize(Current, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(settingsFile, json);
            }
            catch (Exception ex)
            {
                Console.Write($"Error saving settings. {ex.Message}");
            }
        }
    }
}