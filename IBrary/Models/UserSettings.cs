using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using IBrary.Models;
using System.Drawing;
using System.Security.Cryptography;


namespace IBrary.App_settings
{
    public class UserSettings
    {
        [JsonIgnore]
        public string Username
        {
            get => username;
            set
            {
                username = value;
                userHash = string.IsNullOrEmpty(value) ? null : GenerateUserHash(value);
            }
        }

        [JsonPropertyName("username")]
        public string username { get; set; }

        [JsonPropertyName("user_verification")]
        public string userHash { get; set; }

        public List<string> MySubjectIds { get; set; } = new List<string>();
        public Dictionary<string, Level> MySubjectLevels { get; set; } = new Dictionary<string, Level>();
        public int ErrorRateWeight { get; set; }
        public int TimeFactorWeight { get; set; }
        public int ImportantTagWeight { get; set; }
        public string LastSelectedSubjectId { get; set; }
        public List<string> LastSelectedTopicIds { get; set; } = new List<string>();
        public List<string> BlockedUsers { get; set; } = new List<string>();
        public string Theme { get; set; } = "Light"; // Default theme

        // Validation method
        public bool IsUsernameValid()
        {
            if (string.IsNullOrEmpty(username)) return true; // No username set is valid
            return userHash == GenerateUserHash(username);
        }

        private static string GenerateUserHash(string username)
        {
            try
            {
                string machineId = Environment.MachineName + Environment.UserName;
                string combined = username + machineId + "IBrary2025";

                using (var sha256 = SHA256.Create())
                {
                    byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
                    return Convert.ToBase64String(hash);
                }
            }
            catch
            {
                return null;
            }
        }
    }
}

/*namespace IBrary.App_settings
{
    public class UserSettings
    {
        public string Username { get; set; }
        public List<string> MySubjectIds { get; set; } = new List<string>();
        public Dictionary<string, Level> MySubjectLevels { get; set; } = new Dictionary<string, Level>();
        public int ErrorRateWeight { get; set; }
        public int TimeFactorWeight { get; set; }
        public int ImportantTagWeight { get; set; }
        public string LastSelectedSubjectId { get; set; }
        public List<string> LastSelectedTopicIds { get; set; } = new List<string>();
        public List<string> BlockedUsers { get; set; }
        public string Theme {get; set; } = "Light"; // Default theme
        
        
    }

}*/
