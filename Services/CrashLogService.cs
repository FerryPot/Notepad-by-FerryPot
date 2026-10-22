using System;
using System.IO;
using System.Text;

namespace NotepadByFerryPot.Services
{
    public static class CrashLogService
    {
        public static string LogsDirectory => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "NotepadByFerryPot",
            "logs"
        );

        public static string LogError(string context, Exception ex)
        {
            try
            {
                Directory.CreateDirectory(LogsDirectory);
                string fileName = $"error_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}.log";
                string fullPath = Path.Combine(LogsDirectory, fileName);

                var sb = new StringBuilder();
                sb.AppendLine("=== Notepad by FerryPot Diagnostic Log ===");
                sb.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                sb.AppendLine($"OS Version: {Environment.OSVersion}");
                sb.AppendLine($".NET Runtime: {Environment.Version}");
                sb.AppendLine($"64-bit OS: {Environment.Is64BitOperatingSystem}, 64-bit Process: {Environment.Is64BitProcess}");
                sb.AppendLine($"Context: {context}");
                sb.AppendLine();
                sb.AppendLine("=== Exception Summary ===");
                sb.AppendLine($"Type: {ex.GetType().FullName}");
                sb.AppendLine($"Message: {ex.Message}");
                sb.AppendLine();
                sb.AppendLine("=== Stack Trace ===");
                sb.AppendLine(ex.StackTrace);

                if (ex.InnerException != null)
                {
                    sb.AppendLine();
                    sb.AppendLine("=== Inner Exception ===");
                    sb.AppendLine($"Type: {ex.InnerException.GetType().FullName}");
                    sb.AppendLine($"Message: {ex.InnerException.Message}");
                    sb.AppendLine($"Stack Trace: {ex.InnerException.StackTrace}");
                }

                File.WriteAllText(fullPath, sb.ToString(), Encoding.UTF8);
                return fullPath;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
