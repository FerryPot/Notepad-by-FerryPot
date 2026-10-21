using System;
using System.Linq;
using System.Windows;
using NotepadByFerryPot.Models;

namespace NotepadByFerryPot.Views
{
    public partial class StatsWindow : Window
    {
        public StatsWindow(NotepadDocument doc)
        {
            InitializeComponent();

            string content = doc.Content ?? string.Empty;
            int words = doc.WordCount;
            int charsWithSpaces = content.Length;
            int charsNoSpaces = content.Count(c => !char.IsWhiteSpace(c));
            int lines = doc.TotalLines;

            // Reading time at ~200 words per minute
            double readingMinutes = words / 200.0;
            string readingTime = readingMinutes < 1.0 ? "< 1 min" : $"~{Math.Ceiling(readingMinutes)} min";

            // Speaking time at ~130 words per minute
            double speakingMinutes = words / 130.0;
            string speakingTime = speakingMinutes < 1.0 ? "< 1 min" : $"~{Math.Ceiling(speakingMinutes)} min";

            WordsText.Text = words.ToString("N0");
            CharsWithSpacesText.Text = charsWithSpaces.ToString("N0");
            CharsNoSpacesText.Text = charsNoSpaces.ToString("N0");
            LinesText.Text = lines.ToString("N0");
            ReadingTimeText.Text = readingTime;
            SpeakingTimeText.Text = speakingTime;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
