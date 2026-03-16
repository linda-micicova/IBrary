using System;
using System.IO;
using System.Collections.Generic;
using IBrary.Models;
using IBrary.Managers;

namespace IBrary.Models
{
    public class CardVersion
    {
        public string Question { get; set; }
        public string Answer { get; set; }
        public string Editor { get; set; }
        public bool Deleted { get; set; }
        public string QuestionImagePath { get; set; } = null;
        public string AnswerImagePath { get; set; } = null;
        public string QuestionImageBase64 { get; set; } = null;
        public string AnswerImageBase64 { get; set; } = null;
        public DateTime Timestamp { get; set; } = DateTime.Now;

        // Default constructor for serialization
        public CardVersion() { }

        //Create version without images
        public CardVersion(string Question, string Answer, string Editor)
        {
            this.Question = Question;
            this.Answer = Answer;
            this.Editor = Editor;
        }

        //old version
/*public CardVersion(string Question, string Answer, string Editor, string QuestionImagePath, string AnswerImagePath)
{
    this.Question = Question;
    this.Answer = Answer;
    this.Editor = Editor;
    this.QuestionImagePath = QuestionImagePath;
    this.AnswerImagePath = AnswerImagePath;
}
//Create version with images
public CardVersion(string Question, string Answer, string Editor, string QuestionImagePath, string AnswerImagePath)
{
    this.Question = Question;
    this.Answer = Answer;
    this.Editor = Editor;
    this.QuestionImagePath = QuestionImagePath;
    this.AnswerImagePath = AnswerImagePath;

    // Convert to Base64 for portability
    if (QuestionImagePath != null && File.Exists(QuestionImagePath))
        this.QuestionImageBase64 = Convert.ToBase64String(File.ReadAllBytes(QuestionImagePath));
    if (AnswerImagePath != null && File.Exists(AnswerImagePath))
        this.AnswerImageBase64 = Convert.ToBase64String(File.ReadAllBytes(AnswerImagePath));
}*/
        public CardVersion(string Question, string Answer, string Editor,
    string QuestionImagePath, string AnswerImagePath,
    string QuestionImageBase64 = null, string AnswerImageBase64 = null)
        {
            this.Question = Question;
            this.Answer = Answer;
            this.Editor = Editor;
            this.QuestionImagePath = QuestionImagePath;
            this.AnswerImagePath = AnswerImagePath;
            this.QuestionImageBase64 = QuestionImageBase64;
            this.AnswerImageBase64 = AnswerImageBase64;
        }
        //Flag flashcard as deleted
        public CardVersion(string Editor, bool Deleted)
{
    this.Editor = Editor;
    this.Deleted = Deleted;
}

}
}