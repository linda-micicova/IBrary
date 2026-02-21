using IBrary.Managers;
using IBrary.Models;
using IBrary.Properties;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace IBrary.UserControls
{
    public partial class AddOrEditFlashcardUserControl : UserControl
    {
        private RichTextBox questionRichTextBox;
        private RichTextBox answerRichTextBox;
        private ComboBox subjectComboBox;
        private CheckedListBox topicCheckedListBox;
        private ComboBox levelComboBox;
        private MinimalButton saveButton;

        //toto moc nejde
        private readonly string imageStoragePath = Path.Combine(Application.StartupPath, "FlashcardImages");

        private PictureBox questionPictureBox;
        private PictureBox answerPictureBox;
        private MinimalButton questionImageButton;
        private MinimalButton answerImageButton;
        private string questionImagePath = null;
        private string answerImagePath = null;

        private Label questionLabel;
        private Label answerLabel;
        private Label subjectLabel;
        private Label topicsLabel;
        private Label levelLabel;

        public Flashcard flashcard; // For editing existing flashcards
        public CardVersion cardVersion;
        public bool editting = false;

        public event Action<UserControl> RequestUserControlSwitch;

        public AddOrEditFlashcardUserControl()
        {
            InitializeComponent();
            Directory.CreateDirectory(imageStoragePath);
            InitializeUI();
            this.Resize += AddFlashcard_Resize;
        }

        public AddOrEditFlashcardUserControl(Flashcard flashcard, CardVersion cardVersion)
        {
            InitializeComponent();
            Directory.CreateDirectory(imageStoragePath);
            InitializeUI();
            this.Resize += AddFlashcard_Resize;

            this.flashcard = flashcard;
            this.cardVersion = cardVersion;

            SetupEdittingMode();
        }

        /*private void SetupEdittingMode()
        {
            editting = true;

            // Completely disable selectors
            subjectComboBox.Enabled = false;
            topicCheckedListBox.Enabled = false;
            levelComboBox.Enabled = false;

            // Populate tex fields and selectors
            questionRichTextBox.Text = cardVersion.Question;
            answerRichTextBox.Text = cardVersion.Answer;
            if (!string.IsNullOrEmpty(cardVersion.QuestionImagePath) && File.Exists(cardVersion.QuestionImagePath))
            {
                questionImagePath = cardVersion.QuestionImagePath;
                questionPictureBox.Image = Image.FromFile(questionImagePath);
            }
            if (!string.IsNullOrEmpty(cardVersion.AnswerImagePath) && File.Exists(cardVersion.AnswerImagePath))
            {
                answerImagePath = cardVersion.AnswerImagePath;
                answerPictureBox.Image = Image.FromFile(answerImagePath);
            }
            subjectComboBox.SelectedItem = SettingsManager.MySubjects.FirstOrDefault(s => flashcard.Topics.Any(sid => s.Topics.Contains(sid)));
            topicCheckedListBox.Items.Cast<Topic>().ToList().ForEach(t =>
            {
                if (flashcard.Topics.Contains(t.TopicId))
                    topicCheckedListBox.SetItemChecked(topicCheckedListBox.Items.IndexOf(t), true);
            });
        }*/
        private void SetupEdittingMode()
        {
            editting = true;

            // Completely disable selectors
            subjectComboBox.Enabled = false;
            topicCheckedListBox.Enabled = false;
            levelComboBox.Enabled = false;

            // Populate text fields and selectors
            questionRichTextBox.Text = cardVersion.Question;
            answerRichTextBox.Text = cardVersion.Answer;

            // CHANGED: Handle both absolute paths (old) and relative filenames (new)
            if (!string.IsNullOrEmpty(cardVersion.QuestionImagePath))
            {
                // If it's an absolute path, extract filename and build new path
                string imagePath = Path.IsPathRooted(cardVersion.QuestionImagePath)
                    ? Path.Combine(imageStoragePath, Path.GetFileName(cardVersion.QuestionImagePath))
                    : Path.Combine(imageStoragePath, cardVersion.QuestionImagePath);

                if (File.Exists(imagePath))
                {
                    questionImagePath = imagePath;
                    questionPictureBox.Image = Image.FromFile(questionImagePath);
                }
            }

            if (!string.IsNullOrEmpty(cardVersion.AnswerImagePath))
            {
                // If it's an absolute path, extract filename and build new path
                string imagePath = Path.IsPathRooted(cardVersion.AnswerImagePath)
                    ? Path.Combine(imageStoragePath, Path.GetFileName(cardVersion.AnswerImagePath))
                    : Path.Combine(imageStoragePath, cardVersion.AnswerImagePath);

                if (File.Exists(imagePath))
                {
                    answerImagePath = imagePath;
                    answerPictureBox.Image = Image.FromFile(answerImagePath);
                }
            }

            subjectComboBox.SelectedItem = App.Settings.MySubjects.FirstOrDefault(s => flashcard.Topics.Any(sid => s.Topics.Contains(sid)));
            topicCheckedListBox.Items.Cast<Topic>().ToList().ForEach(t =>
            {
                if (flashcard.Topics.Contains(t.TopicId))
                    topicCheckedListBox.SetItemChecked(topicCheckedListBox.Items.IndexOf(t), true);
            });
        }

        private void InitializeUI()
        {
            // Labels
            questionLabel = new Label
            {
                Text = "Question:",
                Font = new Font("Arial", 12, FontStyle.Regular),
                ForeColor = App.Settings.TextColor,
                AutoSize = true
            };

            answerLabel = new Label
            {
                Text = "Answer:",
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

            topicsLabel = new Label
            {
                Text = "Topics:",
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

            // Rich text boxes
            questionRichTextBox = new RichTextBox
            {
                Font = new Font("Arial", 12, FontStyle.Regular),
                BackColor = App.Settings.FlashcardColor,
                ForeColor = App.Settings.TextColor
            };

            answerRichTextBox = new RichTextBox
            {
                Font = new Font("Arial", 12, FontStyle.Regular),
                BackColor = App.Settings.FlashcardColor,
                ForeColor = App.Settings.TextColor,
            };

            // Combo boxes and lists
            subjectComboBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Arial", 12, FontStyle.Regular),
                BackColor = App.Settings.FlashcardColor,
                ForeColor = App.Settings.TextColor
            };

            topicCheckedListBox = new CheckedListBox
            {
                Font = new Font("Arial", 10, FontStyle.Regular),
                CheckOnClick = true,
                BackColor = App.Settings.FlashcardColor,
                ForeColor = App.Settings.TextColor,
                IntegralHeight = false
            };

            levelComboBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Arial", 12, FontStyle.Regular),
                BackColor = App.Settings.FlashcardColor,
                ForeColor = App.Settings.TextColor
            };

            // Load data
            subjectComboBox.DataSource = App.Settings.MySubjects;
            subjectComboBox.DisplayMember = "SubjectName";
            subjectComboBox.SelectedIndexChanged += SubjectComboBox_SelectedIndexChanged;

            levelComboBox.DataSource = Enum.GetValues(typeof(Level));

            topicCheckedListBox.DisplayMember = "TopicName";
            

            // Save button
            saveButton = new MinimalButton
            {
                Text = "Save",
                Size = new Size(120, 35)
            };
            saveButton.Click += SaveButton_Click;

            //OBRAZKY
            // Small picture boxes next to text fields
            questionPictureBox = new PictureBox
            {
                BackColor = App.Settings.FlashcardColor,
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom,
                Size = new Size(60, 60),
                Image = App.Settings.CurrentSettings.Theme == "Light" ? Resources.uploadBlack : Resources.uploadWhite
            };

            answerPictureBox = new PictureBox
            {
                BackColor = App.Settings.FlashcardColor,
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom,
                Size = new Size(60, 60),
                Image = App.Settings.CurrentSettings.Theme == "Light" ? Resources.uploadBlack : Resources.uploadWhite
            };

            questionImageButton = new MinimalButton
            {
                Text = "Question image",
                Size = new Size(120, 35)
            };
            questionImageButton.Click += QuestionImageButton_Click;

            answerImageButton = new MinimalButton
            {
                Text = "Answer image",
                Size = new Size(120, 35)
            };
            answerImageButton.Click += AnswerImageButton_Click;
            //KONIEC

            // Add all controls
            this.Controls.Add(questionLabel);
            this.Controls.Add(questionRichTextBox);
            this.Controls.Add(answerLabel);
            this.Controls.Add(answerRichTextBox);
            this.Controls.Add(subjectLabel);
            this.Controls.Add(subjectComboBox);
            this.Controls.Add(topicsLabel);
            this.Controls.Add(topicCheckedListBox);
            this.Controls.Add(levelLabel);
            this.Controls.Add(levelComboBox);
            this.Controls.Add(saveButton);
            this.Controls.Add(questionPictureBox);
            this.Controls.Add(answerPictureBox);
            this.Controls.Add(questionImageButton);
            this.Controls.Add(answerImageButton);

            UpdateSizes();
        }

        private void QuestionImageButton_Click(object sender, EventArgs e)
        {
            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.gif;*.bmp";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string uniqueFileName = GenerateUniqueFileName(openFileDialog.FileName);
                    questionImagePath = Path.Combine(imageStoragePath, uniqueFileName);
                    File.Copy(openFileDialog.FileName, questionImagePath);
                    questionPictureBox.Image = Image.FromFile(questionImagePath);
                }
            }
        }

        private void AnswerImageButton_Click(object sender, EventArgs e)
        {
            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.gif;*.bmp";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string uniqueFileName = GenerateUniqueFileName(openFileDialog.FileName);
                    answerImagePath = Path.Combine(imageStoragePath, uniqueFileName);
                    File.Copy(openFileDialog.FileName, answerImagePath);
                    answerPictureBox.Image = Image.FromFile(answerImagePath);
                }
            }
        }

        private void SubjectComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            var selectedSubject = subjectComboBox.SelectedItem as Subject;
            if (selectedSubject == null) return;


            var allTopics = App.Topics.Load();
            var topicIdSet = new HashSet<string>(selectedSubject.Topics);
            var filteredTopics = allTopics.Where(t => topicIdSet.Contains(t.TopicId)).ToList();

            topicCheckedListBox.DataSource = filteredTopics;
            topicCheckedListBox.DisplayMember = "TopicName";
        }

        /*private void SaveButton_Click(object sender, EventArgs e)
        {
            if(!ValidateInput()) return;

            var selectedSubject = (Subject)subjectComboBox.SelectedItem;
            var selectedTopicIds = topicCheckedListBox.CheckedItems
                .Cast<Topic>()
                .Select(t => t.TopicId)
                .ToList();

            if (editting)
            {
                var version = new CardVersion(
                    questionRichTextBox.Text,
                    answerRichTextBox.Text,
                    SettingsManager.CurrentSettings.Username,
                    questionImagePath,
                    answerImagePath
                );


                // Save flashcard
                FlashcardManager.EditFlashcard(flashcard.FlashcardId, version);

                // Clear form or go back
                Navigator.GoToFlashcards();
            }
            else
            {
                // Create flashcard
                var flashcard = new Flashcard(
                    questionRichTextBox.Text,
                    answerRichTextBox.Text,
                    (Level)levelComboBox.SelectedItem,
                    selectedTopicIds
                );

                // Save flashcard
                FlashcardManager.AddFlashcard(flashcard);
                SubjectManager.AddFlashcardToSubject(flashcard, selectedSubject);

                // Clear form or go back
                ClearForm();
            }
            MessageBox.Show("Flashcard saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

        }*/
        private void SaveButton_Click(object sender, EventArgs e)
        {
            if (!ValidateInput()) return;

            var selectedSubject = (Subject)subjectComboBox.SelectedItem;
            var selectedTopicIds = topicCheckedListBox.CheckedItems
                .Cast<Topic>()
                .Select(t => t.TopicId)
                .ToList();

            // CHANGE: Extract only filenames from full paths
            string questionImageFilename = questionImagePath != null ? Path.GetFileName(questionImagePath) : null;
            string answerImageFilename = answerImagePath != null ? Path.GetFileName(answerImagePath) : null;

            if (editting)
            {
                var version = new CardVersion(
                    questionRichTextBox.Text,
                    answerRichTextBox.Text,
                    App.Settings.CurrentSettings.Username,
                    questionImageFilename,  // CHANGED: was questionImagePath
                    answerImageFilename     // CHANGED: was answerImagePath
                );

                // Save flashcard
                FlashcardManager.EditFlashcard(flashcard.FlashcardId, version);

                // Clear form or go back
                Navigator.GoToFlashcards();
            }
            else
            {
                // Create flashcard
                var flashcard = new Flashcard(
                    questionRichTextBox.Text,
                    answerRichTextBox.Text,
                    (Level)levelComboBox.SelectedItem,
                    selectedTopicIds,
                    questionImageFilename,  // ADD THIS
                    answerImageFilename     // ADD THIS
                );

                // Save flashcard
                App.Flashcards.AddFlashcard(flashcard);
                App.Subjects.AddFlashcardToSubject(flashcard, selectedSubject);

                // Clear form or go back
                ClearForm();
            }
            /*else
            {
                // Create flashcard
                var flashcard = new Flashcard(
                    questionRichTextBox.Text,
                    answerRichTextBox.Text,
                    (Level)levelComboBox.SelectedItem,
                    selectedTopicIds
                );

                // IMPORTANT: Check if your Flashcard constructor also takes image paths
                // If it does, pass questionImageFilename and answerImageFilename instead

                // Save flashcard
                FlashcardManager.AddFlashcard(flashcard);
                SubjectManager.AddFlashcardToSubject(flashcard, selectedSubject);

                // Clear form or go back
                ClearForm();
            }*/
            MessageBox.Show("Flashcard saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(questionRichTextBox.Text) && questionImagePath == null)
            {
                MessageBox.Show("Please enter a question.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(answerRichTextBox.Text) && answerImagePath == null)
            {
                MessageBox.Show("Please enter an answer.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (!editting && subjectComboBox.SelectedItem == null)
            {
                MessageBox.Show("Please select a subject.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (!editting && levelComboBox.SelectedItem == null)
            {
                MessageBox.Show("Please select a level.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }

        private void ClearForm()
        {
            questionRichTextBox.Text = "";
            answerRichTextBox.Text = "";
            for (int i = 0; i < topicCheckedListBox.Items.Count; i++)
            {
                topicCheckedListBox.SetItemChecked(i, false);
            }
            questionImagePath = null;
            answerImagePath = null;
            questionPictureBox.Image = App.Settings.CurrentSettings.Theme == "Light" ? Resources.uploadBlack : Resources.uploadWhite;
            answerPictureBox.Image = App.Settings.CurrentSettings.Theme == "Light" ? Resources.uploadBlack : Resources.uploadWhite;
        }

        // Reusable method to generate unique filenames
        private string GenerateUniqueFileName(string originalPath)
        {
            string ext = Path.GetExtension(originalPath);
            return $"{Guid.NewGuid()}{ext}"; // e.g. "a1b2c3d4.jpg"
        }

        private void AddFlashcard_Resize(object sender, EventArgs e)
            => UpdateSizes();

        private void UpdateSizes()
        {
            var margin = 20;
            var controlSpacing = 10;
            var labelHeight = 25;

            // Left column - Question and Answer
            questionLabel.Location = new Point(margin, margin);
            questionRichTextBox.Location = new Point(margin, questionLabel.Bottom + 5);
            questionRichTextBox.Size = new Size(this.Width / 2 - margin - 10, (int)(this.Height / 2.5 - margin));

            answerLabel.Location = new Point(margin, questionRichTextBox.Bottom + controlSpacing);
            answerRichTextBox.Location = new Point(margin, answerLabel.Bottom + 5);
            answerRichTextBox.Size = new Size(this.Width / 2 - margin - 10, (int)(this.Height / 2.5 - margin - 60));

            questionPictureBox.Size = new Size(answerRichTextBox.Width /3, (this.Height - answerRichTextBox.Bottom) /2);
            answerPictureBox.Size = new Size(answerRichTextBox.Width / 3, (this.Height - answerRichTextBox.Bottom) / 2);

            questionPictureBox.Location = new Point(margin, answerRichTextBox.Bottom + margin);
            questionImageButton.Location = new Point(margin, questionPictureBox.Bottom + margin/2);
            answerPictureBox.Location = new Point(questionPictureBox.Right + margin, answerRichTextBox.Bottom + margin);
            answerImageButton.Location = new Point(answerPictureBox.Left, answerPictureBox.Bottom + margin/2);

            // Right column - Subject, Topics, Level
            var rightColumnX = this.Width / 2 + 10;

            subjectLabel.Location = new Point(rightColumnX, margin);
            subjectComboBox.Location = new Point(rightColumnX, subjectLabel.Bottom + 5);
            subjectComboBox.Size = new Size(this.Width / 2 - margin - 20, 30);

            levelLabel.Location = new Point(rightColumnX, subjectComboBox.Bottom + controlSpacing);
            levelComboBox.Location = new Point(rightColumnX, levelLabel.Bottom + 5);
            levelComboBox.Size = new Size(this.Width / 2 - margin - 20, 30);

            topicsLabel.Location = new Point(rightColumnX, levelComboBox.Bottom + controlSpacing);
            topicCheckedListBox.Location = new Point(rightColumnX, topicsLabel.Bottom + 5);
            topicCheckedListBox.Size = new Size(this.Width / 2 - margin - 20, this.Height - topicsLabel.Bottom - 100);

            saveButton.Location = new Point(this.Width - saveButton.Width - margin, this.Height - saveButton.Height - margin);
        }
    }
}
