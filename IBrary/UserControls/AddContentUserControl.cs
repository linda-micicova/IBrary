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
    public partial class AddContentUserControl : UserControl
    {
        private MinimalButton addFlashcardButton;
        private MinimalButton addTopicButton;
        private MinimalButton addSubjecButtont;
        private Panel navigationPanel;
        private Panel contentPanel = new Panel();
        public AddContentUserControl()
        {
            InitializeComponent();
            InitializeUI();
            this.Resize += AddContent_Resize;
        }
        private void InitializeUI()
        {
            navigationPanel = new Panel
            {
                Location = new Point(0, 0),
            };
            addFlashcardButton = new MinimalButton
            {
                Text = "Add Flashcard",
            };
            addTopicButton = new MinimalButton
            {
                Text = "Add Topic"
            };
            addSubjecButtont = new MinimalButton
            {
                Text = "Add Subject"
            };
            addFlashcardButton.Click += addFlashcardButton_Click;
            addTopicButton.Click += addTopicButton_Click;
            addSubjecButtont.Click += addSubjecButtont_Click;

            this.Controls.Add(navigationPanel);
            this.Controls.Add(contentPanel);
            navigationPanel.Controls.Add(addFlashcardButton);
            navigationPanel.Controls.Add(addTopicButton);
            navigationPanel.Controls.Add(addSubjecButtont);

            SwitchUserControl(new AddOrEditFlashcardUserControl());
        }
        private void SwitchUserControl(Control control)
        {
            contentPanel.Controls.Clear();
            control.Dock = DockStyle.Fill;
            contentPanel.Controls.Add(control);
        }
        private void addFlashcardButton_Click(object sender, EventArgs e)
        {
            //SubjectManager.Load();
            //TopicManager.Load();

            SwitchUserControl(new AddOrEditFlashcardUserControl());
        }
        private void addTopicButton_Click(object sender, EventArgs e)
        {
            //SubjectManager.Load();

            SwitchUserControl(new AddTopicUserControl());
        }
        private void addSubjecButtont_Click(object sender, EventArgs e)
        {
            SwitchUserControl(new AddSubjectUserControl());
        }
        private void AddContent_Resize(object sender, EventArgs e)
            => UpdateSizes();

        private void UpdateSizes()
        {
            navigationPanel.Width = this.Width;
            navigationPanel.Height = Math.Max(this.Height / 6, 100);

            contentPanel.Location = new Point(0, navigationPanel.Height);
            contentPanel.Width = this.Width;
            contentPanel.Height = this.Height - navigationPanel.Height;

            // Button dimensions (same for all)
            var buttonWidth = Math.Max(navigationPanel.Width / 5, 150);
            var buttonHeight = navigationPanel.Height / 3;
            var buttonSpacing = navigationPanel.Width / 40;
            var buttonTop = navigationPanel.Height / 10;

            // Add Flashcard Button (leftmost)
            addFlashcardButton.Location = new Point(buttonSpacing, buttonTop);
            addFlashcardButton.Width = buttonWidth;
            addFlashcardButton.Height = buttonHeight;

            // Add Topic Button (middle)
            addTopicButton.Location = new Point(addFlashcardButton.Right + buttonSpacing, buttonTop);
            addTopicButton.Width = buttonWidth;
            addTopicButton.Height = buttonHeight;

            // Add Subject Button (rightmost)
            addSubjecButtont.Location = new Point(addTopicButton.Right + buttonSpacing, buttonTop);
            addSubjecButtont.Width = buttonWidth;
            addSubjecButtont.Height = buttonHeight;
        }
    }
}
