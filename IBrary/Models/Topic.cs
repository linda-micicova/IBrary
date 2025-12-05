using IBrary.Managers;
using IBrary.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;


namespace IBrary.Models
{
    public class Topic
    {
        public string TopicName { get; set; }
        public string TopicId { get; set; } 
        public Level Level { get; set; }

        [JsonIgnore]
        public List<Flashcard> FlashcardIds
        {
            get
            {
                var flashcardsInTopic = FlashcardManager.Load().Where(f => f.Topics.Contains(TopicId)).ToList();
                return flashcardsInTopic;
            }
        }
        [JsonIgnore]
        public double AverageErrorRate
        {
            get
            {
                var flashcards = FlashcardIds;
                if (flashcards.Count == 0)
                    return 0;

                return flashcards.Average(f => f.ErrorRate);
            }
        }
        [JsonIgnore]
        public double Accuracy
        {
            get
            {
                return 1-AverageErrorRate;
            }
        }

        [JsonIgnore]
        public DateTime? LatestSeen
        {
            get
            {
                var flashcards = FlashcardIds.Where(f => f.LastSeen.HasValue).ToList();
                if (flashcards.Count == 0)
                    return null;

                return flashcards.Max(f => f.LastSeen);
            }
        }
        [JsonIgnore]
        public int TotalSeenCount
        {
            get
            {
                return FlashcardIds.Sum(f => f.Seen);
            }
        }

        public Topic() { }
        public Topic(string TopicName, Level level)
        {
            this.TopicName = TopicName;
            this.TopicId = TopicName.Replace(" ", "_").ToLowerInvariant(); // Simple ID generation
            this.Level = level;
        }
    }
}
