using System.ComponentModel;

namespace RiskierWas.Models
{
    public class Team : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private int _score;
        // Punkte der aktuellen Runde. Gehen bei falscher Antwort verloren.
        // Erst bei "Weitergeben" werden sie dauerhaft gutgeschrieben.
        private int _pendingScore;
        public int PendingScore
        {
            get => _pendingScore;
            set { if (_pendingScore != value) { _pendingScore = value; OnPropertyChanged(nameof(PendingScore)); } }
        }

        public string Name { get => _name; set { _name = value; OnPropertyChanged(nameof(Name)); } }
        public int Score { get => _score; set { _score = value; OnPropertyChanged(nameof(Score)); } }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
