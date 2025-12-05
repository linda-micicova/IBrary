using IBrary.Managers;
using IBrary.Models;
using IBrary.Network;
using IBrary.UserControls;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IBrary
{
    public partial class MainForm : Form
    {
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        private PictureBox flashcardIcon = new PictureBox();
        private PictureBox addIcon = new PictureBox();
        private PictureBox dashboardIcon = new PictureBox();
        private PictureBox settingsIcon = new PictureBox();

        private System.Windows.Forms.Timer syncTimer;
        private NetworkSyncService syncService;

        public MainForm()
        {

            InitializeComponent();

            using (var ms = new MemoryStream(Properties.Resources.appIcon))
            {
                this.Icon = new Icon(ms);
            }
            this.Refresh();

            SettingsManager.Load(); // Load settings from the file

            InitializeUI();
            this.WindowState = FormWindowState.Maximized;

            SetupTitleBar();

            // Initialize Navigator
            Navigator.Initialize(contentPanel, OnThemeChanged);

            this.FormClosing += MainForm_FormClosing;

        }
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            syncTimer?.Stop();
            syncService?.StopListening();

            SettingsManager.Save(); // Save settings to the file
            FlashcardManager.Save(); // Save flashcards to the file
            TopicManager.Save(); // Save topics to the file
        }
        private void OnThemeChanged()
        {
            InitializeUI();
            Navigator.GoToSettings(); // Refresh settings screen
        }
        private void SetupTitleBar()
        {
            if (SettingsManager.CurrentSettings.Theme == "Dark")
            {
                var preference = Convert.ToInt32(true);
                DwmSetWindowAttribute(this.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref preference, sizeof(int));
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            SetupTitleBar();
        }

        private void InitializeUI()
        {
            this.MinimumSize = new Size(600, 400); // Set a minimum size for the form
            menuPanel.BackColor = SettingsManager.MenuColor; // Dark background color for the menu panel
            contentPanel.BackColor = SettingsManager.BackgroundColor; // Light background color for the content panel
            flashcardIcon = new PictureBox
            {
                Image = Properties.Resources.brainWhite, // Replace with your image resource
                SizeMode = PictureBoxSizeMode.Zoom,
                Location = new Point(25, 30), // Adjust the location as needed
                Width = 40, // Set the width of the PictureBox
                Cursor = Cursors.Hand // Set the cursor to hand for the settings icon
            };
            addIcon = new PictureBox
            {
                Image = Properties.Resources.addWhite, // Replace with your image resource
                SizeMode = PictureBoxSizeMode.Zoom,
                Location = new Point(25, 110), // Adjust the location as needed
                Width = 40, // Set the width of the PictureBox
                Cursor = Cursors.Hand // Set the cursor to hand for the settings icon
            };
            dashboardIcon = new PictureBox
            {
                Image = Properties.Resources.statsWhite, // Replace with your image resource
                SizeMode = PictureBoxSizeMode.Zoom,
                Location = new Point(25, 190), // Adjust the location as needed
                Width = 40, // Set the width of the PictureBox
                Cursor = Cursors.Hand // Set the cursor to hand for the settings icon
            };
            settingsIcon = new PictureBox
            {
                Image = Properties.Resources.settingsWhite, // Replace with your image resource
                SizeMode = PictureBoxSizeMode.Zoom,
                Location = new Point(25, 270), // Adjust the location as needed
                Width = 40, // Set the width of the PictureBox
                Cursor = Cursors.Hand // Set the cursor to hand for the settings icon
            };

            //Add click event handlers for the icons
            flashcardIcon.Click += flashcardIcon_Click;
            addIcon.Click += addIcon_Click;
            dashboardIcon.Click += dashboardIcon_Click;
            settingsIcon.Click += settingsIcon_Click;

            //Add controls to the menu panel
            menuPanel.Controls.Add(flashcardIcon);
            menuPanel.Controls.Add(addIcon);
            menuPanel.Controls.Add(dashboardIcon);
            menuPanel.Controls.Add(settingsIcon);
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            SwitchUserControl(new FlashcardUserControl());

            SettingsManager.InitializeDefaultFiles();

            // START AUTOMATIC NETWORK SYNC
            syncService = new IBrary.Network.NetworkSyncService();

            // Subscribe to FlashcardsSynced event for automatic refresh
            syncService.FlashcardsSynced += OnFlashcardsSynced;

            // Start listening for network connections
            syncService.StartListening();

            // Broadcast presence immediately to discover devices
            syncService.BroadcastPresence();

            // Set up periodic broadcast every 30 seconds to discover new devices
            syncTimer = new System.Windows.Forms.Timer();
            syncTimer.Interval = 30000; // 30 seconds
            syncTimer.Tick += (s, _) =>
            {
                syncService.BroadcastPresence();
                syncService.UpdateLocalFileHashes(); // Update hashes in case files changed locally
            };
            syncTimer.Start();
        }

        private void OnFlashcardsSynced()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(OnFlashcardsSynced));
                return;
            }

            // Find FlashcardUserControl if it's currently displayed
            if (contentPanel.Controls.Count > 0 && contentPanel.Controls[0] is FlashcardUserControl flashcardUC)
            {
                flashcardUC.ForceRefresh();
            }

        }

        private void addIcon_Click(object sender, EventArgs e)
        {
            if (SettingsManager.CurrentSettings.Username != null && SettingsManager.CurrentSettings.Username != "")
            {
                SwitchUserControl(new AddContentUserControl());
            }
            else
            {
                MessageBox.Show("You must be logged in to add a flashcard.", "Login Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

        }

        private void dashboardIcon_Click(object sender, EventArgs e)
        {
            SwitchUserControl(new DashboardUserControl());
        }

        private void settingsIcon_Click(object sender, EventArgs e)
        {
            SettingsUserControl settingsUserControl = new SettingsUserControl();
            SwitchUserControl(settingsUserControl);


        }
        private void flashcardIcon_Click(object sender, EventArgs e)
        {

            SwitchUserControl(new FlashcardUserControl());

        }
        public void SwitchUserControl(Control control)
        {
            contentPanel.Controls.Clear();
            control.Dock = DockStyle.Fill;
            contentPanel.Controls.Add(control);

            if (control is SettingsUserControl settings)
            {
                settings.LoginRequested += (s, e) => SwitchUserControl(new LoginUserControl());
                settings.MySubjectsRequested += (s, e) => SwitchUserControl(new MySubjectsUserControl());
                settings.PriorityCalculationsRequested += (s, e) => SwitchUserControl(new PriorityCalculationsUserControl());
                settings.ChangeThemeRequested += (s, e) =>
                {
                    InitializeUI();
                    SwitchUserControl(new SettingsUserControl());

                };
                settings.BlockedUsersRequested += (s, e) => SwitchUserControl(new BlockedUsersUserControl());
            }
            else if (control is FlashcardUserControl flashcardUC)
            {
                flashcardUC.UserViewRequested += (s, e) => SwitchUserControl(new UserViewUserControl());
            }
            else if (control is LoginUserControl login)
            {
                login.SettingsRequested += (s, e) => SwitchUserControl(new SettingsUserControl());
            }
            else if (control is PriorityCalculationsUserControl priorityCalculations)
            {
                priorityCalculations.SettingsRequested += (s, e) => SwitchUserControl(new SettingsUserControl());
            }
            else if (control is MySubjectsUserControl mySubjects)
            {
                mySubjects.SettingsRequested += (s, e) => SwitchUserControl(new SettingsUserControl());
            }
        }
    }
}