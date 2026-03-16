using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using System.Windows.Forms;
using IBrary.Managers;
using IBrary.Models;

namespace IBrary.Network
{
    internal class NetworkSyncService
    {
        private const int UdpPort = 60000;
        private const int TcpPort = 10000;
        private UdpClient udpClient;
        private Thread udpListenThread;
        private Thread tcpThread;
        private TcpListener tcpListener;

        private string dataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "IBrary");

        // Event to handle received JSON data
        public event Action<string> JsonDataReceived;

        // Event to notify when flashcards have been synced
        public event Action FlashcardsSynced;

        // Hash manifest for tracking file versions
        private Dictionary<string, string> localFileHashes = new Dictionary<string, string>();

        private Dictionary<IPAddress, DateTime> lastSyncTimes = new Dictionary<IPAddress, DateTime>();
        private TimeSpan minSyncInterval = TimeSpan.FromSeconds(30); // Reduced to 30 seconds for testing

        private List<IPAddress> discoveredDevices = new List<IPAddress>();
        private volatile bool isRunning = false;

        // Gets the real local WiFi/LAN IP, skipping VirtualBox and other virtual adapters
        public IPAddress GetLocalWifiIP()
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up) continue;
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Tunnel) continue;
                if (ni.Description.Contains("VirtualBox") ||
                    ni.Description.Contains("Virtual") ||
                    ni.Description.Contains("Hyper-V") ||
                    ni.Description.Contains("VMware") ||
                    ni.Description.Contains("Loopback") || 
                    ni.Description.Contains("Launcher")) continue; 

                foreach (var addr in ni.GetIPProperties().UnicastAddresses)
                {
                    if (addr.Address.AddressFamily == AddressFamily.InterNetwork &&
                        !IPAddress.IsLoopback(addr.Address) &&
                        !addr.Address.ToString().StartsWith("169.254")) // skip local link
                        return addr.Address;
                }
            }
            return IPAddress.Loopback;
        }

        public void StartListening()
        {
            if (isRunning)
                return;

            isRunning = true;

            udpClient = new UdpClient(UdpPort);
            udpListenThread = new Thread(ListenForBroadcasts);
            udpListenThread.IsBackground = true;
            udpListenThread.Start();

            StartTcpServer();

            // Initialize local file hashes
            UpdateLocalFileHashes();
        }
        private bool IsNetworkAvailable()
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up) continue;
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Tunnel) continue;
                if (ni.Description.Contains("VirtualBox") ||
                    ni.Description.Contains("Virtual") ||
                    ni.Description.Contains("Hyper-V") ||
                    ni.Description.Contains("VMware") ||
                    ni.Description.Contains("Loopback") ||
                    ni.Description.Contains("Launcher")) continue;

                if (ni.GetIPProperties().GatewayAddresses.Count > 0)
                    return true;
            }
            return false;
        }

        public void BroadcastPresence()
        {
            //MessageBox.Show("Broadcasting presence to discover other devices");
            if (!IsNetworkAvailable())
            {
                //MessageBox.Show("No network available, skipping broadcast");
                return;
            }

            using (UdpClient sender = new UdpClient())
            {
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Broadcast, UdpPort);
                string message = "IBRARY_DISCOVERY";
                byte[] bytes = Encoding.UTF8.GetBytes(message);
                sender.EnableBroadcast = true;
                sender.Send(bytes, bytes.Length, endPoint);
            }
        }

        // Compute SHA-256 hash of file content
        private string ComputeSHA256Hash(string content)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(content));
                return Convert.ToBase64String(bytes);
            }
        }

        // Update local file hashes for the three core files
        public void UpdateLocalFileHashes()
        {
            //MessageBox.Show($"Data folder: {dataFolder}");  

            string[] coreFiles = { "flashcards.json", "topics.json", "subjects.json" };

            localFileHashes.Clear();

            foreach (string fileName in coreFiles)
            {
                string filePath = Path.Combine(dataFolder, fileName);
                if (File.Exists(filePath))
                {
                    string content = File.ReadAllText(filePath);
                    string hash = ComputeSHA256Hash(content);
                    localFileHashes[fileName] = hash;
                }
                else
                {
                    // File doesn't exist, set empty hash
                    localFileHashes[fileName] = string.Empty;
                }
            }
        }

        public void SendHashManifest(IPAddress targetIP)
        {
            //MessageBox.Show($"Sending hash manifest to {targetIP}");

            // Check if we've synced with this device recently
            if (lastSyncTimes.ContainsKey(targetIP))
            {
                TimeSpan timeSinceLastSync = DateTime.Now - lastSyncTimes[targetIP];
                if (timeSinceLastSync < minSyncInterval)
                {
                    //MessageBox.Show($"Skipping sync with {targetIP} - synced {timeSinceLastSync.TotalSeconds:F0} seconds ago");
                    return;
                }
            }

            // Update last sync time
            lastSyncTimes[targetIP] = DateTime.Now;

            var manifest = new
            {
                Type = "HASH_MANIFEST",
                DeviceName = Environment.MachineName,
                FileHashes = localFileHashes,
                Timestamp = DateTime.Now
            };

            SendJsonToDevice(targetIP, manifest);
        }

        // Request sync with all discovered devices
        public void InitiateSyncWithAllDevices()
        {
            var devices = GetDiscoveredDevices();
            //MessageBox.Show($"Initiating sync with {devices.Length} device(s)");

            foreach (IPAddress device in devices)
            {
                SendHashManifest(device);
            }
        }

        // Send specific files that are missing or different
        private void SendRequestedFiles(IPAddress targetIP, List<string> requestedFiles)
        {
            //MessageBox.Show($"Sending {requestedFiles.Count} requested file(s) to {targetIP}");

            foreach (string fileName in requestedFiles)
            {
                string filePath = Path.Combine(dataFolder, fileName);
                if (File.Exists(filePath))
                {
                    SendJsonFile(targetIP, filePath);
                }
            }
        }

        // Compare received manifest with local hashes and request missing files
        private void HandleHashManifest(dynamic manifestObj, IPAddress senderIP)
        {
            //MessageBox.Show($"Received hash manifest from {senderIP}");

            var remoteHashes = JsonConvert.DeserializeObject<Dictionary<string, string>>(
                manifestObj.FileHashes.ToString());

            List<string> filesToRequest = new List<string>();

            foreach (var remoteFile in remoteHashes)
            {
                string fileName = remoteFile.Key;
                string remoteHash = remoteFile.Value;

                // Check if we have this file and if hashes match
                if (!localFileHashes.ContainsKey(fileName) ||
                    localFileHashes[fileName] != remoteHash)
                {
                    // We don't have this file or it's different
                    if (!string.IsNullOrEmpty(remoteHash)) // Only request if remote has the file
                    {
                        filesToRequest.Add(fileName);
                        //MessageBox.Show($"File {fileName} needs to be synced");
                    }
                }
            }

            if (filesToRequest.Count > 0)
            {
                //MessageBox.Show($"Requesting {filesToRequest.Count} files from {senderIP}");

                var request = new
                {
                    Type = "FILE_REQUEST",
                    RequestedFiles = filesToRequest,
                    DeviceName = Environment.MachineName
                };

                SendJsonToDevice(senderIP, request);
            }
            else
            {
                //MessageBox.Show("All files are up to date");
            }

            // Don't send manifest back immediately to avoid loops
            // The other device will send theirs when they receive ours
        }

        // Handle file request from another device
        private void HandleFileRequest(dynamic requestObj, IPAddress senderIP)
        {
            var requestedFiles = JsonConvert.DeserializeObject<List<string>>(
                requestObj.RequestedFiles.ToString());

            SendRequestedFiles(senderIP, requestedFiles);
        }

        // Enhanced method to send JSON data to a specific device
        public void SendJsonToDevice(IPAddress targetIP, object jsonObject)
        {
            TcpClient client = null;
            NetworkStream stream = null;
            try
            {
                client = new TcpClient();
                client.Connect(targetIP, TcpPort);
                stream = client.GetStream();

                // Serialize object to JSON
                string jsonString = JsonConvert.SerializeObject(jsonObject);
                byte[] jsonData = Encoding.UTF8.GetBytes(jsonString);

                // Send the length first (4 bytes)
                byte[] lengthBytes = BitConverter.GetBytes(jsonData.Length);
                stream.Write(lengthBytes, 0, 4);

                // Then send the actual JSON data
                stream.Write(jsonData, 0, jsonData.Length);
            }
            catch (Exception ex)
            {
                //MessageBox.Show($"Error sending JSON to {targetIP}: {ex.Message}");
            }
            finally
            {
                stream?.Dispose();
                client?.Close();
            }
        }

        // Enhanced method to send JSON file with hash verification
        /*public void SendJsonFile(IPAddress targetIP, string filePath)
        {
            try
            {
                string jsonContent = File.ReadAllText(filePath);
                string contentHash = ComputeSHA256Hash(jsonContent);

                // Create a wrapper object that includes metadata and hash
                var fileData = new
                {
                    FileName = Path.GetFileName(filePath),
                    Content = jsonContent,
                    ContentHash = contentHash,
                    Type = "JSON_FILE",
                    Timestamp = DateTime.Now
                };

                SendJsonToDevice(targetIP, fileData);
            }
            catch (Exception ex)
            {
                //MessageBox.Show($"Error sending JSON file: {ex.Message}");
            }
        }*/
        public void SendJsonFile(IPAddress targetIP, string filePath)
        {
            try
            {
                string jsonContent = File.ReadAllText(filePath);
                string contentHash = ComputeSHA256Hash(jsonContent);

                var fileData = new
                {
                    FileName = Path.GetFileName(filePath),
                    Content = jsonContent,
                    ContentHash = contentHash,
                    Type = "JSON_FILE",
                    Timestamp = DateTime.Now
                };

                //MessageBox.Show($"Sending {Path.GetFileName(filePath)} to {targetIP}, size: {jsonContent.Length} chars"); 
                SendJsonToDevice(targetIP, fileData);
                //MessageBox.Show($"SendJsonToDevice completed for {targetIP}"); 
            }
            catch (Exception ex)
            {
                //MessageBox.Show($"Error sending JSON file: {ex.Message}"); 
            }
        }

        // Broadcast JSON to all discovered devices
        public void BroadcastJson(object jsonObject)
        {
            foreach (IPAddress device in GetDiscoveredDevices())
            {
                SendJsonToDevice(device, jsonObject);
            }
        }

        private void ListenForBroadcasts()
        {
            try
            {
                while (isRunning)
                {
                    IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, UdpPort);
                    byte[] data = udpClient.Receive(ref remoteEP);
                    string message = Encoding.UTF8.GetString(data);

                    if (message == "IBRARY_DISCOVERY")
                    {
                        // Don't respond to our own broadcasts
                        if (!IsLocalIPAddress(remoteEP.Address))
                        {
                            //MessageBox.Show($"Received discovery broadcast from {remoteEP.Address}");
                            SendTcpResponse(remoteEP.Address);
                        }
                    }
                }
            }
            catch
            {
                // Socket closed or stop requested
            }
        }

        // Check if IP is local, using GetLocalWifiIP to avoid VirtualBox confusion
        private bool IsLocalIPAddress(IPAddress address)
        {
            try
            {
                // Check against real WiFi IP first
                IPAddress localWifiIP = GetLocalWifiIP();
                if (localWifiIP.Equals(address)) return true;

                // Check all local IPs as fallback
                IPAddress[] localIPs = Dns.GetHostAddresses(Dns.GetHostName());
                foreach (IPAddress localIP in localIPs)
                {
                    if (localIP.Equals(address))
                        return true;
                }

                // Also check for localhost addresses
                return address.Equals(IPAddress.Loopback) ||
                       address.Equals(IPAddress.IPv6Loopback) ||
                       address.ToString() == "127.0.0.1" ||
                       address.ToString() == "::1";
            }
            catch
            {
                // If we can't determine, assume it's not local to be safe
                return false;
            }
        }

        private void SendTcpResponse(IPAddress target)
        {
            //MessageBox.Show($"Sending discovery response to {target}");
            TcpClient client = null;
            NetworkStream stream = null;
            try
            {
                client = new TcpClient();
                client.Connect(target, TcpPort);
                stream = client.GetStream();

                // Add device FIRST before sending response
                AddDiscoveredDevice(target);

                // Send a JSON response with hash manifest
                var response = new
                {
                    Message = "Hello from IBrary!",
                    DeviceName = Environment.MachineName,
                    AppVersion = "1.0",
                    Type = "DISCOVERY_RESPONSE",
                    FileHashes = localFileHashes,
                    SenderIP = GetLocalWifiIP().ToString() 
                };

                string jsonResponse = JsonConvert.SerializeObject(response);
                byte[] responseBytes = Encoding.UTF8.GetBytes(jsonResponse);

                // Send length first, then data
                byte[] lengthBytes = BitConverter.GetBytes(responseBytes.Length);
                stream.Write(lengthBytes, 0, 4);
                stream.Write(responseBytes, 0, responseBytes.Length);
            }
            catch (Exception ex)
            {
                //MessageBox.Show($"Error in TCP response: {ex.Message}");
            }
            finally
            {
                stream?.Dispose();
                client?.Close();
            }
        }

        private void StartTcpServer()
        {
            tcpThread = new Thread(() =>
            {
                try
                {
                    tcpListener = new TcpListener(IPAddress.Any, TcpPort);
                    tcpListener.Start();

                    while (isRunning)
                    {
                        TcpClient client = null;

                        try
                        {
                            client = tcpListener.AcceptTcpClient();
                            IPEndPoint clientEndPoint = client.Client.RemoteEndPoint as IPEndPoint;

                            // Handle each client in a separate thread
                            Thread clientThread = new Thread(() => HandleTcpClient(client, clientEndPoint.Address));
                            clientThread.IsBackground = true;
                            clientThread.Start();
                        }
                        catch
                        {
                            client?.Close();
                            if (!isRunning)
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    //MessageBox.Show($"TCP server error: {ex.Message}");
                }
            });
            tcpThread.IsBackground = true;
            tcpThread.Start();
        }

        private void HandleTcpClient(TcpClient client, IPAddress senderIP)
        {
            NetworkStream stream = null;
            try
            {
                stream = client.GetStream();

                // Read the length first (4 bytes)
                byte[] lengthBuffer = new byte[4];
                int bytesRead = 0;
                while (bytesRead < 4)
                {
                    int read = stream.Read(lengthBuffer, bytesRead, 4 - bytesRead);
                    if (read == 0) return; // Connection closed
                    bytesRead += read;
                }

                int dataLength = BitConverter.ToInt32(lengthBuffer, 0);

                // Read the actual JSON data
                byte[] buffer = new byte[dataLength];
                bytesRead = 0;
                while (bytesRead < dataLength)
                {
                    int read = stream.Read(buffer, bytesRead, dataLength - bytesRead);
                    if (read == 0) return; // Connection closed
                    bytesRead += read;
                }

                string jsonString = Encoding.UTF8.GetString(buffer);

                // Trigger event for received JSON
                JsonDataReceived?.Invoke(jsonString);
                //MessageBox.Show($"TCP received, length: {jsonString.Length}, starts with: {jsonString.Substring(0, Math.Min(50, jsonString.Length))}");
                // Handle specific message types
                HandleReceivedJson(jsonString, senderIP);
            }
            catch (Exception ex)
            {
                //MessageBox.Show($"Error handling TCP client: {ex.Message}");
            }
            finally
            {
                stream?.Dispose();
                client?.Close();
            }
        }

        private void HandleReceivedJson(string jsonString, IPAddress senderIP)
        {
            try
            {
                // Parse the JSON to determine message type
                dynamic jsonObj = JsonConvert.DeserializeObject(jsonString);
                string messageType = jsonObj.Type;

                switch (messageType)
                {
                    case "JSON_FILE":
                        HandleReceivedFile(jsonObj);
                        break;

                    case "DISCOVERY_RESPONSE":
                        HandleDiscoveryResponse(jsonObj, senderIP);
                        break;

                    case "HASH_MANIFEST":
                        HandleHashManifest(jsonObj, senderIP);
                        break;

                    case "FILE_REQUEST":
                        HandleFileRequest(jsonObj, senderIP);
                        break;

                    default:
                        //MessageBox.Show($"Received unknown message type: {messageType}");
                        break;
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show($"Error parsing received JSON: {ex.Message}");
            }
        }

        /*private void HandleReceivedFile(dynamic fileObj)
        {
            try
            {
                string fileName = fileObj.FileName;
                string content = fileObj.Content;
                string receivedHash = fileObj.ContentHash;

                // Verify hash integrity
                string computedHash = ComputeSHA256Hash(content);
                if (computedHash != receivedHash)
                {
                    //MessageBox.Show($"Hash mismatch for {fileName}! File may be corrupted.");
                    return;
                }

                SaveReceivedFile(fileName, content);

                // Update local hash after successful save
                localFileHashes[fileName] = computedHash;

                // Trigger merge process
                TriggerMergeProcess(fileName);
            }
            catch (Exception ex)
            {
                //MessageBox.Show($"Error handling received file: {ex.Message}");
            }
        }*/
        private void HandleReceivedFile(dynamic fileObj)
        {
            try
            {
                string fileName = fileObj.FileName;
                string content = fileObj.Content;
                string receivedHash = fileObj.ContentHash;
                //MessageBox.Show($"Received file: {fileName}, size: {content.Length} chars");

                /*string computedHash = ComputeSHA256Hash(content);
                if (computedHash != receivedHash)
                {
                    MessageBox.Show($"Hash mismatch for {fileName}! File may be corrupted.");
                    return;
                }*/
                string computedHash;
                try
                {
                    computedHash = ComputeSHA256Hash(content);
                    //MessageBox.Show("Hash computed OK");
                }
                catch (Exception ex)
                {
                    //MessageBox.Show($"Hash computation failed: {ex.Message}");
                    return;
                }

                //MessageBox.Show("Hash verified OK");
                SaveReceivedFile(fileName, content);
                //MessageBox.Show("SaveReceivedFile completed");

                TriggerMergeProcess(fileName);
                //MessageBox.Show("TriggerMergeProcess completed");
                localFileHashes[fileName] = computedHash;
            }
            catch (Exception ex)
            {
                //MessageBox.Show($"Error handling received file: {ex.Message}");
            }
        }

        private void HandleDiscoveryResponse(dynamic responseObj, IPAddress senderIP)
        {
            try
            {
                string deviceName = responseObj.DeviceName;
                //MessageBox.Show($"✓ Discovered device: {deviceName} ({senderIP})");

                // Add device to discovered list FIRST
                AddDiscoveredDevice(senderIP);

                // If the response includes file hashes, handle them
                if (responseObj.FileHashes != null)
                {
                    HandleHashManifest(responseObj, senderIP);
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show($"Error handling discovery response: {ex.Message}");
            }
        }

        /*private void SaveReceivedFile(string fileName, string content)
        {
            try
            {
                Directory.CreateDirectory(dataFolder);
                string filePath = Path.Combine(dataFolder, fileName);
                File.WriteAllText(filePath, content);

                //MessageBox.Show($"Saved received file: {filePath}");
            }
            catch (Exception ex)
            {
                //MessageBox.Show($"Error saving received file: {ex.Message}");
            }
        }*/
        /*private void SaveReceivedFile(string fileName, string content)
        {
            try
            {
                Directory.CreateDirectory(dataFolder);
                string filePath = Path.Combine(dataFolder, fileName);
                File.WriteAllText(filePath, content);

                MessageBox.Show($"Saved received file: {filePath}"); // uncomment
                System.Diagnostics.Process.Start("explorer.exe", dataFolder); // opens the folder
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving received file: {ex.Message}"); // uncomment
            }
        }*/
        private void SaveReceivedFile(string fileName, string content)
        {
            try
            {
                Directory.CreateDirectory(dataFolder);
                string filePath = Path.Combine(dataFolder, "temp_" + fileName);
                File.WriteAllText(filePath, content);
                //MessageBox.Show($"Saved received file to temp: {filePath}");
            }
            catch (Exception ex)
            {
                //MessageBox.Show($"Error saving received file: {ex.Message}");
            }
        }

        /*private void TriggerMergeProcess(string fileName)
        {
            //MessageBox.Show($"Triggering merge process for {fileName}");
            string fullPath = Path.Combine(dataFolder, fileName);

            switch (fileName)
            {
                case "flashcards.json":
                    var currentCount = App.Flashcards.Load().Count;
                    //MessageBox.Show($"Current flashcards before merge: {currentCount}");

                    List<Flashcard> flashcards = App.Flashcards.LoadFlashcardsFromJson(fullPath);
                    //MessageBox.Show($"Incoming flashcards: {flashcards.Count}");

                    App.Flashcards.MergeFlashcards(flashcards, false);

                    var afterCount = App.Flashcards.Load().Count;
                    //MessageBox.Show($"Flashcards after merge: {afterCount}");

                    // Notify UI to refresh
                    FlashcardsSynced?.Invoke();
                    break;

                case "topics.json":
                    List<Topic> topics = App.Topics.LoadTopicsFromJson(fullPath);
                    App.Topics.MergeTopics(topics);
                    //MessageBox.Show($"Merged {topics.Count} topics");
                    break;

                case "subjects.json":
                    List<Subject> subjects = App.Subjects.LoadSubjectsFromJson(fullPath);
                    App.Subjects.MergeSubjects(subjects);
                    //MessageBox.Show($"Merged {subjects.Count} subjects");
                    break;

                default:
                    //MessageBox.Show($"No merge function defined for {fileName}");
                    break;
            }
        }*/
        private void TriggerMergeProcess(string fileName)
        {
            //MessageBox.Show($"Triggering merge process for {fileName}");
            string tempPath = Path.Combine(dataFolder, "temp_" + fileName);
            if (!File.Exists(tempPath)) return;

            switch (fileName)
            {
                case "flashcards.json":
                    var currentCount = App.Flashcards.Load().Count;
                    //MessageBox.Show($"Current flashcards before merge: {currentCount}");
                    List<Flashcard> flashcards = App.Flashcards.LoadFlashcardsFromJson(tempPath);
                    //MessageBox.Show($"Incoming flashcards: {flashcards.Count}");
                    App.Flashcards.MergeFlashcards(flashcards, false);
                    //File.Delete(tempPath);
                    var afterCount = App.Flashcards.Load().Count;
                    //MessageBox.Show($"Flashcards after merge: {afterCount}");
                    FlashcardsSynced?.Invoke();
                    break;

                case "topics.json":
                    List<Topic> topics = App.Topics.LoadTopicsFromJson(tempPath);
                    App.Topics.MergeTopics(topics);
                    //File.Delete(tempPath);
                    //MessageBox.Show($"Merged {topics.Count} topics");
                    break;

                case "subjects.json":
                    List<Subject> subjects = App.Subjects.LoadSubjectsFromJson(tempPath);
                    App.Subjects.MergeSubjects(subjects);
                    //File.Delete(tempPath);
                    //MessageBox.Show($"Merged {subjects.Count} subjects");
                    break;

                default:
                    //MessageBox.Show($"No merge function defined for {fileName}");
                    break;
            }
        }

        private void AddDiscoveredDevice(IPAddress deviceIP)
        {
            if (!discoveredDevices.Contains(deviceIP))
            {
                discoveredDevices.Add(deviceIP);
                //MessageBox.Show($"Added device to list: {deviceIP}");
            }
        }

        private IPAddress[] GetDiscoveredDevices()
        {
            return discoveredDevices.ToArray();
        }

        public void PrintDiscoveredDevices()
        {
            if (discoveredDevices.Count == 0)
            {
                //MessageBox.Show("No devices discovered yet.");
            }
            else
            {
                string deviceList = "Discovered devices:\n";
                foreach (IPAddress device in discoveredDevices)
                {
                    deviceList += $"- {device}\n";
                }
                //MessageBox.Show(deviceList);
            }
        }

        public void StopListening()
        {
            isRunning = false;

            try
            {
                udpClient?.Close();
            }
            catch { }

            try
            {
                tcpListener?.Stop();
            }
            catch { }

            try
            {
                udpListenThread?.Join(200);
            }
            catch { }

            try
            {
                tcpThread?.Join(200);
            }
            catch { }
        }
    }
}