using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NotepadByFerryPot.Views
{
    public partial class FindReplaceWindow : Window
    {
        private readonly TextBox _targetTextBox;

        public FindReplaceWindow(TextBox targetTextBox)
        {
            InitializeComponent();
            _targetTextBox = targetTextBox;
            Loaded += (s, e) => FindTextBox.Focus();
        }

        private void FindTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                FindNext();
            }
        }

        private void FindNext_Click(object sender, RoutedEventArgs e)
        {
            FindNext();
        }

        private bool FindNext()
        {
            string query = FindTextBox.Text;
            if (string.IsNullOrEmpty(query)) return false;

            string text = _targetTextBox.Text;
            bool matchCase = MatchCaseCheckBox.IsChecked == true;
            bool wholeWord = MatchWholeWordCheckBox.IsChecked == true;
            bool searchUp = SearchUpCheckBox.IsChecked == true;

            int startIndex = _targetTextBox.SelectionStart;

            if (searchUp)
            {
                // Search backwards
                int searchEnd = startIndex;
                int found = -1;

                if (wholeWord)
                {
                    string pattern = $@"\b{Regex.Escape(query)}\b";
                    var regex = new Regex(pattern, matchCase ? RegexOptions.None : RegexOptions.IgnoreCase);
                    var matches = regex.Matches(text.Substring(0, Math.Max(0, searchEnd)));
                    if (matches.Count > 0)
                    {
                        found = matches[^1].Index;
                    }
                }
                else
                {
                    StringComparison comp = matchCase ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase;
                    found = text.LastIndexOf(query, Math.Max(0, searchEnd - 1), comp);
                }

                if (found >= 0)
                {
                    _targetTextBox.Select(found, query.Length);
                    _targetTextBox.Focus();
                    StatusTextBlock.Text = $"Match found at position {found}.";
                    return true;
                }
                else
                {
                    StatusTextBlock.Text = "Cannot find \"" + query + "\" backwards.";
                    return false;
                }
            }
            else
            {
                // Search forwards
                int searchStart = startIndex + _targetTextBox.SelectionLength;
                int found = -1;

                if (wholeWord)
                {
                    string pattern = $@"\b{Regex.Escape(query)}\b";
                    var regex = new Regex(pattern, matchCase ? RegexOptions.None : RegexOptions.IgnoreCase);
                    var match = regex.Match(text, Math.Min(searchStart, text.Length));
                    if (match.Success)
                    {
                        found = match.Index;
                    }
                }
                else
                {
                    StringComparison comp = matchCase ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase;
                    found = text.IndexOf(query, Math.Min(searchStart, text.Length), comp);
                }

                if (found >= 0)
                {
                    _targetTextBox.Select(found, query.Length);
                    _targetTextBox.Focus();
                    StatusTextBlock.Text = $"Match found at position {found}.";
                    return true;
                }
                else
                {
                    StatusTextBlock.Text = "Cannot find \"" + query + "\".";
                    return false;
                }
            }
        }

        private void Replace_Click(object sender, RoutedEventArgs e)
        {
            string query = FindTextBox.Text;
            if (string.IsNullOrEmpty(query)) return;

            if (_targetTextBox.SelectedText.Equals(query, MatchCaseCheckBox.IsChecked == true ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase))
            {
                _targetTextBox.SelectedText = ReplaceTextBox.Text;
                FindNext();
            }
            else
            {
                FindNext();
            }
        }

        private void ReplaceAll_Click(object sender, RoutedEventArgs e)
        {
            string query = FindTextBox.Text;
            if (string.IsNullOrEmpty(query)) return;

            string replacement = ReplaceTextBox.Text;
            bool matchCase = MatchCaseCheckBox.IsChecked == true;
            bool wholeWord = MatchWholeWordCheckBox.IsChecked == true;

            string text = _targetTextBox.Text;
            int count = 0;

            if (wholeWord)
            {
                string pattern = $@"\b{Regex.Escape(query)}\b";
                var regex = new Regex(pattern, matchCase ? RegexOptions.None : RegexOptions.IgnoreCase);
                count = regex.Matches(text).Count;
                _targetTextBox.Text = regex.Replace(text, replacement);
            }
            else
            {
                var regex = new Regex(Regex.Escape(query), matchCase ? RegexOptions.None : RegexOptions.IgnoreCase);
                count = regex.Matches(text).Count;
                _targetTextBox.Text = regex.Replace(text, replacement);
            }

            StatusTextBlock.Text = $"Replaced {count} occurrence(s).";
        }
    }
}
