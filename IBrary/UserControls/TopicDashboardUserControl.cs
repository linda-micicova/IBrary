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

namespace IBrary.UserControls
{
    public partial class TopicDashboardUserControl : UserControl
    {
        private FlowLayoutPanel TopicPanelContainer;
        private PictureBox _backIcon;
        private Label titleLabel;
        private List<Topic> allTopics;
        private List<Flashcard> allFlashcards;
        private Subject selectedSubject;

        public TopicDashboardUserControl(Subject subject)
        {
            selectedSubject = subject;
            InitializeComponent();
            LoadData();
            InitializeUI();
            this.Resize += TopicsDashboard_Resize;
        }

        private void LoadData()
        {
            allTopics = App.Topics.Load();
            allFlashcards = App.Flashcards.Load();
        }

        private void InitializeUI()
        {
            CreateBackButton();
            CreateTitle();
            CreateTopicPanels();
        }

        private void CreateBackButton()
        {
            _backIcon = new PictureBox
            {
                Image = App.Settings.CurrentSettings.Theme == "Light"
                    ? Properties.Resources.arrow
                    : Properties.Resources.backWhite,
                SizeMode = PictureBoxSizeMode.Zoom,
                Location = new Point(20, 20),
                Size = new Size(30, 30),
                Cursor = Cursors.Hand
            };
            _backIcon.Click += (s, e) => Navigator.GoToDashboard();
            this.Controls.Add(_backIcon);
        }

        private void CreateTitle()
        {
            titleLabel = new Label
            {
                Text = $"{selectedSubject.SubjectName} - Topics",
                Font = new Font("Arial", 16, FontStyle.Bold),
                ForeColor = App.Settings.TextColor,
                Location = new Point(70, 20),
                AutoSize = true
            };
            this.Controls.Add(titleLabel);
        }

        private void CreateTopicPanels()
        {
            TopicPanelContainer = new FlowLayoutPanel
            {
                Location = new Point(20, 70),
                Size = new Size(this.Width - 40, this.Height - 90),
                AutoScroll = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Padding = new Padding(10)
            };
            this.Controls.Add(TopicPanelContainer);

            // Get topics for this specific subject
            var subjectTopics = allTopics.Where(t => selectedSubject.Topics.Contains(t.TopicId)).ToList();

            foreach (var topic in subjectTopics)
            {
                Panel topicPanel = new Panel
                {
                    Width = 200,
                    Height = 120,
                    BackColor = App.Settings.FlashcardColor,
                    Margin = new Padding(10)
                };

                topicPanel.Cursor = Cursors.Hand;

                topicPanel.Click += (s, eArgs) =>
                {
                    // Navigate to flashcards filtered by this topic
                    // You could create a filtered flashcard view here
                };

                // Calculate topic statistics (only for this subject's flashcards)
                var topicFlashcards = allFlashcards
                    .Where(f => f.Topics.Contains(topic.TopicId) &&
                               selectedSubject.Flashcards.Contains(f.FlashcardId))
                    .ToList();

                int totalFlashcards = topicFlashcards.Count;
                int studiedFlashcards = topicFlashcards.Count(f => f.Seen > 0);
                int correctAnswers = topicFlashcards.Sum(f => f.Seen - f.Errors);
                int totalAnswers = topicFlashcards.Sum(f => f.Seen);
                double accuracy = totalAnswers > 0 ? (double)correctAnswers / totalAnswers : 0;

                Label nameLabel = new Label
                {
                    Text = topic.TopicName,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    Location = new Point(10, 10),
                    AutoSize = true,
                    ForeColor = App.Settings.TextColor
                };

                Label accuracyLabel = new Label
                {
                    Text = totalAnswers == 0
                        ? "Accuracy: - %"
                        : $"Accuracy: {accuracy:P1}",
                    Font = new Font("Segoe UI", 8),
                    Location = new Point(10, 35),
                    AutoSize = true,
                    ForeColor = App.Settings.TextColor
                };

                Label flashcardCountLabel = new Label
                {
                    Text = $"Flashcards: {totalFlashcards}",
                    Font = new Font("Segoe UI", 8),
                    Location = new Point(10, 55),
                    AutoSize = true,
                    ForeColor = App.Settings.TextColor
                };

                Label studiedLabel = new Label
                {
                    Text = $"Studied: {studiedFlashcards}",
                    Font = new Font("Segoe UI", 8),
                    Location = new Point(10, 75),
                    AutoSize = true,
                    ForeColor = App.Settings.TextColor
                };

                Label totalAnswersLabel = new Label
                {
                    Text = $"Total answers: {totalAnswers}",
                    Font = new Font("Segoe UI", 8),
                    Location = new Point(10, 95),
                    AutoSize = true,
                    ForeColor = App.Settings.TextColor
                };

                topicPanel.Controls.Add(nameLabel);
                topicPanel.Controls.Add(accuracyLabel);
                topicPanel.Controls.Add(flashcardCountLabel);
                topicPanel.Controls.Add(studiedLabel);
                topicPanel.Controls.Add(totalAnswersLabel);

                TopicPanelContainer.Controls.Add(topicPanel);
            }
        }

        private void TopicsDashboard_Resize(object sender, EventArgs e)
            => UpdateSizes();

        private void UpdateSizes()
        {
            if (TopicPanelContainer != null)
            {
                TopicPanelContainer.Size = new Size(this.Width - 40, this.Height - 90);

                foreach (Control control in TopicPanelContainer.Controls)
                {
                    if (control is Panel topicPanel)
                    {
                        topicPanel.Width = Math.Max(this.Width / 6, 180);
                        topicPanel.Height = Math.Max(this.Height / 6, 120);
                    }
                }
            }
        }
    }
}