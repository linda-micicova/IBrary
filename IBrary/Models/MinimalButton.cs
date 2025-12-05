using IBrary.Managers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IBrary.Models
{
    public class MinimalButton : Button
    {
        // Default style
        public MinimalButton()
        {
            this.FlatStyle = FlatStyle.Flat;
            this.FlatAppearance.BorderSize = 0;
            this.BackColor = SettingsManager.ButtonColor; // Light gray
            this.ForeColor = SettingsManager.TextColor;    // Dark gray
            this.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            this.Padding = new Padding(10, 5, 10, 5);
            this.Cursor = Cursors.Hand;

            // Hover effects
            //this.MouseEnter += (s, e) => this.BackColor = Color.FromArgb(220, 220, 220);
            //this.MouseLeave += (s, e) => this.BackColor = SettingsManager.CurrentSettings.Color3;
        }

        // Accent style for primary actions
        public void SetAccentStyle()
        {
            this.BackColor = Color.FromArgb(0, 122, 204); // Blue
            this.ForeColor = Color.White;
            this.MouseEnter += (s, e) => this.BackColor = Color.FromArgb(0, 96, 180);
            this.MouseLeave += (s, e) => this.BackColor = SettingsManager.ButtonColor;
        }
    }
}
