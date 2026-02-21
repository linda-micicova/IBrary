using IBrary.Managers;
using IBrary.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Windows.Forms;


namespace IBrary.Models
{
    public enum Level
    {
        HL,
        SL
    }
    //To be able to import Quizlet flashcards from Quizlet Exporter
    public class QuizletFlashcard
    {
        public string term { get; set; }
        public string definition { get; set; }
        public QuizletFlashcard() { }
    }
    public class Flashcard
    {
        public string FlashcardId { get; set; } // Unique identifier
        public Level Level { get; set; } // HL or SL
        public List<CardVersion> Versions { get; set; } = new List<CardVersion>(); // History of edits
        public List<string> Topics { get; set; } = new List<string>(); // List of Topic IDs
        public DateTime? FirstSeen { get; set; } // First time studied, null if never studied
        public DateTime? LastSeen { get; set; } // Last time studied, null if never studied
        public int Seen { get; set; } = 0; // Total times studied
        public int Errors { get; set; } = 0; // Total times answered incorrectly
        public bool Important { get; set; } = false; // Star rating flag
        [JsonIgnore] // Not stored in JSON, derived from Seen and Errors
        public double ErrorRate => Seen == 0 ? 0 : (double) Errors / Seen;
        [JsonIgnore] // Not stored in JSON, depends on current date and settings
        public double Priority => CalculatePriority();

        // Calculate priority order for flashcard review
        public double CalculatePriority()
        {
            double errorRate = ErrorRate; // 0 = perfect, 1 = always wrong

            // Time factor: normalize logarithmic time to 0-1 using max expected days 
            double maxDays = Math.Log(1 + 600); // normalization constant, 600 days = approx. Sep Y1 - May Y2 = IB duration
            double sinceFirstSeen = FirstSeen.HasValue ?
                Math.Log(1 + (DateTime.Today - FirstSeen.Value).TotalDays) / maxDays : 0;
            double sinceLastSeen = LastSeen.HasValue ?
                Math.Log(1 + (DateTime.Today - LastSeen.Value).TotalDays) / maxDays : 0;

            // Old cards studied recently get lower priority
            double timeFactor = sinceLastSeen + (sinceFirstSeen * (sinceLastSeen / (sinceLastSeen + 1)));
            timeFactor = Math.Min(timeFactor, 1); // Clamp to 1

            // Star rating, 1 - important, 0 - not important
            double starFactor = Important ? 1.0 : 0.0;

            // Overall priority, weight set in settings
            double flashcardPriority =
                errorRate * App.Settings.CurrentSettings.ErrorRateWeight
                + timeFactor * App.Settings.CurrentSettings.TimeFactorWeight
                + starFactor * App.Settings.CurrentSettings.ImportantTagWeight;

            double topicPriority = 0;
            var allTopics = App.Topics.AllTopics;
            int validTopicCount = 0;
            foreach (var topicId in Topics)
            {
                var topic = allTopics.FirstOrDefault(t => t.TopicId == topicId); // Find topic by ID
                if (topic != null)
                {
                    // Calculate topic priority contribution
                    double topicSinceLastSeen = topic.LatestSeen.HasValue ?
                        Math.Min(Math.Log(1 + (DateTime.Today - topic.LatestSeen.Value).TotalDays) / maxDays, 1) : 0;

                    topicPriority += topic.AverageErrorRate * App.Settings.CurrentSettings.ErrorRateWeight
                        + topicSinceLastSeen * App.Settings.CurrentSettings.TimeFactorWeight;
                    validTopicCount++;
                }
            }
            if (validTopicCount > 0)
                topicPriority /= validTopicCount;
            return flashcardPriority + topicPriority;
        }
        public Flashcard() { }
        public Flashcard(string question, string answer)
        {
            this.FlashcardId = GenerateId();
            this.Versions = new List<CardVersion> { new CardVersion(question, answer, App.Settings.CurrentSettings.Username) };
        }
        public Flashcard(string question, string answer, Level level, List<string> topicIDs)
        {
            this.FlashcardId = GenerateId();
            this.Versions = new List<CardVersion> { new CardVersion(question, answer, App.Settings.CurrentSettings.Username) };
            this.Level = level;
            this.Topics = topicIDs;
        }
        public Flashcard(string question, string answer, Level level, List<string> topicIDs, string questionImagePath, string answerImagePath)
        {
            this.FlashcardId = GenerateId();
            this.Versions = new List<CardVersion> { new CardVersion(question, answer, App.Settings.CurrentSettings.Username, questionImagePath, answerImagePath) };
            this.Level = level;
            this.Topics = topicIDs;
        }
        public void ModifyFlashcard(string question, string answer)
        {
            Versions.Add(new CardVersion(question, answer, App.Settings.CurrentSettings.Username));
        }
        public void DeleteFlashcard()
        {
            Versions.Add(new CardVersion(App.Settings.CurrentSettings.Username, true));
        }

        public void RegisterStudyResult(DateTime date, bool incorrect)
        {
            if (FirstSeen == null)
                FirstSeen = date;

            LastSeen = date;
            Seen++;

            if (incorrect)
                Errors++;
        }
               
        public CardVersion GetDisplayVersion(List<string> blockedUsers)
        {
            return Versions.LastOrDefault(v => !blockedUsers.Contains(v.Editor) && !v.Deleted);
        }

        private string GenerateId()
        {
            string id;
            do
            {
                id = Guid.NewGuid().ToString();
            } while (App.Flashcards.AllFlashcards.Any(f => f.FlashcardId == id));
            return id;
        }
    }
}
