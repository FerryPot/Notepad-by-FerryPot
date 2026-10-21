using System;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace NotepadByFerryPot.Views
{
    public partial class RenameDialog : Window
    {
        public string NewName { get; private set; } = string.Empty;

        public RenameDialog(string currentName)
        {
            InitializeComponent();
            NewNameTextBox.Text = currentName;
            Loaded += (s, e) =>
            {
                NewNameTextBox.Focus();
                NewNameTextBox.SelectAll();
            };
        }

        private void Rename_Click(object sender, RoutedEventArgs e)
        {
            ConfirmRename();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void NewNameTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                ConfirmRename();
            }
            else if (e.Key == Key.Escape)
            {
                e.Handled = true;
                DialogResult = false;
                Close();
            }
        }

        private void ConfirmRename()
        {
            string trimmed = NewNameTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                MessageBox.Show("Please enter a valid name for the document.", "Rename Tab", MessageBoxButton.OK, MessageBoxImage.Warning);
                NewNameTextBox.Focus();
                return;
            }

            // Check for invalid file name characters
            char[] invalidChars = Path.GetInvalidFileNameChars();
            if (trimmed.IndexOfAny(invalidChars) >= 0)
            {
                MessageBox.Show("A document name cannot contain any of the following characters:\n\\ / : * ? \" < > |", "Invalid Name", MessageBoxButton.OK, MessageBoxImage.Warning);
                NewNameTextBox.Focus();
                return;
            }

            NewName = trimmed;
            DialogResult = true;
            Close();
        }
    }
}
