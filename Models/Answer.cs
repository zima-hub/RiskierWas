using System.ComponentModel;

namespace RiskierWas.Models
{
    public class Answer : INotifyPropertyChanged
    {
        private string _text = string.Empty;
        private bool _correct;
        private string? _comment;
        private bool _revealed;

        public string Text { get => _text; set { _text = value; OnPropertyChanged(nameof(Text)); } }
        public bool Correct { get => _correct; set { _correct = value; OnPropertyChanged(nameof(Correct)); } }
        public string? Comment { get => _comment; set { _comment = value; OnPropertyChanged(nameof(Comment)); } }
        public bool Revealed { get => _revealed; set { _revealed = value; OnPropertyChanged(nameof(Revealed)); } }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
