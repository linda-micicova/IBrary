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
        public string FlashcardId { get; set; }
        public Level Level { get; set; }
        public List<CardVersion> Versions { get; set; } = new List<CardVersion>();
        public List<string> Topics { get; set; } = new List<string>(); // List of Topic IDs
        public DateTime? FirstSeen { get; set; }
        public DateTime? LastSeen { get; set; }
        public int Seen { get; set; } = 0;
        public int Errors { get; set; } = 0;
        public bool important { get; set; } = false;

        [JsonIgnore]
        public double ErrorRate => Seen == 0 ? 0 : (double)Errors / Seen;
        [JsonIgnore]
        public double Accuracy => 1 - ErrorRate;

        [JsonIgnore]
        public double Priority => CalculatePriority();

        public double CalculatePriority()
        {
            double flashcardPriority = ErrorRate * SettingsManager.CurrentSettings.ErrorRateWeight
                + ((FirstSeen.HasValue) ? (DateTime.Today - FirstSeen.Value).TotalSeconds / 1000 : 0) /** SettingsManager.CurrentSettings.FirstRevisedWeight*/
                + ((LastSeen.HasValue) ? (DateTime.Today - LastSeen.Value).TotalSeconds / 1000 : 0) * SettingsManager.CurrentSettings.TimeFactorWeight;
            double topicPriority = 0;
            foreach (var topicId in Topics)
            {
                var topic = TopicManager.Load().FirstOrDefault(t => t.TopicId == topicId);
                if (topic != null)
                {
                    topicPriority += topic.AverageErrorRate * SettingsManager.CurrentSettings.ErrorRateWeight
                        + ((topic.LatestSeen.HasValue) ? (DateTime.Today - topic.LatestSeen.Value).TotalSeconds / 1000 : 0) * SettingsManager.CurrentSettings.TimeFactorWeight;
                }
            }
            return flashcardPriority + topicPriority;
        }
        public Flashcard() { }
        public Flashcard(string question, string answer)
        {
            this.FlashcardId = GenerateId();
            this.Versions = new List<CardVersion> { new CardVersion(question, answer, SettingsManager.CurrentSettings.Username) };
        }
        public Flashcard(string question, string answer, Level level, List<string> topicIDs)
        {
            this.FlashcardId = GenerateId();
            this.Versions = new List<CardVersion> { new CardVersion(question, answer, SettingsManager.CurrentSettings.Username) };
            this.Level = level;
            this.Topics = topicIDs;
        }
        public Flashcard(string question, string answer, Level level, List<string> topicIDs, string questionImagePath, string answerImagePath)
        {
            this.FlashcardId = GenerateId();
            this.Versions = new List<CardVersion> { new CardVersion(question, answer, SettingsManager.CurrentSettings.Username, questionImagePath, answerImagePath) };
            this.Level = level;
            this.Topics = topicIDs;
        }
        public void ModifyFlashcard(string question, string answer)
        {
            Versions.Add(new CardVersion(question, answer, SettingsManager.CurrentSettings.Username));
        }
        public void DeleteFlashcard()
        {
            Versions.Add(new CardVersion(SettingsManager.CurrentSettings.Username, true));
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
            } while (FlashcardManager.AllFlashcards.Any(f => f.FlashcardId == id));
            return id;
        }
    }
}
