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
    public partial class BlockedUsersUserControl : UserControl
    {
        private PictureBox BackIcon;
        private Label titleLabel;
        private FlowLayoutPanel BlockedPanelContainer;

        public BlockedUsersUserControl()
        {
            InitializeComponent();
            InitializeUI();
            this.Resize += Dashboard_Resize;
        }

        private void InitializeUI()
        {
            CreateBackIcon();
            CreateTitle();
            CreateBlockedPanelContainer();
            LoadBlockedUsers();
        }

        private void CreateBackIcon()
        {
            BackIcon = new PictureBox
            {
                Image = SettingsManager.CurrentSettings.Theme == "Light"
                    ? Properties.Resources.arrow
                    : Properties.Resources.backWhite,
                SizeMode = PictureBoxSizeMode.Zoom,
                Location = new Point(20, 20),
                Size = new Size(30, 30),
                Cursor = Cursors.Hand
            };

            BackIcon.Click += (s, e) =>
            {
                Navigator.GoToSettings();
            };

            this.Controls.Add(BackIcon);
        }

        private void CreateTitle()
        {
            titleLabel = new Label
            {
                Text = "Blocked Users",
                Font = new Font("Arial", 16, FontStyle.Bold),
                ForeColor = SettingsManager.TextColor,
                Location = new Point(70, 20),
                AutoSize = true
            };
            this.Controls.Add(titleLabel);
        }

        private void CreateBlockedPanelContainer()
        {
            BlockedPanelContainer = new FlowLayoutPanel
            {
                Location = new Point(0, 70),
                Size = new Size(this.Width, this.Height - 90),
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(this.Width / 100, this.Width / 100, this.Width / 100, 0)
            };
            this.Controls.Add(BlockedPanelContainer);
        }

        private void LoadBlockedUsers()
        {
            foreach (var username in SettingsManager.CurrentSettings.BlockedUsers)
            {
                Panel blockedPanel = new Panel
                {
                    Width = (this.Width - 90) / 2,
                    Height = 80,
                    Margin = new Padding(left: this.Width / 100, top: this.Width / 100, right: 0, bottom: 0)
                };
                blockedPanel.Cursor = Cursors.Hand;

                blockedPanel.Click += (s, eArgs) =>
                {
                    Navigator.GoToUserView(username);
                };

                Label nameLabel = new Label
                {
                    Text = username,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    Location = new Point(10, 10),
                    AutoSize = true,
                    ForeColor = SettingsManager.TextColor
                };
                nameLabel.Cursor = Cursors.Hand;

                nameLabel.Click += (s, eArgs) =>
                {
                    Navigator.GoToUserView(username);
                };

                MinimalButton UnblockButton = new MinimalButton
                {
                    Text = "Unblock",
                    Tag = username
                };
                UnblockButton.Click += UnblockButton_Click;

                blockedPanel.Controls.Add(nameLabel);
                blockedPanel.Controls.Add(UnblockButton);
                BlockedPanelContainer.Controls.Add(blockedPanel);
            }
        }

        private void Dashboard_Resize(object sender, EventArgs e)
            => UpdateSizes();

        private void UpdateSizes()
        {
            // Update BlockedPanelContainer size to account for back icon and title
            BlockedPanelContainer.Size = new Size(this.Width, this.Height - 90);

            foreach (Control control in BlockedPanelContainer.Controls)
            {
                if (control is Panel blockedPanel)
                {
                    blockedPanel.Width = Math.Max(this.Width / 4, 150);
                    blockedPanel.Height = Math.Max(this.Height / 15, 50);
                    blockedPanel.Margin = new Padding(left: Math.Max(this.Width / 100, 10), top: Math.Max(this.Width / 100, 10), right: 0, bottom: 0);

                    foreach (Control innerControl in blockedPanel.Controls)
                    {
                        if (innerControl is MinimalButton unblockButton)
                        {
                            unblockButton.Width = Math.Max(this.Width / 10, 80);
                            unblockButton.Height = Math.Max(this.Height / 20, 30);
                            unblockButton.Location = new Point(
                                unblockButton.Parent.Width - unblockButton.Width - 10,
                                (unblockButton.Parent.Height - unblockButton.Height) / 2);
                        }
                        else if (innerControl is Label nameLabel)
                        {
                            nameLabel.Location = new Point(10, (blockedPanel.Height - nameLabel.Height) / 2);
                        }
                    }
                }

                BlockedPanelContainer.Padding = new Padding(Math.Max(this.Width / 100, 10), Math.Max(this.Width / 100, 10), Math.Max(this.Width / 100, 10), 0);
            }
        }

        private void UnblockButton_Click(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.Tag is string username)
            {
                DialogResult dialogResult = MessageBox.Show(
                    $"Are you sure you want to unblock {username}?",
                    "Confirm Unblock",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (dialogResult == DialogResult.Yes)
                {
                    SettingsManager.UnblockUser(username);
                    MessageBox.Show($"{username} has been unblocked.");
                }
            }
        }
    }
}