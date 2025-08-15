using RiskierWas.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Collections.Specialized;

namespace RiskierWas.ViewModels
{
    public class GameViewModel : INotifyPropertyChanged
    {
        private readonly MainViewModel _main;
        private int _questionIndex = -1;
        private int _nextPoints = 50;

        private Question? _currentQuestion;
        public Question? CurrentQuestion
        {
            get => _currentQuestion;
            set
            {
                if (_currentQuestion == value) return;
                _currentQuestion = value;
                OnPropertyChanged(nameof(CurrentQuestion));
                OnPropertyChanged(nameof(InfoLine));
                OnPropertyChanged(nameof(RemainingCorrectCount));
                OnPropertyChanged(nameof(RemainingWrongCount));
            }
        }

        public ObservableCollection<Team> Teams => _main.Teams;
        public Team CurrentTeam => _main.Teams[_main.CurrentTeamIndex];

        /// <summary>
        /// Punkte der aktuellen Runde des aktiven Teams (werden nur bei Weitergeben gutgeschrieben).
        /// </summary>
        public int CurrentRoundPoints => CurrentTeam?.PendingScore ?? 0;

        public int NextPoints
        {
            get => _nextPoints;
            set { if (_nextPoints != value) { _nextPoints = value; OnPropertyChanged(nameof(NextPoints)); OnPropertyChanged(nameof(InfoLine)); } }
        }

        public int RemainingCorrectCount => CurrentQuestion?.Answers.Count(a => a.Correct && !a.Revealed) ?? 0;
        public int RemainingWrongCount => CurrentQuestion?.Answers.Count(a => !a.Correct && !a.Revealed) ?? 0;

        public string InfoLine
        {
            get
            {
                if (CurrentQuestion == null) return string.Empty;
                int correctTotal = CurrentQuestion.Answers.Count(a => a.Correct);
                int revealedCorrect = CurrentQuestion.Answers.Count(a => a.Correct && a.Revealed);
                int revealedWrong = CurrentQuestion.Answers.Count(a => !a.Correct && a.Revealed);
                return $"Richtig {revealedCorrect}/{correctTotal} | Falsch {revealedWrong} | Runde: {CurrentRoundPoints} | Aktuelle Antwort wert: {NextPoints}";
            }
        }

        public RelayCommand RevealAnswerCommand { get; }
        public RelayCommand NextQuestionCommand { get; }
        public RelayCommand PassTurnCommand { get; }
        public RelayCommand BackToStartCommand { get; }

        public GameViewModel(MainViewModel main)
        {
            _main = main;
            SubscribeTeamEvents();

            RevealAnswerCommand = new RelayCommand(RevealAnswer, _ => CurrentQuestion != null);
            NextQuestionCommand = new RelayCommand(_ => NextQuestion());
            PassTurnCommand = new RelayCommand(_ => PassTurn(), _ => CurrentQuestion != null);
            BackToStartCommand = new RelayCommand(_ => _main.NavigateToStart());

            NextQuestion();
        }

        private void RevealAnswer(object? param)
        {
            if (CurrentQuestion == null) return;
            if (param is not Answer answer || answer.Revealed) return;

            answer.Revealed = true;
            if (answer.Correct)
            {
                CurrentTeam.PendingScore += NextPoints;
                OnPropertyChanged(nameof(CurrentRoundPoints));
                NextPoints += 50;
            }
            else
            {
                // falsche Antwort: Rundenpunkte verfallen, Zugwechsel (NextPoints bleibt erhalten!)
                CurrentTeam.PendingScore = 0;
                OnPropertyChanged(nameof(CurrentRoundPoints));
                PassTurn();
            }

            OnPropertyChanged(nameof(InfoLine));
            OnPropertyChanged(nameof(RemainingCorrectCount));
            OnPropertyChanged(nameof(RemainingWrongCount));
            AutoRevealIfDone();
        }

        private void AutoRevealIfDone()
        {
            if (CurrentQuestion == null) return;
            bool noCorrectLeft = CurrentQuestion.Answers.All(a => !a.Correct || a.Revealed);
            bool noWrongLeft = CurrentQuestion.Answers.All(a => a.Correct || a.Revealed);
            if (noCorrectLeft || noWrongLeft)
            {
                foreach (var a in CurrentQuestion.Answers)
                    a.Revealed = true;
            }
        }

        private void PassTurn()
        {
            if (CurrentQuestion == null) return;

            // Punkte „banking“
            if (CurrentTeam.PendingScore > 0)
            {
                CurrentTeam.Score += CurrentTeam.PendingScore;
                CurrentTeam.PendingScore = 0;
                OnPropertyChanged(nameof(CurrentRoundPoints));
            }

            // Zum nächsten Team wechseln
            _main.CurrentTeamIndex = (_main.CurrentTeamIndex + 1) % _main.Teams.Count;
            OnPropertyChanged(nameof(CurrentTeam));
            OnPropertyChanged(nameof(CurrentRoundPoints));

            // Hinweis: NextPoints NICHT zurücksetzen bei Zugwechsel; nur bei NextQuestion()
            OnPropertyChanged(nameof(InfoLine));
        }

        private void NextQuestion()
        {
            var selected = _main.Questions.Where(q => q.Selected).ToList();
            if (selected.Count == 0)
            {
                CurrentQuestion = null;
                return;
            }

            _questionIndex = (_questionIndex + 1) % selected.Count;
            CurrentQuestion = selected[_questionIndex];

            // Neue Frage -> Einsatz zurücksetzen
            NextPoints = 50;
            CurrentTeam.PendingScore = 0;
            OnPropertyChanged(nameof(CurrentRoundPoints));
            OnPropertyChanged(nameof(InfoLine));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private void SubscribeTeamEvents()
        {
            // Subscribe to team changes to relay PendingScore updates
            foreach (var t in _main.Teams)
            {
                t.PropertyChanged += OnTeamPropertyChanged;
            }
            if (_main.Teams is INotifyCollectionChanged ncc)
            {
                ncc.CollectionChanged += (s, e) =>
                {
                    if (e.NewItems != null) foreach (Team t in e.NewItems) t.PropertyChanged += OnTeamPropertyChanged;
                    if (e.OldItems != null) foreach (Team t in e.OldItems) t.PropertyChanged -= OnTeamPropertyChanged;
                };
            }
        }

        private void OnTeamPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Team.PendingScore))
            {
                // Nur für das aktuell aktive Team die Anzeige aktualisieren
                if (ReferenceEquals(sender, CurrentTeam))
                    OnPropertyChanged(nameof(CurrentRoundPoints));
            }
        }
    }
}
