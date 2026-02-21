using IBrary.Managers;
using IBrary.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace IBrary.UserControls
{
    public partial class FlashcardUserControl : UserControl
    {
        private List<Topic> allTopics;
        private List<Subject> allSubjects;
        private List<Flashcard> allFlashcards;

        private Subject selectedSubject;
        private List<Topic> selectedTopics;

        private List<Flashcard> currentFlashcards = new List<Flashcard>();
        private CardVersion currentFlashcardVersion;
        private int currentIndex = 0;
        private bool isInitialized = false;

        // UI elements
        private Panel flashcardPanel;
        private int TopMargin = 50;
        private int LeftMargin = 60;
        private int RightMargin = 60;
        private int BottomMargin = 50;

        private MinimalButton SkipButton;
        private MinimalButton CorrectButton;
        private MinimalButton IncorrectButton;

        private Label flashcardLabel;
        private ComboBox subjectComboBox;
        private CheckedListBox topicCheckedListBox;

        private Label usernameLabel;
        private PictureBox starPictureBox;
        private MinimalButton blockButton;
        private MinimalButton editButton;
        private MinimalButton deleteButton;
        private PictureBox flashcardPictureBox = null;

        public event Action<UserControl> RequestUserControlSwitch;
        public event EventHandler UserViewRequested;

        private ComboBox orderingComboBox;

        private bool isShowingQuestion = true;
        public class OrderingOption
        {
            public string Display { get; set; }
            public string Value { get; set; }

            public override string ToString()
            {
                return Display;
            }
        }
        public FlashcardUserControl() 
        {
            InitializeComponent();
            InitializeUI();
            this.Resize += Flashcards_Resize;

        }
        private void InitializeUI()
        {

            flashcardPanel = new Panel { BackColor = App.Settings.FlashcardColor };
            flashcardPanel.Click += FlashcardPanel_Click;
            flashcardPanel.Cursor = Cursors.Hand;

            subjectComboBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Arial", 12, FontStyle.Regular),
                BackColor = App.Settings.BackgroundColor,
                ForeColor = App.Settings.TextColor,
                Cursor = Cursors.Hand,
            };

            topicCheckedListBox = new CheckedListBox
            {
                Font = new Font("Arial", 12, FontStyle.Regular),
                CheckOnClick = true,
                BackColor = App.Settings.BackgroundColor,
                ForeColor = App.Settings.TextColor,
                Cursor = Cursors.Hand,
            };

            // Initialize the label
            flashcardLabel = new Label
            {
                Text = "Your flashcard text goes here",
                Font = new Font("Arial", 15, FontStyle.Regular),
                ForeColor = App.Settings.TextColor,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,  // Fill the entire flashcardPanel
                AutoSize = false,      // Disable auto-sizing to allow manual control
                Padding = new Padding(20)
            };
            flashcardLabel.Click += flashcardLabel_Click; // Add click event handler

            flashcardPictureBox = new PictureBox
            {
                Size = new Size(200, 200), // Adjust size as needed
                //Location = new Point(flashcardPanel.Right / 2 + flashcardPanel.Left / 2 - 200, flashcardPanel.Top + 20), // Center horizontally
                SizeMode = PictureBoxSizeMode.Zoom, // or PictureBoxSizeMode.StretchImage
                Visible = false // Initially hidden, can be set to true when needed
            };

            usernameLabel = new Label
            {
                Text = "Username",
                Font = new Font("Arial", 6, FontStyle.Regular),
                ForeColor = App.Settings.TextColor,
                AutoSize = true
            };
            starPictureBox = new PictureBox
            {
                Image = Properties.Resources.starEmptyDarkmode,
                SizeMode = PictureBoxSizeMode.Zoom
            };
            starPictureBox.Click += (s, e) =>
            {
                if (currentFlashcardVersion != null)
                {
                    currentFlashcards[currentIndex].Important = !currentFlashcards[currentIndex].Important;
                    starPictureBox.Image = currentFlashcards[currentIndex].Important
                        ? Properties.Resources.starFullDarkmode
                        : Properties.Resources.starEmptyDarkmode;

                    // Get the current flashcard ID
                    var currentFlashcardId = currentFlashcards[currentIndex].FlashcardId;

                    // Find the same flashcard in the complete list
                    var flashcardToUpdate = allFlashcards.FirstOrDefault(f => f.FlashcardId == currentFlashcardId);

                    if (flashcardToUpdate != null)
                    {
                        flashcardToUpdate.Important = currentFlashcards[currentIndex].Important;

                        // Save ALL flashcards
                        App.Flashcards.SaveFlashcards(allFlashcards);
                    }
                }
            };

            orderingComboBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Arial", 10),
                BackColor = App.Settings.BackgroundColor,
                ForeColor = App.Settings.TextColor,
                Size = new Size(200, 25)
            };

            var orderingOptions = new List<OrderingOption>
{
    new OrderingOption { Display = "Priority Algorithm", Value = "priority" },
    new OrderingOption { Display = "High Error Rate First", Value = "error_rate" },
    new OrderingOption { Display = "Not Seen / Old Cards", Value = "not_seen" },
    new OrderingOption { Display = "Least Studied", Value = "seen_few" },
    new OrderingOption { Display = "Starred Cards First", Value = "important" },
    new OrderingOption { Display = "Random Order", Value = "random" }
};

            orderingComboBox.DataSource = orderingOptions;
            orderingComboBox.DisplayMember = "Display";
            orderingComboBox.ValueMember = "Value";

            // Force the ComboBox to process the DataSource
            orderingComboBox.BindingContext = new BindingContext();
            Application.DoEvents();

            // Now set the selected index
            orderingComboBox.SelectedIndex = 0; // Default to priority

            // Set up event handler
            orderingComboBox.SelectedIndexChanged += OrderingComboBox_SelectedIndexChanged;

            //this.Controls.Add(orderingLabel);
            this.Controls.Add(orderingComboBox);

            //Load my subjects
            App.Settings.Load();
            this.allSubjects = App.Settings.MySubjects;
            this.allTopics = App.Topics.Load();

            // Create "All Subjects" option
            var allSubjectsOption = new Subject
            {
                SubjectId = "ALL_SUBJECTS",
                SubjectName = "All Subjects",
                Topics = new List<string>(),
                Flashcards = new List<string>()
            };
            selectedSubject = allSubjectsOption; // Default to "All Subjects"

            // Add "All Subjects" to the beginning of the list
            var subjectsList = new List<Subject> { allSubjectsOption };
            subjectsList.AddRange(App.Settings.MySubjects);

            // Populate the subjectComboBox
            subjectComboBox.DataSource = subjectsList;
            subjectComboBox.DisplayMember = "SubjectName";
            topicCheckedListBox.DisplayMember = "TopicName";

            // Force the ComboBox to process the DataSource
            subjectComboBox.BindingContext = new BindingContext();
            Application.DoEvents();

            // Set up event handlers
            subjectComboBox.SelectedIndexChanged += subjectComboBox_SelectedIndexChanged;
            topicCheckedListBox.ItemCheck += topicCheckedListBox_ItemCheck;

            // Mark as initialized BEFORE restoring
            isInitialized = true;

            if (App.Settings.CurrentSettings.LastSelectedSubjectId != null)
            {
                var comboBoxSubjects = (List<Subject>)subjectComboBox.DataSource;
                var index = comboBoxSubjects.FindIndex(s => s.SubjectId == App.Settings.CurrentSettings.LastSelectedSubjectId);

                if (index >= 0 && index < subjectComboBox.Items.Count)
                {
                    subjectComboBox.SelectedIndex = index;

                    // If we just selected ALL_SUBJECTS, manually trigger the logic
                    if (App.Settings.CurrentSettings.LastSelectedSubjectId == "ALL_SUBJECTS")
                    {
                        subjectComboBox_SelectedIndexChanged(null, null);
                    }
                }
            }

            //Buttons
            SkipButton = new MinimalButton { Text = "Skip", Size = new Size(100, 30) };
            SkipButton.Click += skipButton_Click;

            CorrectButton = new MinimalButton { Text = "Correct", Size = new Size(100, 30) };
            CorrectButton.Click += correctButton_Click;

            IncorrectButton = new MinimalButton { Text = "Incorrect", Size = new Size(100, 30) };
            IncorrectButton.Click += incorrectButton_Click;

            deleteButton = new MinimalButton { Text = "Delete", Size = new Size(100, 30) };
            deleteButton.Click += deleteButton_Click;

            editButton = new MinimalButton { Text = "Edit", Size = new Size(100, 30) };
            editButton.Click += editButton_Click;

            usernameLabel.Cursor = Cursors.Hand;
            usernameLabel.Click += usernameLabel_Click; // Make the username label clickable

            flashcardPanel.Controls.Add(flashcardPictureBox);
            flashcardPanel.Controls.Add(usernameLabel);
            flashcardPanel.Controls.Add(starPictureBox);
            flashcardPanel.Controls.Add(deleteButton);
            flashcardPanel.Controls.Add(editButton);
            this.Controls.Add(flashcardPanel);
            this.Controls.Add(SkipButton);
            this.Controls.Add(CorrectButton);
            this.Controls.Add(IncorrectButton);
            this.Controls.Add(subjectComboBox);
            this.Controls.Add(topicCheckedListBox);
            flashcardPanel.Controls.Add(flashcardLabel);

            UpdateSizes();

            //Display flashcard
            ShowCurrentQuestion();

        }
        private void OrderingComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!isInitialized) return;

            RefreshFlashcards();
        }
        private void StarPictureBox_Click(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private void usernameLabel_Click(object sender, EventArgs e)
        {
            Navigator.GoToUserView(usernameLabel.Text);
        }
        private void flashcardLabel_Click(object sender, EventArgs e)
        {
            FlashcardPanel_Click(sender, e);
        }
        
        private void FlashcardPanel_Click(object sender, EventArgs e)
        {
            if (currentFlashcardVersion == null)
                return;

            if (isShowingQuestion)
            {
                // Currently showing question, flip to answer
                flashcardLabel.Text = currentFlashcardVersion.Answer;
                isShowingQuestion = false;

                if (currentFlashcardVersion.AnswerImagePath != null)
                {
                    try
                    {
                        ImageFlashcard(currentFlashcardVersion.AnswerImagePath);
                    }
                    catch (Exception ex)
                    {
                        NoImageFlashcard();
                    }
                }
                else NoImageFlashcard();
            }
            else
            {
                // Currently showing answer, flip to question
                flashcardLabel.Text = currentFlashcardVersion.Question;
                isShowingQuestion = true;

                if (currentFlashcardVersion.QuestionImagePath != null)
                {
                    try
                    {
                        ImageFlashcard(currentFlashcardVersion.QuestionImagePath);
                    }
                    catch (Exception ex)
                    {
                        NoImageFlashcard();
                    }
                }
                else NoImageFlashcard();
            }
        }

        private void RefreshFlashcards()
        {
            LoadSelection();
            LoadFlashcards();
            currentIndex = 0; // Reset to start after refresh
            ShowCurrentQuestion();
        }

        private void LoadSelection()
        {
            selectedSubject = subjectComboBox.SelectedItem as Subject;
            selectedTopics = topicCheckedListBox.CheckedItems.Cast<Topic>().ToList();
        }

        private void LoadFlashcards()
        {
            allFlashcards = App.Flashcards.Load();
            var selectedTopicIds = selectedTopics.Select(t => t.TopicId).ToHashSet();

            var allowedLevels = new HashSet<Level> { Level.SL, Level.HL };
            if (selectedSubject != null && selectedSubject.SubjectId != "ALL_SUBJECTS")
            {
                var userLevel = App.Settings.CurrentSettings.MySubjectLevels.TryGetValue(selectedSubject.SubjectId, out var level) ? level : Level.HL;
                allowedLevels = (userLevel == Level.HL) ? new HashSet<Level> { Level.SL, Level.HL } : new HashSet<Level> { Level.SL };
            }

            var validFlashcards = allFlashcards.Where(f => IsValidFlashcard(f, selectedTopicIds, allowedLevels)).ToList();

            // Apply selected ordering
            var selectedOrdering = (OrderingOption)orderingComboBox.SelectedItem;
            currentFlashcards = ApplyOrdering(validFlashcards, selectedOrdering.Value);
        }
        // Applies ordering o
        private List<Flashcard> ApplyOrdering(List<Flashcard> flashcards, string orderingType)
        {
            switch (orderingType)
            {
                case "priority":
                    return FlashcardManager.GetFlashcardsOrderedByPriority(flashcards);

                case "error_rate":
                    return flashcards.OrderByDescending(f => f.Seen > 0 ? (double)f.Errors / f.Seen : 0).ToList();

                case "not_seen":
                    return flashcards.OrderBy(f => f.Seen == 0 ? DateTime.MinValue : f.LastSeen).ToList();

                case "seen_few":
                    return flashcards.OrderBy(f => f.Seen).ToList();

                case "important":
                    return flashcards.OrderByDescending(f => f.Important)
                                   .ThenBy(f => f.Seen == 0 ? DateTime.MinValue : f.LastSeen).ToList();

                case "random":
                    var random = new Random();
                    return flashcards.OrderBy(f => random.Next()).ToList();

                default:
                    return FlashcardManager.GetFlashcardsOrderedByPriority(flashcards);
            }
        }

        private bool IsValidFlashcard(Flashcard f, HashSet<string> selectedTopicIds, HashSet<Level> allowedLevels)
        {
            // Check if flashcard belongs to any of "my subjects"
            bool belongsToMySubjects;
            bool levelAllowed;

            if (selectedSubject?.SubjectId == "ALL_SUBJECTS")
            {
                // Find which subject this flashcard belongs to
                var flashcardSubject = App.Settings.MySubjects.FirstOrDefault(s => s.Flashcards.Contains(f.FlashcardId));
                belongsToMySubjects = flashcardSubject != null;

                if (belongsToMySubjects)
                {
                    // Check the user's level for THIS specific subject
                    var userLevelForThisSubject = App.Settings.CurrentSettings.MySubjectLevels.TryGetValue(flashcardSubject.SubjectId, out var level) ? level : Level.HL;
                    var allowedLevelsForThisSubject = (userLevelForThisSubject == Level.HL)
                        ? new HashSet<Level> { Level.SL, Level.HL }
                        : new HashSet<Level> { Level.SL };
                    levelAllowed = allowedLevelsForThisSubject.Contains(f.Level);
                }
                else
                {
                    levelAllowed = false;
                }
            }
            else
            {
                belongsToMySubjects = selectedSubject?.Flashcards?.Contains(f.FlashcardId) ?? true;
                levelAllowed = allowedLevels.Contains(f.Level);
            }

            return f.Versions?.OrderByDescending(v => v.Timestamp).FirstOrDefault()?.Deleted == false &&
                   belongsToMySubjects &&
                   levelAllowed &&
                   (selectedTopicIds.Count == 0 || f.Topics.Any(id => selectedTopicIds.Contains(id)));
        }

        private void ShowCurrentQuestion()
        {
            // Ensure currentFlashcards is not null
            if (currentFlashcards == null)
            {
                currentFlashcards = new List<Flashcard>();
            }

            // Endless cycling
            if (currentIndex >= currentFlashcards.Count)
                currentIndex = 0;

            if (currentFlashcards.Count == 0)
            {
                currentFlashcardVersion = null;
                NoFlashcard();
                return;
            }

            var version = FindValidVersion();
            if (version == null)
            {
                currentIndex = (currentIndex + 1) % currentFlashcards.Count;
                return;
            }

            DisplayFlashcard(version);
        }

        private CardVersion FindValidVersion()
        {
            if (currentFlashcards == null || currentFlashcards.Count == 0)
                return null;

            int startIndex = currentIndex;

            // Ensure currentIndex is within bounds
            if (currentIndex >= currentFlashcards.Count)
                currentIndex = 0;

            do
            {
                var currentFlashcard = currentFlashcards[currentIndex];

                // Add null check for the flashcard itself
                if (currentFlashcard == null)
                {
                    currentIndex = (currentIndex + 1) % currentFlashcards.Count;
                    continue;
                }

                var version = currentFlashcard.GetDisplayVersion(App.Settings.CurrentSettings.BlockedUsers ?? new List<string>());

                if (version != null)
                    return version;

                currentIndex = (currentIndex + 1) % currentFlashcards.Count;

            } while (currentIndex != startIndex);

            return null;
        }

        private void DisplayFlashcard(CardVersion version)
        {
            flashcardLabel.Text = version.Question ?? "No question available";
            usernameLabel.Text = version.Editor ?? "Unknown";
            currentFlashcardVersion = version;
            starPictureBox.Image = currentFlashcards[currentIndex].Important
                ? Properties.Resources.starFullDarkmode
                : Properties.Resources.starEmptyDarkmode
            ;

            // Handle images
            if (version.QuestionImagePath != null)
            {
                try { ImageFlashcard(version.QuestionImagePath); }
                catch { NoImageFlashcard(); }
            }
            else
                NoImageFlashcard();
        }
        private void NoFlashcard()
        {
            flashcardLabel.Text = "No flashcards available";
            NoImageFlashcard();
            usernameLabel.Text = "username";
        }
        private void NoImageFlashcard()
        {
            flashcardPictureBox.Visible = false;
            flashcardPictureBox.Image = null;
            flashcardLabel.Dock = DockStyle.Fill;
        }
        private void ImageFlashcard(string imagePath)
        {
            try
            {
                flashcardPictureBox.Visible = true;

                // Handle both absolute paths (old) and relative filenames (new)
                string fullPath = Path.IsPathRooted(imagePath)
                    ? Path.Combine(Path.Combine(Application.StartupPath, "FlashcardImages"), Path.GetFileName(imagePath))
                    : Path.Combine(Application.StartupPath, "FlashcardImages", imagePath);

                if (File.Exists(fullPath))
                {
                    flashcardPictureBox.Image = Image.FromFile(fullPath);
                }
                else
                {
                    NoImageFlashcard();
                    return;
                }

                // Switch to manual positioning when there's an image
                flashcardLabel.Dock = DockStyle.None;
                UpdateSizes();
            }
            catch (Exception ex)
            {
                NoImageFlashcard();
            }
        }
        private void subjectComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            var selectedSubject = subjectComboBox.SelectedItem as Subject;
            if (selectedSubject == null || selectedSubject.SubjectId == "ALL_SUBJECTS")
            {
                // Show ALL topics logic...
                var allTopicsFromMySubjects = App.Settings.MySubjects
                    .SelectMany(s => s.Topics)
                    .Distinct()
                    .Select(topicId => allTopics.FirstOrDefault(t => t.TopicId == topicId))
                    .Where(t => t != null)
                    .ToList();

                topicCheckedListBox.DataSource = allTopicsFromMySubjects;
            }
            else
            {
                // Show topics for specific subject
                var topicIdSet = new HashSet<string>(selectedSubject.Topics);
                var filteredTopics = allTopics.Where(t => topicIdSet.Contains(t.TopicId)).ToList();
                topicCheckedListBox.DataSource = filteredTopics;
            }

            topicCheckedListBox.DisplayMember = "TopicName";

            // DEBUG: Check what we're trying to restore
            var savedTopicIds = new HashSet<string>(App.Settings.CurrentSettings.LastSelectedTopicIds);

            // Clear visual selections
            for (int i = 0; i < topicCheckedListBox.Items.Count; i++)
            {
                topicCheckedListBox.SetItemChecked(i, false);
            }

            // DEBUG: Check what topics are available
            var availableTopicIds = new List<string>();
            for (int i = 0; i < topicCheckedListBox.Items.Count; i++)
            {
                var topic = topicCheckedListBox.Items[i] as Topic;
                if (topic != null)
                    availableTopicIds.Add(topic.TopicId);
            }

            // Save and refresh
            if (isInitialized)
            {
                App.Settings.CurrentSettings.LastSelectedSubjectId = selectedSubject.SubjectId;
                App.Settings.Save();
            }

            RefreshFlashcards();
            
            if (savedTopicIds.Count > 0)
            {
                // Use a longer delay to ensure CheckedListBox is populated
                var timer = new System.Windows.Forms.Timer();
                timer.Interval = 50; // 50ms delay
                timer.Tick += (s, args) =>
                {
                    timer.Stop();
                    timer.Dispose();

                    int restoredCount = 0;
                    for (int i = 0; i < topicCheckedListBox.Items.Count; i++)
                    {
                        var topic = topicCheckedListBox.Items[i] as Topic;
                        if (topic != null && savedTopicIds.Contains(topic.TopicId))
                        {
                            topicCheckedListBox.SetItemChecked(i, true);
                            restoredCount++;
                        }
                    }
                };
                timer.Start();
            }
        }
        private void topicCheckedListBox_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            this.BeginInvoke((MethodInvoker)(() =>
            {
                // Save topic selection
                App.Settings.CurrentSettings.LastSelectedTopicIds = topicCheckedListBox.CheckedItems
                    .Cast<Topic>()
                    .Select(t => t.TopicId)
                    .ToList();
                App.Settings.Save();

                RefreshFlashcards(); 
            }));
        }
        private void incorrectButton_Click(object sender, EventArgs e)
        {
            if (currentIndex < currentFlashcards.Count)
            {
                var currentFlashcardId = currentFlashcards[currentIndex].FlashcardId;
                var allFlashcards = App.Flashcards.Load();
                var flashcardToUpdate = allFlashcards.FirstOrDefault(f => f.FlashcardId == currentFlashcardId);

                if (flashcardToUpdate != null)
                {
                    flashcardToUpdate.RegisterStudyResult(DateTime.Today, true);
                    App.Flashcards.SaveFlashcards(allFlashcards);
                    currentFlashcards[currentIndex].RegisterStudyResult(DateTime.Today, true);
                }
                currentIndex++;
                ShowCurrentQuestion();
            }
        }

        private void skipButton_Click(object sender, EventArgs e)
        {
            if (currentIndex < currentFlashcards.Count)
            {
                var currentFlashcard = currentFlashcards[currentIndex];
                currentIndex++;
                ShowCurrentQuestion();
            }
        }

        private void correctButton_Click(object sender, EventArgs e)
        {
            if (currentIndex < currentFlashcards.Count)
            {
                // 1. Get the current flashcard ID
                var currentFlashcardId = currentFlashcards[currentIndex].FlashcardId;

                // 2. Load ALL flashcards
                var allFlashcards = App.Flashcards.Load();

                // 3. Find the same flashcard in the complete list
                var flashcardToUpdate = allFlashcards.FirstOrDefault(f => f.FlashcardId == currentFlashcardId);

                if (flashcardToUpdate != null)
                {
                    // 4. Update the complete list version
                    flashcardToUpdate.RegisterStudyResult(DateTime.Today, false);

                    // 5. Save ALL flashcards
                    App.Flashcards.SaveFlashcards(allFlashcards);

                    // 6. Update our working copy too
                    currentFlashcards[currentIndex].RegisterStudyResult(DateTime.Today, false);
                }

                currentIndex++;
                ShowCurrentQuestion();
            }
        }

        private void Flashcards_Resize(object sender, EventArgs e)
            => UpdateSizes();


        private void UpdateSizes()
        {
            TopMargin = 30 + this.Size.Height / 8;
            LeftMargin = 10 + this.Size.Width / 10;
            RightMargin = 10 + this.Size.Width / 10;
            BottomMargin = 10 + this.Size.Height / 10;

            if (flashcardPanel != null)
            {
                flashcardPanel.Size = new Size(
                    this.Width - LeftMargin - RightMargin,
                    this.Height - TopMargin - BottomMargin
                );
                flashcardPanel.Location = new Point(LeftMargin, TopMargin);
                editButton.Location = new Point(flashcardPanel.Width - editButton.Width - 10, 10);
                deleteButton.Location = new Point(flashcardPanel.Width - deleteButton.Width - 10, editButton.Bottom + 10);
                starPictureBox.Size = new Size(flashcardPanel.Width / 20, flashcardPanel.Height / 20);
                starPictureBox.Location = new Point(flashcardPanel.Height / 15 - starPictureBox.Height, flashcardPanel.Height - flashcardPanel.Height / 15);
            }
            if (usernameLabel != null)
            {
                usernameLabel.Location = new Point(10, 10);
                AdjustFontSizeToFit(usernameLabel);
            }

            if (SkipButton != null)
            {
                SkipButton.Location = new Point(
                    (int)(this.Width / 2 - SkipButton.Width / 2),
                    flashcardPanel.Bottom + this.Height / 30
                );
            }
            if (CorrectButton != null)
            {
                CorrectButton.Location = new Point(SkipButton.Right + this.Width / 50, flashcardPanel.Bottom + this.Height / 30);
            }
            if (IncorrectButton != null)
            {
                IncorrectButton.Location = new Point(SkipButton.Left - 100 - this.Width / 50, flashcardPanel.Bottom + this.Height / 30);
            }
            if (subjectComboBox != null)
            {
                subjectComboBox.Location = new Point(LeftMargin, 10);
                subjectComboBox.Size = new Size(150, this.Height / 4);
            }
            if (topicCheckedListBox != null)
            {
                topicCheckedListBox.Location = new Point(subjectComboBox.Right + this.Width / 20, subjectComboBox.Top);
                topicCheckedListBox.Size = new Size(180 + this.Width / 20, this.Height / 7);
            }

            // Position the ordering controls
            /*if (orderingLabel != null)
            {
                subjectComboBox.Location = new Point(LeftMargin, subjectComboBox.Bottom + 15);
                subjectComboBox.Size = new Size(150, this.Height / 4);
            }*/
            if (orderingComboBox != null)
            {
                orderingComboBox.Location = new Point(LeftMargin, subjectComboBox.Bottom + Math.Min(this.Height / 20, 10));
                orderingComboBox.Size = new Size(150, this.Height / 4);
            }
            
            if (flashcardPictureBox != null && flashcardLabel != null)
            {
                flashcardPictureBox.Size = new Size(flashcardPanel.Width / 2, flashcardPanel.Height / 2);
                flashcardPictureBox.Location = new Point(flashcardPanel.Width / 2 - flashcardPictureBox.Width / 2, flashcardPanel.Height/2 - flashcardPictureBox.Height/2 - flashcardLabel.Height/2 ); //referovanie na flashcardlabel.height je trochu blbost as its not set yet
                
                flashcardLabel.Location = new Point(10, flashcardPictureBox.Bottom + 10);
                flashcardLabel.Size = new Size(flashcardPanel.Width - 20, flashcardPanel.Height - flashcardPictureBox.Height - 40);
                AdjustFontSizeToFit(flashcardLabel);
            }
            
        }
        private void AdjustFontSizeToFit(Label label)
        {
            if (label.Text.Length == 0) return;

            float fontSize = 15f;
            label.Font = new Font(label.Font.FontFamily, fontSize, label.Font.Style);

            int availableWidth = label.Width - label.Padding.Left - label.Padding.Right;
            int availableHeight = label.Height - label.Padding.Top - label.Padding.Bottom;

            Size textSize = TextRenderer.MeasureText(label.Text, label.Font);

            while ((textSize.Width > availableWidth || textSize.Height > availableHeight) && fontSize > 8)
            {
                fontSize -= 0.5f;
                label.Font = new Font(label.Font.FontFamily, fontSize, label.Font.Style);
                textSize = TextRenderer.MeasureText(label.Text, label.Font);
            }
        }
        private void deleteButton_Click(object sender, EventArgs e)
        {
            if(App.Settings.CurrentSettings.Username != null)
            {
                DialogResult result = MessageBox.Show("Are you sure you want to delete this flashcard?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (result == DialogResult.Yes)
                {
                    FlashcardManager.DeleteFlashcard(currentFlashcards[currentIndex].FlashcardId, App.Settings.CurrentSettings.Username);
                    currentFlashcards = FlashcardManager.GetFlashcardsOrderedByPriority(App.Flashcards.Load());
                    RefreshFlashcards();
                }
            }
            else
            {
               MessageBox.Show("You must be logged in to delete a flashcard.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void editButton_Click(object sender, EventArgs e)
        {
            Navigator.EditFlashcard(currentFlashcards[currentIndex], currentFlashcardVersion);
        }
        public void ForceRefresh()
        {
            RefreshFlashcards();
        }
    }
}