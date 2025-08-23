using RiskierWas.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Collections.Specialized;
using System.Windows.Threading;

namespace RiskierWas.ViewModels
{
    public class GameViewModel : INotifyPropertyChanged
    {
        private readonly MainViewModel _main;
        private int _questionIndex = -1;
        private int _nextPoints = 50;
        private readonly Random _rng = new();
        private int _baseNextPoints = 50;
        private readonly DispatcherTimer _decayTimer = new() { Interval = TimeSpan.FromSeconds(10) };
        private readonly DispatcherTimer _decayProgressTimer = new() { Interval = TimeSpan.FromMilliseconds(100) };
        private DateTime _decayStartTime;
        private double _decayProgress;
        private bool _decayPaused;

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
        public RelayCommand ToggleDecayPauseCommand { get; }

        public bool EnablePointDecay => _main.EnablePointDecay;

        public double DecayProgress
        {
            get => _decayProgress;
            private set
            {
                if (Math.Abs(_decayProgress - value) > 0.0001)
                {
                    _decayProgress = value;
                    OnPropertyChanged(nameof(DecayProgress));
                }
            }
        }

        public bool DecayPaused
        {
            get => _decayPaused;
            private set
            {
                if (_decayPaused != value)
                {
                    _decayPaused = value;
                    OnPropertyChanged(nameof(DecayPaused));
                    OnPropertyChanged(nameof(PauseButtonText));
                }
            }
        }

        public string PauseButtonText => DecayPaused ? "Fortsetzen" : "Pause";

        public GameViewModel(MainViewModel main)
        {
            _main = main;
            SubscribeTeamEvents();

            _decayTimer.Tick += (_, _) =>
            {
                NextPoints = Math.Max(1, (int)Math.Ceiling(NextPoints * 0.9));
                _decayStartTime = DateTime.Now;
                DecayProgress = 0;
            };
            _decayProgressTimer.Tick += (_, _) =>
            {
                var elapsed = DateTime.Now - _decayStartTime;
                DecayProgress = Math.Min(1.0, elapsed.TotalMilliseconds / _decayTimer.Interval.TotalMilliseconds);
            };

            RevealAnswerCommand = new RelayCommand(RevealAnswer, _ => CurrentQuestion != null);
            NextQuestionCommand = new RelayCommand(_ => NextQuestion());
            PassTurnCommand = new RelayCommand(_ => PassTurn(), _ => CurrentQuestion != null);
            BackToStartCommand = new RelayCommand(_ => BackToStart());
            ToggleDecayPauseCommand = new RelayCommand(_ => ToggleDecayPause());

            NextQuestion();
        }

        private void StartDecayTimer()
        {
            if (_main.EnablePointDecay && !DecayPaused)
            {
                _decayStartTime = DateTime.Now;
                DecayProgress = 0;
                _decayTimer.Stop();
                _decayProgressTimer.Stop();
                _decayTimer.Start();
                _decayProgressTimer.Start();
            }
        }

        private void StopDecayTimer()
        {
            _decayTimer.Stop();
            _decayProgressTimer.Stop();
            DecayProgress = 0;
        }

        private void ToggleDecayPause()
        {
            if (DecayPaused)
            {
                DecayPaused = false;
                StartDecayTimer();
            }
            else
            {
                DecayPaused = true;
                StopDecayTimer();
            }
        }

        private void RevealAnswer(object? param)
        {
            if (CurrentQuestion == null) return;
            if (param is not Answer answer || answer.Revealed) return;

            StopDecayTimer();

            answer.Revealed = true;
            if (answer.Correct)
            {
                CurrentTeam.PendingScore += NextPoints;
                OnPropertyChanged(nameof(CurrentRoundPoints));
                _baseNextPoints += 50;
                NextPoints = _baseNextPoints;
            }
            else
            {
                // falsche Antwort: Rundenpunkte verfallen, Zugwechsel (NextPoints bleibt erhalten!)
                CurrentTeam.PendingScore = 0;
                OnPropertyChanged(nameof(CurrentRoundPoints));
                NextPoints = _baseNextPoints;
                PassTurn();
            }

            OnPropertyChanged(nameof(InfoLine));
            OnPropertyChanged(nameof(RemainingCorrectCount));
            OnPropertyChanged(nameof(RemainingWrongCount));
            AutoRevealIfDone();
            StartDecayTimer();
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

            StopDecayTimer();

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

            NextPoints = _baseNextPoints;

            // Hinweis: NextPoints NICHT zurücksetzen bei Zugwechsel; nur bei NextQuestion()
            OnPropertyChanged(nameof(InfoLine));
            StartDecayTimer();
        }

        private void BackToStart()
        {
            StopDecayTimer();
            _main.NavigateToStart();
        }

        private void NextQuestion()
        {
            StopDecayTimer();

            var selected = _main.Questions.Where(q => q.Selected).ToList();
            if (selected.Count == 0)
            {
                CurrentQuestion = null;
                return;
            }

            _questionIndex = (_questionIndex + 1) % selected.Count;
            CurrentQuestion = selected[_questionIndex];

            if (CurrentQuestion != null)
            {
                var shuffled = CurrentQuestion.Answers.OrderBy(_ => _rng.Next()).ToList();
                CurrentQuestion.Answers = new ObservableCollection<Answer>(shuffled);
            }

            // Neue Frage -> Einsatz zurücksetzen
            _baseNextPoints = 50;
            NextPoints = _baseNextPoints;
            CurrentTeam.PendingScore = 0;
            OnPropertyChanged(nameof(CurrentRoundPoints));
            OnPropertyChanged(nameof(InfoLine));
            StartDecayTimer();
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
