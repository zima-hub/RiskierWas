using System.ComponentModel;

namespace RiskierWas.Models
{
    public class Team : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private int _score;

        public string Name { get => _name; set { _name = value; OnPropertyChanged(nameof(Name)); } }
        public int Score { get => _score; set { _score = value; OnPropertyChanged(nameof(Score)); } }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
