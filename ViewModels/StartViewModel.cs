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

        private bool _enablePointDecay;
        public bool EnablePointDecay
        {
            get => _enablePointDecay;
            set { if (_enablePointDecay != value) { _enablePointDecay = value; OnPropertyChanged(nameof(EnablePointDecay)); } }
        }

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
            // Teams passend zur Auswahl neu aufbauen
            RebuildTeams();

            _main.EnablePointDecay = EnablePointDecay;

            // los geht's
            _main.NavigateToGame();
        }


        private void RebuildTeams()
        {
            // TeamCount auf 2..4 begrenzen
            var count = TeamCount;
            if (count < 2) count = 2;
            if (count > 4) count = 4;

            // _main.Teams neu aufbauen aus den ersten N Templates (this.Teams)
            _main.Teams.Clear();
            for (int i = 0; i < count; i++)
            {
                string name = (i < Teams.Count && !string.IsNullOrWhiteSpace(Teams[i].Name))
                                ? Teams[i].Name
                                : $"Team {i + 1}";

                _main.Teams.Add(new Team
                {
                    Name = name,
                    Score = 0,
                    PendingScore = 0
                });
            }

            // aktives Team auf Team 1 setzen
            _main.CurrentTeamIndex = 0;
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
