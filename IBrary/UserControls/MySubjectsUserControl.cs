using IBrary.Managers;
using IBrary.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace IBrary.UserControls
{
    public partial class MySubjectsUserControl : UserControl
    {
        private PictureBox BackIcon;
        private Label titleLabel;
        private Panel subjectsPanel;
        private Dictionary<string, ComboBox> levelComboBoxes = new Dictionary<string, ComboBox>();

        public List<string> SelectedSubjects { get; private set; } = new List<string>();
        public EventHandler SettingsRequested;

        public MySubjectsUserControl()
        {
            InitializeComponent();
            InitializeUI();
        }

        private void InitializeUI()
        {
            CreateBackIcon();
            CreateTitle();
            CreateSubjectsPanel();
            LoadSubjects();
            AttachEventHandlers();
        }

        private void CreateBackIcon()
        {
            BackIcon = new PictureBox
            {
                Image = App.Settings.CurrentSettings.Theme == "Light"
                    ? Properties.Resources.arrow
                    : Properties.Resources.backWhite,
                SizeMode = PictureBoxSizeMode.Zoom,
                Location = new Point(20, 20),
                Size = new Size(30, 30),
                Cursor = Cursors.Hand
            };
            this.Controls.Add(BackIcon);
        }

        private void CreateTitle()
        {
            titleLabel = new Label
            {
                Text = "My Subjects",
                Font = new Font("Arial", 16, FontStyle.Bold),
                ForeColor = App.Settings.TextColor,
                Location = new Point(70, 20),
                AutoSize = true
            };
            this.Controls.Add(titleLabel);
        }

        private void CreateSubjectsPanel()
        {
            subjectsPanel = new Panel
            {
                Location = new Point(20, 70),
                Size = new Size(400, 500),
                AutoScroll = true,
                BackColor = App.Settings.BackgroundColor
            };
            this.Controls.Add(subjectsPanel);
        }

        private void LoadSubjects()
        {
            // Clear existing controls
            subjectsPanel.Controls.Clear();
            levelComboBoxes.Clear();

            int yPosition = 10;
            int rowHeight = 40;

            // Add all subjects
            foreach (var subject in App.Subjects.AllSubjects)
            {
                // Subject name label
                var nameLabel = new Label
                {
                    Text = subject.SubjectName,
                    Font = new Font("Segoe UI", 14, FontStyle.Regular),
                    ForeColor = App.Settings.TextColor,
                    Location = new Point(10, yPosition + 5),
                    AutoSize = true
                };
                subjectsPanel.Controls.Add(nameLabel);

                // Level ComboBox
                var levelCombo = new ComboBox
                {
                    Size = new Size(70, 30),
                    Location = new Point(300, yPosition),
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    BackColor = App.Settings.BackgroundColor,
                    ForeColor = App.Settings.TextColor,
                    Font = new Font("Segoe UI", 12)
                };

                levelCombo.Items.Add("-");
                levelCombo.Items.Add("SL");
                levelCombo.Items.Add("HL");

                // Set default or saved level
                if (App.Settings.MySubjects.Any(s => s.SubjectId == subject.SubjectId))
                {
                    var savedLevel = App.Settings.CurrentSettings.MySubjectLevels.ContainsKey(subject.SubjectId)
                        ? App.Settings.CurrentSettings.MySubjectLevels[subject.SubjectId]
                        : Level.HL;
                    levelCombo.SelectedItem = savedLevel.ToString();
                }
                else
                {
                    levelCombo.SelectedItem = "-";
                }

                levelCombo.SelectedIndexChanged += (s, e) => SaveLevelSelection();

                levelComboBoxes[subject.SubjectId] = levelCombo;
                subjectsPanel.Controls.Add(levelCombo);

                yPosition += rowHeight;
            }

            // Initialize SelectedSubjects
            UpdateSelectedSubjects();
        }

        private void SaveLevelSelection()
        {
            UpdateSelectedSubjects();
            App.Settings.UpdateMySubjects(SelectedSubjects);

            foreach (var kvp in levelComboBoxes)
            {
                var subjectId = kvp.Key;
                var combo = kvp.Value;

                if (combo.SelectedItem?.ToString() != "-" &&
                    Enum.TryParse<Level>(combo.SelectedItem?.ToString(), out var level))
                {
                    App.Settings.CurrentSettings.MySubjectLevels[subjectId] = level;
                }
                else if (combo.SelectedItem?.ToString() == "-")
                {
                    // Remove from levels if set to "-"
                    if (App.Settings.CurrentSettings.MySubjectLevels.ContainsKey(subjectId))
                    {
                        App.Settings.CurrentSettings.MySubjectLevels.Remove(subjectId);
                    }
                }
            }
            App.Settings.Save();
        }

        private void AttachEventHandlers()
        {
            BackIcon.Click += (s, e) =>
            {
                UpdateSelectedSubjects();
                App.Settings.UpdateMySubjects(SelectedSubjects);
                SaveLevelSelection();
                Navigator.GoToSettings();
            };
        }

        private void UpdateSelectedSubjects()
        {
            SelectedSubjects.Clear();
            foreach (var kvp in levelComboBoxes)
            {
                if (kvp.Value.SelectedItem?.ToString() != "-")
                {
                    SelectedSubjects.Add(kvp.Key);
                }
            }
        }
    }
}
/*using IBrary.Managers;
using IBrary.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace IBrary.UserControls
{
    public partial class MySubjectsUserControl : UserControl
    {
        private PictureBox BackIcon;
        private Label titleLabel;
        private CheckedListBox SubjectsListBox;
        private Dictionary<string, ComboBox> levelComboBoxes = new Dictionary<string, ComboBox>();

        public List<string> SelectedSubjects { get; private set; } = new List<string>();
        public EventHandler SettingsRequested;

        public MySubjectsUserControl()
        {
            InitializeComponent();
            InitializeUI();
        }

        private void InitializeUI()
        {
            CreateBackIcon();
            CreateTitle();
            CreateSubjectsListBox();
            LoadSubjects();
            AttachEventHandlers();
        }

        private void CreateBackIcon()
        {
            BackIcon = new PictureBox
            {
                Image = SettingsManager.CurrentSettings.Theme == "Light"
                    ? Properties.Resources.arrow
                    : Properties.Resources.backWhite,
                SizeMode = PictureBoxSizeMode.Zoom,
                Location = new Point(20, 20),
                Size = new Size(30, 30),
                Cursor = Cursors.Hand
            };
            this.Controls.Add(BackIcon);
        }

        private void CreateTitle()
        {
            titleLabel = new Label
            {
                Text = "My Subjects",
                Font = new Font("Arial", 16, FontStyle.Bold),
                ForeColor = SettingsManager.TextColor,
                Location = new Point(70, 20),
                AutoSize = true
            };
            this.Controls.Add(titleLabel);
        }

        private void CreateSubjectsListBox()
        {
            SubjectsListBox = new CheckedListBox
            {
                Size = new Size(350, 500),
                Location = new Point(20, 70),
                ForeColor = SettingsManager.TextColor,
                BackColor = SettingsManager.BackgroundColor,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 16, FontStyle.Regular),
                DisplayMember = "SubjectName",
                IntegralHeight = false,
                ScrollAlwaysVisible = false
            };
            this.Controls.Add(SubjectsListBox);
        }

        private void LoadSubjects()
        {
            // Clear existing controls
            foreach (var combo in levelComboBoxes.Values)
            {
                this.Controls.Remove(combo);
                combo.Dispose();
            }
            levelComboBoxes.Clear();

            // Add all subjects
            foreach (var subject in SubjectManager.AllSubjects)
            {
                SubjectsListBox.Items.Add(subject);

                // Create level ComboBox for each subject
                var levelCombo = new ComboBox
                {
                    Size = new Size(60, 25),
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    BackColor = SettingsManager.BackgroundColor,
                    ForeColor = SettingsManager.TextColor,
                    Font = new Font("Segoe UI", 12)
                };

                levelCombo.Items.Add("SL");
                levelCombo.Items.Add("HL");

                // Set default or saved level
                var savedLevel = SettingsManager.CurrentSettings.MySubjectLevels.ContainsKey(subject.SubjectId)
                    ? SettingsManager.CurrentSettings.MySubjectLevels[subject.SubjectId]
                    : Level.HL;
                levelCombo.SelectedItem = savedLevel.ToString();

                levelCombo.SelectedIndexChanged += (s, e) => SaveLevelSelection();

                levelComboBoxes[subject.SubjectId] = levelCombo;
                this.Controls.Add(levelCombo);
            }

            // Position the ComboBoxes and check subjects
            PositionLevelControls();
            CheckMySubjects();

            // Initialize SelectedSubjects
            foreach (Subject item in SubjectsListBox.CheckedItems)
            {
                SelectedSubjects.Add(item.SubjectId);
            }
        }

        private void PositionLevelControls()
        {
            for (int i = 0; i < SubjectsListBox.Items.Count; i++)
            {
                var subject = (Subject)SubjectsListBox.Items[i];
                if (levelComboBoxes.ContainsKey(subject.SubjectId))
                {
                    var combo = levelComboBoxes[subject.SubjectId];

                    // Position next to each subject item
                    int itemHeight = SubjectsListBox.GetItemHeight(i);
                    combo.Location = new Point(SubjectsListBox.Right + 5, SubjectsListBox.Top + (i * itemHeight) + 2);
                }
            }
        }

        private void CheckMySubjects()
        {
            // Auto-check subjects that are in MySubjects
            for (int i = 0; i < SubjectsListBox.Items.Count; i++)
            {
                Subject subject = (Subject)SubjectsListBox.Items[i];
                if (SettingsManager.MySubjects.Any(s => s.SubjectId == subject.SubjectId))
                {
                    SubjectsListBox.SetItemChecked(i, true);
                }
            }
        }

        private void SaveLevelSelection()
        {
            foreach (var kvp in levelComboBoxes)
            {
                var subjectId = kvp.Key;
                var combo = kvp.Value;

                if (Enum.TryParse<Level>(combo.SelectedItem?.ToString(), out var level))
                {
                    SettingsManager.CurrentSettings.MySubjectLevels[subjectId] = level;
                }
            }
            SettingsManager.Save();
        }

        private void AttachEventHandlers()
        {
            SubjectsListBox.ItemCheck += SubjectsListBox_ItemCheck;

            BackIcon.Click += (s, e) =>
            {
                UpdateSelectedSubjects();
                SettingsManager.UpdateMySubjects(SelectedSubjects);
                SaveLevelSelection(); // Ensure levels are saved
                Navigator.GoToSettings();
            };
        }

        private void UpdateSelectedSubjects()
        {
            SelectedSubjects.Clear();
            foreach (Subject item in SubjectsListBox.CheckedItems)
            {
                SelectedSubjects.Add(item.SubjectId);
            }
        }

        private void SubjectsListBox_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            this.BeginInvoke((MethodInvoker)(() =>
            {
                UpdateSelectedSubjects();
                SettingsManager.UpdateMySubjects(SelectedSubjects);
            }));
        }

        // Handle resizing to reposition level controls
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (levelComboBoxes.Count > 0)
            {
                PositionLevelControls();
            }
        }
    }
}*/
/*using IBrary.Managers;
using IBrary.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace IBrary.UserControls
{
    public partial class MySubjectsUserControl : UserControl
    {
        private PictureBox BackIcon;
        private Label titleLabel;
        private CheckedListBox SubjectsListBox;
        private Dictionary<string, ComboBox> levelComboBoxes = new Dictionary<string, ComboBox>();

        public List<string> SelectedSubjects { get; private set; } = new List<string>();
        public EventHandler SettingsRequested;

        public MySubjectsUserControl()
        {
            InitializeComponent();
            InitializeUI();
        }

        private void InitializeUI()
        {
            CreateBackIcon();
            CreateTitle();
            CreateSubjectsListBox();
            LoadSubjects();
            AttachEventHandlers();
        }

        private void CreateBackIcon()
        {
            BackIcon = new PictureBox
            {
                Image = SettingsManager.CurrentSettings.Theme == "Light"
                    ? Properties.Resources.arrow
                    : Properties.Resources.backWhite,
                SizeMode = PictureBoxSizeMode.Zoom,
                Location = new Point(20, 20),
                Size = new Size(30, 30),
                Cursor = Cursors.Hand
            };
            this.Controls.Add(BackIcon);
        }

        private void CreateTitle()
        {
            titleLabel = new Label
            {
                Text = "My Subjects",
                Font = new Font("Arial", 16, FontStyle.Bold),
                ForeColor = SettingsManager.TextColor,
                Location = new Point(70, 20),
                AutoSize = true
            };
            this.Controls.Add(titleLabel);
        }

        private void CreateSubjectsListBox()
        {
            SubjectsListBox = new CheckedListBox
            {
                Size = new Size(200, 300), // Made narrower to fit level selection
                Location = new Point(20, 70),
                ForeColor = SettingsManager.TextColor,
                BackColor = SettingsManager.BackgroundColor,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 16, FontStyle.Regular),
                DisplayMember = "SubjectName"
            };
            this.Controls.Add(SubjectsListBox);
        }

        private void LoadSubjects()
        {
            // Clear existing controls
            foreach (var combo in levelComboBoxes.Values)
            {
                this.Controls.Remove(combo);
                combo.Dispose();
            }
            levelComboBoxes.Clear();

            // Add all subjects
            foreach (var subject in SubjectManager.AllSubjects)
            {
                SubjectsListBox.Items.Add(subject);

                // Create level ComboBox for each subject
                var levelCombo = new ComboBox
                {
                    Size = new Size(60, 25),
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    BackColor = SettingsManager.BackgroundColor,
                    ForeColor = SettingsManager.TextColor,
                    Font = new Font("Segoe UI", 12)
                };

                levelCombo.Items.Add("SL");
                levelCombo.Items.Add("HL");

                // Set default or saved level
                var savedLevel = SettingsManager.CurrentSettings.MySubjectLevels.ContainsKey(subject.SubjectId)
                    ? SettingsManager.CurrentSettings.MySubjectLevels[subject.SubjectId]
                    : Level.HL;
                levelCombo.SelectedItem = savedLevel.ToString();

                levelCombo.SelectedIndexChanged += (s, e) => SaveLevelSelection();

                levelComboBoxes[subject.SubjectId] = levelCombo;
                this.Controls.Add(levelCombo);
            }

            // Position the ComboBoxes and check subjects
            PositionLevelControls();
            CheckMySubjects();

            // Initialize SelectedSubjects
            foreach (Subject item in SubjectsListBox.CheckedItems)
            {
                SelectedSubjects.Add(item.SubjectId);
            }
        }

        private void PositionLevelControls()
        {
            for (int i = 0; i < SubjectsListBox.Items.Count; i++)
            {
                var subject = (Subject)SubjectsListBox.Items[i];
                if (levelComboBoxes.ContainsKey(subject.SubjectId))
                {
                    var combo = levelComboBoxes[subject.SubjectId];

                    // Position next to each subject item
                    int itemHeight = SubjectsListBox.GetItemHeight(i);
                    combo.Location = new Point(SubjectsListBox.Right + 10, SubjectsListBox.Top + (i * itemHeight) + 2);
                }
            }
        }

        private void CheckMySubjects()
        {
            // Auto-check subjects that are in MySubjects
            for (int i = 0; i < SubjectsListBox.Items.Count; i++)
            {
                Subject subject = (Subject)SubjectsListBox.Items[i];
                if (SettingsManager.MySubjects.Any(s => s.SubjectId == subject.SubjectId))
                {
                    SubjectsListBox.SetItemChecked(i, true);
                }
            }
        }

        private void SaveLevelSelection()
        {
            foreach (var kvp in levelComboBoxes)
            {
                var subjectId = kvp.Key;
                var combo = kvp.Value;

                if (Enum.TryParse<Level>(combo.SelectedItem?.ToString(), out var level))
                {
                    SettingsManager.CurrentSettings.MySubjectLevels[subjectId] = level;
                }
            }
            SettingsManager.Save();
        }

        private void AttachEventHandlers()
        {
            SubjectsListBox.ItemCheck += SubjectsListBox_ItemCheck;

            BackIcon.Click += (s, e) =>
            {
                UpdateSelectedSubjects();
                SettingsManager.UpdateMySubjects(SelectedSubjects);
                SaveLevelSelection(); // Ensure levels are saved
                Navigator.GoToSettings();
            };
        }

        private void UpdateSelectedSubjects()
        {
            SelectedSubjects.Clear();
            foreach (Subject item in SubjectsListBox.CheckedItems)
            {
                SelectedSubjects.Add(item.SubjectId);
            }
        }

        private void SubjectsListBox_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            this.BeginInvoke((MethodInvoker)(() =>
            {
                UpdateSelectedSubjects();
                SettingsManager.UpdateMySubjects(SelectedSubjects);
            }));
        }

        // Handle resizing to reposition level controls
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (levelComboBoxes.Count > 0)
            {
                PositionLevelControls();
            }
        }
    }
}*/
/*using IBrary.Managers;
using IBrary.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace IBrary.UserControls
{
    public partial class MySubjectsUserControl : UserControl
    {
        private PictureBox BackIcon;
        private Label titleLabel;
        private CheckedListBox SubjectsListBox;

        public List<string> SelectedSubjects { get; private set; } = new List<string>();
        public EventHandler SettingsRequested;

        public MySubjectsUserControl()
        {
            InitializeComponent();
            InitializeUI();
        }

        private void InitializeUI()
        {
            CreateBackIcon();
            CreateTitle();
            CreateSubjectsListBox();
            LoadSubjects();
            AttachEventHandlers();
        }

        private void CreateBackIcon()
        {
            BackIcon = new PictureBox
            {
                Image = SettingsManager.CurrentSettings.Theme == "Light"
                    ? Properties.Resources.arrow
                    : Properties.Resources.backWhite,
                SizeMode = PictureBoxSizeMode.Zoom,
                Location = new Point(20, 20),
                Size = new Size(30, 30),
                Cursor = Cursors.Hand
            };
            this.Controls.Add(BackIcon);
        }

        private void CreateTitle()
        {
            titleLabel = new Label
            {
                Text = "My Subjects",
                Font = new Font("Arial", 16, FontStyle.Bold),
                ForeColor = SettingsManager.TextColor,
                Location = new Point(70, 20),
                AutoSize = true
            };
            this.Controls.Add(titleLabel);
        }

        private void CreateSubjectsListBox()
        {
            SubjectsListBox = new CheckedListBox
            {
                Size = new Size(250, 300),
                Location = new Point(20, 70), // Changed to align with title structure
                ForeColor = SettingsManager.TextColor,
                BackColor = SettingsManager.BackgroundColor,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 18, FontStyle.Regular),
                DisplayMember = "SubjectName"
            };
            this.Controls.Add(SubjectsListBox);
        }

        private void LoadSubjects()
        {
            // Add all subjects
            foreach (var subject in SubjectManager.AllSubjects)
            {
                SubjectsListBox.Items.Add(subject);
            }

            // Auto-check subjects that are in MySubjects
            for (int i = 0; i < SubjectsListBox.Items.Count; i++)
            {
                Subject subject = (Subject)SubjectsListBox.Items[i];
                if (SettingsManager.MySubjects.Any(s => s.SubjectId == subject.SubjectId))
                {
                    SubjectsListBox.SetItemChecked(i, true);
                }
            }

            // Initialize SelectedSubjects
            foreach (Subject item in SubjectsListBox.CheckedItems)
            {
                SelectedSubjects.Add(item.SubjectId);
            }
        }

        private void AttachEventHandlers()
        {
            SubjectsListBox.ItemCheck += SubjectsListBox_ItemCheck;

            BackIcon.Click += (s, e) =>
            {
                UpdateSelectedSubjects();
                SettingsManager.UpdateMySubjects(SelectedSubjects);
                Navigator.GoToSettings();
            };
        }

        private void UpdateSelectedSubjects()
        {
            SelectedSubjects.Clear();
            foreach (Subject item in SubjectsListBox.CheckedItems)
            {
                SelectedSubjects.Add(item.SubjectId);
            }
        }

        private void SubjectsListBox_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            this.BeginInvoke((MethodInvoker)(() =>
            {
                UpdateSelectedSubjects();
                SettingsManager.UpdateMySubjects(SelectedSubjects);
            }));
        }
    }
}*/