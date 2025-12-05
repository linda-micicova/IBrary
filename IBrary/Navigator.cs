// Create this as a new file: Navigator.cs
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using IBrary.UserControls;
using IBrary.Models;

namespace IBrary
{
    public static class Navigator
    {
        private static Panel _contentPanel;
        private static Action _onThemeChanged; // For theme updates

        public static void Initialize(Panel contentPanel, Action onThemeChanged = null)
        {
            _contentPanel = contentPanel;
            _onThemeChanged = onThemeChanged;
        }

        // Generic navigation method
        public static void GoTo<T>() where T : UserControl, new()
        {
            GoTo(new T());
        }

        // Navigation with existing control instance
        public static void GoTo(UserControl control)
        {
            // Clean up existing controls
            foreach (Control existingControl in _contentPanel.Controls)
            {
                existingControl.Dispose();
            }
            _contentPanel.Controls.Clear();

            control.Dock = DockStyle.Fill;
            _contentPanel.Controls.Add(control);
        }

        // Specific navigation methods (easier to use)
        public static void GoToFlashcards() => GoTo<FlashcardUserControl>();
        public static void GoToSettings() => GoTo<SettingsUserControl>();
        public static void GoToLogin() => GoTo<LoginUserControl>();
        public static void GoToAddContent() => GoTo<AddContentUserControl>();
        public static void GoToDashboard() => GoTo<DashboardUserControl>();

        public static void GoToUserView(string username = null)
        {
            GoTo(new UserViewUserControl(username));
        }

        public static void EditFlashcard(Flashcard flashcard, CardVersion version)
        {
            GoTo(new AddOrEditFlashcardUserControl(flashcard, version));
        }

        public static void GoToTopicsDashboard(Subject subject)
        {
            GoTo(new TopicDashboardUserControl(subject));
        }

        public static void GoToMySubjects() => GoTo<MySubjectsUserControl>();
        public static void GoToPriorityCalculations() => GoTo<PriorityCalculationsUserControl>();
        public static void GoToBlockedUsers() => GoTo<BlockedUsersUserControl>();
        public static void GoToImportFromQuizlet() => GoTo<ImportFromQuizletUserControl>();

        public static void GoToManageNetworkSync() => GoTo<ManageNetworkSyncUserControl>();


        // Updated method with parameters for QuizletFlashcardsView
        public static void GoToQuizletFlashcardsView(List<Flashcard> flashcards = null, Subject subject = null)
        {
            var control = new QuizletFlashcardsViewUserControl();

            // Initialize with parameters if provided
            if (flashcards != null && subject != null)
            {
                control.Initialize(flashcards, subject);
            }

            GoTo(control);
        }

        // Special method for theme changes (refreshes current screen)
        public static void RefreshCurrentScreen()
        {
            _onThemeChanged?.Invoke();
        }
    }
}