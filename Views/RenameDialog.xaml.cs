using System;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace NotepadByFerryPot.Views
{
    public partial class RenameDialog : Window
    {
        private static readonly System.Collections.Generic.HashSet<string> ReservedDeviceNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "CON", "PRN", "AUX", "NUL",
            "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
            "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
        };

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
            string raw = NewNameTextBox.Text;

            // 1. Reject empty names
            if (string.IsNullOrWhiteSpace(raw))
            {
                MessageBox.Show("Please enter a document name. The name cannot be empty.", "Invalid Name", MessageBoxButton.OK, MessageBoxImage.Warning);
                NewNameTextBox.Focus();
                return;
            }

            // 2. Reject names ending in a dot or space
            if (raw.EndsWith('.') || raw.EndsWith(' '))
            {
                MessageBox.Show("A document name cannot end with a period (.) or space.", "Invalid Name", MessageBoxButton.OK, MessageBoxImage.Warning);
                NewNameTextBox.Focus();
                return;
            }

            string trimmed = raw.Trim();

            // 3. Reject '.' and '..'
            if (trimmed == "." || trimmed == "..")
            {
                MessageBox.Show("A document name cannot be '.' or '..' as these are reserved relative path identifiers.", "Invalid Name", MessageBoxButton.OK, MessageBoxImage.Warning);
                NewNameTextBox.Focus();
                return;
            }

            // 4. Reject invalid file name characters
            char[] invalidChars = Path.GetInvalidFileNameChars();
            if (trimmed.IndexOfAny(invalidChars) >= 0)
            {
                MessageBox.Show("A document name cannot contain any of the following characters:\n\\ / : * ? \" < > |", "Invalid Name", MessageBoxButton.OK, MessageBoxImage.Warning);
                NewNameTextBox.Focus();
                return;
            }

            // 5. Reject reserved device names (with or without extension)
            string nameWithoutExt = Path.GetFileNameWithoutExtension(trimmed);
            if (ReservedDeviceNames.Contains(nameWithoutExt) || ReservedDeviceNames.Contains(trimmed))
            {
                MessageBox.Show($"\"{nameWithoutExt}\" is a reserved Windows device name (CON, PRN, AUX, NUL, COM1-9, LPT1-9) and cannot be used.", "Reserved Name", MessageBoxButton.OK, MessageBoxImage.Warning);
                NewNameTextBox.Focus();
                return;
            }

            NewName = trimmed;
            DialogResult = true;
            Close();
        }
    }
}
