using RiskierWas.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Documents;

namespace RiskierWas.ViewModels
{
    public class GameViewModel : INotifyPropertyChanged
    {
        private readonly MainViewModel _main;
        private int _questionIndex = -1;
        private int _nextPoints = 50;

        public Question? CurrentQuestion { get; set; }
        public ObservableCollection<Team> Teams => _main.Teams;
        public Team CurrentTeam
        {
            get
            {
                if (_main.Teams.Count == 0) return _dummyTeam;
                int i = _main.CurrentTeamIndex;
                if (i < 0) i = 0;
                if (i >= _main.Teams.Count) i = _main.Teams.Count - 1;
                return _main.Teams[i];
            }
        }

        private readonly Team _dummyTeam = new Team { Name = "—", Score = 0 };
        public int NextPoints
        {
            get => _nextPoints;
            set { if (_nextPoints != value) { _nextPoints = value; OnPropertyChanged(nameof(NextPoints)); OnPropertyChanged(nameof(InfoLine)); } }
        }

        public string Info =>
            CurrentQuestion == null ? string.Empty :
            $"Richtig: {CurrentQuestion.Answers.Count(a => a.Revealed && a.Correct)} / {CurrentQuestion.Answers.Count(a => a.Correct)} | " +
            $"Falsch: {CurrentQuestion.Answers.Count(a => a.Revealed && !a.Correct)} | " +
            $"Punkte aktuelle Runde: {CurrentTeam.PendingScore} | " +
            $"Aktuelle Antwort wert: {NextPoints}";

        public RelayCommand RevealAnswerCommand { get; }
        public RelayCommand NextQuestionCommand { get; }
        public RelayCommand PassTurnCommand { get; }
        public RelayCommand BackToStartCommand { get; }

        public GameViewModel(MainViewModel main)
        {
            _main = main;
            // Falls noch keine Teams angelegt sind, mindestens 2 Default-Teams erstellen
            if (_main.Teams.Count == 0)
            {
                _main.Teams.Add(new Team { Name = "Team 1" });
                _main.Teams.Add(new Team { Name = "Team 2" });
            }

            // Index in gültigen Bereich zwingen
            if (_main.CurrentTeamIndex < 0 || _main.CurrentTeamIndex >= _main.Teams.Count)
                _main.CurrentTeamIndex = 0;
            RevealAnswerCommand = new RelayCommand(
    p => { if (p is Answer a) RevealAnswer(a); },
    p => p is Answer && CurrentQuestion != null
);
            NextQuestionCommand = new RelayCommand(_ => NextQuestion());
            PassTurnCommand = new RelayCommand(_ => ManualPass());
            BackToStartCommand = new RelayCommand(_ => _main.NavigateToStart());
            NextQuestion();
        }

        private void ResetRound()
        {
         
            OnPropertyChanged(nameof(Info));
        }

        public void NextQuestion()
        {
            var pool = _main.Questions.Where(q => q.Selected).ToList();
            if (pool.Count == 0) pool = _main.Questions.ToList();
            if (pool.Count == 0) { CurrentQuestion = null; return; }

            _questionIndex = (_questionIndex + 1) % pool.Count;
            CurrentQuestion = pool[_questionIndex];
            foreach (var a in CurrentQuestion.Answers) a.Revealed = false;

            NextPoints = 50;                 // nur den nächsten Wert zurücksetzen
            OnPropertyChanged(nameof(InfoLine));
        }

        public void RevealAnswer(Answer a)
        {
            if (CurrentQuestion == null || a.Revealed) return;
            a.Revealed = true;

            if (a.Correct)
            {
                CurrentTeam.PendingScore += NextPoints;
                NextPoints += 50;
            }
            else
            {
                CurrentTeam.PendingScore = 0;
                PassTurn();  // NextPoints bleibt
            }

            AutoRevealIfDone();
            OnPropertyChanged(nameof(InfoLine));
        }

        private void ManualPass()
        {
            // Bei manuellem Weitergeben behält das Team seine erspielten Punkte; Rundenscore für das neue Team startet bei 0
            PassTurn();
            ResetRound();
        }

        public void PassTurn()
        {
            if (_main.Teams.Count == 0) return; // nichts zu tun
            if (CurrentTeam.PendingScore > 0)
            {
                CurrentTeam.Score += CurrentTeam.PendingScore; // bank
                CurrentTeam.PendingScore = 0;
            }

            _main.CurrentTeamIndex = (_main.CurrentTeamIndex + 1) % _main.Teams.Count;
            OnPropertyChanged(nameof(CurrentTeam));
            OnPropertyChanged(nameof(InfoLine));
        }

        private void AutoRevealIfDone()
        {
            if (CurrentQuestion == null) return;
            bool allCorrectRevealed = CurrentQuestion.Answers.Where(a => a.Correct).All(a => a.Revealed);
            bool allIncorrectRevealed = CurrentQuestion.Answers.Where(a => !a.Correct).All(a => a.Revealed);
            if (allCorrectRevealed || allIncorrectRevealed)
            {
                foreach (var a in CurrentQuestion.Answers)
                    a.Revealed = true;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    
    public string InfoLine
        {
            get
            {
                if (CurrentQuestion == null) return "Keine Frage geladen.";
                int correctTotal = CurrentQuestion.Answers.Count(a => a.Correct);
                int revealedCorrect = CurrentQuestion.Answers.Count(a => a.Correct && a.Revealed);
                int revealedWrong = CurrentQuestion.Answers.Count(a => !a.Correct && a.Revealed);
                return $"Richtig {revealedCorrect}/{correctTotal} | Falsch {revealedWrong} | Punkte aktuelle Runde: {CurrentTeam.PendingScore} | Aktuelle Antwort wert: {NextPoints}";
            }
        }

    } 
}
