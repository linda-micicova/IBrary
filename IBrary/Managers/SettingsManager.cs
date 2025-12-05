using IBrary.App_settings;
using IBrary.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IBrary.Managers
{
    internal static class SettingsManager
    {
        // json file path
        /*private static readonly string settingsPath = Path.Combine(
        Application.StartupPath,
        "Data", "settings.json");*/
        private static readonly string settingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "IBrary",
        "settings.json");

        public static UserSettings CurrentSettings { get; set; } = new UserSettings();
        public static List<Subject> MySubjects { get; private set; } = new List<Subject>();

        public static Color FlashcardColor;
        public static Color BackgroundColor;
        public static Color ButtonColor;
        public static Color MenuColor;
        public static Color TextColor;

        // LOAD SETTINGS FROM MEMORY
        public static UserSettings Load()
        {
            List<Subject> allSubjects = SubjectManager.Load(); // Ensure subjects are loaded before accessing
            if (File.Exists(settingsPath))
            {
                try
                {
                    var json = File.ReadAllText(settingsPath);
                    var options = new JsonSerializerOptions();
                    options.Converters.Add(new JsonStringEnumConverter());

                    CurrentSettings = JsonSerializer.Deserialize<UserSettings>(json, options) ?? new UserSettings();

                    // Validate username integrity
                    if (!CurrentSettings.IsUsernameValid())
                    {
                        // Username was tampered with - clear it
                        CurrentSettings.Username = null;
                        Save(); // Save the corrected settings
                    }

                    // Ensure MySubjectLevels is initialized
                    if (CurrentSettings.MySubjectLevels == null)
                    {
                        CurrentSettings.MySubjectLevels = new Dictionary<string, Level>();
                    }

                    // Initialize default levels for subjects that don't have them
                    foreach (var subject in allSubjects)
                    {
                        if (!CurrentSettings.MySubjectLevels.ContainsKey(subject.SubjectId))
                        {
                            CurrentSettings.MySubjectLevels[subject.SubjectId] = Level.HL; // Default to HL
                        }
                    }

                    MySubjects = allSubjects?
                        .Where(s => s != null && CurrentSettings.MySubjectIds.Contains(s.SubjectId))
                        .ToList() ?? new List<Subject>();
                }
                catch (Exception ex)
                {
                    CurrentSettings = new UserSettings();
                    // Ensure MySubjectLevels is initialized even after exception
                    if (CurrentSettings.MySubjectLevels == null)
                    {
                        CurrentSettings.MySubjectLevels = new Dictionary<string, Level>();
                    }
                }
            }
            // If settings missing, set subjects to all
            else
            {
                MySubjects = allSubjects;
                // Initialize levels for all subjects when creating new settings
                if (CurrentSettings.MySubjectLevels == null)
                {
                    CurrentSettings.MySubjectLevels = new Dictionary<string, Level>();
                }
                foreach (var subject in allSubjects)
                {
                    if (!CurrentSettings.MySubjectLevels.ContainsKey(subject.SubjectId))
                    {
                        CurrentSettings.MySubjectLevels[subject.SubjectId] = Level.HL;
                    }
                }
            }

            // Set theme
            if (CurrentSettings.Theme == "Dark")
            {
                DarkTheme();
            }
            else
            {
                LightTheme();
            }

            return CurrentSettings;
        }

        public static void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(settingsPath));

                var options = new JsonSerializerOptions { WriteIndented = true };
                options.Converters.Add(new JsonStringEnumConverter());

                File.WriteAllText(settingsPath, JsonSerializer.Serialize(CurrentSettings, options));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settings: {ex.Message}");
            }
        }

        public static void Save(UserSettings settings)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(settingsPath));

                var options = new JsonSerializerOptions { WriteIndented = true };
                options.Converters.Add(new JsonStringEnumConverter());

                File.WriteAllText(settingsPath, JsonSerializer.Serialize(settings, options));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settings: {ex.Message}");
            }
        }

        public static void InitializeDefaultFiles()
        {
            string dataFolder = Path.Combine(Application.StartupPath, "Data");

            Directory.CreateDirectory(dataFolder);

            // Create settings.json if missing
            if (!File.Exists(settingsPath))
            {
                var defaultSettings = new UserSettings();
                Save(defaultSettings);
            }

            // Create other files if missing
            CreateFileIfMissing(Path.Combine(dataFolder, "subjects.json"), "[]");
            CreateFileIfMissing(Path.Combine(dataFolder, "topics.json"), "[]");
            CreateFileIfMissing(Path.Combine(dataFolder, "flashcards.json"), "[]");
        }

        private static void CreateFileIfMissing(string filePath, string defaultContent)
        {
            if (!File.Exists(filePath))
            {
                File.WriteAllText(filePath, defaultContent);
            }
        }

        public static void UnblockUser(string username)
        {
            if (CurrentSettings.BlockedUsers.Contains(username))
            {
                CurrentSettings.BlockedUsers.Remove(username);
                Save();
            }
        }

        // Updated method to set username securely
        public static void SetUsername(string username)
        {
            CurrentSettings.Username = username; // This will auto-generate the hash
            Save();
        }

        // Updated LogOut method
        public static void LogOut()
        {
            CurrentSettings.Username = null; // This will clear the hash too
            Save();
        }

        public static void UpdateMySubjects(List<string> subjects)
        {
            CurrentSettings.MySubjectIds = subjects;
            Save();
            Load(); // Reload to refresh MySubjects list
        }

        // Update subject level
        public static void UpdateSubjectLevel(string subjectId, Level level)
        {
            if (CurrentSettings.MySubjectLevels == null)
            {
                CurrentSettings.MySubjectLevels = new Dictionary<string, Level>();
            }

            CurrentSettings.MySubjectLevels[subjectId] = level;
            Save();
        }

        // Get subject level
        public static Level GetSubjectLevel(string subjectId)
        {
            if (CurrentSettings.MySubjectLevels == null)
            {
                CurrentSettings.MySubjectLevels = new Dictionary<string, Level>();
            }

            return CurrentSettings.MySubjectLevels.ContainsKey(subjectId)
                ? CurrentSettings.MySubjectLevels[subjectId]
                : Level.HL; // Default to HL
        }

        //Theme settings
        public static void ChangeTheme()
        {
            if (CurrentSettings.Theme == "Dark")
            {
                LightTheme();
                CurrentSettings.Theme = "Light";
            }
            else
            {
                DarkTheme();
                CurrentSettings.Theme = "Dark";
            }
            Save();
        }

        // Color codes for themes
        private static void LightTheme()
        {
            FlashcardColor = Color.FromArgb(210, 200, 190);    // Darker, less pink
            BackgroundColor = Color.FromArgb(240, 235, 230);   // Less pink background  
            ButtonColor = Color.FromArgb(255, 130, 100);       // More orange coral
            MenuColor = Color.FromArgb(45, 50, 55);           // Professional blue-gray
            TextColor = Color.FromArgb(40, 40, 40);           // Dark gray text
        }
        private static void DarkTheme()
        {
            FlashcardColor = Color.FromArgb(28, 26, 24);     // Warm dark for cards
            BackgroundColor = Color.FromArgb(48, 45, 42);   // Warm medium gray
            ButtonColor = Color.FromArgb(205, 80, 68);     // Coral
            MenuColor = Color.FromArgb(20, 18, 16);       // Warm deep black
            TextColor = Color.White;
        }
    }
}
/*using IBrary.App_settings;
using IBrary.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IBrary.Managers
{
    internal static class SettingsManager
    {
        // json file path
        private static readonly string settingsPath = Path.Combine(
        Application.StartupPath,
        "Data", "settings.json");

        public static UserSettings CurrentSettings { get; set; } = new UserSettings();
        public static List<Subject> MySubjects { get; private set; } = new List<Subject>();

        public static Color FlashcardColor;
        public static Color BackgroundColor;
        public static Color ButtonColor;
        public static Color MenuColor;
        public static Color TextColor;

        // LOAD SETTINGS FROM MEMORY
        public static UserSettings Load()
        {
            List<Subject> allSubjects = SubjectManager.Load(); // Ensure subjects are loaded before accessing
            if (File.Exists(settingsPath))
            {
                try
                {
                    var json = File.ReadAllText(settingsPath);
                    var options = new JsonSerializerOptions();
                    options.Converters.Add(new JsonStringEnumConverter());

                    CurrentSettings = JsonSerializer.Deserialize<UserSettings>(json, options) ?? new UserSettings();

                    // Ensure MySubjectLevels is initialized
                    if (CurrentSettings.MySubjectLevels == null)
                    {
                        CurrentSettings.MySubjectLevels = new Dictionary<string, Level>();
                    }

                    // Initialize default levels for subjects that don't have them
                    foreach (var subject in allSubjects)
                    {
                        if (!CurrentSettings.MySubjectLevels.ContainsKey(subject.SubjectId))
                        {
                            CurrentSettings.MySubjectLevels[subject.SubjectId] = Level.HL; // Default to HL
                        }
                    }

                    MySubjects = allSubjects?
                        .Where(s => s != null && CurrentSettings.MySubjectIds.Contains(s.SubjectId))
                        .ToList() ?? new List<Subject>();
                }
                catch (Exception ex)
                {
                    CurrentSettings = new UserSettings();
                    // Ensure MySubjectLevels is initialized even after exception
                    if (CurrentSettings.MySubjectLevels == null)
                    {
                        CurrentSettings.MySubjectLevels = new Dictionary<string, Level>();
                    }
                }
            }
            // If settings missing, set subjects to all
            else
            {
                MySubjects = allSubjects;
                // Initialize levels for all subjects when creating new settings
                if (CurrentSettings.MySubjectLevels == null)
                {
                    CurrentSettings.MySubjectLevels = new Dictionary<string, Level>();
                }
                foreach (var subject in allSubjects)
                {
                    if (!CurrentSettings.MySubjectLevels.ContainsKey(subject.SubjectId))
                    {
                        CurrentSettings.MySubjectLevels[subject.SubjectId] = Level.HL;
                    }
                }
            }

            // Set theme
            if (CurrentSettings.Theme == "Dark")
            {
                DarkTheme();
            }
            else
            {
                LightTheme();
            }

            return CurrentSettings;
        }

        public static void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(settingsPath));

                var options = new JsonSerializerOptions { WriteIndented = true };
                options.Converters.Add(new JsonStringEnumConverter());

                File.WriteAllText(settingsPath, JsonSerializer.Serialize(CurrentSettings, options));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settings: {ex.Message}");
            }
        }

        public static void Save(UserSettings settings)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(settingsPath));

                var options = new JsonSerializerOptions { WriteIndented = true };
                options.Converters.Add(new JsonStringEnumConverter());

                File.WriteAllText(settingsPath, JsonSerializer.Serialize(settings, options));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settings: {ex.Message}");
            }
        }

        public static void InitializeDefaultFiles()
        {
            string dataFolder = Path.Combine(Application.StartupPath, "Data");

            Directory.CreateDirectory(dataFolder);

            // Create settings.json if missing
            if (!File.Exists(settingsPath))
            {
                var defaultSettings = new UserSettings();
                Save(defaultSettings);
            }

            // Create other files if missing
            CreateFileIfMissing(Path.Combine(dataFolder, "subjects.json"), "[]");
            CreateFileIfMissing(Path.Combine(dataFolder, "topics.json"), "[]");
            CreateFileIfMissing(Path.Combine(dataFolder, "flashcards.json"), "[]");
        }

        private static void CreateFileIfMissing(string filePath, string defaultContent)
        {
            if (!File.Exists(filePath))
            {
                File.WriteAllText(filePath, defaultContent);
            }
        }

        public static void UnblockUser(string username)
        {
            if (CurrentSettings.BlockedUsers.Contains(username))
            {
                CurrentSettings.BlockedUsers.Remove(username);
                Save();
            }
        }

        public static void LogOut()
        {
            CurrentSettings.Username = null;
            Save();
        }

        public static void UpdateMySubjects(List<string> subjects)
        {
            CurrentSettings.MySubjectIds = subjects;
            Save();
            Load(); // Reload to refresh MySubjects list
        }

        // New method to update subject level
        public static void UpdateSubjectLevel(string subjectId, Level level)
        {
            if (CurrentSettings.MySubjectLevels == null)
            {
                CurrentSettings.MySubjectLevels = new Dictionary<string, Level>();
            }

            CurrentSettings.MySubjectLevels[subjectId] = level;
            Save();
        }

        // New method to get subject level
        public static Level GetSubjectLevel(string subjectId)
        {
            if (CurrentSettings.MySubjectLevels == null)
            {
                CurrentSettings.MySubjectLevels = new Dictionary<string, Level>();
            }

            return CurrentSettings.MySubjectLevels.ContainsKey(subjectId)
                ? CurrentSettings.MySubjectLevels[subjectId]
                : Level.HL; // Default to HL
        }

        //Theme settings
        public static void ChangeTheme()
        {
            if (CurrentSettings.Theme == "Dark")
            {
                LightTheme();
                CurrentSettings.Theme = "Light";
            }
            else
            {
                DarkTheme();
                CurrentSettings.Theme = "Dark";
            }
            Save();
        }

        // Color codes for themes
        private static void LightTheme()
        {
            FlashcardColor = Color.FromArgb(210, 200, 190);    // Darker, less pink
            BackgroundColor = Color.FromArgb(240, 235, 230);   // Less pink background  
            ButtonColor = Color.FromArgb(255, 130, 100);       // More orange coral
            MenuColor = Color.FromArgb(45, 50, 55);           // Professional blue-gray
            TextColor = Color.FromArgb(40, 40, 40);           // Dark gray text
        }
        private static void DarkTheme()
        {
            FlashcardColor = Color.FromArgb(28, 26, 24);     // Warm dark for cards
            BackgroundColor = Color.FromArgb(48, 45, 42);   // Warm medium gray
            ButtonColor = Color.FromArgb(205, 80, 68);     // Coral
            MenuColor = Color.FromArgb(20, 18, 16);       // Warm deep black
            TextColor = Color.White;
        }
    }
}*/

/*using IBrary.App_settings;
using IBrary.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IBrary.Managers
{
    internal static class SettingsManager
    {
        // json file path
        private static readonly string settingsPath = Path.Combine(
        Application.StartupPath,
        "Data", "settings.json");
        
        public static UserSettings CurrentSettings { get; set; } = new UserSettings();
        public static List<Subject> MySubjects { get; private set; } = new List<Subject>();

        public static Color FlashcardColor;
        public static Color BackgroundColor;
        public static Color ButtonColor;
        public static Color MenuColor;
        public static Color TextColor;

        // LOAD SETTINGS FROM MEMORY
        public static UserSettings Load()
        {
            
            List<Subject> allSubjects = SubjectManager.Load(); // Ensure subjects are loaded before accessing
            if (File.Exists(settingsPath))
            {
                try
                {
                    var json = File.ReadAllText(settingsPath);
                    var options = new JsonSerializerOptions();
                    options.Converters.Add(new JsonStringEnumConverter());

                    CurrentSettings = JsonSerializer.Deserialize<UserSettings>(json, options) ?? new UserSettings();

                    MySubjects = allSubjects?
                        .Where(s => s != null && CurrentSettings.MySubjectIds.Contains(s.SubjectId))
                        .ToList() ?? new List<Subject>();
                }
                catch (Exception ex)
                {
                    CurrentSettings = new UserSettings();
                }
            }
            // If settings missing, set subjects to all
            else
            {
                MySubjects = allSubjects;
            }

            // Set theme
            if (CurrentSettings.Theme == "Dark")
            {
                DarkTheme();
            }
            else
            {
                LightTheme();
            }

            return CurrentSettings;
        }


        public static void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(settingsPath));

                var options = new JsonSerializerOptions { WriteIndented = true };
                options.Converters.Add(new JsonStringEnumConverter());

                File.WriteAllText(settingsPath, JsonSerializer.Serialize(CurrentSettings, options));
            }
            catch (Exception ex)
            {
                 
            }
        }
        public static void Save(UserSettings settings)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(settingsPath));

                var options = new JsonSerializerOptions { WriteIndented = true };
                options.Converters.Add(new JsonStringEnumConverter());

                File.WriteAllText(settingsPath, JsonSerializer.Serialize(settings, options));

            }
            catch (Exception ex)
            {

            }
        }
        public static void InitializeDefaultFiles()
        {
            string dataFolder = Path.Combine(Application.StartupPath, "Data");

            Directory.CreateDirectory(dataFolder);

            // Create settings.json if missing
            if (!File.Exists(settingsPath))
            {
                var defaultSettings = new UserSettings();
                Save(defaultSettings);
            }

            // Create other files if missing
            CreateFileIfMissing(Path.Combine(dataFolder, "subjects.json"), "[]");
            CreateFileIfMissing(Path.Combine(dataFolder, "topics.json"), "[]");
            CreateFileIfMissing(Path.Combine(dataFolder, "flashcards.json"), "[]");
        }

        private static void CreateFileIfMissing(string filePath, string defaultContent)
        {
            if (!File.Exists(filePath))
            {
                File.WriteAllText(filePath, defaultContent);
            }
        }

        public static void UnblockUser(string username)
        {
            if (CurrentSettings.BlockedUsers.Contains(username))
            {
                CurrentSettings.BlockedUsers.Remove(username);
                Save();
            }

        }
        public static void LogOut()
        {
            CurrentSettings.Username = null;
            Save();
        }
        public static void UpdateMySubjects(List<string> subjects)
        {
            CurrentSettings.MySubjectIds = subjects;
            Save();
            Load();
        }
        //Theme settings
        public static void ChangeTheme()
        {
            if(CurrentSettings.Theme == "Dark")
            {
                LightTheme();
                CurrentSettings.Theme = "Light";
            }
            else
            {
                DarkTheme();
                CurrentSettings.Theme = "Dark";
            }
            Save();
        }

        // Color codes for themes
        private static void LightTheme()
        {
            FlashcardColor = Color.FromArgb(210, 200, 190);    // Darker, less pink
            BackgroundColor = Color.FromArgb(240, 235, 230);   // Less pink background  
            ButtonColor = Color.FromArgb(255, 130, 100);       // More orange coral
            MenuColor = Color.FromArgb(45, 50, 55);           // Professional blue-gray
            TextColor = Color.FromArgb(40, 40, 40);           // Dark gray text
        }
        private static void DarkTheme()
        {
            FlashcardColor = Color.FromArgb(28, 26, 24);     // Warm dark for cards
            BackgroundColor = Color.FromArgb(48, 45, 42);   // Warm medium gray
            ButtonColor = Color.FromArgb(205, 80, 68);     // Coral
            MenuColor = Color.FromArgb(20, 18, 16);       // Warm deep black
            TextColor = Color.White;
        }
    }

}*/
