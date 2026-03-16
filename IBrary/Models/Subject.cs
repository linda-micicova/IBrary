using IBrary.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace IBrary.Models
{
    public class Subject
    {
        public string SubjectName {  get; set; }
        public string SubjectId { get; set; }
        public List<string> Topics { get; set; } // List of Topic IDs
        public List<string> Flashcards { get; set; } // List of Flashcard IDs

        [JsonIgnore]
        public List<Flashcard> FlashcardObjects
        {
            get
            {
                return App.Flashcards.AllFlashcards
                    .Where(f => Flashcards.Contains(f.FlashcardId))
                    .ToList();
            }
        }
        [JsonIgnore]
        public double AverageErrorRate
        {
            get
            {
                var flashcards = FlashcardObjects;
                return flashcards.Count == 0 ? 0 : flashcards.Average(f => f.ErrorRate);
            }
        }
        [JsonIgnore]
        public double Accuracy { get { return 1-AverageErrorRate; } }
        [JsonIgnore]
        public int TotalSeenCount
        {
            get
            {
                return FlashcardObjects.Sum(f => f.Seen);
            }
        }
        public Subject()
        {
            Topics = new List<string>();
            Flashcards = new List<string>();
        }
    }
}