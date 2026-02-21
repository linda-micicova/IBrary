using IBrary.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IBrary.Models;

namespace IBrary
{
    public static class App
    {
        public static FlashcardManager Flashcards { get; } = new FlashcardManager();
        public static TopicManager Topics { get; } = new TopicManager();
        public static SubjectManager Subjects { get; } = new SubjectManager();
        public static SettingsManager Settings { get; } = new SettingsManager();
    }
}
