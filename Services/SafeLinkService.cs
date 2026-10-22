using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;

namespace NotepadByFerryPot.Services
{
    public static class SafeLinkService
    {
        public static bool TryNormalizeAndValidateUrl(string? input, [NotNullWhen(true)] out Uri? validUri, out string? errorMessage)
        {
            validUri = null;
            errorMessage = null;

            if (string.IsNullOrWhiteSpace(input))
            {
                errorMessage = "Please enter a valid web address or email.";
                return false;
            }

            string trimmed = input.Trim();

            // Detect email address without scheme (e.g. user@domain.com)
            if (!trimmed.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) &&
                trimmed.Contains('@') && !trimmed.Contains('/') && !trimmed.Contains(':'))
            {
                trimmed = "mailto:" + trimmed;
            }
            // If no scheme is present, check if it looks like a domain name (e.g. google.com or www.site.com)
            else if (!trimmed.Contains("://") && !trimmed.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
            {
                // Must not contain spaces and must contain at least one dot
                if (!trimmed.Contains(' ') && trimmed.Contains('.'))
                {
                    trimmed = "https://" + trimmed;
                }
            }

            if (!Uri.TryCreate(trimmed, UriKind.Absolute, out Uri? uri))
            {
                errorMessage = "Please enter a valid web address (e.g. https://example.com).";
                return false;
            }

            string scheme = uri.Scheme.ToLowerInvariant();
            if (scheme != "http" && scheme != "https" && scheme != "mailto")
            {
                errorMessage = "Only web links (http, https) and email links (mailto) are allowed.";
                return false;
            }

            if (uri.IsUnc)
            {
                errorMessage = "Network share (UNC) links are blocked for security.";
                return false;
            }

            // For http/https, verify that the host is non-empty and well-formed
            if (scheme == "http" || scheme == "https")
            {
                if (string.IsNullOrWhiteSpace(uri.Host) || uri.Host.Length < 1)
                {
                    errorMessage = "Please enter a valid web address with a hostname.";
                    return false;
                }

                if (!uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) && !uri.Host.Contains('.'))
                {
                    errorMessage = "Please enter a valid domain name (e.g. example.com).";
                    return false;
                }
            }

            validUri = uri;
            return true;
        }

        public static bool TryNormalizeAndValidateUrl(string? input, [NotNullWhen(true)] out Uri? validUri)
        {
            return TryNormalizeAndValidateUrl(input, out validUri, out _);
        }

        public static void ConfigureHyperlink(Hyperlink hyperlink, string url)
        {
            hyperlink.ToolTip = string.IsNullOrWhiteSpace(url)
                ? "Ctrl+Click to open link"
                : $"Ctrl+Click to open link: {url}";

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
            if (!TryNormalizeAndValidateUrl(rawUrl, out Uri? uri, out string? errorMessage))
            {
                MessageBox.Show(
                    errorMessage ?? "The link is invalid or blocked for security.",
                    "Invalid Link",
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
