using Managers;
using IBrary.App_settings;
using IBrary.Managers;
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
    public static class SubjectManager
    {
        public static readonly string subjectsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "IBrary",
        "subjects.json");

        // list of all currently stored subjects
        public static List<Subject> AllSubjects { get; private set; } = new List<Subject>();

        // LOAD SUBJECTS FROM MEMORY
        public static List<Subject> Load()
        {

            AllSubjects = LoadSubjectsFromJson(subjectsPath);
            return AllSubjects;
        }
        public static List<Subject> LoadSubjectsFromJson(string path)
        {
            if (!File.Exists(path))
                return new List<Subject>();

            try
            {
                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<List<Subject>>(json) ?? new List<Subject>();
            }
            catch (Exception ex)
            {
                return new List<Subject>();
            }

        }

        // Save any modifications of currently loaded subjects
        public static void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(subjectsPath));
                File.WriteAllText(subjectsPath, JsonSerializer.Serialize(AllSubjects, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving settings: {ex.Message}");
            }
        }

        // MODIFY SUBJECTS

        // Link subject to new flashcard
        public static void AddFlashcardToSubject(Flashcard flashcard, Subject subject)
        {
            if (AllSubjects.Any(s => s.SubjectId == subject.SubjectId))
            {
                var existingSubject = AllSubjects.First(s => s.SubjectId == subject.SubjectId);
                if (!existingSubject.Flashcards.Contains(flashcard.FlashcardId))
                {
                    existingSubject.Flashcards.Add(flashcard.FlashcardId);
                    Save();
                }
            }
        }

        // Add new subject
        public static void AddSubject(Subject subject)
        {
            if (!AllSubjects.Any(s => s.SubjectId == subject.SubjectId))
            {
                AllSubjects.Add(subject);
                Save();
            }
        }

        // Delete subject
        public static void RemoveSubject(Subject subject)
        {
            if (UserManager.isAdmin() && AllSubjects.Any(s => s.SubjectId == subject.SubjectId))
            {
                AllSubjects.Remove(subject);
                Save();
            }

        }

        // Merge subjects 
        public static void MergeSubjects(List<Subject> subjects)
        {
            var existingSubjects = Load();
            foreach (var subject in subjects)
            {
                if (!existingSubjects.Any(s => s.SubjectId == subject.SubjectId))
                {
                    AllSubjects.Add(subject);
                }

                // Include flashcard and topic IDs from both current and input lists
                else
                {
                    var existingSubject = existingSubjects.First(s => s.SubjectId == subject.SubjectId);
                    foreach (var topic in subject.Topics)
                    {
                        if (!existingSubject.Topics.Contains(topic))
                        {
                            existingSubject.Topics.Add(topic);
                        }
                    }
                    foreach (var flashcard in subject.Flashcards)
                    {
                        if (!existingSubject.Flashcards.Contains(flashcard))
                        {
                            existingSubject.Flashcards.Add(flashcard);
                        }
                    }
                }
            }
            Save();
        }
    }

}

