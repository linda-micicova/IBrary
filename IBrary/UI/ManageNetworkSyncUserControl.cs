using IBrary.Managers;
using IBrary.Models;
using IBrary.Network;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using System.Windows.Forms;


namespace IBrary.UserControls
{
    // NOT ACTUALLYY USED, ONLY TO DEBUG NETWORKSYNCSERVICE
    // Start Listening button tries to use the same port as the service that runs
    // on the background, so this only works if MainForm doesn't start the service.
    public partial class ManageNetworkSyncUserControl : UserControl
    {
        private NetworkSyncService networkService;

        // UI Components
        private Panel statusPanel;
        private Panel devicePanel;
        private Panel messagePanel;
        private Panel logPanel;

        private Label statusLabel;
        private Label localIPLabel;
        private MinimalButton startStopButton;
        private MinimalButton discoverButton;

        private ListBox deviceListBox;
        private Label deviceCountLabel;
        private MinimalButton refreshDevicesButton;
        private MinimalButton clearDevicesButton;

        private TextBox messageTextBox;
        private MinimalButton sendMessageButton;
        private Label selectedDeviceLabel;

        private TextBox logTextBox;
        private MinimalButton clearLogButton;

        private bool isServiceRunning = false;

        public ManageNetworkSyncUserControl()
        {
            InitializeComponent();
            InitializeNetworkService();
            InitializeUI();
            this.Resize += NetworkSync_Resize;
        }

        private void InitializeNetworkService()
        {
            networkService = new NetworkSyncService();
            networkService.JsonDataReceived += OnJsonDataReceived;
        }

        private void InitializeUI()
        {
            this.BackColor = App.Settings.BackgroundColor;

            CreateStatusPanel();
            CreateDevicePanel();
            CreateMessagePanel();
            CreateLogPanel();

            UpdateSizes();
            LogMessage("Network Sync Manager initialized");
        }

        private void CreateStatusPanel()
        {
            statusPanel = new Panel
            {
                BackColor = App.Settings.FlashcardColor,
                BorderStyle = BorderStyle.FixedSingle
            };

            statusLabel = new Label
            {
                Text = "Status: Stopped",
                Font = new Font("Arial", 12, FontStyle.Bold),
                ForeColor = Color.Red,
                AutoSize = true,
                Location = new Point(10, 10)
            };

            localIPLabel = new Label
            {
                Text = $"Local IP: {GetLocalIPAddress()}",
                Font = new Font("Arial", 10),
                ForeColor = App.Settings.TextColor,
                AutoSize = true,
                Location = new Point(10, 35)
            };

            startStopButton = new MinimalButton
            {
                Text = "Start Listening",
                Size = new Size(120, 35),
                Location = new Point(10, 60)
            };
            startStopButton.Click += StartStopButton_Click;

            discoverButton = new MinimalButton
            {
                Text = "Discover Devices",
                Size = new Size(120, 35),
                Location = new Point(140, 60),
                Enabled = false
            };
            discoverButton.Click += DiscoverButton_Click;

            statusPanel.Controls.AddRange(new Control[] {
                statusLabel, localIPLabel, startStopButton, discoverButton
            });

            this.Controls.Add(statusPanel);
        }

        private void CreateDevicePanel()
        {
            devicePanel = new Panel
            {
                BackColor = App.Settings.FlashcardColor,
                BorderStyle = BorderStyle.FixedSingle
            };

            var deviceLabel = new Label
            {
                Text = "Discovered Devices:",
                Font = new Font("Arial", 12, FontStyle.Bold),
                ForeColor = App.Settings.TextColor,
                AutoSize = true,
                Location = new Point(10, 10)
            };

            deviceCountLabel = new Label
            {
                Text = "Count: 0",
                Font = new Font("Arial", 10),
                ForeColor = App.Settings.TextColor,
                AutoSize = true,
                Location = new Point(150, 10)
            };

            deviceListBox = new ListBox
            {
                BackColor = App.Settings.BackgroundColor,
                ForeColor = App.Settings.TextColor,
                Font = new Font("Consolas", 10),
                Location = new Point(10, 35)
            };
            deviceListBox.SelectedIndexChanged += DeviceListBox_SelectedIndexChanged;

            refreshDevicesButton = new MinimalButton
            {
                Text = "Refresh",
                Size = new Size(80, 30)
            };
            refreshDevicesButton.Click += RefreshDevicesButton_Click;

            clearDevicesButton = new MinimalButton
            {
                Text = "Clear List",
                Size = new Size(80, 30)
            };
            clearDevicesButton.Click += ClearDevicesButton_Click;

            devicePanel.Controls.AddRange(new Control[] {
                deviceLabel, deviceCountLabel, deviceListBox,
                refreshDevicesButton, clearDevicesButton
            });

            this.Controls.Add(devicePanel);
        }

        private void CreateMessagePanel()
        {
            messagePanel = new Panel
            {
                BackColor = App.Settings.FlashcardColor,
                BorderStyle = BorderStyle.FixedSingle
            };

            var messageLabel = new Label
            {
                Text = "Send Message:",
                Font = new Font("Arial", 12, FontStyle.Bold),
                ForeColor = App.Settings.TextColor,
                AutoSize = true,
                Location = new Point(10, 10)
            };

            selectedDeviceLabel = new Label
            {
                Text = "No device selected",
                Font = new Font("Arial", 10),
                ForeColor = App.Settings.TextColor,
                AutoSize = true,
                Location = new Point(10, 35)
            };

            messageTextBox = new TextBox
            {
                BackColor = App.Settings.BackgroundColor,
                ForeColor = App.Settings.TextColor,
                Font = new Font("Arial", 10),
                Multiline = true,
                Text = "Hello from IBrary!",
                Location = new Point(10, 60)
            };

            sendMessageButton = new MinimalButton
            {
                Text = "Send Message",
                Size = new Size(120, 35),
                Enabled = false
            };
            sendMessageButton.Click += SendMessageButton_Click;

            messagePanel.Controls.AddRange(new Control[] {
                messageLabel, selectedDeviceLabel, messageTextBox, sendMessageButton
            });

            this.Controls.Add(messagePanel);
        }

        private void CreateLogPanel()
        {
            logPanel = new Panel
            {
                BackColor = App.Settings.FlashcardColor,
                BorderStyle = BorderStyle.FixedSingle
            };

            var logLabel = new Label
            {
                Text = "Activity Log:",
                Font = new Font("Arial", 12, FontStyle.Bold),
                ForeColor = App.Settings.TextColor,
                AutoSize = true,
                Location = new Point(10, 10)
            };

            logTextBox = new TextBox
            {
                BackColor = App.Settings.BackgroundColor,
                ForeColor = App.Settings.TextColor,
                Font = new Font("Consolas", 9),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Location = new Point(10, 35)
            };

            clearLogButton = new MinimalButton
            {
                Text = "Clear Log",
                Size = new Size(80, 30)
            };
            clearLogButton.Click += ClearLogButton_Click;

            logPanel.Controls.AddRange(new Control[] {
                logLabel, logTextBox, clearLogButton
            });

            this.Controls.Add(logPanel);
        }

        private void StartStopButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (!isServiceRunning)
                {
                    networkService.StartListening();
                    isServiceRunning = true;
                    statusLabel.Text = "Status: Running";
                    statusLabel.ForeColor = Color.Green;
                    startStopButton.Text = "Stop Listening";
                    discoverButton.Enabled = true;
                    LogMessage("Network service started - listening on UDP:9999 and TCP:10000");
                }
                else
                {
                    networkService.StopListening();
                    isServiceRunning = false;
                    statusLabel.Text = "Status: Stopped";
                    statusLabel.ForeColor = Color.Red;
                    startStopButton.Text = "Start Listening";
                    discoverButton.Enabled = false;
                    LogMessage("Network service stopped");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"ERROR: {ex.Message}");
                MessageBox.Show($"Error: {ex.Message}", "Network Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DiscoverButton_Click(object sender, EventArgs e)
        {
            try
            {
                networkService.BroadcastPresence();
                LogMessage("Broadcasting discovery request...");

                // Clear existing devices before discovering new ones
                deviceListBox.Items.Clear();
                UpdateDeviceCount();
                selectedDeviceLabel.Text = "No device selected";
                sendMessageButton.Enabled = false;

                // Refresh device list after a short delay
                var timer = new Timer();
                timer.Interval = 3000; // 3 seconds - increased to allow more time for responses
                timer.Tick += (s, args) =>
                {
                    timer.Stop();
                    LogMessage("Discovery timeout reached");
                };
                timer.Start();
            }
            catch (Exception ex)
            {
                LogMessage($"ERROR broadcasting: {ex.Message}");
            }
        }

        private void DeviceListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (deviceListBox.SelectedItem != null)
            {
                selectedDeviceLabel.Text = $"Target: {deviceListBox.SelectedItem}";
                sendMessageButton.Enabled = true;
            }
            else
            {
                selectedDeviceLabel.Text = "No device selected";
                sendMessageButton.Enabled = false;
            }
        }

        private void SendMessageButton_Click(object sender, EventArgs e)
        {
            if (deviceListBox.SelectedItem == null || string.IsNullOrWhiteSpace(messageTextBox.Text))
                return;

            try
            {
                // Extract IP from the display string (format: "IP - DeviceName")
                string selectedItem = deviceListBox.SelectedItem.ToString();
                string targetIPString = selectedItem.Split(' ')[0]; // Get first part before " - "

                IPAddress targetIP = IPAddress.Parse(targetIPString);

                var message = new
                {
                    Type = "USER_MESSAGE",
                    Sender = Environment.MachineName,
                    Username = App.Settings.CurrentSettings.Username ?? "Anonymous",
                    Message = messageTextBox.Text,
                    Timestamp = DateTime.Now
                };

                networkService.SendJsonToDevice(targetIP, message);
                LogMessage($"Sent message to {targetIP}: \"{messageTextBox.Text}\"");
            }
            catch (Exception ex)
            {
                LogMessage($"ERROR sending message: {ex.Message}");
            }
        }

        private void RefreshDevicesButton_Click(object sender, EventArgs e)
        {
            RefreshDeviceList();
        }

        private void ClearDevicesButton_Click(object sender, EventArgs e)
        {
            deviceListBox.Items.Clear();
            UpdateDeviceCount();
            selectedDeviceLabel.Text = "No device selected";
            sendMessageButton.Enabled = false;
            LogMessage("Device list cleared");
        }

        private void ClearLogButton_Click(object sender, EventArgs e)
        {
            logTextBox.Clear();
            LogMessage("Log cleared");
        }

        private void RefreshDeviceList()
        {
            try
            {
                // Get devices from NetworkSyncService
                // You'll need to modify NetworkSyncService to return a list of discovered devices
                // For now, this will trigger the discovery process again
                DiscoverButton_Click(null, null);
            }
            catch (Exception ex)
            {
                LogMessage($"ERROR refreshing devices: {ex.Message}");
            }
        }

        private void OnJsonDataReceived(string jsonData)
        {
            // Handle incoming messages
            try
            {
                dynamic data = Newtonsoft.Json.JsonConvert.DeserializeObject(jsonData);
                string messageType = data.Type;

                switch (messageType)
                {
                    case "USER_MESSAGE":
                        string sender = data.Sender ?? "Unknown";
                        string username = data.Username ?? "Anonymous";
                        string message = data.Message ?? "";

                        // Show message in a popup
                        this.Invoke(new Action(() =>
                        {
                            MessageBox.Show($"Message from {username} ({sender}):\n\n{message}",
                                "Received Message", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }));

                        LogMessage($"Received message from {username} ({sender}): \"{message}\"");
                        break;

                    case "DISCOVERY_RESPONSE":
                        // Extract device information and add to list
                        string deviceName = data.DeviceName?.ToString() ?? "Unknown Device";
                        string deviceIP = data.DeviceIP?.ToString() ?? "";

                        // If DeviceIP is not in the response, try to get it from the sender
                        if (string.IsNullOrEmpty(deviceIP) && data.SenderIP != null)
                        {
                            deviceIP = data.SenderIP.ToString();
                        }

                        this.Invoke(new Action(() =>
                        {
                            if (!string.IsNullOrEmpty(deviceIP))
                            {
                                // Create display string with IP and device name
                                string displayText = $"{deviceIP} - {deviceName}";

                                // Check if device already exists in the list
                                bool deviceExists = false;
                                foreach (var item in deviceListBox.Items)
                                {
                                    if (item.ToString().StartsWith(deviceIP))
                                    {
                                        deviceExists = true;
                                        break;
                                    }
                                }

                                // Add device if it doesn't exist
                                if (!deviceExists)
                                {
                                    deviceListBox.Items.Add(displayText);
                                    UpdateDeviceCount();
                                    LogMessage($"Added discovered device: {displayText}");
                                }
                            }
                            else
                            {
                                LogMessage($"Discovered device without IP: {deviceName}");
                            }
                        }));
                        break;

                    case "DISCOVERY_REQUEST":
                        // This is when another device is looking for us
                        // The NetworkSyncService should handle sending a response automatically
                        string requesterName = data.DeviceName?.ToString() ?? "Unknown Device";
                        LogMessage($"Received discovery request from: {requesterName}");
                        break;
                }
            }
            catch (Exception ex)
            {
                LogMessage($"ERROR parsing received data: {ex.Message}");
                LogMessage($"Raw data: {jsonData}");
            }
        }

        private void UpdateDeviceCount()
        {
            deviceCountLabel.Text = $"Count: {deviceListBox.Items.Count}";
        }

        private void LogMessage(string message)
        {
            if (logTextBox.InvokeRequired)
            {
                logTextBox.Invoke(new Action<string>(LogMessage), message);
                return;
            }

            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            logTextBox.AppendText($"[{timestamp}] {message}\r\n");
            logTextBox.SelectionStart = logTextBox.Text.Length;
            logTextBox.ScrollToCaret();
        }

        private string GetLocalIPAddress()
        {
            try
            {
                var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        return ip.ToString();
                    }
                }
                return "127.0.0.1";
            }
            catch
            {
                return "Unknown";
            }
        }

        private void NetworkSync_Resize(object sender, EventArgs e)
            => UpdateSizes();

        private void UpdateSizes()
        {
            int margin = 10;
            int panelHeight = (this.Height - (5 * margin)) / 4;

            // Status Panel (top)
            statusPanel.Location = new Point(margin, margin);
            statusPanel.Size = new Size(this.Width - (2 * margin), panelHeight);

            // Device Panel (second)
            devicePanel.Location = new Point(margin, statusPanel.Bottom + margin);
            devicePanel.Size = new Size((this.Width / 2) - (2 * margin), panelHeight * 2);

            // Update device list box size
            deviceListBox.Size = new Size(devicePanel.Width - 20, devicePanel.Height - 80);
            refreshDevicesButton.Location = new Point(10, devicePanel.Height - 35);
            clearDevicesButton.Location = new Point(100, devicePanel.Height - 35);

            // Message Panel (right of device panel)
            messagePanel.Location = new Point(devicePanel.Right + margin, devicePanel.Top);
            messagePanel.Size = new Size((this.Width / 2) - (2 * margin), panelHeight * 2);

            // Update message text box size
            messageTextBox.Size = new Size(messagePanel.Width - 20, messagePanel.Height - 120);
            sendMessageButton.Location = new Point(10, messagePanel.Height - 45);

            // Log Panel (bottom)
            logPanel.Location = new Point(margin, devicePanel.Bottom + margin);
            logPanel.Size = new Size(this.Width - (2 * margin), panelHeight);

            // Update log text box size
            logTextBox.Size = new Size(logPanel.Width - 20, logPanel.Height - 50);
            clearLogButton.Location = new Point(logPanel.Width - 90, 10);
        }

        /*protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                networkService?.StopListening();
            }
            base.Dispose(disposing);
        }*/
    }
}