using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using RiskierWas.Models;

namespace RiskierWas.Services
{
    public static class QuestionService
    {
        public static ObservableCollection<Question> LoadFromJson(string path)
        {
            var json = File.ReadAllText(path);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var data = JsonSerializer.Deserialize<List<Question>>(json, options) ?? new List<Question>();
            // Ensure ObservableCollections
            foreach (var q in data)
            {
                var items = q.Answers != null ? (IEnumerable<Answer>)q.Answers : Array.Empty<Answer>();
                q.Answers = new ObservableCollection<Answer>(items);
            }
            return new ObservableCollection<Question>(data);
        }

        public static void SaveToJson(string path, IEnumerable<Question> questions)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(questions, options);
            File.WriteAllText(path, json);
        }
    }
}
