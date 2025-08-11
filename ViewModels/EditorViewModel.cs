using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using RiskierWas.Models;
using RiskierWas.Services;

namespace RiskierWas.ViewModels
{
    public class EditorViewModel
    {
        private readonly MainViewModel _main;
        public ObservableCollection<Question> Questions => _main.Questions;
        public Question? SelectedQuestion { get; set; }

        public RelayCommand BackCommand { get; }
        public RelayCommand ExportJsonCommand { get; }
        public RelayCommand AddQuestionCommand { get; }
        public RelayCommand RemoveQuestionCommand { get; }
        public RelayCommand AddAnswerCommand { get; }
        public RelayCommand RemoveAnswerCommand { get; }

        public EditorViewModel(MainViewModel main)
        {
            _main = main;
            SelectedQuestion = Questions.FirstOrDefault();
            BackCommand = new RelayCommand(_ => _main.NavigateToStart());
            ExportJsonCommand = new RelayCommand(_ => ExportJson());
            AddQuestionCommand = new RelayCommand(_ => AddQuestion());
            RemoveQuestionCommand = new RelayCommand(_ => RemoveQuestion(), _ => SelectedQuestion != null);
            AddAnswerCommand = new RelayCommand(_ => AddAnswer(), _ => SelectedQuestion != null);
            RemoveAnswerCommand = new RelayCommand(RemoveAnswer, _ => SelectedQuestion != null);
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
                catch (System.Exception ex)
                {
                    MessageBox.Show($"Fehler beim Export: {ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void AddQuestion()
        {
            var q = new Question{ Text = "Neue Frage", Selected = true };
            Questions.Add(q);
            SelectedQuestion = q;
        }

        private void RemoveQuestion()
        {
            if (SelectedQuestion != null)
            {
                Questions.Remove(SelectedQuestion);
                SelectedQuestion = Questions.FirstOrDefault();
            }
        }

        private void AddAnswer()
        {
            if (SelectedQuestion == null) return;
            SelectedQuestion.Answers.Add(new Answer{ Text = "Antwort", Correct = false });
        }

        private void RemoveAnswer(object? param)
        {
            if (SelectedQuestion == null) return;
            if (param is Answer a)
            {
                SelectedQuestion.Answers.Remove(a);
            }
        }
    }
}
