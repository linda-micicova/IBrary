using IBrary.Managers;
using IBrary.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace IBrary.UserControls
{
    public partial class SettingsUserControl : UserControl
    {
        private Panel LoginPanel;
        private Panel SettingsPanel;
        private PictureBox ProfilePictureBox;
        private Label UsernameLabel = new Label();
        private MinimalButton LogInOutButton;
        private MinimalButton MySubjectsButton;
        private MinimalButton PriorityCalculationsButton;
        private MinimalButton BlockedUsersButton;
        private MinimalButton ImportButton;
        private MinimalButton ImportQuizletButton;
        private MinimalButton ExportButton;
        private MinimalButton ThemeButton;
        private MinimalButton ResetFlashcardStatsButton;
        //private MinimalButton SyncButton;

        // Public events for main form to subscribe to
        public event EventHandler LoginRequested;
        public event EventHandler MySubjectsRequested;
        public event EventHandler PriorityCalculationsRequested;
        public event EventHandler ChangeThemeRequested;
        public event EventHandler BlockedUsersRequested;
        public SettingsUserControl()
        {
            InitializeComponent();
            InitializeUI();
            this.Resize += Settings_Resize;
        }
        private void LoadLoginPanel()
        {
            LoginPanel?.Controls.Clear(); // Dispose of the old panel if it exists
            App.Settings.Load(); // Ensure settings are loaded before accessing
            
            ProfilePictureBox = new PictureBox
            {
                Size = new Size(20, 20),
                Image = App.Settings.CurrentSettings.Theme == "Light" ? Properties.Resources.userBlack : Properties.Resources.userWhite, // Use a default image if none is set
                SizeMode = PictureBoxSizeMode.Zoom,
            };
            LogInOutButton = new MinimalButton
            {
                Size = new Size(100, 30)
            };

            UsernameLabel.ForeColor = App.Settings.TextColor;

            if (App.Settings.CurrentSettings.Username != null &&
                    !string.IsNullOrWhiteSpace(App.Settings.CurrentSettings.Username))
            {
                UsernameLabel.Text = App.Settings.CurrentSettings.Username;
                LogInOutButton.Text = "Log Out";

                LogInOutButton.Click += (s, e) =>
                {
                    DialogResult dialogResult = MessageBox.Show(
                        "Are you sure you want to log out?",
                        "Confirm Logout",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (dialogResult == DialogResult.Yes)
                    {
                        App.Settings.LogOut();
                    }
                    LoadLoginPanel();
                    UpdateSizes();
                };
            }
            else
            {
                UsernameLabel.Text = "Not logged in";
                LogInOutButton.Text = "Log In";
                LogInOutButton.Click += (s, e) => LoginRequested?.Invoke(this, EventArgs.Empty);
            }
            LoginPanel.Controls.Add(ProfilePictureBox);
            LoginPanel.Controls.Add(LogInOutButton);
            LoginPanel.Controls.Add(UsernameLabel);

        }
        private void InitializeUI()
        {
            // Set the background color of the user control
            this.BackColor = App.Settings.BackgroundColor;

            LoginPanel = new Panel
            {
                Padding = new Padding(10),
                Location = new Point(0, 0),
            };

            LoadLoginPanel();
            
            
            SettingsPanel = new Panel
            {
                
            };
            MySubjectsButton = new MinimalButton
            {
                Text = "My Subjects"
            };
            PriorityCalculationsButton = new MinimalButton
            {
                Text = "Flashcard priority calculations"
            };
            BlockedUsersButton = new MinimalButton
            {
                Text = "Blocked Users"
            };
            ImportButton = new MinimalButton
            {
                Text = "Import data"
            };
            ImportButton.Click += (s, e) => ImportButton_Click(s, e);
            ImportQuizletButton = new MinimalButton
            {
                Text = "Import from quizlet exporter"
            };
            ImportQuizletButton.Click += (s, e) =>
            {
                ImportQuizletButton_Click(s, e);
            };
            ExportButton = new MinimalButton
            {
                Text = "Export data"
            };
            ExportButton.Click += (s, e) =>
            {
                ExportButton_Click(s, e);
            };
            ThemeButton = new MinimalButton
            {
                Text = "Switch theme"
            };
            ResetFlashcardStatsButton = new MinimalButton
            {
                Text = "Reset all flashcard stats"
            };
            ResetFlashcardStatsButton.Click += (s, e) =>
            {
                var result = MessageBox.Show(
                    "Are you sure you want to reset all flashcard statistics? All progress will be reset. This action cannot be undone.",
                    "Confirm Reset",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    FlashcardManager.ResetAllFlashcardStats();
                }
            };
            /*SyncButton = new MinimalButton
            {
                Text = "Manage Network Sync"
            };*/

            // Set the font for all labels in the user control
            foreach (Control control in this.Controls)
            {
                if (control is Label label)
                {
                    label.Font = new Font("Arial", 12, FontStyle.Regular);
                }
            }

            MySubjectsButton.Click += (s, e) => Navigator.GoToMySubjects();
            PriorityCalculationsButton.Click += (s, e) => Navigator.GoToPriorityCalculations();
            BlockedUsersButton.Click += (s, e) => Navigator.GoToBlockedUsers();
            //SyncButton.Click += (s, e) => Navigator.GoToManageNetworkSync();

            ThemeButton.Click += (s, e) =>
            {
                App.Settings.ChangeTheme();
                Navigator.RefreshCurrentScreen(); // This will call OnThemeChanged in MainForm
            };


            SettingsPanel.Controls.Add(MySubjectsButton);
            SettingsPanel.Controls.Add(PriorityCalculationsButton);
            SettingsPanel.Controls.Add(BlockedUsersButton);
            SettingsPanel.Controls.Add(ImportButton);
            SettingsPanel.Controls.Add(ImportQuizletButton);
            SettingsPanel.Controls.Add(ExportButton);
            SettingsPanel.Controls.Add(ThemeButton);
            SettingsPanel.Controls.Add(ResetFlashcardStatsButton);
            //SettingsPanel.Controls.Add(SyncButton);


            this.Controls.Add(LoginPanel);
            this.Controls.Add(SettingsPanel);


        }

        private void ImportButton_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "JSON Files (*.json)|*.json";
                ofd.Title = "Select IBrary Data Export File";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string json = File.ReadAllText(ofd.FileName);
                        dynamic importData = JsonConvert.DeserializeObject(json);

                        // Import each data type
                        ImportFlashcards(importData.Flashcards, importData.Settings);
                        ImportTopics(importData.Topics);
                        ImportSubjects(importData.Subjects);
                        ImportSettings(importData.Settings);

                        MessageBox.Show("Flashcards, topics and subjects imported successfully!");

                        // Refresh your UI components
                        RefreshAllData();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error importing data: {ex.Message}");
                    }
                }
            }
        }
        private void ImportFlashcards(dynamic flashcardsData, dynamic settingsData)
        {
            if (flashcardsData != null)
            {
                var flashcards = JsonConvert.DeserializeObject<List<Flashcard>>(flashcardsData.ToString());

                string username = "";
                if (settingsData?.Username != null)
                {
                    username = settingsData.Username.ToString();
                }

                FlashcardManager.MergeFlashcards(flashcards, username);
            }
        }

        private void ImportTopics(dynamic topicsData)
        {
            if (topicsData != null)
            {
                var topics = JsonConvert.DeserializeObject<List<Topic>>(topicsData.ToString());
                App.Topics.MergeTopics(topics); // or your save method
            }
        }

        private void ImportSubjects(dynamic subjectsData)
        {
            if (subjectsData != null)
            {
                var subjects = JsonConvert.DeserializeObject<List<Subject>>(subjectsData.ToString());
                App.Subjects.MergeSubjects(subjects); // or your save method
            }
        }

        private void ImportSettings(dynamic settingsData)
        {
            if (settingsData != null)
            {
                try
                {
                    var importedSettings = JsonConvert.DeserializeObject<UserSettings>(settingsData.ToString());

                    // Check if the imported username matches the current logged-in user
                    if (importedSettings.Username == App.Settings.CurrentSettings.Username)
                    {
                        App.Settings.Save(importedSettings);
                        MessageBox.Show("Settings imported successfully!");
                    }
                    else
                    {
                        string currentUser = App.Settings.CurrentSettings.Username ?? "guest";
                        MessageBox.Show($"Settings not imported - they belong to user '{importedSettings.Username}' but you are logged in as '{currentUser}'.");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Warning: Could not import settings. {ex.Message}");
                }
            }
        }

        private void RefreshAllData()
        {
            // Refresh your UI components after import
            // Example:
            // LoadSubjectsToComboBox();
            // LoadTopicsToList();
            // RefreshFlashcardDisplay();
            // ApplySettings();
        }
        private void ImportQuizletButton_Click(object sender, EventArgs e)
        {
            Navigator.GoToImportFromQuizlet();
        }
        
        private void ExportButton_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "JSON Files (*.json)|*.json";
                sfd.FileName = "IBraryDataExport.json";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        // Ask user about including stats and settings (linked together)
                        var includeStatsAndSettings = MessageBox.Show(
                            "Do you want to include statistics and settings in the export?",
                            "Include Statistics & Settings?",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question) == DialogResult.Yes;

                        // Get flashcards with or without stats
                        var flashcards = App.Flashcards.Load();
                        if (!includeStatsAndSettings)
                        {
                            flashcards = FlashcardManager.ResetAllFlashcardStats(flashcards);
                        }

                        // Create export data object
                        var exportData = new
                        {
                            Flashcards = flashcards,
                            Topics = App.Topics.Load(),
                            Subjects = App.Subjects.Load(),
                            Settings = includeStatsAndSettings ? App.Settings.Load() : null,
                            ExportDate = DateTime.Now,
                            Version = "1.0",
                            IncludesStats = includeStatsAndSettings,
                            IncludesSettings = includeStatsAndSettings
                        };

                        string json = JsonConvert.SerializeObject(exportData, Newtonsoft.Json.Formatting.Indented);
                        File.WriteAllText(sfd.FileName, json);

                        MessageBox.Show("Data exported successfully!");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error exporting data: {ex.Message}");
                    }
                }
            }
        }

        private void Settings_Resize(object sender, EventArgs e)
            => UpdateSizes();
        private void UpdateSizes()
        {
            //LoginPanel
            LoginPanel.Size = new Size(this.Width, this.Height / 10);

            //Controls in LoginPanel
            ProfilePictureBox.Location = new Point(LoginPanel.Width / 20, LoginPanel.Height / 3);
            UsernameLabel.Location = new Point(ProfilePictureBox.Right + 10, ProfilePictureBox.Top + 5);
            LogInOutButton.Location = new Point(UsernameLabel.Right + 10, ProfilePictureBox.Top - 5);

            //SettingsPanel
            SettingsPanel.Location = new Point(0, LoginPanel.Bottom + 10);
            SettingsPanel.Size = new Size(this.Width, this.Height - LoginPanel.Height - 10);

            //Buttons in SettingsPanel
            foreach (Control control in SettingsPanel.Controls)
            {
                if (control is Button button)
                {
                    button.Size = new Size(LogInOutButton.Right - button.Left, Math.Max(SettingsPanel.Height / 15, 30));
                }
            }

            MySubjectsButton.Location = new Point(SettingsPanel.Width / 20, 10);
            PriorityCalculationsButton.Location = new Point(SettingsPanel.Width / 20, MySubjectsButton.Bottom + 10);
            BlockedUsersButton.Location = new Point(SettingsPanel.Width / 20, PriorityCalculationsButton.Bottom + 10);
            ImportButton.Location = new Point(SettingsPanel.Width / 20, BlockedUsersButton.Bottom + 10);
            ImportQuizletButton.Location = new Point(SettingsPanel.Width / 20, ImportButton.Bottom + 10);
            ExportButton.Location = new Point(SettingsPanel.Width / 20, ImportQuizletButton.Bottom + 10);
            ThemeButton.Location = new Point(SettingsPanel.Width / 20, ExportButton.Bottom + 10);
            ResetFlashcardStatsButton.Location = new Point(SettingsPanel.Width / 20, ThemeButton.Bottom + 10);
            //SyncButton.Location = new Point(SettingsPanel.Width / 20, ResetFlashcardStatsButton.Bottom + 10);

            foreach (Control control in SettingsPanel.Controls)
            {
                if (control is Button button)
                {
                    button.Size = new Size(LogInOutButton.Right - button.Left, Math.Max(SettingsPanel.Height / 15, 30));
                }
            }
        }
    }
}
