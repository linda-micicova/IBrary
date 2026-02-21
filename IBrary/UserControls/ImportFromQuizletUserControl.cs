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
using static System.Net.Mime.MediaTypeNames;
using static System.Windows.Forms.LinkLabel;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolTip;

namespace IBrary.UserControls
{
    public partial class ImportFromQuizletUserControl : UserControl
    {
        // UI Elements
        private PictureBox backIcon;
        private MinimalButton selectFileButton;
        private MinimalButton publishButton;
        private MinimalButton individualTopicButton;
        private System.Windows.Forms.ComboBox subjectComboBox;
        private CheckedListBox topicCheckedListBox;
        private Label subjectLabel;
        private Label topicLabel;
        private Label fileSelectedLabel;
        private PictureBox moreInfoIcon;

        private List<Topic> allTopics;
        private List<Subject> allSubjects;
        private List<Flashcard> loadedFlashcards; // Store loaded flashcards
        private string selectedFilePath;

        public ImportFromQuizletUserControl()
        {
            InitializeComponent();
            InitializeUI();
            this.Resize += ImportFromQuizlet_Resize;
        }

        private void InitializeUI()
        {
            CreateBackButton();
            CreateControls();
            LoadData();
            UpdateButtonStates();
        }

        private void CreateBackButton()
        {
            backIcon = new PictureBox
            {
                Image = App.Settings.CurrentSettings.Theme == "Light"
                    ? Properties.Resources.arrow
                    : Properties.Resources.backWhite,
                SizeMode = PictureBoxSizeMode.Zoom,
                Location = new Point(20, 20),
                Size = new Size(30, 30),
                Cursor = Cursors.Hand
            };
            backIcon.Click += (s, e) => Navigator.GoToSettings();
            this.Controls.Add(backIcon);
        }

        private void CreateControls()
        {
            // Subject label and combobox
            subjectLabel = new Label
            {
                Text = "Select Subject:",
                Font = new Font("Arial", 12),
                ForeColor = App.Settings.TextColor,
                Location = new Point(20, 70),
                AutoSize = true
            };

            subjectComboBox = new System.Windows.Forms.ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Arial", 12),
                BackColor = App.Settings.BackgroundColor,
                ForeColor = App.Settings.TextColor,
                Location = new Point(20, 100),
                Size = new Size(200, 30)
            };

            // Topic label and checkedlistbox
            topicLabel = new Label
            {
                Text = "Select Topics:",
                Font = new Font("Arial", 12),
                ForeColor = App.Settings.TextColor,
                Location = new Point(250, 70),
                AutoSize = true
            };

            topicCheckedListBox = new CheckedListBox
            {
                Font = new Font("Arial", 12),
                CheckOnClick = true,
                BackColor = App.Settings.BackgroundColor,
                ForeColor = App.Settings.TextColor,
                Location = new Point(250, 100),
                Size = new Size(200, 150)
            };

            // File selection button
            selectFileButton = new MinimalButton
            {
                Text = "Select Quizlet File",
                Location = new Point(20, 280),
                Size = new Size(150, 40)
            };

            // File selected indicator
            fileSelectedLabel = new Label
            {
                Text = "No file selected",
                Font = new Font("Arial", 10),
                ForeColor = App.Settings.TextColor,
                Location = new Point(20, 330),
                AutoSize = true
            };

            // Publish button (for general topic selection)
            publishButton = new MinimalButton
            {
                Text = "Publish with Selected Topics",
                Location = new Point(250, 280),
                Size = new Size(200, 40),
                Enabled = false
            };

            // Individual topic selection button
            individualTopicButton = new MinimalButton
            {
                Text = "Select Topics Individually",
                Location = new Point(250, 330),
                Size = new Size(200, 40),
                Enabled = false
            };
            moreInfoIcon = new PictureBox
            {
                Image = App.Settings.CurrentSettings.Theme == "Light"
                    ? Properties.Resources.info
                    : Properties.Resources.infoWhite,
                SizeMode = PictureBoxSizeMode.Zoom,
                Size = new Size(25, 25),
                Location = new Point(topicCheckedListBox.Right-25, 20),
                Cursor = Cursors.Hand
            };
            moreInfoIcon.Click += MoreInfo_Click;

            // Set up event handlers
            subjectComboBox.SelectedIndexChanged += SubjectComboBox_SelectedIndexChanged;
            selectFileButton.Click += SelectFileButton_Click;
            publishButton.Click += PublishButton_Click;
            individualTopicButton.Click += IndividualTopicButton_Click;
            topicCheckedListBox.ItemCheck += TopicCheckedListBox_ItemCheck;

            // Add controls
            this.Controls.Add(subjectLabel);
            this.Controls.Add(subjectComboBox);
            this.Controls.Add(topicLabel);
            this.Controls.Add(topicCheckedListBox);
            this.Controls.Add(selectFileButton);
            this.Controls.Add(fileSelectedLabel);
            this.Controls.Add(publishButton);
            this.Controls.Add(individualTopicButton);
            this.Controls.Add(moreInfoIcon);
        }

        private void LoadData()
        {
            allSubjects = App.Settings.MySubjects;
            allTopics = App.Topics.Load();

            // Populate subject combobox
            subjectComboBox.DataSource = allSubjects;
            subjectComboBox.DisplayMember = "SubjectName";
            subjectComboBox.ValueMember = "SubjectId";

            // Set default selection
            if (allSubjects.Count > 0)
            {
                subjectComboBox.SelectedIndex = 0;
            }
        }

        private void SubjectComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            var selectedSubject = subjectComboBox.SelectedItem as Subject;
            if (selectedSubject != null)
            {
                // Filter topics based on selected subject
                var subjectTopicIds = new HashSet<string>(selectedSubject.Topics);
                var filteredTopics = allTopics.Where(t => subjectTopicIds.Contains(t.TopicId)).ToList();

                topicCheckedListBox.DataSource = filteredTopics;
                topicCheckedListBox.DisplayMember = "TopicName";
                topicCheckedListBox.ValueMember = "TopicId";
            }
            UpdateButtonStates();
        }

        private void TopicCheckedListBox_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            // Use BeginInvoke to ensure the check state is updated before checking
            this.BeginInvoke((Action)(() => UpdateButtonStates()));
        }

        private void SelectFileButton_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "JSON Files (*.json)|*.json";
                ofd.Title = "Select Quizlet Export File";

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        // Load flashcards from file but don't save them yet
                        loadedFlashcards = FlashcardManager.LoadFromQuizletFile(ofd.FileName);
                        selectedFilePath = ofd.FileName;

                        if (loadedFlashcards != null && loadedFlashcards.Count > 0)
                        {
                            fileSelectedLabel.Text = $"File selected: {System.IO.Path.GetFileName(selectedFilePath)} ({loadedFlashcards.Count} flashcards)";
                            fileSelectedLabel.ForeColor = Color.Green;
                        }
                        else
                        {
                            fileSelectedLabel.Text = "No valid flashcards found in file";
                            fileSelectedLabel.ForeColor = Color.Red;
                            loadedFlashcards = null;
                        }
                    }
                    catch (Exception ex)
                    {
                        fileSelectedLabel.Text = $"Error loading file: {ex.Message}";
                        fileSelectedLabel.ForeColor = Color.Red;
                        loadedFlashcards = null;
                    }

                    UpdateButtonStates();
                }
            }
        }

        private void PublishButton_Click(object sender, EventArgs e)
        {
            var selectedSubject = subjectComboBox.SelectedItem as Subject;
            var selectedTopics = topicCheckedListBox.CheckedItems.Cast<Topic>().ToList();

            if (!ValidateSelection(selectedSubject, selectedTopics))
                return;

            try
            {
                // Assign selected topics to all flashcards
                foreach (var flashcard in loadedFlashcards)
                {
                    flashcard.Topics = selectedTopics.Select(t => t.TopicId).ToList();

                    // Add flashcard to subject
                    if (!selectedSubject.Flashcards.Contains(flashcard.FlashcardId))
                    {
                        selectedSubject.Flashcards.Add(flashcard.FlashcardId);
                    }
                }

                // Save flashcards using new method
                FlashcardManager.ImportFlashcards(loadedFlashcards);

                MessageBox.Show($"Successfully imported {loadedFlashcards.Count} flashcard(s) to {selectedSubject.SubjectName}!");

                // Reset state
                ResetImportState();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error importing flashcards: {ex.Message}");
            }
        }
        private void MoreInfo_Click(object sender, EventArgs e)
        {
            // Show more information about the import process
            MessageBox.Show("How to get Quizlet flashcards with Quizlet Exporter (Chrome)\n" +
                            "1. Install the Quizlet Exporter Chrome extension.\n" +
                            "2. Open the Quizlet set you want to import.\n" +
                            "3. Click the Quizlet Exporter icon in Chrome’s toolbar.\n" +
                            "4. In the exporter, choose the delimiters Tab & New line.\n" +
                            "5. Click Export as JSON to get the exported text file.",
                            "Import from Quizlet Help", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void IndividualTopicButton_Click(object sender, EventArgs e)
        {
            var selectedSubject = subjectComboBox.SelectedItem as Subject;

            if (selectedSubject == null)
            {
                MessageBox.Show("Please select a subject.");
                return;
            }

            if (loadedFlashcards == null || loadedFlashcards.Count == 0)
            {
                MessageBox.Show("Please select a file first.");
                return;
            }

            // Navigate to individual topic selection with parameters
            Navigator.GoToQuizletFlashcardsView(loadedFlashcards, selectedSubject);
        }
        /*private void IndividualTopicButton_Click(object sender, EventArgs e)
        {
            var selectedSubject = subjectComboBox.SelectedItem as Subject;

            if (selectedSubject == null)
            {
                MessageBox.Show("Please select a subject.");
                return;
            }

            if (loadedFlashcards == null || loadedFlashcards.Count == 0)
            {
                MessageBox.Show("Please select a file first.");
                return;
            }

            // Navigate to individual topic selection (FlashcardPreview)
            Navigator.GoToQuizletFlashcardsView(loadedFlashcards, selectedSubject);
        }*/

        private bool ValidateSelection(Subject selectedSubject, List<Topic> selectedTopics)
        {
            if (selectedSubject == null)
            {
                MessageBox.Show("Please select a subject.");
                return false;
            }

            if (loadedFlashcards == null || loadedFlashcards.Count == 0)
            {
                MessageBox.Show("Please select a file first.");
                return false;
            }

            if (selectedTopics.Count == 0)
            {
                MessageBox.Show("Please select at least one topic.");
                return false;
            }

            return true;
        }

        private void UpdateButtonStates()
        {
            bool hasFile = loadedFlashcards != null && loadedFlashcards.Count > 0;
            bool hasSubject = subjectComboBox.SelectedItem != null;
            bool hasTopics = topicCheckedListBox.CheckedItems.Count > 0;

            publishButton.Enabled = hasFile && hasSubject && hasTopics;
            individualTopicButton.Enabled = hasFile && hasSubject;
        }

        private void ResetImportState()
        {
            loadedFlashcards = null;
            selectedFilePath = null;
            fileSelectedLabel.Text = "No file selected";
            fileSelectedLabel.ForeColor = App.Settings.TextColor;

            // Clear topic selections
            for (int i = 0; i < topicCheckedListBox.Items.Count; i++)
            {
                topicCheckedListBox.SetItemChecked(i, false);
            }

            UpdateButtonStates();
        }

        private void ImportFromQuizlet_Resize(object sender, EventArgs e)
        {
            // Keep simple positioning for now
        }
    }
}