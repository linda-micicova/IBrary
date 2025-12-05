using System;
using System.Collections.Generic;
using IBrary.Models;
using IBrary.Managers;

namespace IBrary.Models
{
    public class CardVersion
    {
        public string Question { get; set; }
        public string Answer { get; set; }
        public string Editor {  get; set; } 
        public bool Deleted { get; set; }
        public string QuestionImagePath { get; set; } = null;
        public string AnswerImagePath { get; set; } = null;
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

        //Create version with images
        public CardVersion(string Question, string Answer, string Editor, string QuestionImagePath, string AnswerImagePath)
        {
            this.Question = Question;
            this.Answer = Answer;
            this.Editor = Editor;
            this.QuestionImagePath = QuestionImagePath;
            this.AnswerImagePath = AnswerImagePath;
        }
        //Flag flashcard as deleted
        public CardVersion(string Editor, bool Deleted)
        {
            this.Editor = Editor;
            this.Deleted = Deleted;
        }
        
    }
}

