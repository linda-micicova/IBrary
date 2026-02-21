using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using IBrary.Models;
using IBrary.Managers;

namespace IBrary.UserControls
{
    public partial class AddTopicUserControl : UserControl
    {
        private TextBox topicNameTextBox;
        private ComboBox levelComboBox;
        private ComboBox subjectComboBox;
        private MinimalButton saveButton;

        private Label topicNameLabel;
        private Label levelLabel;
        private Label subjectLabel;

        public event Action<UserControl> RequestUserControlSwitch;

        public AddTopicUserControl()
        {
            InitializeComponent();
            InitializeUI();
            this.Resize += AddTopic_Resize;
        }

        private void InitializeUI()
        {
            this.BackColor = App.Settings.BackgroundColor;

            // Labels
            topicNameLabel = new Label
            {
                Text = "Topic Name:",
                Font = new Font("Arial", 12, FontStyle.Regular),
                ForeColor = App.Settings.TextColor,
                AutoSize = true
            };

            levelLabel = new Label
            {
                Text = "Level:",
                Font = new Font("Arial", 12, FontStyle.Regular),
                ForeColor = App.Settings.TextColor,
                AutoSize = true
            };

            subjectLabel = new Label
            {
                Text = "Subject:",
                Font = new Font("Arial", 12, FontStyle.Regular),
                ForeColor = App.Settings.TextColor,
                AutoSize = true
            };

            // Input controls
            topicNameTextBox = new TextBox
            {
                Font = new Font("Arial", 12, FontStyle.Regular),
                BackColor = App.Settings.FlashcardColor,
                ForeColor = App.Settings.TextColor,
                BorderStyle = BorderStyle.FixedSingle
            };

            levelComboBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Arial", 12, FontStyle.Regular),
                BackColor = App.Settings.FlashcardColor,
                ForeColor = App.Settings.TextColor
            };

            subjectComboBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Arial", 12, FontStyle.Regular),
                BackColor = App.Settings.FlashcardColor,
                ForeColor = App.Settings.TextColor
            };

            // Load data
            levelComboBox.DataSource = Enum.GetValues(typeof(Level));
            subjectComboBox.DataSource = App.Settings.MySubjects;
            subjectComboBox.DisplayMember = "SubjectName";

            // Button
            saveButton = new MinimalButton
            {
                Text = "Save Topic",
                Size = new Size(120, 35)
            };
            saveButton.Click += SaveButton_Click;

            // Add controls to form
            this.Controls.Add(topicNameLabel);
            this.Controls.Add(topicNameTextBox);
            this.Controls.Add(levelLabel);
            this.Controls.Add(levelComboBox);
            this.Controls.Add(subjectLabel);
            this.Controls.Add(subjectComboBox);
            this.Controls.Add(saveButton);

            UpdateSizes();
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(topicNameTextBox.Text))
            {
                MessageBox.Show("Please enter a topic name.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (levelComboBox.SelectedItem == null)
            {
                MessageBox.Show("Please select a level.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (subjectComboBox.SelectedItem == null)
            {
                MessageBox.Show("Please select a subject.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Check if topic already exists
            var existingTopics = App.Topics.Load();
            if (existingTopics.Any(t => t.TopicName.Equals(topicNameTextBox.Text.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("A topic with this name already exists.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Create new topic
            var newTopic = new Topic
            {
                TopicId = Guid.NewGuid().ToString(),
                TopicName = topicNameTextBox.Text.Trim(),
                Level = (Level)levelComboBox.SelectedItem
            };

            // Get selected subject
            var selectedSubject = (Subject)subjectComboBox.SelectedItem;

            // Save topic to manager
            App.Topics.AddTopic(newTopic);

            // Add topic to selected subject in memory
            selectedSubject.Topics.Add(newTopic.TopicId);
            App.Subjects.Save(); // Save the updated subject

            MessageBox.Show($"Topic '{newTopic.TopicName}' created and added to '{selectedSubject.SubjectName}' successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

            // Clear form
            ClearForm();
        }

        private void ClearForm()
        {
            topicNameTextBox.Text = "";
            levelComboBox.SelectedIndex = -1;
            subjectComboBox.SelectedIndex = -1;
            topicNameTextBox.Focus();
        }

        private void AddTopic_Resize(object sender, EventArgs e)
            => UpdateSizes();

        private void UpdateSizes()
        {
            var margin = 20;
            var controlSpacing = 15;
            var inputHeight = 30;
            var centerX = this.Width / 2;
            var controlWidth = Math.Min(400, this.Width - 2 * margin);

            // Calculate total height needed for all controls
            var totalControlHeight = 3 * 25 + 3 * inputHeight + 4 * controlSpacing + saveButton.Height; // labels + inputs + spacing + button

            // Start positioning - center vertically but with reasonable limits
            var startY = Math.Max(30, Math.Min(this.Height / 6, (this.Height - totalControlHeight) / 2));

            // Topic name
            topicNameLabel.Location = new Point(centerX - controlWidth / 2, startY);
            topicNameTextBox.Location = new Point(centerX - controlWidth / 2, topicNameLabel.Bottom + 5);
            topicNameTextBox.Size = new Size(controlWidth, inputHeight);

            // Level
            levelLabel.Location = new Point(centerX - controlWidth / 2, topicNameTextBox.Bottom + controlSpacing);
            levelComboBox.Location = new Point(centerX - controlWidth / 2, levelLabel.Bottom + 5);
            levelComboBox.Size = new Size(controlWidth, inputHeight);

            // Subject
            subjectLabel.Location = new Point(centerX - controlWidth / 2, levelComboBox.Bottom + controlSpacing);
            subjectComboBox.Location = new Point(centerX - controlWidth / 2, subjectLabel.Bottom + 5);
            subjectComboBox.Size = new Size(controlWidth, inputHeight);

            // Button - positioned relative to last control with minimum margin
            var buttonY = subjectComboBox.Bottom + controlSpacing * 2;
            saveButton.Location = new Point(centerX - saveButton.Width / 2, buttonY);
        }
    }
}