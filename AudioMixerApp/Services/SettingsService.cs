using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace AudioMixerApp.Services
{
    // Defines the structure of our settings
    public class AppSettings
    {
        public string? LastInputDeviceId { get; set; }
        public string? LastOutputDeviceId { get; set; }
        public double LastVolumePercent { get; set; } = 75.0; // Default volume
        public bool LastMuteState { get; set; } = false; // Default mute state
    }

    // Service to load and save settings
    public class SettingsService
    {
        private readonly string _settingsFilePath;
        private static readonly JsonSerializerOptions _options = new() { WriteIndented = true };

        public SettingsService()
        {
            // Store settings in a dedicated folder within the user's AppData directory
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appFolder = Path.Combine(appDataPath, "AudioMixerApp");
            Directory.CreateDirectory(appFolder); // Ensure the directory exists
            _settingsFilePath = Path.Combine(appFolder, "Settings.json");
        }

        // Asynchronously loads settings from the JSON file
        public async Task<AppSettings> LoadSettingsAsync()
        {
            if (!File.Exists(_settingsFilePath))
            {
                // Return default settings if the file doesn't exist yet
                return new AppSettings();
            }

            try
            {
                string json = await File.ReadAllTextAsync(_settingsFilePath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                // Return default settings if deserialization results in null
                return settings ?? new AppSettings();
            }
            catch (Exception ex) // Catch potential IO or Json exceptions during loading
            {
                Console.WriteLine($"Error loading settings from {_settingsFilePath}: {ex.Message}");
                // Consider implementing more robust logging here
                return new AppSettings(); // Return default settings on error
            }
        }

        // Asynchronously saves the current settings to the JSON file
        public async Task SaveSettingsAsync(AppSettings settings)
        {
            try
            {
                string json = JsonSerializer.Serialize(settings, _options);
                await File.WriteAllTextAsync(_settingsFilePath, json);
            }
            catch (Exception ex) // Catch potential IO or Json exceptions during saving
            {
                 Console.WriteLine($"Error saving settings to {_settingsFilePath}: {ex.Message}");
                 // Consider implementing more robust logging here
            }
        }
    }
}
