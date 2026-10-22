using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;

namespace NotepadByFerryPot.Services
{
    public static class SafeLinkService
    {
        public static void ConfigureHyperlink(Hyperlink hyperlink, string url)
        {
            hyperlink.ToolTip = string.IsNullOrWhiteSpace(url)
                ? "Ctrl+Click to open link"
                : $"{url}\nCtrl+Click to open link";

            hyperlink.RequestNavigate += (sender, e) =>
            {
                e.Handled = true;

                // Open only with Ctrl+Click
                bool isCtrlPressed = (Keyboard.Modifiers & ModifierKeys.Control) != 0;
                if (!isCtrlPressed)
                {
                    return;
                }

                string target = e.Uri?.OriginalString ?? url;
                Open(target);
            };
        }

        public static void Open(string rawUrl)
        {
            if (string.IsNullOrWhiteSpace(rawUrl))
            {
                MessageBox.Show("The link URL is empty.", "Invalid Link", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string trimmed = rawUrl.Trim();

            // Try to resolve as an absolute URI
            Uri? uri;
            if (!Uri.TryCreate(trimmed, UriKind.Absolute, out uri))
            {
                // Prepend https:// if no scheme is specified (e.g. "github.com")
                if (!trimmed.Contains("://") && !trimmed.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
                {
                    Uri.TryCreate("https://" + trimmed, UriKind.Absolute, out uri);
                }
            }

            if (uri == null)
            {
                MessageBox.Show($"\"{trimmed}\" is not a valid web URL format.", "Invalid Link", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Only allow http, https, and mailto
            string scheme = uri.Scheme.ToLowerInvariant();
            if (scheme != "http" && scheme != "https" && scheme != "mailto")
            {
                MessageBox.Show(
                    $"The link was blocked for your security:\n\n{trimmed}\n\nOnly web links (http, https) and email links (mailto) can be opened.",
                    "Blocked Link Protocol",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // Block UNC paths (e.g. \\server\share)
            if (uri.IsUnc)
            {
                MessageBox.Show(
                    "Network share (UNC) links are blocked for security.",
                    "Blocked Link",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = uri.AbsoluteUri,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Could not open the link in your web browser:\n\n{ex.Message}",
                    "Unable to Open Link",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
    }
}
