using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
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

        private Question? _selectedQuestion;
        public Question? SelectedQuestion
        {
            get => _selectedQuestion;
            set
            {
                if (_selectedQuestion == value) return;
                _selectedQuestion = value;
                OnPropertyChanged(nameof(SelectedQuestion));

                // Buttons aktivieren/deaktivieren – null-sicher
                RemoveQuestionCommand?.RaiseCanExecuteChanged();
                AddAnswerCommand?.RaiseCanExecuteChanged();
                RemoveAnswerCommand?.RaiseCanExecuteChanged();
            }
        }

        public RelayCommand BackCommand { get; }
        public RelayCommand ExportJsonCommand { get; }
        public RelayCommand AddQuestionCommand { get; }
        public RelayCommand RemoveQuestionCommand { get; }
        public RelayCommand AddAnswerCommand { get; }
        public RelayCommand RemoveAnswerCommand { get; }

        // NEU: Auswahl-Buttons
        public RelayCommand DeselectAllCommand { get; }
        public RelayCommand SelectRandom20Command { get; }

        public EditorViewModel(MainViewModel main)
        {
            _main = main;

            BackCommand = new RelayCommand(_ => _main.NavigateToStart());
            ExportJsonCommand = new RelayCommand(_ => ExportJson());
            AddQuestionCommand = new RelayCommand(_ => AddQuestion());
            RemoveQuestionCommand = new RelayCommand(_ => RemoveQuestion(), _ => SelectedQuestion != null);

            AddAnswerCommand = new RelayCommand(
                                        _ => AddAnswer(),
                                        _ => SelectedQuestion != null && SelectedQuestion.Answers.Count < 16);

            RemoveAnswerCommand = new RelayCommand(
                                        a => RemoveAnswer(a as Answer),
                                        a => SelectedQuestion != null && a is Answer);

            DeselectAllCommand = new RelayCommand(_ => DeselectAll());
            SelectRandom20Command = new RelayCommand(_ => SelectRandom20());

            // Wichtig: erst JETZT initiale Auswahl setzen (nach Command-Initialisierung)
            SelectedQuestion = Questions.FirstOrDefault();
        }

        private void ExportJson()
        {
            var sfd = new SaveFileDialog
            {
                Filter = "JSON (*.json)|*.json",
                Title = "Fragen exportieren",
                FileName = "questions.json"
            };
            if (sfd.ShowDialog() == true)
            {
                try
                {
                    QuestionService.SaveToJson(sfd.FileName, Questions);
                    MessageBox.Show("Export erfolgreich.", "OK", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Fehler beim Export: {ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                }
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

            // sinnvolle neue Auswahl setzen
            SelectedQuestion = Questions.Count == 0
                ? null
                : Questions[Math.Min(idx, Questions.Count - 1)];
        }

        private void AddAnswer()
        {
            if (SelectedQuestion == null) return;
            SelectedQuestion.Answers.Add(new Answer { Text = "Antwort", Correct = false });
            AddAnswerCommand.RaiseCanExecuteChanged();      // max. 16 beachten
            OnPropertyChanged(nameof(SelectedQuestion));
        }

        private void RemoveAnswer(Answer? a)
        {
            if (SelectedQuestion == null || a == null) return;
            SelectedQuestion.Answers.Remove(a);
            AddAnswerCommand.RaiseCanExecuteChanged();
            OnPropertyChanged(nameof(SelectedQuestion));
        }

        // Alle Fragen abwählen
        private void DeselectAll()
        {
            foreach (var q in Questions)
                q.Selected = false;

            OnPropertyChanged(nameof(Questions));
        }

        // 20 zufällige Fragen auswählen (oder weniger, wenn nicht genug da sind)
        private void SelectRandom20()
        {
            if (Questions.Count == 0) return;

            foreach (var q in Questions)
                q.Selected = false;

            int take = Math.Min(20, Questions.Count);

            // Fisher–Yates-Shuffle über Indizes
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
