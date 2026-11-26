using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace NewsCMS.Services.Chatbot;

/// <summary>Routes each chat turn to only the trusted data sources it needs.</summary>
public sealed class ChatOrchestratorService
{
    [Flags]
    public enum DataSources
    {
        None = 0,
        Articles = 1,
        SourceCode = 2,
        LiveInformation = 4
    }

    private readonly OllamaService _ollama;
    private readonly TavilySearchService _tavily;
    private readonly SourceCodeReaderService _sourceReader;
    private readonly DatabaseArticleService _articles;

    public ChatOrchestratorService(
        OllamaService ollama,
        TavilySearchService tavily,
        SourceCodeReaderService sourceReader,
        DatabaseArticleService articles)
    {
        _ollama = ollama;
        _tavily = tavily;
        _sourceReader = sourceReader;
        _articles = articles;
    }

    public async Task<string> AnswerAsync(
        string question,
        IReadOnlyList<ChatHistoryItem> history,
        CancellationToken cancellationToken = default)
    {
        if (ChatContentModerationService.IsDisallowed(question))
            return "Xin lỗi, tôi không hỗ trợ câu hỏi thuộc nội dung này. Bạn có thể đặt câu hỏi khác phù hợp hơn.";

        if (IsRestrictedDataRequest(question))
            return "Xin lỗi, vì lý do bảo mật nên tôi chỉ được phép truy cập dữ liệu bài viết đã hoàn thành và danh mục công khai; không thể truy xuất thông tin tài khoản, nhân viên, độc giả, dữ liệu cá nhân, thông tin công ty hoặc dữ liệu nội bộ khác.";

        string contextQuestion = GetContextQuestion(question, history);
        DataSources sources = ClassifyQuestion(contextQuestion);

        string sourceCode = "";
        string articleData = "";
        string webSearchData = "";

        if (sources.HasFlag(DataSources.SourceCode))
        {
            sourceCode = await _sourceReader.ReadRelevantFilesAsync(
                contextQuestion, cancellationToken);
        }

        if (sources.HasFlag(DataSources.Articles))
        {
            if (sources == DataSources.Articles && IsCountOnlyArticleQuestion(Normalize(contextQuestion)))
            {
                long count = await _articles.GetCompletedArticleCountAsync(
                    cancellationToken);
                return $"Hiện có {count:N0} bài viết đã hoàn thành trên NewsCMS.";
            }

            articleData = await _articles.GetRelevantArticlesAsync(
                contextQuestion, cancellationToken);
        }

        if (sources.HasFlag(DataSources.LiveInformation))
        {
            string searchQuery = sources.HasFlag(DataSources.Articles)
                ? ExtractWebSearchQuery(contextQuestion)
                : contextQuestion;
            try
            {
                webSearchData = await _tavily.SearchAsync(searchQuery, cancellationToken);
                if (string.IsNullOrWhiteSpace(webSearchData))
                {
                    if (!sources.HasFlag(DataSources.Articles) && !sources.HasFlag(DataSources.SourceCode))
                    {
                        return "Tìm kiếm Internet (Tavily) chưa tìm thấy nguồn dữ liệu phù hợp cho câu hỏi này, nên tôi chưa thể xác minh thông tin mới nhất.";
                    }

                    webSearchData = "Tìm kiếm Internet (Tavily) không tìm thấy kết quả phù hợp cho truy vấn này.";
                }
            }
            catch (TavilySearchException ex) when (sources.HasFlag(DataSources.Articles) || sources.HasFlag(DataSources.SourceCode))
            {
                webSearchData = $"Tìm kiếm Internet (Tavily) thất bại: {ex.Message}";
            }
        }

        return await _ollama.GenerateAnswerAsync(
            contextQuestion, history, sourceCode, articleData, webSearchData, cancellationToken);
    }

    /// <summary>Classifies a question without calling SQL, the filesystem, Tavily, or Ollama.</summary>
    public static DataSources ClassifyQuestion(string question)
    {
        string normalized = Normalize(question);
        bool needsArticles = IsArticleRequest(normalized);
        bool needsSource = IsSourceCodeRequest(normalized);
        bool needsLiveInformation = IsCurrentInformationRequest(normalized);

        var sources = DataSources.None;
        if (needsArticles) sources |= DataSources.Articles;
        if (needsSource) sources |= DataSources.SourceCode;
        if (needsLiveInformation) sources |= DataSources.LiveInformation;
        return sources;
    }

    private static string GetContextQuestion(
        string question,
        IReadOnlyList<ChatHistoryItem> history)
    {
        string lastUserTurn = history.LastOrDefault(x => x.Role == "user")?.Content ?? "";
        if (string.IsNullOrWhiteSpace(lastUserTurn))
            return question;

        string normalizedQuestion = Normalize(question);
        int wordCount = normalizedQuestion.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

        // Avoid polluting standalone general-knowledge questions (e.g. "AI là gì?", "Hadoop là gì?")
        // with a prior SQL, source-code, or live-information turn.
        if (IsStandaloneGeneralQuestion(normalizedQuestion))
            return question;

        // Retain the prior topic for short or elliptical follow-up questions.
        if ((wordCount <= 5 || IsFollowUpReference(normalizedQuestion)) &&
            !IsArticleRequest(normalizedQuestion) &&
            !IsSourceCodeRequest(normalizedQuestion) &&
            !IsCurrentInformationRequest(normalizedQuestion))
        {
            return $"{lastUserTurn} {question}".Trim();
        }

        return question;
    }

    private static bool IsStandaloneGeneralQuestion(string text) =>
        ContainsAny(text, "la gi", "khac gi", "khac nhau nhu the nao", "dinh nghia", "khai niem",
            "what is", "difference between") &&
        !IsFollowUpReference(text) &&
        !ContainsAny(text, "newscms", "chatbot", "he thong", "website", "du an", "bai viet", "u17");

    private static bool IsFollowUpReference(string text) =>
        ContainsAny(text,
            "bai do", "bai nay", "bai thu", "bai so", "con bai", "them bai", "chi tiet hon",
            "noi ro hon", "giai thich them", "tom tat lai", "tom tat them", "the con",
            "con gi nua", "duong dan", "link bai", "vua tim", "vua roi", "vua neu", "bai tren",
            "tell me more", "more details", "what about");

    private static bool IsRestrictedDataRequest(string question)
    {
        string text = Normalize(question);
        if (ContainsAny(text,
            "taikhoan", "tai khoan", "nhanvien", "nhan vien", "docgia", "doc gia",
            "lichsuduyet", "lich su duyet", "vaitro", "binhluan",
            "thong tin ca nhan", "du lieu ca nhan", "thong tin cong ty", "thong tin noi bo",
            "mat khau", "password", "ten dang nhap", "connection string", "connectionstring",
            "so dien thoai", "cccd", "cmnd", "select from", "drop table", "delete from",
            "update ", "insert into", "alter table"))
        {
            return true;
        }

        bool mentionsSensitiveEntity = ContainsAny(text, "vai tro", "binh luan", "nguoi dung", "quan tri vien", "tac gia", "admin");
        bool asksDatabaseOrPrivateDetails = ContainsAny(text,
            "bang ", "table ", "csdl", "co so du lieu", "sql", "truy van", "danh sach",
            "du lieu", "email", "ho so", "thong tin", "bao nhieu");

        return mentionsSensitiveEntity && asksDatabaseOrPrivateDetails;
    }

    private static bool IsArticleRequest(string text)
    {
        bool mentionsArticleWord = ContainsAny(text,
            "bai viet", "bai bao", "tin tuc", "tin moi", "article", "articles", "posts");
        if (!mentionsArticleWord)
            return false;

        // World/Internet-only news queries must not be misclassified as NewsCMS SQL queries.
        bool hasNewsCmsContext = ContainsAny(text,
            "newscms", "website", "web site", "trang web", "tren web", "tren trang", "co so du lieu", "danh muc");
        bool mentionsExplicitCmsArticle = ContainsAny(text, "bai viet", "bai bao", "article", "articles", "posts");
        bool asksExternalWorldOnly = ContainsAny(text,
            "tren the gioi", "the gioi moi nhat", "tren internet", "tren mang", "world news") &&
            !hasNewsCmsContext;
        if (asksExternalWorldOnly)
            return false;

        if (!hasNewsCmsContext && !mentionsExplicitCmsArticle &&
            ContainsAny(text, "tin tuc ai moi", "tin tuc moi nhat", "tin moi nhat", "tin nong", "thoi su"))
            return false;

        return ContainsAny(text,
            "newscms", "website", "web site", "trang web", "tren web", "tren trang", "co so du lieu",
            "bai viet da dang", "bai viet da xuat ban", "bai viet nao", "bai nao",
            "bai viet hoan thanh", "bai viet da hoan thanh", "bao nhieu bai viet", "tong so bai viet",
            "how many articles", "how many posts", "number of articles", "count articles",
            "tim bai", "tim kiem bai", "liet ke bai", "danh sach bai", "tom tat bai",
            "noi dung bai", "tieu de bai", "doc bai viet", "xem bai viet", "bai viet moi nhat",
            "bai moi nhat", "tin moi nhat", "tin tuc moi nhat", "goi y", "dang doc",
            "danh muc", "chu de", "co bai", "cong nghe", "the thao", "giao duc",
            "kinh te", "suc khoe", "doi song", "khoa hoc", "du lich", "moi truong",
            "which articles", "find article", "summarize article", "latest article");
    }

    private static bool IsCountOnlyArticleQuestion(string text)
    {
        bool asksCount = ContainsAny(text, "bao nhieu", "tong so", "so luong", "dem so",
            "how many", "number of articles", "count articles", "how many posts");
        bool alsoRequestsDetails = ContainsAny(text,
            "liet ke", "danh sach", "tim bai", "tom tat", "noi dung", "tieu de");
        return asksCount && !alsoRequestsDetails;
    }

    private static bool IsSourceCodeRequest(string text)
    {
        if (ContainsAny(text,
            "chatcontroller", "chatorchestratorservice", "ollamaservice", "tavilysearchservice",
            "databasearticleservice", "sourcecodereaderservice", "chatcontentmoderationservice"))
            return true;

        bool asksAboutChatbotCode = ContainsAny(text, "ma nguon", "source code", "codebase", "code") &&
            ContainsAny(text, "chatbot", "newscms", "du an", "he thong", "website", "tro ly");
        if (asksAboutChatbotCode)
            return true;

        bool projectContext = ContainsAny(text,
            "newscms", "du an", "ma nguon", "source code", "trong code", "codebase",
            "repo", "code cua toi", "file nay", "ung dung nay", "he thong nay",
            "chatbot", "in this project", "in this repo", "in this codebase");
        bool codeSubject = ContainsAny(text,
            "controller", "model", "view", "service", "api", "endpoint", "luong xu ly",
            "cau truc", "class", "method", "file", "debug", "loi bien dich", "hoat dong", "xu ly");
        bool asksAboutCode = ContainsAny(text,
            "la gi", "lam gi", "nhiem vu", "hoat dong", "xu ly", "hoat dong nhu the nao",
            "duoc goi", "duoc dang ky", "nam o dau", "dien ra nhu the nao", "giai thich",
            "how does", "what does", "where is");

        return projectContext && codeSubject && asksAboutCode;
    }

    private static bool IsCurrentInformationRequest(string text)
    {
        bool asksAboutNewsCms = ContainsAny(text,
            "newscms", "website", "web site", "trang web", "tren web", "tren trang",
            "co so du lieu", "bai viet", "bai bao", "danh muc");
        bool comparesWithExternalNews = ContainsAny(text, "so sanh", "doi chieu") &&
            ContainsAny(text, "tin tuc", "tin moi", "internet", "the gioi", "thi truong", "gia ");

        // A. Nhóm giá cả, tỷ giá, thị trường & B. Thể thao trực tiếp & D. Thời tiết / chính sách / thời gian thực.
        bool hasPriceOrMarketIntent =
            ContainsAny(text,
                "ty gia", "ti gia", "ngoai te", "usd vnd", "eur vnd",
                "gia vang", "gia xang", "gia dau", "gia lon", "gia heo", "gia bo", "gia ga",
                "gia ca phe", "gia tieu", "gia gao", "gia lua", "gia nong san", "gia thuy san",
                "gia dien", "gia chung khoan", "chung khoan hom nay", "vn index", "vnindex",
                "gia bitcoin", "gia coin", "gia san pham", "gia ca thi truong") ||
            (ContainsWholeWord(text, "gia") &&
             !ContainsAny(text, "tac gia", "danh gia", "quoc gia", "gia tri", "tham gia", "gia dinh", "chuyen gia", "gia su", "giai", "gia lap") &&
             ContainsAny(text,
                 "hom nay", "hien nay", "hien tai", "moi nhat", "bay gio", "luc nay",
                 "bao nhieu", "thi truong", "mien bac", "mien nam", "mien trung",
                 "vang", "xang", "dau", "lon", "heo", "nong san", "ca phe", "gao", "usd", "san pham"));

        bool hasSportsLiveIntent =
            ContainsAny(text,
                "ket qua bong da", "ket qua tran", "ket qua thi dau", "ket qua u17", "ket qua u23",
                "u17 viet nam", "u23 viet nam", "lich thi dau", "ty so", "ti so",
                "bang xep hang", "ngoai hang anh", "champions league", "world cup",
                "v league", "aff cup", "asean cup", "sea games",
                "latest football result", "upcoming fixtures");

        bool hasWeatherIntent =
            ContainsAny(text,
                "thoi tiet", "du bao thoi tiet", "nhiet do hom nay", "troi mua", "co mua khong",
                "bao so", "ap thap nhiet doi", "weather today");

        bool hasExplicitExternalLiveTopic =
            hasPriceOrMarketIntent ||
            hasSportsLiveIntent ||
            hasWeatherIntent ||
            ContainsAny(text,
                "tren internet", "tren mang", "ngoai internet",
                "tren the gioi", "ai moi nhat tren the gioi", "the gioi moi nhat",
                "cong bo hom nay", "ra mat hom nay", "moi duoc cong bo", "moi ban hanh",
                "su kien moi xay ra", "vua xay ra", "moi xay ra", "chinh sach moi",
                "tinh hinh kinh te hien nay", "luat moi", "nghi dinh moi", "muc phat moi",
                "latest ai news", "latest news", "current exchange rate");

        if (hasExplicitExternalLiveTopic)
        {
            // Nếu câu hỏi chỉ hỏi bài viết trên NewsCMS (ví dụ: "bài viết về chính sách mới trên NewsCMS")
            // và không có từ khóa so sánh/Internet/thị trường/thể thao/thời tiết thực tế thì giữ ở SQL.
            bool pureNewsCmsArticleQuery = asksAboutNewsCms && !comparesWithExternalNews &&
                !hasPriceOrMarketIntent && !hasSportsLiveIntent && !hasWeatherIntent &&
                !ContainsAny(text, "tren internet", "tren mang", "tren the gioi");
            return !pureNewsCmsArticleQuery;
        }

        if (comparesWithExternalNews)
            return true;

        if (asksAboutNewsCms)
            return false;

        // Dấu hiệu ý định tìm kiếm thông tin biến động theo thời gian hoặc sự kiện bên ngoài nói chung.
        bool hasTemporalSignal = ContainsAny(text,
            "hom nay", "hien nay", "hien tai", "moi nhat", "vua moi", "gan day",
            "bay gio", "luc nay", "tuan nay", "thang nay", "dang dien ra", "cap nhat");
        bool hasMutableWorldSubject = ContainsAny(text,
            "tin tuc", "tin nong", "thoi su", "su kien", "tinh hinh", "kinh te",
            "chinh sach", "luat", "nghi dinh", "quy dinh", "thi truong", "lai suat",
            "cong nghe moi", "ai", "san pham");

        return (hasTemporalSignal && hasMutableWorldSubject) ||
            ContainsAny(text, "tin tuc the gioi", "tin tuc ai moi", "tin tuc moi nhat");
    }

    private static bool ContainsWholeWord(string text, string word) =>
        Regex.IsMatch(text, $@"(?<![a-z0-9_]){Regex.Escape(word)}(?![a-z0-9_])", RegexOptions.CultureInvariant);

    private static string ExtractWebSearchQuery(string question)
    {
        var splitMatch = Regex.Match(
            question,
            @"(?i)\b(?:với|và\s+cập\s+nhật|và\s+tìm|và\s+tra\s+cứu)\b\s+(.+)$");
        if (splitMatch.Success && splitMatch.Groups[1].Value.Trim().Length >= 8)
        {
            return splitMatch.Groups[1].Value.Trim().TrimEnd('.', '?', '!');
        }

        string cleaned = Regex.Replace(
            question,
            @"(?i)\b(trên\s+newscms|trong\s+newscms|trên\s+website|so\s+sánh)\b",
            " ");
        cleaned = Regex.Replace(cleaned, @"\s+", " ").Trim();
        return string.IsNullOrWhiteSpace(cleaned) ? question : cleaned;
    }

    private static bool ContainsAny(string text, params string[] phrases) =>
        phrases.Any(text.Contains);

    private static string Normalize(string text)
    {
        string decomposed = text.ToLowerInvariant().Replace('đ', 'd').Normalize(NormalizationForm.FormD);
        var result = new StringBuilder(decomposed.Length);
        foreach (char c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                result.Append(c);
        }

        return Regex.Replace(result.ToString().Normalize(NormalizationForm.FormC),
            @"[^a-z0-9_]+", " ").Trim();
    }
}
