using IBrary.Managers;
using IBrary.Models;
using IBrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
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
                return App.Flashcards.Load()
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
        public DateTime? LatestSeen
        {
            get
            {
                var flashcards = FlashcardObjects.Where(f => f.LastSeen.HasValue).ToList();
                return flashcards.Count == 0 ? null : flashcards.Max(f => f.LastSeen);
            }
        }
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
        public Subject(string name) { 
            this.SubjectName = name;
            this.SubjectId = name.Replace(" ", "_").ToLowerInvariant(); // Simple ID generation
            this.Topics = new List<string>();
            this.Flashcards = new List<string>();
        }
        public Subject(string name, string id, List<string> topics, List<string> flashcards)
        {
            this.SubjectName = name;
            this.SubjectId = id; 
            this.Topics = topics ?? new List<string>();
            this.Flashcards = flashcards ?? new List<string>();
        }
        public Topic AddTopic(Topic topic)
        {
            Topics.Add(topic.TopicId);
            return topic;
        }
        /*public Topic FindOrAddTopic(string name)
        {
            foreach (string topicId in Topics)
            {
                Topic topic = TopicManager.Instance.GetTopicById(topicId);
                if (topic != null && topic.TopicName == name)
                {
                    return topic;
                }
            }

            // Not found: create new topic and add it
            Topic newTopic = new Topic(name);
            AddTopic(newTopic);
            return newTopic;
        }*/
        public Flashcard AddFlashcard(Flashcard flashcard)
        {
            Flashcards.Add(flashcard.FlashcardId);
            return flashcard;
        }
    }
}