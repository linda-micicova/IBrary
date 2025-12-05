using Managers;
using IBrary.App_settings;
using IBrary.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IBrary.Managers
{
    public static class TopicManager
    {
        public static readonly string topicsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "IBrary",
        "topics.json");

        // list of all currently stored topics
        public static List<Topic> AllTopics { get; private set; } = new List<Topic>();

        // LOAD TOPICS FROM MEMORY
        public static List<Topic> Load()
        {
            AllTopics = LoadTopicsFromJson(topicsPath);
            return AllTopics;
        }

        public static List<Topic> LoadTopicsFromJson(string path)
        {
            if (!File.Exists(path))
                return new List<Topic>();
            try
            {
                string json = File.ReadAllText(path);
                var options = new JsonSerializerOptions
                {
                    Converters = { new JsonStringEnumConverter() }
                };
                return JsonSerializer.Deserialize<List<Topic>>(json, options)
                    ?? new List<Topic>();
            }
            catch (Exception ex)
            {
                return new List<Topic>();
            }
        }

        // Save any modifications of currently loaded subjects
        public static void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(topicsPath));

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Converters = { new JsonStringEnumConverter() }
                };

                File.WriteAllText(topicsPath, JsonSerializer.Serialize(AllTopics, options));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving flashcards: {ex.Message}");
            }
        }

        // MODIFY TOPICS

        // Create new topic
        public static void AddTopic(Topic topic)
        {
            if (!AllTopics.Any(t => t.TopicId == topic.TopicId))
            {
                AllTopics.Add(topic);
                Save();
            }
        }

        // Delete topic

        public static void RemoveTopic(Topic topic)
        {
            if (UserManager.isAdmin() && AllTopics.Any(t => t.TopicId == topic.TopicId))
            {
                AllTopics.Remove(topic);
                Save();
            }

        }

        // Merge topics
        public static void MergeTopics(List<Topic> topicsToMerge)
        {
            foreach (Topic topic in topicsToMerge)
            {
                var existingTopic = AllTopics.FirstOrDefault(f => f.TopicId == topic.TopicId);

                // If topic exists, just check if level matches
                if (existingTopic != null)
                {
                    // If level doesn't match, merge to default - SL
                    if (existingTopic.Level != topic.Level)
                    {
                        existingTopic.Level = Level.SL;
                    }
                }
                // If topic doesn't exist, add it
                else
                {
                    AllTopics.Add(topic);
                }
            }
            Save();
        }
    }
}

