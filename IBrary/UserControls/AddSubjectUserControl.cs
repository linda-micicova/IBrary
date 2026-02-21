using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using IBrary.Models;
using IBrary.Managers;

namespace IBrary.UserControls
{
    public partial class AddSubjectUserControl : UserControl
    {
        private TextBox subjectNameTextBox;
        private MinimalButton saveButton;

        private Label subjectNameLabel;

        public event Action<UserControl> RequestUserControlSwitch;

        public AddSubjectUserControl()
        {
            InitializeComponent();
            InitializeUI();
            this.Resize += AddSubject_Resize;
        }

        private void InitializeUI()
        {
            
            // Labels
            subjectNameLabel = new Label
            {
                Text = "Subject Name:",
                Font = new Font("Arial", 12, FontStyle.Regular),
                ForeColor = App.Settings.TextColor,
                AutoSize = true
            };


            // Input controls
            subjectNameTextBox = new TextBox
            {
                Font = new Font("Arial", 12, FontStyle.Regular),
                BackColor = App.Settings.FlashcardColor,
                ForeColor = App.Settings.TextColor,
                BorderStyle = BorderStyle.FixedSingle
            };


            // Load all topics
            var allTopics = App.Topics.Load();

            // Button
            saveButton = new MinimalButton
            {
                Text = "Save Subject",
                Size = new Size(120, 35)
            };
            saveButton.Click += SaveButton_Click;

            // Add controls to form
            this.Controls.Add(subjectNameLabel);
            this.Controls.Add(subjectNameTextBox);
            this.Controls.Add(saveButton);

            UpdateSizes();
        }
        private void SaveButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(subjectNameTextBox.Text))
            {
                MessageBox.Show("Please enter a subject name.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            App.Subjects.Load();

            var newSubject = new Subject
            {
                SubjectId = Guid.NewGuid().ToString(),
                SubjectName = subjectNameTextBox.Text.Trim(),
                Flashcards = new List<string>()
            };

            App.Subjects.AddSubject(newSubject);

            MessageBox.Show(
                $"Subject '{newSubject.SubjectName}' created successfully!\n\nYou can add it to your subjects in Settings.",
                "Success",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            // Ask user if they want to add to their subjects
            /*var result = MessageBox.Show(
            $"Do you want to add '{newSubject.SubjectName}' to your subjects?\n\nYes = HL\nNo = SL\nCancel = Don't add",
            "Add to Your Subjects",
            MessageBoxButtons.YesNoCancel,
            MessageBoxIcon.Question);

            var currentMySubjects = SettingsManager.MySubjects.Select(s => s.SubjectId).ToList();

            if (result == DialogResult.Yes) // Yes, HL
            {
                if (!currentMySubjects.Contains(newSubject.SubjectId))
                {
                    currentMySubjects.Add(newSubject.SubjectId);
                    SettingsManager.UpdateMySubjects(currentMySubjects);
                }
                MessageBox.Show($"Subject '{newSubject.SubjectName}' created and added to your subjects as HL!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else if (result == DialogResult.No) // Yes, SL
            {
                if (!currentMySubjects.Contains(newSubject.SubjectId))
                {
                    currentMySubjects.Add(newSubject.SubjectId);
                    SettingsManager.UpdateMySubjects(currentMySubjects);
                }
                MessageBox.Show($"Subject '{newSubject.SubjectName}' created and added to your subjects as SL!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else // Cancel - No
            {
                MessageBox.Show($"Subject '{newSubject.SubjectName}' created successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }*/

            ClearForm();
        }
        
        /*private void SaveButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(subjectNameTextBox.Text))
            {
                MessageBox.Show("Please enter a subject name.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SubjectManager.Load();

            var newSubject = new Subject
            {
                SubjectId = Guid.NewGuid().ToString(),
                SubjectName = subjectNameTextBox.Text.Trim(),
                Flashcards = new List<string>()
            };

            SubjectManager.AddSubject(newSubject);

            MessageBox.Show($"Subject '{newSubject.SubjectName}' created successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

            ClearForm();
        }*/
        /*private void SaveButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(subjectNameTextBox.Text))
            {
                MessageBox.Show("Please enter a subject name.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Create new subject
            var newSubject = new Subject
            {
                SubjectId = Guid.NewGuid().ToString(),
                SubjectName = subjectNameTextBox.Text.Trim(),
                Flashcards = new List<string>() // Empty initially
            };

            // Save to manager
            SubjectManager.AddSubject(newSubject);

            MessageBox.Show($"Subject '{newSubject.SubjectName}' created successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

            // Clear form
            ClearForm();
        }*/

        private void ClearForm()
        {
            subjectNameTextBox.Text = "";
            subjectNameTextBox.Focus();
        }

        private void AddSubject_Resize(object sender, EventArgs e)
            => UpdateSizes();

        private void UpdateSizes()
        {
            var margin = 20;
            var labelHeight = 25;
            var inputHeight = 30;
            var controlSpacing = 15;
            var centerX = this.Width / 2;
            var controlWidth = Math.Min(400, this.Width - 2 * margin);

            // Calculate total height needed for all controls
            var totalControlHeight = 3 * 25 + 3 * inputHeight + 4 * controlSpacing + saveButton.Height; // labels + inputs + spacing + button

            // Start positioning - center vertically but with reasonable limits
            var startY = Math.Max(30, Math.Min(this.Height / 6, (this.Height - totalControlHeight) / 2));

            // Subject name
            subjectNameLabel.Location = new Point(centerX - controlWidth / 2, startY);
            subjectNameTextBox.Location = new Point(centerX - controlWidth / 2, subjectNameLabel.Bottom + 5);
            subjectNameTextBox.Size = new Size(controlWidth, inputHeight);

            // Button - positioned relative to last control with minimum margin
            var buttonY = subjectNameTextBox.Bottom + controlSpacing * 2;
            saveButton.Location = new Point(centerX - saveButton.Width / 2, buttonY);

            
        }
    }
}