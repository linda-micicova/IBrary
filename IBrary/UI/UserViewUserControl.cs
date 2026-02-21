using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using IBrary.Managers;
using IBrary.Models;

namespace IBrary.UserControls
{
    public partial class UserViewUserControl : UserControl
    {
        private string username;
        private TextBox usernameSearchBox;
        private MinimalButton searchButton;
        private Panel userStatsPanel;
        private Panel flashcardsPanel;
        private ScrollableControl flashcardsContainer;
        private Label statsLabel;
        private MinimalButton blockUserButton;

        private string currentUsername = "";

        public UserViewUserControl(string username = null)
        {
            InitializeComponent();
            this.username = username;
            InitializeUI();
            this.Resize += UserView_Resize;
        }

        private void InitializeUI()
        {
            this.BackColor = App.Settings.BackgroundColor;

            // Search section
            var searchLabel = new Label
            {
                Text = "Search User:",
                Font = new Font("Arial", 12, FontStyle.Regular),
                ForeColor = App.Settings.TextColor,
                AutoSize = true
            };

            usernameSearchBox = new TextBox
            {
                Font = new Font("Arial", 12, FontStyle.Regular),
                BackColor = App.Settings.FlashcardColor,
                ForeColor = App.Settings.TextColor,
                BorderStyle = BorderStyle.FixedSingle
            };
            usernameSearchBox.KeyPress += (s, e) => { if (e.KeyChar == (char)Keys.Enter) SearchUser(); };

            searchButton = new MinimalButton
            {
                Text = "Search",
                Size = new Size(80, 30)
            };
            searchButton.Click += (s, e) => SearchUser();

            blockUserButton = new MinimalButton
            {
                Text = "Block User",
                Size = new Size(100, 30),
                Visible = false
            };
            blockUserButton.Click += BlockUser_Click;

            // Stats panel
            userStatsPanel = new Panel
            {
                BackColor = App.Settings.FlashcardColor,
                BorderStyle = BorderStyle.None,
                Visible = false
            };

            statsLabel = new Label
            {
                Font = new Font("Arial", 10, FontStyle.Regular),
                ForeColor = App.Settings.TextColor,
                AutoSize = false,
                TextAlign = ContentAlignment.TopLeft,
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };
            userStatsPanel.Controls.Add(statsLabel);

            // Flashcards container with scroll
            flashcardsContainer = new Panel
            {
                AutoScroll = true,
                BorderStyle = BorderStyle.None,
                BackColor = App.Settings.BackgroundColor,
                Visible = false
            };

            flashcardsPanel = new Panel
            {
                AutoSize = true
            };
            flashcardsContainer.Controls.Add(flashcardsPanel);

            // Add all controls
            this.Controls.Add(searchLabel);
            this.Controls.Add(usernameSearchBox);
            this.Controls.Add(searchButton);
            this.Controls.Add(blockUserButton);
            this.Controls.Add(userStatsPanel);
            this.Controls.Add(flashcardsContainer);

            UpdateSizes();

            if (username != null)
            {
                usernameSearchBox.Text = username;

                // Delay the search to ensure layout is complete
                var timer = new System.Windows.Forms.Timer();
                timer.Interval = 50; // 50ms delay
                timer.Tick += (s, e) =>
                {
                    timer.Stop();
                    timer.Dispose();
                    SearchUser();
                };
                timer.Start();
            }
        }

        private void SearchUser()
        {
            var username = usernameSearchBox.Text.Trim();
            if (string.IsNullOrEmpty(username))
            {
                MessageBox.Show("Please enter a username to search.", "Search Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Check if user exists in any flashcard
            var allCards = App.Flashcards.Load();
            var userExists = allCards.Any(f => f.Versions.Any(v => v.Editor.Equals(username, StringComparison.OrdinalIgnoreCase)));

            if (!userExists)
            {
                MessageBox.Show($"User '{username}' not found.", "Search Result", MessageBoxButtons.OK, MessageBoxIcon.Information);
                HideUserInfo();
                return;
            }

            currentUsername = username;
            ShowUserInfo(username);
        }

        private void ShowUserInfo(string username)
        {
            // Show user stats
            DisplayUserStats(username);

            // Show UI elements first
            userStatsPanel.Visible = true;
            flashcardsContainer.Visible = true;

            // Update sizes to ensure proper layout
            UpdateSizes();

            // Then show flashcards with correct width
            DisplayUserFlashcards(username);

            // Show block button
            var isBlocked = App.Settings.CurrentSettings.BlockedUsers.Contains(username);
            blockUserButton.Text = isBlocked ? "Unblock User" : "Block User";
            blockUserButton.Visible = true;
        }

        private void HideUserInfo()
        {
            userStatsPanel.Visible = false;
            flashcardsContainer.Visible = false;
            blockUserButton.Visible = false;
            currentUsername = "";
            UpdateSizes();
        }

        private void DisplayUserStats(string username)
        {
            var allCards = App.Flashcards.Load();
            var userCards = allCards.Where(f => f.Versions.Any(v => v.Editor.Equals(username, StringComparison.OrdinalIgnoreCase))).ToList();

            var created = userCards.Count(f => f.Versions.FirstOrDefault()?.Editor.Equals(username, StringComparison.OrdinalIgnoreCase) == true);
            var edited = userCards.Sum(f => f.Versions.Skip(1).Count(v => v.Editor.Equals(username, StringComparison.OrdinalIgnoreCase) && !v.Deleted));
            var deleted = userCards.Count(f => f.Versions.LastOrDefault()?.Deleted == true &&
                                              f.Versions.LastOrDefault()?.Editor.Equals(username, StringComparison.OrdinalIgnoreCase) == true);

            var isBlocked = App.Settings.CurrentSettings.BlockedUsers.Contains(username);
            var blockStatus = isBlocked ? " (BLOCKED)" : "";

            statsLabel.Text = $"User: {username}{blockStatus}\n\n" +
                             $"Flashcards Created: {created}\n" +
                             $"Flashcards Edited: {edited}\n" +
                             $"Flashcards Deleted: {deleted}\n" +
                             $"Total Contributions: {created + edited + deleted}";
        }

        private void DisplayUserFlashcards(string username)
        {
            flashcardsPanel.Controls.Clear();

            var allCards = App.Flashcards.Load()
                .Where(f => f.Versions.Any(v => v.Editor.Equals(username, StringComparison.OrdinalIgnoreCase)))
                .Take(20) // Limit to 20 for performance
                .ToList();

            var random = new Random();
            int x = 0, y = 10;
            const int cardWidth = 180;
            const int cardHeight = 100;
            const int spacing = 10;

            foreach (var card in allCards)
            {
                var version = card.Versions.LastOrDefault(v => !v.Deleted);
                if (version == null) continue;

                // Create flashcard preview
                var cardPanel = new Panel
                {
                    Size = new Size(cardWidth, cardHeight),
                    Location = new Point(x, y),
                    BackColor = App.Settings.FlashcardColor,
                    BorderStyle = BorderStyle.None,
                    Cursor = Cursors.Hand
                };

                // Question preview
                var questionLabel = new Label
                {
                    Text = version.Question.Length > 150 ? version.Question.Substring(0, 150) + "..." : version.Question,
                    Font = new Font("Arial", 9, FontStyle.Regular),
                    ForeColor = App.Settings.TextColor,
                    Location = new Point(5, 5),
                    Size = new Size(cardWidth - 10, cardHeight - 25),
                    TextAlign = ContentAlignment.TopLeft
                };

                // Editor info
                var editorLabel = new Label
                {
                    Text = $"by {version.Editor}",
                    Font = new Font("Arial", 8, FontStyle.Italic),
                    ForeColor = Color.Gray,
                    Location = new Point(5, cardHeight - 20),
                    Size = new Size(cardWidth - 10, 15),
                    TextAlign = ContentAlignment.BottomLeft
                };

                cardPanel.Controls.Add(questionLabel);
                cardPanel.Controls.Add(editorLabel);

                // Click to view full flashcard
                cardPanel.Click += (s, e) => ShowFullFlashcard(card);
                questionLabel.Click += (s, e) => ShowFullFlashcard(card);

                flashcardsPanel.Controls.Add(cardPanel);

                // Calculate next position (3 cards per row)
                x += cardWidth + spacing;
                if (x + cardWidth > flashcardsPanel.Width)
                {
                    x = 0;
                    y += cardHeight + spacing;
                }
            }

            // Set panel height
            flashcardsPanel.Height = Math.Max(y + cardHeight + 20, 200);
        }

        private void ShowFullFlashcard(Flashcard flashcard)
        {
            var version = flashcard.Versions.LastOrDefault(v => !v.Deleted);
            if (version == null) return;

            var message = $"Question: {version.Question}\n\nAnswer: {version.Answer}\n\nCreated by: {version.Editor}";
            MessageBox.Show(message, "Flashcard Details", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BlockUser_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(currentUsername)) return;

            var isBlocked = App.Settings.CurrentSettings.BlockedUsers.Contains(currentUsername);

            if (isBlocked)
            {
                App.Settings.UnblockUser(currentUsername);
                MessageBox.Show($"User '{currentUsername}' has been unblocked.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                App.Settings.CurrentSettings.BlockedUsers.Add(currentUsername);
                App.Settings.Save();
                MessageBox.Show($"User '{currentUsername}' has been blocked.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            // Refresh display
            ShowUserInfo(currentUsername);
        }

        private void UserView_Resize(object sender, EventArgs e)
            => UpdateSizes();

        private void UpdateSizes()
        {
            var margin = 20;
            var controlSpacing = 10;

            // Search section at top
            var searchLabel = this.Controls[0] as Label;
            searchLabel.Location = new Point(margin, margin);

            usernameSearchBox.Location = new Point(margin, searchLabel.Bottom + 5);
            usernameSearchBox.Size = new Size(200, 30);

            searchButton.Location = new Point(usernameSearchBox.Right + 10, usernameSearchBox.Top);

            blockUserButton.Location = new Point(searchButton.Right + 20, searchButton.Top);

            // Stats panel
            userStatsPanel.Location = new Point(margin, searchButton.Bottom + controlSpacing);
            userStatsPanel.Size = new Size(this.Width - 2 * margin, 120);

            // Flashcards container
            flashcardsContainer.Location = new Point(margin, userStatsPanel.Bottom + controlSpacing);
            flashcardsContainer.Size = new Size(this.Width - 2 * margin, this.Height - flashcardsContainer.Top - margin);

            // Update flashcards panel width
            if (flashcardsPanel != null)
                flashcardsPanel.Width = flashcardsContainer.Width - 20;
        }
    }
}

