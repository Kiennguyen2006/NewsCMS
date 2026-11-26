
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using Microsoft.Extensions.Configuration;

namespace NewsCMS.Services.Chatbot
{
    public class SourceCodeReaderService
    {
        private readonly IConfiguration _configuration;

        private static readonly HashSet<string> AllowedExtensions =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ".cs", ".cshtml"
            };

        private static readonly HashSet<string> ExcludedFolders =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ".vs", ".git", "bin", "obj", "node_modules",
                "packages", "images", "uploads", "lib", "Areas", "Data"
            };

        public SourceCodeReaderService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<string> ReadRelevantFilesAsync(
            string question,
            CancellationToken cancellationToken = default)
        {
            string root = ResolveProjectRoot();

            if (string.IsNullOrWhiteSpace(root) ||
                !Directory.Exists(root))
            {
                return "Chưa cấu hình đúng đường dẫn thư mục mã nguồn.";
            }

            var keywords = GetKeywords(question);

            if (!keywords.Any())
                return "Không tìm thấy từ khóa phù hợp để tìm mã nguồn.";

            var results = new List<(string Path, int Score, string Content)>();

            foreach (string file in EnumerateSourceFiles(root))
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var info = new FileInfo(file);

                    if (!info.Exists || info.Length > 200 * 1024)
                        continue;

                    string content = await File.ReadAllTextAsync(
                        file, cancellationToken);

                    string normalizedContent = Normalize(content);
                    string normalizedName = Normalize(
                        Path.GetFileNameWithoutExtension(file));

                    int score = keywords.Count(k =>
                        normalizedName.Contains(k));

                    score += keywords.Count(k =>
                        normalizedContent.Contains(k));

                    if (score > 0)
                    {
                        content = RedactPossibleSecrets(content);

                        string relativePath = Path.GetRelativePath(root, file);

                        results.Add((relativePath, score, content));
                    }
                }
                catch
                {
                    // Bỏ qua tệp không đọc được.
                }
            }

            var selected = results
                .OrderByDescending(x => x.Score)
                .Take(8)
                .ToList();

            if (!selected.Any())
                return "Không tìm thấy tệp mã nguồn liên quan đến câu hỏi.";

            var output = new StringBuilder();
            int totalCharacters = 0;

            foreach (var item in selected)
            {
                string content = item.Content;

                if (content.Length > 9000)
                    content = content.Substring(0, 9000);

                if (totalCharacters + content.Length > 40000)
                    break;

                output.AppendLine();
                output.AppendLine("===== TỆP: " + item.Path + " =====");
                output.AppendLine(content);

                totalCharacters += content.Length;
            }

            return output.ToString();
        }

        private static IEnumerable<string> EnumerateSourceFiles(string root)
        {
            var folders = new Stack<string>();
            folders.Push(root);

            while (folders.Count > 0)
            {
                string current = folders.Pop();

                string[] files;
                string[] directories;

                try
                {
                    files = Directory.GetFiles(current);
                    directories = Directory.GetDirectories(current);
                }
                catch
                {
                    continue;
                }

                foreach (string file in files)
                {
                    if (!AllowedExtensions.Contains(Path.GetExtension(file)))
                        continue;

                    string relativePath = Path.GetRelativePath(root, file).Replace('\\', '/');
                    if (!IsChatbotRelevantSourceFile(relativePath))
                        continue;

                    yield return file;
                }

                foreach (string directory in directories)
                {
                    try
                    {
                        if ((File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0)
                            continue;
                    }
                    catch
                    {
                        continue;
                    }

                    string name = Path.GetFileName(directory);

                    if (!ExcludedFolders.Contains(name))
                        folders.Push(directory);
                }
            }
        }

        private static bool IsChatbotRelevantSourceFile(string relativePath)
        {
            string name = Path.GetFileName(relativePath);
            if (name.Contains("secret", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("credential", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("taikhoan", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("nhanvien", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("docgia", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("hoso", StringComparison.OrdinalIgnoreCase) ||
                name.EndsWith(".user", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            return relativePath.StartsWith("Services/Chatbot/", StringComparison.OrdinalIgnoreCase) ||
                   relativePath.Equals("Controllers/ChatController.cs", StringComparison.OrdinalIgnoreCase) ||
                   relativePath.Equals("Controllers/GiaodienbaivietController.cs", StringComparison.OrdinalIgnoreCase) ||
                   relativePath.Equals("Program.cs", StringComparison.OrdinalIgnoreCase);
        }

        private string ResolveProjectRoot()
        {
            var candidates = new List<string>();
            string configuredRoot = _configuration["Chatbot:SourceRoot"] ?? "";
            if (!string.IsNullOrWhiteSpace(configuredRoot))
            {
                candidates.Add(configuredRoot);
                candidates.Add(Path.Combine(configuredRoot, "NewsCMS", "NewsCMS"));
            }

            foreach (string start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
            {
                if (string.IsNullOrWhiteSpace(start))
                    continue;

                var dir = new DirectoryInfo(start);
                for (int i = 0; i < 6 && dir != null; i++, dir = dir.Parent)
                {
                    candidates.Add(dir.FullName);
                }
            }

            foreach (string candidate in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(candidate))
                    continue;

                try
                {
                    string fullPath = Path.GetFullPath(candidate);
                    if (Directory.Exists(fullPath) &&
                        File.Exists(Path.Combine(fullPath, "Program.cs")) &&
                        File.Exists(Path.Combine(fullPath, "NewsCMS.csproj")))
                        return fullPath;
                }
                catch
                {
                    // Skip invalid or inaccessible roots; never broaden the search.
                }
            }

            return "";
        }

        private static List<string> GetKeywords(string text)
        {
            string normalized = Normalize(text);

            return normalized
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(x => x.Length >= 3)
                .Distinct()
                .ToList();
        }

        private static string Normalize(string text)
        {
            string value = text.ToLowerInvariant()
                .Normalize(NormalizationForm.FormD);

            var result = new StringBuilder();

            foreach (char c in value)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) !=
                    UnicodeCategory.NonSpacingMark)
                {
                    result.Append(c);
                }
            }

            return Regex.Replace(
                result.ToString()
                    .Normalize(NormalizationForm.FormC),
                @"[^a-z0-9_]+", " ");
        }

        private static string RedactPossibleSecrets(string content)
        {
            string redacted = Regex.Replace(
                content,
                @"(?i)(api[_-]?key|password|secret|token|connectionstring)\s*[:=]\s*[""'][^""']*[""']",
                "$1=\"[REDACTED]\"");
            redacted = Regex.Replace(
                redacted,
                @"(?i)Server=[^;""']+;Database=[^""']+",
                "[REDACTED_CONNECTION_STRING]");
            return redacted;
        }
    }
}
