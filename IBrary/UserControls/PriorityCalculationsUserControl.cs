using System;
using System.Drawing;
using System.Windows.Forms;
using IBrary.Managers;
using IBrary.Models;

namespace IBrary.UserControls
{
    public partial class PriorityCalculationsUserControl : UserControl
    {
        // UI Elements
        private PictureBox _backIcon;
        private Label titleLabel;
        private Label _errorRateLabel, _timeFactorLabel, _importantLabel;

        // Constants
        private const int ButtonSpacing = 250;
        private const int ButtonWidth = 200;
        private const int ButtonHeight = 50;
        public EventHandler SettingsRequested;

        public PriorityCalculationsUserControl()
        {
            InitializeComponent();
            InitializeUI();
            this.Resize += PriorityCalculations_Resize;
        }

        private void InitializeUI()
        {
            CreateBackButton();
            CreateTitle();
            CreatePrioritySections();
        }

        private void CreateBackButton()
        {
            _backIcon = new PictureBox
            {
                Image = SettingsManager.CurrentSettings.Theme == "Light"
                    ? Properties.Resources.arrow
                    : Properties.Resources.backWhite,
                SizeMode = PictureBoxSizeMode.Zoom,
                Location = new Point(20, 20),
                Size = new Size(30, 30),
                Cursor = Cursors.Hand
            };
            _backIcon.Click += (s, e) => Navigator.GoToSettings();
            this.Controls.Add(_backIcon);
        }

        private void CreateTitle()
        {
            titleLabel = new Label
            {
                Text = "Priority Calculations",
                Font = new Font("Arial", 16, FontStyle.Bold),
                ForeColor = SettingsManager.TextColor,
                Location = new Point(70, 20),
                AutoSize = true
            };
            this.Controls.Add(titleLabel);
        }

        private void CreatePrioritySections()
        {
            int yPosition = 100;

            // Error Rate Section
            _errorRateLabel = CreateSectionLabel("Importance of error rate", yPosition);
            var errorRatePanel = CreateButtonPanel(_errorRateLabel.Bottom + 10);
            CreatePriorityButtons(errorRatePanel, "ErrorRateWeight");

            // Time Factor Section
            yPosition += 150;
            _timeFactorLabel = CreateSectionLabel("Importance of when flashcard was studied", yPosition);
            var timeFactorPanel = CreateButtonPanel(_timeFactorLabel.Bottom + 10);
            CreatePriorityButtons(timeFactorPanel, "TimeFactorWeight");

            // Star Tag Section
            yPosition += 150;
            _importantLabel = CreateSectionLabel("Importance of star tag", yPosition);
            var starTagPanel = CreateButtonPanel(_importantLabel.Bottom + 10);
            CreatePriorityButtons(starTagPanel, "StarTagWeight");
        }

        private Panel CreateButtonPanel(int yPos)
        {
            var panel = new Panel
            {
                Location = new Point(20, yPos),
                Size = new Size(ButtonWidth * 3 + ButtonSpacing * 2, ButtonHeight + 20),
                BackColor = Color.Transparent
            };
            this.Controls.Add(panel);
            return panel;
        }

        private Label CreateSectionLabel(string text, int yPos)
        {
            var label = new Label
            {
                Text = text,
                Font = new Font("Arial", 12),
                ForeColor = SettingsManager.TextColor,
                Location = new Point(20, yPos),
                AutoSize = true
            };
            this.Controls.Add(label);
            return label;
        }

        private void CreatePriorityButtons(Panel parentPanel, string settingKey)
        {
            string[] priorityLevels = { "Low", "Medium", "High" };
            int[] weightValues = { 3, 5, 8 };

            for (int i = 0; i < priorityLevels.Length; i++)
            {
                var button = new MinimalButton
                {
                    Text = priorityLevels[i],
                    Location = new Point(i * (ButtonWidth + ButtonSpacing), 10),
                    Size = new Size(ButtonWidth, ButtonHeight),
                    BackColor = GetCurrentWeight(settingKey) == weightValues[i] ? SettingsManager.ButtonColor : SettingsManager.FlashcardColor,
                    Tag = (settingKey, weightValues[i])
                };

                button.Click += PriorityButton_Click;
                parentPanel.Controls.Add(button);
            }
        }

        private void PriorityButton_Click(object sender, EventArgs e)
        {
            if (sender is MinimalButton button &&
                button.Tag is ValueTuple<string, int> setting)
            {
                var (settingKey, weight) = setting;

                // Update settings
                switch (settingKey)
                {
                    case "ErrorRateWeight":
                        SettingsManager.CurrentSettings.ErrorRateWeight = weight;
                        break;
                    case "TimeFactorWeight":
                        SettingsManager.CurrentSettings.TimeFactorWeight = weight;
                        break;
                    case "StarTagWeight":
                        SettingsManager.CurrentSettings.ImportantTagWeight = weight;
                        break;
                }

                SettingsManager.Save();

                // Reset all buttons in this panel to base color
                var parentPanel = button.Parent as Panel;
                ResetButtonColors(parentPanel);

                // Set clicked button to bright color
                button.BackColor = SettingsManager.ButtonColor;
            }
        }

        private void ResetButtonColors(Panel panel)
        {
            foreach (Control control in panel.Controls)
            {
                if (control is MinimalButton btn)
                {
                    btn.BackColor = SettingsManager.FlashcardColor;
                }
            }
        }

        private void SetInitialButtonState(Panel panel, string settingKey)
        {
            int currentWeight = GetCurrentWeight(settingKey);

            foreach (Control control in panel.Controls)
            {
                if (control is MinimalButton btn &&
                    btn.Tag is ValueTuple<string, int> setting)
                {
                    var (key, weight) = setting;
                    if (weight == currentWeight)
                    {
                        btn.BackColor = SettingsManager.ButtonColor;
                    }
                }
            }
        }

        private int GetCurrentWeight(string settingKey)
        {
            switch (settingKey)
            {
                case "ErrorRateWeight":
                    return SettingsManager.CurrentSettings.ErrorRateWeight;
                case "TimeFactorWeight":
                    return SettingsManager.CurrentSettings.TimeFactorWeight;
                case "StarTagWeight":
                    return SettingsManager.CurrentSettings.ImportantTagWeight;
                default:
                    return 5; // Default to medium
            }
        }

        private void PriorityCalculations_Resize(object sender, EventArgs e)
        {
            // Handle responsive layout if needed
        }
    }
}