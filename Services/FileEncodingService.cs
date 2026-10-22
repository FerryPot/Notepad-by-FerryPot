using System;
using System.IO;
using System.Text;

namespace NotepadByFerryPot.Services
{
    public static class FileEncodingService
    {
        public static (string content, Encoding encoding) ReadFileWithEncoding(string filePath)
        {
            byte[] bytes = File.ReadAllBytes(filePath);
            if (bytes.Length == 0)
            {
                return (string.Empty, new UTF8Encoding(false));
            }

            // 1. Check for BOMs
            // UTF-8 BOM: EF BB BF
            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            {
                var enc = new UTF8Encoding(true);
                return (enc.GetString(bytes, 3, bytes.Length - 3), enc);
            }

            // UTF-16 LE BOM: FF FE
            if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
            {
                var enc = Encoding.Unicode;
                return (enc.GetString(bytes, 2, bytes.Length - 2), enc);
            }

            // UTF-16 BE BOM: FE FF
            if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
            {
                var enc = Encoding.BigEndianUnicode;
                return (enc.GetString(bytes, 2, bytes.Length - 2), enc);
            }

            // 2. No BOM: Test if valid UTF-8
            var strictUtf8 = new UTF8Encoding(false, throwOnInvalidBytes: true);
            try
            {
                string text = strictUtf8.GetString(bytes);
                return (text, new UTF8Encoding(false));
            }
            catch (DecoderFallbackException)
            {
                // 3. Fallback to system ANSI code page
                Encoding ansiEncoding;
                try
                {
                    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                    ansiEncoding = Encoding.GetEncoding(0);
                }
                catch
                {
                    ansiEncoding = Encoding.Latin1;
                }

                string text = ansiEncoding.GetString(bytes);
                return (text, ansiEncoding);
            }
        }
    }
}
