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
        public List<Flashcard> ListOfFlashcards
        {
            get
            {
                return App.Flashcards.AllFlashcards.Where(f => f.Topics.Contains(TopicId)).ToList();
            }
        }
        [JsonIgnore]
        public double AverageErrorRate
        {
            get
            {
                var flashcards = ListOfFlashcards;
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
                return 1 - AverageErrorRate;
            }
        }
        [JsonIgnore]
        public DateTime? LatestSeen
        {
            get
            {
                var flashcards = ListOfFlashcards.Where(f => f.LastSeen.HasValue).ToList();
                if (flashcards.Count == 0)
                    return null;
                return flashcards.Max(f => f.LastSeen);
            }
        }
        public Topic() { }
    }
}
