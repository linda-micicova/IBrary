using Managers;
using IBrary.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IBrary.Managers
{
    public static class FlashcardManager
    {
        //json file path
        /*public static readonly string flashcardsPath = Path.Combine(
        Application.StartupPath,
        "Data",
        "flashcards.json");*/
        public static readonly string flashcardsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "IBrary",
        "flashcards.json");

        //list of all currently stored flashcards
        public static List<Flashcard> AllFlashcards { get; private set; } = new List<Flashcard>();

        // LOAD FLASHCARDS FROM MEMORY
        public static List<Flashcard> Load()
        {
            AllFlashcards = LoadFlashcardsFromJson(flashcardsPath);
            return AllFlashcards;
        }

        public static List<Flashcard> LoadFlashcardsFromJson(string path)
        {
            if (!File.Exists(path))
                return new List<Flashcard>();

            try
            {
                string json = File.ReadAllText(path);

                var options = new JsonSerializerOptions
                {
                    Converters = { new JsonStringEnumConverter() }
                };

                return JsonSerializer.Deserialize<List<Flashcard>>(json, options)
                    ?? new List<Flashcard>();
            }
            catch (Exception ex)
            {
                return new List<Flashcard>();
            }
        }
        public static void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(flashcardsPath));

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Converters = { new JsonStringEnumConverter() }
                };

                File.WriteAllText(flashcardsPath, JsonSerializer.Serialize(AllFlashcards, options));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving flashcards: {ex.Message}");
            }
        }

        // Save input flashcards
        public static void SaveFlashcards(List<Flashcard> flashcards)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(flashcardsPath));

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Converters = { new JsonStringEnumConverter() }
                };

                File.WriteAllText(flashcardsPath, JsonSerializer.Serialize(flashcards, options));
                AllFlashcards = flashcards;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving flashcards: {ex.Message}");
            }
        }

        //MODIFY FLASHCARDS METHODS

        // Add a new flashcard

        public static void AddFlashcard(Flashcard newCard)
        {
            if (newCard == null) throw new ArgumentNullException(nameof(newCard));

            var flashcards = Load();
            flashcards.Add(newCard);
            SaveFlashcards(flashcards);
        }

        // Edit an existing flashcard by adding a new version
        public static void EditFlashcard(string flashcardId, string question, string answer, string editor)
        {
            var flashcards = Load();

            int index = flashcards.FindIndex(f => f.FlashcardId == flashcardId);
            if (index != -1)
            {
                // Add version entry
                flashcards[index].Versions.Add(new CardVersion(question, answer, editor));

                // Save changes
                SaveFlashcards(flashcards);
            }
        }

        public static void EditFlashcard(string flashcardId, CardVersion version)
        {
            var flashcards = Load();

            int index = flashcards.FindIndex(f => f.FlashcardId == flashcardId);
            if (index != -1)
            {
                // Add version entry
                flashcards[index].Versions.Add(version);

                // Save changes
                SaveFlashcards(flashcards);
            }
        }

        // Delete a flashcard (flag as deleted or remove completely based on user role)
        public static void DeleteFlashcard(string flashcardId, string editor)
        {
            var flashcards = Load();

            int index = flashcards.FindIndex(f => f.FlashcardId == flashcardId);
            if (index != -1)
            {
                if (UserManager.isAdmin())
                {
                    // Remove flashcard completely
                    flashcards.RemoveAt(index);
                }
                else
                {
                    // Flag flashcard as deleted
                    flashcards[index].Versions.Add(new CardVersion(editor, true));
                }

                // Save changes
                SaveFlashcards(flashcards);
            }
        }

        // Get flashcards ordered by priority (highest priority first)
        public static List<Flashcard> GetFlashcardsOrderedByPriority(List<Flashcard> flashcardsToBeOrdered)
        {
            return flashcardsToBeOrdered.OrderByDescending(f => f.Priority).ToList();
        }

        // IMPORT AND MERGE FLASHCARDS METHODS

        // Uses current user's username to decide whether to preserve stats or not
        public static void MergeFlashcards(List<Flashcard> flashcardsToMerge, string username)
        {
            MergeFlashcards(flashcardsToMerge, username == SettingsManager.CurrentSettings.Username);
        }

        // Merges with currently stored flashcards
        public static void MergeFlashcards(List<Flashcard> flashcardsToMerge, bool preserveStats = false)
        {
            var allFlashcards = Load();

            if (!preserveStats)
            {
                flashcardsToMerge = ResetAllFlashcardStats(flashcardsToMerge);
            }

            foreach (var flashcard in flashcardsToMerge)
            {
                var existingCard = allFlashcards.FirstOrDefault(f => f.FlashcardId == flashcard.FlashcardId);

                if (existingCard != null)
                {
                    foreach (var newVersion in flashcard.Versions)
                    {
                        bool versionExists = existingCard.Versions.Any(v =>
                            v.Timestamp == newVersion.Timestamp &&
                            v.Editor == newVersion.Editor);

                        if (!versionExists)
                        {
                            existingCard.Versions.Add(newVersion);
                        }
                    }
                    // New flashcard seems to be more up to date than existing one, so we update the stats
                    // If !preserveStats, new one should be set to 0 already
                    if (flashcard.LastSeen > existingCard.LastSeen)
                    {
                        existingCard.Seen = flashcard.Seen;
                        existingCard.Errors = flashcard.Errors;
                        existingCard.FirstSeen = flashcard.FirstSeen;
                        existingCard.LastSeen = flashcard.LastSeen;
                    }
                }
                else
                {
                    allFlashcards.Add(flashcard);
                }
            }

            SaveFlashcards(allFlashcards);
        }

        // Helper method to import already created Flashcard objects
        public static void ImportFlashcards(List<Flashcard> flashcardsToImport)
        {
            var allFlashcards = Load();
            allFlashcards.AddRange(flashcardsToImport);
            SaveFlashcards(allFlashcards);
        }

        // Resets progress stats for all currently stored flashcards
        public static void ResetAllFlashcardStats()
        {
            foreach (var flashcard in AllFlashcards)
            {
                flashcard.Seen = 0;
                flashcard.Errors = 0;
                flashcard.FirstSeen = null;
                flashcard.LastSeen = null;
            }
            SaveFlashcards(AllFlashcards);
        }

        // Resets progress stats for input flashcards
        public static List<Flashcard> ResetAllFlashcardStats(List<Flashcard> flashcards)
        {
            foreach (var flashcard in flashcards)
            {
                flashcard.Seen = 0;
                flashcard.Errors = 0;
                flashcard.FirstSeen = null;
                flashcard.LastSeen = null;
                flashcard.important = false;
            }
            return flashcards;
        }

        //QUIZLET FLASHCARDS METHODS

        //Load quizlet flashcards from json file
        private static List<QuizletFlashcard> LoadQuizletFlashcardsFromJson(string path)
        {
            List<QuizletFlashcard> flashcards;
            try
            {
                if (!File.Exists(path))
                    flashcards = new List<QuizletFlashcard>();

                string json = File.ReadAllText(path);

                flashcards = JsonSerializer.Deserialize<List<QuizletFlashcard>>(json)
                    ?? new List<QuizletFlashcard>();
            }
            catch
            {
                flashcards = new List<QuizletFlashcard>();
            }
            return flashcards;
        }

        //Import quizlet flashcards into the app (merge them with current ones)
        public static bool ImportFromQuizlet(string path)
        {
            bool importSuccessful = false;
            List<QuizletFlashcard> flashcardsToImport = LoadQuizletFlashcardsFromJson(path);
            var allFlashcards = Load();
            foreach (QuizletFlashcard flashcard in flashcardsToImport)
            {
                if (flashcard.term != null || flashcard.definition != null)
                {
                    allFlashcards.Add(new Flashcard(flashcard.term, flashcard.definition));
                    importSuccessful = true;
                }
            }

            SaveFlashcards(allFlashcards);
            return importSuccessful;
        }

        // Only load flashcards without saving them
        public static List<Flashcard> LoadFromQuizletFile(string path)
        {
            var flashcards = new List<Flashcard>();
            List<QuizletFlashcard> flashcardsToImport = LoadQuizletFlashcardsFromJson(path);

            foreach (QuizletFlashcard flashcard in flashcardsToImport)
            {
                if (flashcard.term != null || flashcard.definition != null)
                {
                    flashcards.Add(new Flashcard(flashcard.term, flashcard.definition));
                }
            }

            return flashcards;
        }
    }
}