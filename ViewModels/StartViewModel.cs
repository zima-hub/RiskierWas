using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows;
using Microsoft.Win32;
using RiskierWas.Models;
using RiskierWas.Services;

namespace RiskierWas.ViewModels
{
    public class StartViewModel : INotifyPropertyChanged
    {
        private readonly MainViewModel _main;

        public int TeamCount { get; set; } = 2;
        public ObservableCollection<Team> Teams { get; set; } = new()
        {
            new Team{ Name = "Team 1"},
            new Team{ Name = "Team 2"},
            new Team{ Name = "Team 3"},
            new Team{ Name = "Team 4"},
        };

        public RelayCommand StartGameCommand { get; }
        public RelayCommand OpenEditorCommand { get; }
        public RelayCommand LoadQuestionsCommand { get; }

        public bool HasQuestions => _main.Questions.Count > 0;
        public string LoadStatus => HasQuestions ? $"\u2714 {_main.Questions.Count} Fragen geladen" : "⚠ Keine Fragen geladen";

        public StartViewModel(MainViewModel main)
        {
            _main = main;
            StartGameCommand = new RelayCommand(_ => StartGame(), _ => HasQuestions && TeamCount >= 2 && TeamCount <= 4);
            OpenEditorCommand = new RelayCommand(_ => _main.NavigateToEditor(), _ => HasQuestions);
            LoadQuestionsCommand = new RelayCommand(_ => LoadQuestions());
        }

        private void StartGame()
        {
            _main.Teams.Clear();
            for (int i = 0; i < TeamCount; i++)
            {
                _main.Teams.Add(Teams[i]);
            }
            _main.CurrentTeamIndex = 0;
            _main.NavigateToGame();
        }

        private void LoadQuestions()
        {
            var ofd = new OpenFileDialog
            {
                Filter = "JSON (*.json)|*.json",
                Title = "Fragen laden"
            };
            if (ofd.ShowDialog() == true)
            {
                try
                {
                    _main.Questions = QuestionService.LoadFromJson(ofd.FileName);
                    OnPropertyChanged(nameof(HasQuestions));
                    OnPropertyChanged(nameof(LoadStatus));
                    StartGameCommand.RaiseCanExecuteChanged();
                    OpenEditorCommand.RaiseCanExecuteChanged();
                    MessageBox.Show($"Es wurden {_main.Questions.Count} Fragen geladen.", "Erfolg", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"Fehler beim Laden: {ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
