using System.Collections.ObjectModel;
using System.ComponentModel;

namespace RiskierWas.Models
{
    public class Question : INotifyPropertyChanged
    {
        private string _text = string.Empty;
        private bool _selected = true;
        private ObservableCollection<Answer> _answers = new();

        public string Text { get => _text; set { _text = value; OnPropertyChanged(nameof(Text)); } }
        public bool Selected { get => _selected; set { _selected = value; OnPropertyChanged(nameof(Selected)); } }
        public ObservableCollection<Answer> Answers { get => _answers; set { _answers = value; OnPropertyChanged(nameof(Answers)); } }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
