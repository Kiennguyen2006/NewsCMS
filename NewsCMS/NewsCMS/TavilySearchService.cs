using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace NewsCMS.Services.Chatbot;

/// <summary>Performs live Internet searches via the Tavily Search API.</summary>
public sealed class TavilySearchService
{
    private const string SearchEndpoint = "https://api.tavily.com/search";
    private const string SearchDepth = "basic";
    private const int MaxResults = 5;
    private const int MaxSnippetLength = 700;

    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TavilySearchService> _logger;

    public TavilySearchService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<TavilySearchService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string> SearchAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        string cleanQuery = (query ?? "").Trim();
        if (string.IsNullOrWhiteSpace(cleanQuery))
            throw new ArgumentException("Từ khóa tìm kiếm Tavily không được để trống.", nameof(query));

        string apiKey = _configuration["Chatbot:TavilyApiKey"]?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new TavilySearchException(
                "Chưa cấu hình Chatbot:TavilyApiKey trong User Secrets phía máy chủ.",
                HttpStatusCode.ServiceUnavailable);
        }

        var payload = new
        {
            query = cleanQuery,
            search_depth = SearchDepth,
            max_results = MaxResults,
            include_answer = false,
            include_raw_content = false
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, SearchEndpoint)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
        request.Headers.Accept.ParseAdd("application/json");

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning("CHATBOT - Lỗi kết nối tới Tavily API.");
            throw new TavilySearchException(
                "Không thể kết nối tới dịch vụ tìm kiếm Tavily. Vui lòng thử lại sau.",
                HttpStatusCode.BadGateway,
                ex);
        }

        using (response)
        {
            string responseText = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                string safeDetail = RedactSecrets(responseText, apiKey);
                _logger.LogWarning(
                    "CHATBOT - Tavily API trả HTTP {StatusCode}. Chi tiết: {Detail}",
                    (int)response.StatusCode,
                    safeDetail);

                string userMessage = response.StatusCode switch
                {
                    HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden =>
                        "Tavily API Key không hợp lệ hoặc không có quyền truy cập. Hãy kiểm tra Chatbot:TavilyApiKey trong User Secrets.",
                    HttpStatusCode.TooManyRequests =>
                        "Dịch vụ tìm kiếm Tavily đang giới hạn số lượng yêu cầu. Vui lòng thử lại sau.",
                    _ =>
                        $"Dịch vụ tìm kiếm Tavily trả lỗi HTTP {(int)response.StatusCode}. Vui lòng thử lại sau."
                };

                throw new TavilySearchException(userMessage, response.StatusCode);
            }

            return ParseSearchResults(responseText);
        }
    }

    private string ParseSearchResults(string responseText)
    {
        try
        {
            using var document = JsonDocument.Parse(responseText);
            if (!document.RootElement.TryGetProperty("results", out var resultsElement) ||
                resultsElement.ValueKind != JsonValueKind.Array)
            {
                throw new TavilySearchException(
                    "Tavily trả về dữ liệu tìm kiếm không hợp lệ.",
                    HttpStatusCode.BadGateway);
            }

            var sb = new StringBuilder();
            int index = 0;

            foreach (var item in resultsElement.EnumerateArray())
            {
                if (index >= MaxResults)
                    break;

                string title = item.TryGetProperty("title", out var t) && t.ValueKind == JsonValueKind.String
                    ? t.GetString()?.Trim() ?? ""
                    : "";
                string rawUrl = item.TryGetProperty("url", out var u) && u.ValueKind == JsonValueKind.String
                    ? u.GetString()?.Trim() ?? ""
                    : "";
                string content = item.TryGetProperty("content", out var c) && c.ValueKind == JsonValueKind.String
                    ? Regex.Replace(c.GetString() ?? "", @"\s+", " ").Trim()
                    : "";
                string publishedDate = item.TryGetProperty("published_date", out var pd) && pd.ValueKind == JsonValueKind.String
                    ? pd.GetString()?.Trim() ?? ""
                    : "";

                if (!Uri.TryCreate(rawUrl, UriKind.Absolute, out var uri) ||
                    uri.Scheme is not ("http" or "https"))
                {
                    continue;
                }

                if (content.Length > MaxSnippetLength)
                    content = content[..MaxSnippetLength] + "...";

                index++;
                sb.AppendLine($"[{index}] Tiêu đề: {(string.IsNullOrWhiteSpace(title) ? uri.Host : title)}");
                sb.AppendLine($"URL: {uri}");
                if (!string.IsNullOrWhiteSpace(publishedDate))
                    sb.AppendLine($"Thời gian: {publishedDate}");
                if (!string.IsNullOrWhiteSpace(content))
                    sb.AppendLine($"Tóm tắt: {content}");
                sb.AppendLine();
            }

            if (index == 0)
            {
                return "";
            }

            return sb.ToString().Trim();
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "CHATBOT - Tavily trả JSON không hợp lệ.");
            throw new TavilySearchException(
                "Tavily trả về dữ liệu JSON không hợp lệ.",
                HttpStatusCode.BadGateway,
                ex);
        }
    }

    private static string RedactSecrets(string body, string apiKey)
    {
        string safe = body ?? "";
        if (!string.IsNullOrEmpty(apiKey))
            safe = safe.Replace(apiKey, "[REDACTED_KEY]", StringComparison.Ordinal);

        safe = Regex.Replace(safe, @"\btvly-[A-Za-z0-9_-]+\b", "[REDACTED_KEY]", RegexOptions.CultureInvariant);
        safe = Regex.Replace(safe, @"(?i)\b(Bearer\s+)[A-Za-z0-9._~+/-]+=*", "$1[REDACTED_KEY]", RegexOptions.CultureInvariant);
        return safe.Length <= 500 ? safe : safe[..500] + "...";
    }
}

public sealed class TavilySearchException : Exception
{
    public HttpStatusCode StatusCode { get; }

    public TavilySearchException(string message, HttpStatusCode statusCode, Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
    }
}
