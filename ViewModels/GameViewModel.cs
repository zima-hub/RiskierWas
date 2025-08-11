using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using RiskierWas.Models;

namespace RiskierWas.ViewModels
{
    public class GameViewModel : INotifyPropertyChanged
    {
        private readonly MainViewModel _main;
        private int _questionIndex = -1;
        private int _nextPoints = 50;
        private int _currentRoundScore = 0;

        public Question? CurrentQuestion { get; set; }
        public ObservableCollection<Team> Teams => _main.Teams;
        public Team CurrentTeam => Teams[_main.CurrentTeamIndex];

        public int NextPoints
        {
            get => _nextPoints;
            private set { _nextPoints = value; OnPropertyChanged(nameof(NextPoints)); }
        }

        public string Info =>
            CurrentQuestion == null ? string.Empty :
            $"Richtig: {CurrentQuestion.Answers.Count(a=>a.Revealed && a.Correct)} / {CurrentQuestion.Answers.Count(a=>a.Correct)} | " +
            $"Falsch: {CurrentQuestion.Answers.Count(a=>a.Revealed && !a.Correct)} | " +
            $"Aktuelle Antwort wert: {NextPoints}";

        public RelayCommand RevealAnswerCommand { get; }
        public RelayCommand NextQuestionCommand { get; }
        public RelayCommand PassTurnCommand { get; }
        public RelayCommand BackToStartCommand { get; }

        public GameViewModel(MainViewModel main)
        {
            _main = main;
            RevealAnswerCommand = new RelayCommand(RevealAnswer, _ => CurrentQuestion != null);
            NextQuestionCommand = new RelayCommand(_ => NextQuestion());
            PassTurnCommand = new RelayCommand(_ => ManualPass());
            BackToStartCommand = new RelayCommand(_ => _main.NavigateToStart());
            NextQuestion();
        }

        private void ResetRound()
        {
            _currentRoundScore = 0;
            OnPropertyChanged(nameof(Info));
        }

        private void NextQuestion()
        {
            var selected = _main.Questions.Where(q => q.Selected).ToList();
            _questionIndex++;
            if (_questionIndex >= selected.Count)
            {
                MessageBox.Show("Keine weiteren ausgewählten Fragen.", "Ende", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            CurrentQuestion = selected[_questionIndex];
            foreach (var a in CurrentQuestion.Answers)
                a.Revealed = false;

            NextPoints = 50;
            ResetRound();

            OnPropertyChanged(nameof(CurrentQuestion));
            OnPropertyChanged(nameof(Info));
        }

        private void RevealAnswer(object? param)
        {
            if (param is not Answer answer || CurrentQuestion == null) return;
            if (answer.Revealed) return;

            answer.Revealed = true;

            if (answer.Correct)
            {
                CurrentTeam.Score += NextPoints;
                _currentRoundScore += NextPoints;
                NextPoints += 50; // erhöht sich nur nach einer richtigen Antwort
            }
            else
            {
                // Falsch: alle Punkte dieser Runde verlieren und Teamwechsel, NextPoints bleibt erhalten
                CurrentTeam.Score -= _currentRoundScore;
                ResetRound();
                PassTurn();
            }

            OnPropertyChanged(nameof(Info));
            AutoRevealIfDone();
        }

        private void ManualPass()
        {
            // Bei manuellem Weitergeben behält das Team seine erspielten Punkte; Rundenscore für das neue Team startet bei 0
            PassTurn();
            ResetRound();
        }

        private void PassTurn()
        {
            _main.CurrentTeamIndex = (_main.CurrentTeamIndex + 1) % Teams.Count;
            OnPropertyChanged(nameof(CurrentTeam));
            OnPropertyChanged(nameof(Info));
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
    }
}
