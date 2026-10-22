using System;
using System.Windows;
using System.Windows.Input;
using NotepadByFerryPot.Services;

namespace NotepadByFerryPot.Views
{
    public partial class LinkDialog : Window
    {
        public string LinkText { get; private set; } = string.Empty;
        public string Url { get; private set; } = string.Empty;
        public Uri? ValidUri { get; private set; }

        private readonly bool _hasSelectedText;

        public LinkDialog(string initialText, bool hasSelectedText)
        {
            InitializeComponent();
            _hasSelectedText = hasSelectedText;

            if (_hasSelectedText)
            {
                LinkText = initialText;
                LinkTextPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                LinkTextBox.Text = initialText;
                LinkTextPanel.Visibility = Visibility.Visible;
            }

            Loaded += (s, e) =>
            {
                if (_hasSelectedText)
                {
                    UrlTextBox.Focus();
                }
                else if (string.IsNullOrWhiteSpace(LinkTextBox.Text))
                {
                    LinkTextBox.Focus();
                }
                else
                {
                    UrlTextBox.Focus();
                }
            };
        }

        private void Input_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                ValidateAndSubmit();
            }
            else if (e.Key == Key.Escape)
            {
                e.Handled = true;
                DialogResult = false;
                Close();
            }
        }

        private void Insert_Click(object sender, RoutedEventArgs e)
        {
            ValidateAndSubmit();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void ValidateAndSubmit()
        {
            string rawUrl = UrlTextBox.Text;

            if (!SafeLinkService.TryNormalizeAndValidateUrl(rawUrl, out Uri? validUri, out string? errorMessage))
            {
                ErrorTextBlock.Text = errorMessage ?? "Please enter a valid web address.";
                ErrorTextBlock.Visibility = Visibility.Visible;
                UrlTextBox.Focus();
                return;
            }

            if (!_hasSelectedText)
            {
                string text = LinkTextBox.Text.Trim();
                LinkText = string.IsNullOrEmpty(text) ? validUri!.OriginalString : text;
            }

            Url = validUri!.OriginalString;
            ValidUri = validUri;
            DialogResult = true;
            Close();
        }
    }
}
