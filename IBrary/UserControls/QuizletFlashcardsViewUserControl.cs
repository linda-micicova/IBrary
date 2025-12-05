using IBrary.Managers;
using IBrary.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace IBrary.UserControls
{
    public partial class QuizletFlashcardsViewUserControl : UserControl
    {
        // UI Elements
        private PictureBox _backIcon;
        private Panel flashcardsContainer;
        private ScrollableControl scrollPanel;
        private MinimalButton confirmImportButton;
        private Label titleLabel;

        // Data
        private List<Flashcard> previewFlashcards;
        private Subject selectedSubject;
        private List<Topic> availableTopics;
        private Dictionary<string, List<string>> flashcardTopicAssignments; // FlashcardId -> TopicIds

        public event EventHandler ImportConfirmed;

        public QuizletFlashcardsViewUserControl()
        {
            InitializeComponent();
            flashcardTopicAssignments = new Dictionary<string, List<string>>();
        }

        public void Initialize(List<Flashcard> flashcards, Subject subject)
        {
            previewFlashcards = flashcards;
            selectedSubject = subject;
            availableTopics = TopicManager.Load()
                .Where(t => subject.Topics.Contains(t.TopicId))
                .ToList();

            InitializeUI();
            CreateFlashcardPreviews();
        }

        private void InitializeUI()
        {
            CreateBackButton();
            CreateTitleAndControls();
            CreateScrollableContainer();
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
            _backIcon.Click += (s, e) => this.Parent?.Controls.Remove(this);
            this.Controls.Add(_backIcon);
        }

        private void CreateTitleAndControls()
        {
            titleLabel = new Label
            {
                Text = $"Preview Import - {selectedSubject.SubjectName}",
                Font = new Font("Arial", 16, FontStyle.Bold),
                ForeColor = SettingsManager.TextColor,
                Location = new Point(70, 20),
                AutoSize = true
            };

            confirmImportButton = new MinimalButton
            {
                Text = "Confirm Import",
                Size = new Size(150, 40),
                Location = new Point(this.Width - 170, 15)
            };
            confirmImportButton.Click += ConfirmImportButton_Click;

            this.Controls.Add(titleLabel);
            this.Controls.Add(confirmImportButton);
        }

        private void CreateScrollableContainer()
        {
            scrollPanel = new Panel
            {
                Location = new Point(20, 70),
                Size = new Size(this.Width - 40, this.Height - 90),
                AutoScroll = true,
                BackColor = SettingsManager.BackgroundColor
            };

            flashcardsContainer = new Panel
            {
                Location = new Point(0, 0),
                AutoSize = true,
                BackColor = Color.Transparent
            };

            scrollPanel.Controls.Add(flashcardsContainer);
            this.Controls.Add(scrollPanel);
        }

        private void CreateFlashcardPreviews()
        {
            flashcardsContainer.Controls.Clear();
            int yPosition = 10;

            foreach (var flashcard in previewFlashcards)
            {
                var previewPanel = CreateFlashcardPreviewPanel(flashcard, yPosition);
                flashcardsContainer.Controls.Add(previewPanel);
                yPosition += previewPanel.Height + 20;
            }

            // Update container size
            flashcardsContainer.Size = new Size(scrollPanel.Width - 20, yPosition);
        }

        private Panel CreateFlashcardPreviewPanel(Flashcard flashcard, int yPosition)
        {
            var panel = new Panel
            {
                Location = new Point(10, yPosition),
                Size = new Size(scrollPanel.Width - 40, 200),
                BackColor = SettingsManager.FlashcardColor,
                BorderStyle = BorderStyle.FixedSingle
            };

            // Flashcard content
            var version = flashcard.Versions?.OrderByDescending(v => v.Timestamp).FirstOrDefault();
            if (version != null)
            {
                var questionLabel = new Label
                {
                    Text = $"Q: {version.Question ?? "No question"}",
                    Font = new Font("Arial", 12, FontStyle.Bold),
                    ForeColor = SettingsManager.TextColor,
                    Location = new Point(10, 10),
                    Size = new Size(panel.Width - 20, 30),
                    AutoEllipsis = true
                };

                var answerLabel = new Label
                {
                    Text = $"A: {version.Answer ?? "No answer"}",
                    Font = new Font("Arial", 10),
                    ForeColor = SettingsManager.TextColor,
                    Location = new Point(10, 45),
                    Size = new Size(panel.Width - 20, 25),
                    AutoEllipsis = true
                };

                panel.Controls.Add(questionLabel);
                panel.Controls.Add(answerLabel);
            }

            // Topic selection
            var topicLabel = new Label
            {
                Text = "Select Topics:",
                Font = new Font("Arial", 10, FontStyle.Bold),
                ForeColor = SettingsManager.TextColor,
                Location = new Point(10, 80),
                AutoSize = true
            };

            var topicCheckedListBox = new CheckedListBox
            {
                Font = new Font("Arial", 9),
                CheckOnClick = true,
                BackColor = SettingsManager.BackgroundColor,
                ForeColor = SettingsManager.TextColor,
                Location = new Point(10, 105),
                Size = new Size(panel.Width - 20, 85),
                Tag = flashcard.FlashcardId // Store flashcard ID for reference
            };

            // Populate topics
            topicCheckedListBox.DataSource = availableTopics.ToList();
            topicCheckedListBox.DisplayMember = "TopicName";
            topicCheckedListBox.ValueMember = "TopicId";

            // Initialize assignment tracking
            if (!flashcardTopicAssignments.ContainsKey(flashcard.FlashcardId))
            {
                flashcardTopicAssignments[flashcard.FlashcardId] = new List<string>();
            }

            // Handle topic selection changes
            topicCheckedListBox.ItemCheck += (sender, e) =>
            {
                this.BeginInvoke((Action)(() =>
                {
                    var checkedListBox = sender as CheckedListBox;
                    var flashcardId = checkedListBox.Tag.ToString();

                    // Update assignments
                    flashcardTopicAssignments[flashcardId] = checkedListBox.CheckedItems
                        .Cast<Topic>()
                        .Select(t => t.TopicId)
                        .ToList();
                }));
            };

            panel.Controls.Add(topicLabel);
            panel.Controls.Add(topicCheckedListBox);

            return panel;
        }

        private void ConfirmImportButton_Click(object sender, EventArgs e)
        {
            // Validate that all flashcards have at least one topic assigned
            var unassignedFlashcards = flashcardTopicAssignments
                .Where(kvp => kvp.Value.Count == 0)
                .Select(kvp => kvp.Key)
                .ToList();

            if (unassignedFlashcards.Count > 0)
            {
                var result = MessageBox.Show(
                    $"{unassignedFlashcards.Count} flashcard(s) have no topics assigned. " +
                    "Do you want to continue without importing them?",
                    "Unassigned Flashcards",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.No)
                    return;
            }

            try
            {
                // Apply topic assignments to flashcards
                foreach (var flashcard in previewFlashcards)
                {
                    if (flashcardTopicAssignments.ContainsKey(flashcard.FlashcardId) &&
                        flashcardTopicAssignments[flashcard.FlashcardId].Count > 0)
                    {
                        flashcard.Topics = flashcardTopicAssignments[flashcard.FlashcardId];

                        // Add flashcard to subject
                        if (!selectedSubject.Flashcards.Contains(flashcard.FlashcardId))
                        {
                            selectedSubject.Flashcards.Add(flashcard.FlashcardId);
                        }
                    }
                }

                // Filter out unassigned flashcards
                var flashcardsToImport = previewFlashcards
                    .Where(f => flashcardTopicAssignments.ContainsKey(f.FlashcardId) &&
                               flashcardTopicAssignments[f.FlashcardId].Count > 0)
                    .ToList();

                // Save flashcards using new method
                FlashcardManager.ImportFlashcards(flashcardsToImport);

                MessageBox.Show($"Successfully imported {flashcardsToImport.Count} flashcard(s)!");

                // Trigger import confirmed event
                ImportConfirmed?.Invoke(this, EventArgs.Empty);

                // Remove this control
                this.Parent?.Controls.Remove(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error importing flashcards: {ex.Message}");
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            if (scrollPanel != null)
            {
                scrollPanel.Size = new Size(this.Width - 40, this.Height - 90);

                if (confirmImportButton != null)
                {
                    confirmImportButton.Location = new Point(this.Width - 170, 15);
                }

                // Recreate flashcard previews with new size
                if (previewFlashcards != null)
                {
                    CreateFlashcardPreviews();
                }
            }
        }
    }
}
