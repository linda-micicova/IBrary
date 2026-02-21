using IBrary.Managers;
using IBrary.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace IBrary.UserControls
{
    public partial class DashboardUserControl : UserControl
    {
        private Label titleLabel;
        private FlowLayoutPanel SubjectPanelContainer;

        public DashboardUserControl()
        {
            InitializeComponent();
            InitializeUI();
            this.Resize += Dashboard_Resize;
        }

        private void InitializeUI()
        {
            CreateTitle();
            CreateSubjectPanelContainer();
            LoadSubjects();
        }

        private void CreateTitle()
        {
            titleLabel = new Label
            {
                Text = "My Subjects Dashboard",
                Font = new Font("Arial", 16, FontStyle.Bold),
                ForeColor = App.Settings.TextColor,
                Location = new Point(20, 20),
                AutoSize = true
            };
            this.Controls.Add(titleLabel);
        }

        private void CreateSubjectPanelContainer()
        {
            SubjectPanelContainer = new FlowLayoutPanel
            {
                Location = new Point(0, 70),
                Size = new Size(this.Width, this.Height - 90),
                AutoScroll = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Padding = new Padding(this.Width / 100, this.Width / 100, this.Width / 100, 0)
            };
            this.Controls.Add(SubjectPanelContainer);
        }

        private void LoadSubjects()
        {
            foreach (var subject in App.Settings.MySubjects)
            {
                Panel subjectPanel = new Panel
                {
                    Width = (this.Width - 90) / 2,
                    Height = 80,
                    BackColor = App.Settings.FlashcardColor,
                    Margin = new Padding(left: this.Width / 100, top: this.Width / 100, right: 0, bottom: 0)
                };

                // Get the user's level for this subject, defaulting to HL if not found
                if (!App.Settings.CurrentSettings.MySubjectLevels.TryGetValue(subject.SubjectId, out Level userLevel))
                {
                    userLevel = Models.Level.HL;
                }

                subjectPanel.Cursor = Cursors.Hand;

                subjectPanel.Click += (s, eArgs) =>
                {
                    Navigator.GoToTopicsDashboard(subject);
                };

                Label nameLabel = new Label
                {
                    Text = subject.SubjectName + " " + userLevel,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    Location = new Point(10, 10),
                    AutoSize = true,
                    ForeColor = App.Settings.TextColor
                };

                Label accuracyLabel = new Label
                {
                    Text = subject.TotalSeenCount == 0
                        ? "Accuracy: - %"
                        : $"Accuracy: {subject.Accuracy:P1}",
                    Font = new Font("Segoe UI", 8),
                    Location = new Point(10, 30),
                    AutoSize = true,
                    ForeColor = App.Settings.TextColor
                };

                Label totalLabel = new Label
                {
                    Text = $"Total flashcards studied: {subject.TotalSeenCount}",
                    Font = new Font("Segoe UI", 8),
                    Location = new Point(10, 45),
                    AutoSize = true,
                    ForeColor = App.Settings.TextColor
                };

                subjectPanel.Controls.Add(nameLabel);
                subjectPanel.Controls.Add(accuracyLabel);
                subjectPanel.Controls.Add(totalLabel);

                SubjectPanelContainer.Controls.Add(subjectPanel);
            }
        }

        private void Dashboard_Resize(object sender, EventArgs e)
            => UpdateSizes();

        private void UpdateSizes()
        {
            // Update SubjectPanelContainer size to account for title
            SubjectPanelContainer.Size = new Size(this.Width, this.Height - 90);

            foreach (Control control in SubjectPanelContainer.Controls)
            {
                if (control is Panel subjectPanel)
                {
                    subjectPanel.Width = Math.Max(this.Width / 5, 150);
                    subjectPanel.Height = Math.Max(this.Height / 5, 100);
                    subjectPanel.Margin = new Padding(left: Math.Max(this.Width / 100, 10), top: Math.Max(this.Width / 100, 10), right: 0, bottom: 0);
                }
                SubjectPanelContainer.Padding = new Padding(Math.Max(this.Width / 100, 10), Math.Max(this.Width / 100, 10), Math.Max(this.Width / 100, 10), 0);
            }
        }
    }
}