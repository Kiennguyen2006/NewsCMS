
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;

namespace NewsCMS.Services.Chatbot
{
    public class DatabaseArticleService
    {
        private readonly IConfiguration _configuration;

        public DatabaseArticleService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<long> GetCompletedArticleCountAsync(
            CancellationToken cancellationToken = default)
        {
            string connectionString =
                _configuration.GetConnectionString("DefaultConnection") ?? "";

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Không tìm thấy ConnectionStrings:DefaultConnection.");
            }

            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            using var command = new SqlCommand(
                "SELECT COUNT_BIG(1) FROM dbo.BaiViet WHERE TrangThai = N'Hoàn Thành'",
                connection);

            object? result = await command.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt64(result, CultureInfo.InvariantCulture);
        }

        private static readonly HashSet<string> StopWords = new(StringComparer.Ordinal)
        {
            "tim", "kiem", "hay", "bai", "viet", "bao", "tin", "tuc", "newscms", "website",
            "web", "site", "trang", "danh", "muc", "chu", "de", "moi", "nhat", "gan", "day",
            "tom", "tat", "noi", "dung", "chinh", "cua", "tung", "nhung", "cac", "co", "nao",
            "khong", "cho", "minh", "toi", "ban", "xem", "doc", "liet", "ke", "goi", "dang",
            "hom", "nay", "tieu", "hoan", "thanh", "da", "xuat", "cap", "nhat", "ket", "qua",
            "tran", "dau", "sau", "do", "ve", "va", "voi", "trong", "tren", "theo", "la", "gi",
            "bao", "nhieu", "tong", "so", "luong", "dem", "tai", "sao", "nhu", "the", "mot",
            "vai", "hon", "nua", "khac", "u17", "lich", "thi", "tiep", "hoac", "giup", "duoc",
            "phai", "neu", "can", "nhe", "biet", "hoi", "tra", "loi", "nhieu"
        };

        public async Task<string> GetRelevantArticlesAsync(
            string question,
            CancellationToken cancellationToken = default)
        {
            // Sử dụng đúng ConnectionString đã khai báo
            string connectionString =
                _configuration.GetConnectionString("DefaultConnection") ?? "";

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Không tìm thấy ConnectionStrings:DefaultConnection.");
            }

            string normalizedQuestion = Normalize(question);

            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            var activeCategories = new List<(int Id, string Name, string NormalizedName)>();
            using (var catCommand = new SqlCommand(
                "SELECT MaDanhMuc, TenDanhMuc FROM dbo.DanhMuc WHERE TrangThai = 1",
                connection))
            using (var catReader = await catCommand.ExecuteReaderAsync(cancellationToken))
            {
                while (await catReader.ReadAsync(cancellationToken))
                {
                    int catId = Convert.ToInt32(catReader["MaDanhMuc"], CultureInfo.InvariantCulture);
                    string catName = Convert.ToString(catReader["TenDanhMuc"]) ?? "";
                    string normalizedCat = Normalize(catName);
                    if (!string.IsNullOrWhiteSpace(normalizedCat))
                    {
                        activeCategories.Add((catId, catName, normalizedCat));
                    }
                }
            }

            var matchedCategories = activeCategories
                .Where(c => ContainsPhrase(normalizedQuestion, c.NormalizedName))
                .ToList();

            var categoryWords = matchedCategories
                .SelectMany(c => c.NormalizedName.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                .ToHashSet(StringComparer.Ordinal);

            string[] keywords = normalizedQuestion
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(x => (x.Length >= 3 || x == "ai") && !StopWords.Contains(x) && !categoryWords.Contains(x))
                .Distinct()
                .ToArray();

            bool isLatestOrBrowseQuestion =
                normalizedQuestion.Contains("moi nhat") ||
                normalizedQuestion.Contains("tin moi") ||
                normalizedQuestion.Contains("bai viet moi") ||
                normalizedQuestion.Contains("gan day") ||
                normalizedQuestion.Contains("goi y") ||
                normalizedQuestion.Contains("dang doc");

            bool asksSpecificTitle = normalizedQuestion.Contains("tieu de");
            string[] titleSearchKeywords = Array.Empty<string>();
            if (asksSpecificTitle)
            {
                int idx = normalizedQuestion.IndexOf("tieu de", StringComparison.Ordinal);
                string afterTitle = normalizedQuestion[(idx + "tieu de".Length)..];
                titleSearchKeywords = afterTitle
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Where(x => (x.Length >= 3 || x == "ai") && !StopWords.Contains(x))
                    .Distinct()
                    .ToArray();
            }
            var sqlBuilder = new StringBuilder(@"
                SELECT TOP (60)
                    b.MaBaiViet,
                    b.TieuDe,
                    ISNULL(b.TomTat, N'') AS TomTat,
                    ISNULL(b.NoiDung, N'') AS NoiDung,
                    b.NgayTao,
                    b.NgayXuatBan,
                    d.TenDanhMuc
                FROM dbo.BaiViet b
                INNER JOIN dbo.DanhMuc d ON b.MaDanhMuc = d.MaDanhMuc
                WHERE b.TrangThai = N'Hoàn Thành'
                  AND d.TrangThai = 1");

            using var command = new SqlCommand();
            command.Connection = connection;

            if (matchedCategories.Count > 0 && !asksSpecificTitle)
            {
                var paramNames = new List<string>();
                for (int i = 0; i < matchedCategories.Count; i++)
                {
                    string paramName = $"@Cat{i}";
                    paramNames.Add(paramName);
                    command.Parameters.AddWithValue(paramName, matchedCategories[i].Id);
                }

                sqlBuilder.Append($" AND b.MaDanhMuc IN ({string.Join(", ", paramNames)})");
            }

            sqlBuilder.Append(" ORDER BY b.NgayTao DESC");
            command.CommandText = sqlBuilder.ToString();

            var articles = new List<ArticleItem>();
            using (var reader = await command.ExecuteReaderAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    string rawContent = Convert.ToString(reader["NoiDung"]) ?? "";
                    string content = Regex.Replace(rawContent, "<.*?>", " ");
                    content = Regex.Replace(content, @"\s+", " ").Trim();
                    if (content.Length > 2500)
                        content = content[..2500];

                    articles.Add(new ArticleItem
                    {
                        Id = Convert.ToInt32(reader["MaBaiViet"], CultureInfo.InvariantCulture),
                        Title = Convert.ToString(reader["TieuDe"]) ?? "",
                        CategoryName = Convert.ToString(reader["TenDanhMuc"]) ?? "",
                        Summary = Convert.ToString(reader["TomTat"]) ?? "",
                        Content = content,
                        CreatedAt = reader["NgayTao"] == DBNull.Value
                            ? null
                            : Convert.ToDateTime(reader["NgayTao"], CultureInfo.InvariantCulture),
                        PublishedAt = reader["NgayXuatBan"] == DBNull.Value
                            ? null
                            : Convert.ToDateTime(reader["NgayXuatBan"], CultureInfo.InvariantCulture)
                    });
                }
            }

            bool isCategoryLatestOnly =
                matchedCategories.Count > 0 &&
                !asksSpecificTitle &&
                (isLatestOrBrowseQuestion || keywords.Length == 0 || normalizedQuestion.Contains("danh muc"));

            var scored = articles
                .Select(a =>
                {
                    string title = Normalize(a.Title);
                    string summary = Normalize(a.Summary);
                    string content = Normalize(a.Content);
                    string category = Normalize(a.CategoryName);

                    int score = 0;
                    int matchedKeywordCount = 0;
                    bool matchedTitle = false;

                    int matchedInTitleCount = titleSearchKeywords.Count(k => ContainsKeyword(title, k));

                    foreach (string keyword in keywords)
                    {
                        bool inTitle = ContainsKeyword(title, keyword);
                        bool inSummary = ContainsKeyword(summary, keyword);
                        bool inContent = ContainsKeyword(content, keyword);
                        bool inCategory = ContainsKeyword(category, keyword);

                        if (inTitle)
                        {
                            score += 4;
                            matchedTitle = true;
                            matchedKeywordCount++;
                        }
                        else if (inSummary)
                        {
                            score += 2;
                            matchedKeywordCount++;
                        }
                        else if (inContent || inCategory)
                        {
                            score += 1;
                            matchedKeywordCount++;
                        }
                    }

                    return new
                    {
                        Article = a,
                        Score = score,
                        MatchedKeywordCount = matchedKeywordCount,
                        MatchedTitle = matchedTitle,
                        MatchedInTitleCount = matchedInTitleCount
                    };
                })
                .ToList();

            var matched = scored
                .Where(x =>
                {
                    if (isCategoryLatestOnly)
                        return true;

                    if (matchedCategories.Count == 0 && isLatestOrBrowseQuestion && keywords.Length == 0)
                        return true;

                    if (asksSpecificTitle && titleSearchKeywords.Length > 0)
                    {
                        int requiredTitleHits = Math.Max(1, (titleSearchKeywords.Length + 1) / 2);
                        return x.MatchedInTitleCount >= requiredTitleHits;
                    }

                    if (keywords.Length >= 2)
                        return x.MatchedKeywordCount >= 2 || x.MatchedTitle;

                    return x.Score > 0;
                })
                .OrderByDescending(x => isLatestOrBrowseQuestion ? 0 : x.Score)
                .ThenByDescending(x => x.Article.CreatedAt ?? DateTime.MinValue)
                .Take(5)
                .ToList();

            long? requestedCount = null;
            bool asksCount = normalizedQuestion.Contains("bao nhieu") ||
                normalizedQuestion.Contains("tong so") ||
                normalizedQuestion.Contains("so luong") ||
                normalizedQuestion.Contains("dem so");
            if (asksCount)
            {
                using var countCommand = new SqlCommand(
                    "SELECT COUNT_BIG(1) FROM dbo.BaiViet WHERE TrangThai = N'Hoàn Thành'",
                    connection);
                requestedCount = Convert.ToInt64(
                    await countCommand.ExecuteScalarAsync(cancellationToken),
                    CultureInfo.InvariantCulture);
            }

            if (!matched.Any())
            {
                string noMatch = "Không tìm thấy bài viết đã hoàn thành phù hợp " +
                    "trong cơ sở dữ liệu NewsCMS.";
                return requestedCount.HasValue
                    ? $"Tổng số bài viết đã hoàn thành: {requestedCount.Value:N0}.\n{noMatch}"
                    : noMatch;
            }

            var result = new StringBuilder();
            if (requestedCount.HasValue)
            {
                result.AppendLine(
                    $"Tổng số bài viết đã hoàn thành (đếm trực tiếp): {requestedCount.Value:N0}");
            }

            if (matchedCategories.Count > 0 && !asksSpecificTitle)
            {
                result.AppendLine(
                    $"Danh mục đã lọc từ dbo.DanhMuc: {string.Join(", ", matchedCategories.Select(c => c.Name))} (chỉ lấy tối đa 5 bài thực sự thuộc danh mục này).");
            }

            foreach (var item in matched)
            {
                var a = item.Article;

                result.AppendLine("Mã bài viết: " + a.Id);
                result.AppendLine("Danh mục: " + a.CategoryName);
                result.AppendLine("Tiêu đề: " + a.Title);
                result.AppendLine("Ngày tạo: " + a.CreatedAt?.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
                if (a.PublishedAt.HasValue)
                    result.AppendLine("Ngày xuất bản: " + a.PublishedAt.Value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
                result.AppendLine("Tóm tắt: " + a.Summary);
                result.AppendLine("Nội dung: " + a.Content);
                result.AppendLine("Đường dẫn: /Giaodienbaiviet/ChiTiet/" + a.Id);
                result.AppendLine();
            }

            return result.ToString();
        }

        private static string Normalize(string text)
        {
            string value = (text ?? "").ToLowerInvariant()
                .Replace('đ', 'd')
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
                result.ToString().Normalize(
                    NormalizationForm.FormC),
                @"[^a-z0-9_]+", " ").Trim();
        }

        private static bool ContainsPhrase(string text, string phrase)
        {
            if (string.IsNullOrWhiteSpace(phrase))
                return false;

            return Regex.IsMatch(
                text,
                $@"(?<![a-z0-9_]){Regex.Escape(phrase)}(?![a-z0-9_])",
                RegexOptions.CultureInvariant);
        }

        private static bool ContainsKeyword(string text, string keyword)
        {
            if (keyword == "ai")
            {
                return Regex.IsMatch(
                    text,
                    @"(?<![a-z0-9_])ai(?![a-z0-9_])");
            }

            return ContainsPhrase(text, keyword);
        }

        private class ArticleItem
        {
            public int Id { get; set; }
            public string Title { get; set; } = "";
            public string CategoryName { get; set; } = "";
            public string Summary { get; set; } = "";
            public string Content { get; set; } = "";
            public DateTime? CreatedAt { get; set; }
            public DateTime? PublishedAt { get; set; }
        }
    }
}
