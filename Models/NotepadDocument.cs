using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace NotepadByFerryPot.Models
{
    public class NotepadDocument : INotifyPropertyChanged
    {
        private string _title = "Untitled";
        private string? _filePath;
        private string _content = string.Empty;
        private string _savedContent = string.Empty;
        private bool _isModified = false;
        private int _currentLine = 1;
        private int _currentColumn = 1;
        private int _totalLines = 1;
        private int _wordCount = 0;
        private int _characterCount = 0;
        private string _lineEnding = "Windows (CRLF)";
        private Encoding _encoding = Encoding.UTF8;

        public Guid Id { get; } = Guid.NewGuid();

        public string Title
        {
            get => _title;
            set
            {
                if (SetProperty(ref _title, value))
                {
                    OnPropertyChanged(nameof(DisplayTitle));
                }
            }
        }

        public string DisplayTitle
        {
            get
            {
                string prefix = IsModified ? "● " : "";
                string suffix = IsReadOnly ? " [Read-Only]" : "";
                return $"{prefix}{Title}{suffix}";
            }
        }

        private bool _isReadOnly = false;
        public bool IsReadOnly
        {
            get => _isReadOnly;
            set
            {
                if (SetProperty(ref _isReadOnly, value))
                {
                    OnPropertyChanged(nameof(DisplayTitle));
                }
            }
        }

        private bool? _isFormattedView;
        public bool? IsFormattedView
        {
            get => _isFormattedView;
            set => SetProperty(ref _isFormattedView, value);
        }

        public string? FilePath
        {
            get => _filePath;
            set => SetProperty(ref _filePath, value);
        }

        public string Content
        {
            get => _content;
            set
            {
                if (SetProperty(ref _content, value))
                {
                    IsModified = _content != _savedContent;
                    UpdateStatistics();
                }
            }
        }

        public string SavedContent
        {
            get => _savedContent;
            set
            {
                if (SetProperty(ref _savedContent, value))
                {
                    IsModified = _content != _savedContent;
                }
            }
        }

        public bool IsModified
        {
            get => _isModified;
            set
            {
                if (SetProperty(ref _isModified, value))
                {
                    OnPropertyChanged(nameof(DisplayTitle));
                }
            }
        }

        public int CurrentLine
        {
            get => _currentLine;
            set => SetProperty(ref _currentLine, value);
        }

        public int CurrentColumn
        {
            get => _currentColumn;
            set => SetProperty(ref _currentColumn, value);
        }

        public int TotalLines
        {
            get => _totalLines;
            set => SetProperty(ref _totalLines, value);
        }

        public int WordCount
        {
            get => _wordCount;
            set => SetProperty(ref _wordCount, value);
        }

        public int CharacterCount
        {
            get => _characterCount;
            set => SetProperty(ref _characterCount, value);
        }

        public string LineEnding
        {
            get => _lineEnding;
            set => SetProperty(ref _lineEnding, value);
        }

        public Encoding Encoding
        {
            get => _encoding;
            set
            {
                if (SetProperty(ref _encoding, value))
                {
                    OnPropertyChanged(nameof(EncodingName));
                }
            }
        }

        public string EncodingName
        {
            get
            {
                if (_encoding is UTF8Encoding utf8)
                {
                    return utf8.GetPreamble().Length > 0 ? "UTF-8 (BOM)" : "UTF-8";
                }
                if (_encoding == Encoding.Unicode) return "UTF-16 LE";
                if (_encoding == Encoding.BigEndianUnicode) return "UTF-16 BE";
                if (_encoding == Encoding.ASCII) return "ASCII";
                return _encoding.EncodingName;
            }
        }

        public void MarkAsSaved(string filePath)
        {
            FilePath = filePath;
            Title = Path.GetFileName(filePath);
            SavedContent = Content;
            IsModified = false;
            try
            {
                if (File.Exists(filePath))
                {
                    IsReadOnly = (File.GetAttributes(filePath) & FileAttributes.ReadOnly) != 0;
                }
            }
            catch { }
        }

        public void UpdateStatistics()
        {
            CharacterCount = _content.Length;

            if (string.IsNullOrEmpty(_content))
            {
                TotalLines = 1;
                WordCount = 0;
                return;
            }

            // Count lines
            int lines = 1;
            for (int i = 0; i < _content.Length; i++)
            {
                if (_content[i] == '\n') lines++;
            }
            TotalLines = lines;

            // Detect Line Ending
            if (_content.Contains("\r\n"))
                LineEnding = "Windows (CRLF)";
            else if (_content.Contains("\n"))
                LineEnding = "Unix (LF)";

            // Count words
            int words = 0;
            bool inWord = false;
            for (int i = 0; i < _content.Length; i++)
            {
                if (char.IsWhiteSpace(_content[i]))
                {
                    inWord = false;
                }
                else if (!inWord)
                {
                    inWord = true;
                    words++;
                }
            }
            WordCount = words;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(storage, value)) return false;
            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
