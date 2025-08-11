using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using RiskierWas.Models;
using RiskierWas.Services;

namespace RiskierWas.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private object _currentViewModel = null!;
        public object CurrentViewModel
        {
            get => _currentViewModel;
            set { _currentViewModel = value; OnPropertyChanged(nameof(CurrentViewModel)); }
        }

        public ObservableCollection<Question> Questions { get; set; } = new();
        public ObservableCollection<Team> Teams { get; set; } = new();
        public int CurrentTeamIndex { get; set; } = 0;
        public int PointsPerCorrect => 50;

        public MainViewModel()
        {
            CurrentViewModel = new StartViewModel(this);
            TryLoadDefaultQuestions();
        }

        public void NavigateToGame() => CurrentViewModel = new GameViewModel(this);
        public void NavigateToEditor() => CurrentViewModel = new EditorViewModel(this);
        public void NavigateToStart() => CurrentViewModel = new StartViewModel(this);

        private void TryLoadDefaultQuestions()
        {
            try
            {
                var exeDir = AppContext.BaseDirectory;
                var defaultPath = Path.Combine(exeDir, "Data", "questions.json");
                if (File.Exists(defaultPath))
                {
                    Questions = QuestionService.LoadFromJson(defaultPath);
                }
            }
            catch { }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
