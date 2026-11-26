using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using NewsCMS.Services.Chatbot;

namespace NewsCMS.Controllers
{
    public class ChatController : Controller
    {
        private readonly ChatOrchestratorService _chatOrchestrator;
        private readonly ILogger<ChatController> _logger;

        public ChatController(
            ChatOrchestratorService chatOrchestrator,
            ILogger<ChatController> logger)
        {
            _chatOrchestrator = chatOrchestrator;
            _logger = logger;
        }

        [HttpPost]
        [RequestSizeLimit(30000)]
        public async Task<IActionResult> HoiAI(
            [FromBody] ChatRequest? req,
            CancellationToken cancellationToken)
        {
            string question = req?.Message?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(question))
            {
                return BadRequest(new
                {
                    reply = "Bạn hãy nhập câu hỏi trước nhé!"
                });
            }

            if (question.Length > 1500)
            {
                return BadRequest(new
                {
                    reply = "Câu hỏi quá dài. Tối đa 1500 ký tự."
                });
            }

            try
            {
                // 1. Kiểm tra và giới hạn lịch sử hội thoại
                var history = (req?.History ?? new List<ChatHistoryItem>())
                    .Where(x =>
                        x != null &&
                        (x.Role == "user" || x.Role == "assistant") &&
                        !string.IsNullOrWhiteSpace(x.Content))
                    .TakeLast(10)
                    .Select(x => new ChatHistoryItem
                    {
                        Role = x.Role,
                        Content = x.Content.Length > 2500
                            ? x.Content.Substring(0, 2500)
                            : x.Content
                    })
                    .ToList();

                // The chatbot orchestrator selects only the required trusted sources.
                string answer = await _chatOrchestrator.AnswerAsync(
                    question, history, cancellationToken);

                if (string.IsNullOrWhiteSpace(answer))
                {
                    answer =
                        "AI chưa tạo được câu trả lời. Bạn hãy thử lại.";
                }

                _logger.LogInformation(
                    "CHATBOT - Xử lý câu hỏi thành công.");

                return Ok(new
                {
                    reply = answer
                });
            }
            catch (OllamaUnavailableException ex)
            {
                _logger.LogWarning(ex, "CHATBOT - Ollama Local không sẵn sàng.");
                return StatusCode(503, new
                {
                    reply = ex.Message
                });
            }
            catch (TavilySearchException ex)
            {
                _logger.LogWarning("CHATBOT - Lỗi Tavily Search (HTTP {StatusCode}).", (int)ex.StatusCode);
                int statusCode = ex.StatusCode == HttpStatusCode.TooManyRequests ? 429 : 502;
                return StatusCode(statusCode, new
                {
                    reply = ex.Message
                });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning("CHATBOT - Dịch vụ AI trả lỗi HTTP {StatusCode}.", ex.StatusCode is null ? 0 : (int)ex.StatusCode.Value);
                return StatusCode(502, new
                {
                    reply = "Không thể nhận phản hồi hợp lệ từ Ollama Local. Vui lòng kiểm tra Ollama và thử lại."
                });
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(
                    "CHATBOT - Yêu cầu đã bị hủy.");

                return StatusCode(499, new
                {
                    reply = "Yêu cầu đã bị hủy. Bạn hãy thử lại."
                });
            }
            catch (OperationCanceledException ex)
            {
                // Trường hợp HttpClient hết thời gian chờ
                _logger.LogError(
                    ex,
                    "CHATBOT - Dịch vụ AI/tìm kiếm phản hồi quá thời gian chờ.");

                return StatusCode(504, new
                {
                    reply =
                        "Hệ thống AI hoặc tìm kiếm phản hồi quá chậm. " +
                        "Bạn vui lòng thử lại sau."
                });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "CHATBOT - Lỗi truy vấn SQL Server (Number={SqlErrorNumber}).", ex.Number);
                return StatusCode(503, new
                {
                    reply = "Không thể truy vấn cơ sở dữ liệu SQL Server lúc này nên chưa thể xác minh dữ liệu bài viết. Vui lòng thử lại sau."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError("CHATBOT - Lỗi xử lý loại {ExceptionType}.", ex.GetType().Name);

                if (ex is InvalidOperationException)
                    return StatusCode(503, new { reply = ex.Message });

                return StatusCode(500, new
                {
                    reply = "Chatbot đang gặp lỗi nội bộ. Vui lòng thử lại sau."
                });
            }
        }
    }

    public class ChatRequest
    {
        public string Message { get; set; } = "";

        public List<ChatHistoryItem> History { get; set; } = new();
    }
}
 
