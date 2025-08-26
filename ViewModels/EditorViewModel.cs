using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using Microsoft.Win32;
using RiskierWas.Models;
using RiskierWas.Services;

namespace RiskierWas.ViewModels
{
    public class EditorViewModel : INotifyPropertyChanged
    {
        private readonly MainViewModel _main;
        private readonly Random _rng = new();

        public ObservableCollection<Question> Questions => _main.Questions;
        private readonly ICollectionView _filteredQuestions;
        public ICollectionView FilteredQuestions => _filteredQuestions;

        private bool _filterSelectedOnly;
        public bool FilterSelectedOnly
        {
            get => _filterSelectedOnly;
            set
            {
                if (_filterSelectedOnly == value) return;
                _filterSelectedOnly = value;
                OnPropertyChanged(nameof(FilterSelectedOnly));
                _filteredQuestions.Refresh();
                MoveQuestionUpCommand.RaiseCanExecuteChanged();
                MoveQuestionDownCommand.RaiseCanExecuteChanged();
            }
        }

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
        public RelayCommand SaveCommand { get; }          // Speichern (fest)
        public RelayCommand SaveAsCommand { get; }        // Speichern unter…
        public RelayCommand AddQuestionCommand { get; }
        public RelayCommand RemoveQuestionCommand { get; }          // löscht SelectedQuestion
        public RelayCommand RemoveQuestionItemCommand { get; }      // löscht per Parameter (Listeneintrag)
        public RelayCommand MoveQuestionUpCommand { get; }
        public RelayCommand MoveQuestionDownCommand { get; }
        public RelayCommand AddAnswerCommand { get; }
        public RelayCommand RemoveAnswerCommand { get; }
        public RelayCommand DeselectAllCommand { get; }
        public RelayCommand SelectRandom20Command { get; }

        public EditorViewModel(MainViewModel main)
        {
            _main = main;

            _filteredQuestions = CollectionViewSource.GetDefaultView(Questions);
            _filteredQuestions.Filter = o => !FilterSelectedOnly || ((Question)o).Selected;

            foreach (var q in Questions)
                q.PropertyChanged += Question_PropertyChanged;
            Questions.CollectionChanged += Questions_CollectionChanged;

            BackCommand = new RelayCommand(_ => _main.NavigateToStart());
            SaveCommand = new RelayCommand(_ => SaveToDefault());
            SaveAsCommand = new RelayCommand(_ => SaveAs());
            AddQuestionCommand = new RelayCommand(_ => AddQuestion());

            RemoveQuestionCommand = new RelayCommand(_ => RemoveQuestion(), _ => SelectedQuestion != null);
            RemoveQuestionItemCommand = new RelayCommand(q => RemoveQuestion(q as Question), q => q is Question);
            MoveQuestionUpCommand = new RelayCommand(q => MoveQuestion(q as Question, -1),
                                             q => CanMoveQuestion(q as Question, -1));
            MoveQuestionDownCommand = new RelayCommand(q => MoveQuestion(q as Question, 1),
                                             q => CanMoveQuestion(q as Question, 1));

            AddAnswerCommand = new RelayCommand(_ => AddAnswer(),
                                        _ => SelectedQuestion != null && SelectedQuestion.Answers.Count < 16);
            RemoveAnswerCommand = new RelayCommand(a => RemoveAnswer(a as Answer),
                                        a => SelectedQuestion != null && a is Answer);

            DeselectAllCommand = new RelayCommand(_ => DeselectAll());
            SelectRandom20Command = new RelayCommand(_ => SelectRandom20());

            // Nach der Command-Initialisierung auswählen
            SelectedQuestion = Questions.FirstOrDefault();
        }

        // ---------- Speichern ----------

        private string GetDefaultPath()
        {
            var baseDir = AppContext.BaseDirectory;
            var dataDir = Path.Combine(baseDir, "Data");
            Directory.CreateDirectory(dataDir);
            return Path.Combine(dataDir, "questions.json");
        }

        private void SaveToDefault()
        {
            var path = GetDefaultPath();
            SaveTo(path, showOkInfo: true);
        }

        private void SaveAs()
        {
            var sfd = new SaveFileDialog
            {
                Filter = "JSON (*.json)|*.json",
                Title = "Speichern unter…",
                FileName = "questions.json",
                InitialDirectory = Path.GetDirectoryName(GetDefaultPath())
            };
            if (sfd.ShowDialog() == true)
            {
                SaveTo(sfd.FileName, showOkInfo: true);
            }
        }

        private void SaveTo(string path, bool showOkInfo)
        {
            try
            {
                var dir = Path.GetDirectoryName(path)!;
                Directory.CreateDirectory(dir);
                QuestionService.SaveToJson(path, Questions);
                if (showOkInfo)
                    MessageBox.Show($"Gespeichert: {path}", "Speichern", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Speichern: {ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ---------- Fragen & Antworten ----------

        private void AddQuestion()
        {
            var q = new Question { Text = "Neue Frage", Selected = true };
            // optional: gleich eine leere Antwort
            // q.Answers.Add(new Answer { Text = "", Correct = false });
            Questions.Add(q);
            SelectedQuestion = q;
            MoveQuestionUpCommand.RaiseCanExecuteChanged();
            MoveQuestionDownCommand.RaiseCanExecuteChanged();
        }

        // Löscht die aktuell ausgewählte Frage
        private void RemoveQuestion() => RemoveQuestion(SelectedQuestion);

        // Löscht eine konkrete Frage (für ✕-Button/ContextMenu)
        private void RemoveQuestion(Question? q)
        {
            if (q == null) return;

            // Optional: Sicherheitsabfrage
            var ask = MessageBox.Show(
                "Diese Frage wirklich löschen?",
                "Löschen bestätigen",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (ask != MessageBoxResult.Yes) return;

            var idx = Questions.IndexOf(q);
            Questions.Remove(q);

            if (ReferenceEquals(q, SelectedQuestion))
            {
                SelectedQuestion = Questions.Count == 0
                    ? null
                    : Questions[Math.Min(idx, Math.Max(0, Questions.Count - 1))];
            }

            RemoveQuestionCommand.RaiseCanExecuteChanged();
            MoveQuestionUpCommand.RaiseCanExecuteChanged();
            MoveQuestionDownCommand.RaiseCanExecuteChanged();
        }
        private bool CanMoveQuestion(Question? q, int delta)
        {
            if (q == null) return false;
            IEnumerable<Question> list = FilterSelectedOnly ? Questions.Where(x => x.Selected) : Questions;
            var arr = list.ToList();
            int index = arr.IndexOf(q);
            if (index < 0) return false;
            int newIndex = index + delta;
            return newIndex >= 0 && newIndex < arr.Count;
        }

        private void MoveQuestion(Question? q, int delta)
        {
            if (q == null) return;
            if (FilterSelectedOnly)
            {
                var selected = Questions.Where(x => x.Selected).ToList();
                int index = selected.IndexOf(q);
                int targetIndex = index + delta;
                if (targetIndex < 0 || targetIndex >= selected.Count) return;
                var other = selected[targetIndex];
                int i = Questions.IndexOf(q);
                int j = Questions.IndexOf(other);
                Swap(i, j);
            }
            else
            {
                int oldIndex = Questions.IndexOf(q);
                int newIndex = oldIndex + delta;
                if (newIndex < 0 || newIndex >= Questions.Count) return;
                Questions.Move(oldIndex, newIndex);
            }
            MoveQuestionUpCommand.RaiseCanExecuteChanged();
            MoveQuestionDownCommand.RaiseCanExecuteChanged();
        }

        private void Swap(int i, int j)
        {
            if (i == j) return;
            Questions.Move(i, j);
            Questions.Move(j > i ? j - 1 : j + 1, i);
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

        private void Questions_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
                foreach (Question q in e.NewItems)
                    q.PropertyChanged += Question_PropertyChanged;
            if (e.OldItems != null)
                foreach (Question q in e.OldItems)
                    q.PropertyChanged -= Question_PropertyChanged;
            MoveQuestionUpCommand.RaiseCanExecuteChanged();
            MoveQuestionDownCommand.RaiseCanExecuteChanged();
        }

        private void Question_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Question.Selected))
            {
                if (FilterSelectedOnly)
                    _filteredQuestions.Refresh();
                MoveQuestionUpCommand.RaiseCanExecuteChanged();
                MoveQuestionDownCommand.RaiseCanExecuteChanged();
            }
        }

        // ---------- Auswahl-Tools ----------

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
