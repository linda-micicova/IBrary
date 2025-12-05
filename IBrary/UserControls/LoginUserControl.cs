using Managers;
using IBrary.Managers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using IBrary.Models;


namespace IBrary.UserControls
{
    public partial class LoginUserControl : UserControl
    {
        private Label keyLabel = new Label();
        private TextBox keyTextBox = new TextBox();
        private MinimalButton loginButton = new MinimalButton();
        private MinimalButton cancelButton = new MinimalButton();
        public event EventHandler SettingsRequested;
        public LoginUserControl()
        {
            InitializeComponent();
            InitializeUI();

            this.Resize += Settings_Resize;
        }


        private void InitializeUI()
        {
            // Key Label
            keyLabel = new Label
            {
                Text = "Enter Access Key:",
                AutoSize = true,
                Font = new Font("Arial", 12, FontStyle.Regular),
                ForeColor = SettingsManager.TextColor,
            };

            // Key TextBox
            keyTextBox = new TextBox
            {
                TabIndex = 0,
                MaxLength = 44
            };

            // Login Button
            loginButton = new MinimalButton
            {
                Text = "Login",
                TabIndex = 1
            };
            loginButton.Click += (s, e) => ValidateKey(keyTextBox.Text);

            // Press Enter to login
            keyTextBox.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) ValidateKey(keyTextBox.Text); };

            this.Controls.AddRange(new Control[] { keyLabel, keyTextBox, loginButton });
        }

        private void ValidateKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                MessageBox.Show("Please enter an access key", "Error",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                string username = UserManager.GetUsernameFromKey(key);
                SettingsManager.CurrentSettings.Username = username;
                SettingsManager.Save();

                SettingsRequested?.Invoke(this, EventArgs.Empty);
            }
            catch (FormatException)
            {
                MessageBox.Show("The key format is invalid. Please check and try again.");
            }
            catch (CryptographicException)
            {
                MessageBox.Show("Failed to decrypt the key. The key might be invalid.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("An unexpected error occurred: " + ex.Message);
            }

        }

        private void Settings_Resize(object sender, EventArgs e)
            => UpdateSizes();
        private void UpdateSizes()
        {
            keyLabel.Size = new Size(this.Width - 40, keyLabel.Height);
            keyLabel.Location = new Point(20, 20);
            keyTextBox.Size = new Size(this.Width - 40, 30);
            keyTextBox.Location = new Point(20, keyLabel.Bottom + 10);
            loginButton.Size = new Size(100, 30);
            loginButton.Location = new Point(this.Width - 120, keyTextBox.Bottom + 10);
        }
    }
}
