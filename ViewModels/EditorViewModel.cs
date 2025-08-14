using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using RiskierWas.Models;
using RiskierWas.Services;

namespace RiskierWas.ViewModels
{
    public class EditorViewModel : INotifyPropertyChanged
    {
        private readonly MainViewModel _main;
        private readonly Random _rng = new();

        public ObservableCollection<Question> Questions => _main.Questions;

        private Question? _selectedQuestion;
        public Question? SelectedQuestion
        {
            get => _selectedQuestion;
            set
            {
                if (_selectedQuestion == value) return;
                _selectedQuestion = value;
                OnPropertyChanged(nameof(SelectedQuestion));
                RemoveQuestionCommand?.RaiseCanExecuteChanged();
                AddAnswerCommand?.RaiseCanExecuteChanged();
                RemoveAnswerCommand?.RaiseCanExecuteChanged();
            }
        }

        public RelayCommand BackCommand { get; }
        public RelayCommand SaveCommand { get; }              // <- NEU (statt ExportJsonCommand)
        public RelayCommand AddQuestionCommand { get; }
        public RelayCommand RemoveQuestionCommand { get; }
        public RelayCommand AddAnswerCommand { get; }
        public RelayCommand RemoveAnswerCommand { get; }
        public RelayCommand DeselectAllCommand { get; }
        public RelayCommand SelectRandom20Command { get; }

        public EditorViewModel(MainViewModel main)
        {
            _main = main;

            BackCommand = new RelayCommand(_ => _main.NavigateToStart());
            SaveCommand = new RelayCommand(_ => SaveToDefault());    // <- hier speichern
            AddQuestionCommand = new RelayCommand(_ => AddQuestion());
            RemoveQuestionCommand = new RelayCommand(_ => RemoveQuestion(), _ => SelectedQuestion != null);

            AddAnswerCommand = new RelayCommand(_ => AddAnswer(),
                                        _ => SelectedQuestion != null && SelectedQuestion.Answers.Count < 16);
            RemoveAnswerCommand = new RelayCommand(a => RemoveAnswer(a as Answer),
                                        a => SelectedQuestion != null && a is Answer);

            DeselectAllCommand = new RelayCommand(_ => DeselectAll());
            SelectRandom20Command = new RelayCommand(_ => SelectRandom20());

            SelectedQuestion = Questions.FirstOrDefault();
        }

        // Speichert nach <App>\Data\questions.json
        private void SaveToDefault()
        {
            try
            {
                var baseDir = AppContext.BaseDirectory; // Ordner der exe
                var dataDir = Path.Combine(baseDir, "Data");
                Directory.CreateDirectory(dataDir);
                var path = Path.Combine(dataDir, "questions.json");

                QuestionService.SaveToJson(path, Questions);
                MessageBox.Show($"Gespeichert: {path}", "Speichern", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Speichern: {ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddQuestion()
        {
            var q = new Question { Text = "Neue Frage", Selected = true };
            Questions.Add(q);
            SelectedQuestion = q;
        }

        private void RemoveQuestion()
        {
            if (SelectedQuestion == null) return;
            var idx = Questions.IndexOf(SelectedQuestion);
            Questions.Remove(SelectedQuestion);
            SelectedQuestion = Questions.Count == 0 ? null : Questions[Math.Min(idx, Questions.Count - 1)];
        }

        private void AddAnswer()
        {
            if (SelectedQuestion == null) return;
            SelectedQuestion.Answers.Add(new Answer { Text = "Antwort", Correct = false });
            AddAnswerCommand.RaiseCanExecuteChanged();
            OnPropertyChanged(nameof(SelectedQuestion));
        }

        private void RemoveAnswer(Answer? a)
        {
            if (SelectedQuestion == null || a == null) return;
            SelectedQuestion.Answers.Remove(a);
            AddAnswerCommand.RaiseCanExecuteChanged();
            OnPropertyChanged(nameof(SelectedQuestion));
        }

        private void DeselectAll()
        {
            foreach (var q in Questions) q.Selected = false;
            OnPropertyChanged(nameof(Questions));
        }

        private void SelectRandom20()
        {
            if (Questions.Count == 0) return;

            foreach (var q in Questions) q.Selected = false;

            int take = Math.Min(20, Questions.Count);
            var indices = Enumerable.Range(0, Questions.Count).ToArray();
            for (int i = indices.Length - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                (indices[i], indices[j]) = (indices[j], indices[i]);
            }
            for (int k = 0; k < take; k++)
                Questions[indices[k]].Selected = true;

            OnPropertyChanged(nameof(Questions));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
