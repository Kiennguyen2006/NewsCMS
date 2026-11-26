using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace NewsCMS.Services.Chatbot;

/// <summary>Conservative server-side refusal checks for clearly disallowed requests.</summary>
public static class ChatContentModerationService
{
    private static readonly string[] RefusalPatterns =
    [
        @"\b(bỏ qua|bo qua)\b.{0,45}\b(quy tắc|quy tac|chỉ dẫn|chi dan|hướng dẫn|huong dan)\b",
        @"\b(ignore|bypass|override)\b.{0,40}\b(previous|prior|system|safety|instructions?|rules?)\b",
        @"\b(hiện|hien|tiết lộ|tiet lo|in ra|đưa ra|dua ra)\b.{0,45}\b(api key|mật khẩu|mat khau|secret|token|system prompt)\b",
        @"\b(hướng dẫn|huong dan|cách làm|cach lam|chế tạo|che tao)\b.{0,55}\b(bom|vũ khí|vu khi|đầu độc|dau doc|tấn công|tan cong|hack tài khoản|hack tai khoan)\b",
        @"\b(viet|tao|mo ta|ke|sang tac)\b.{0,60}\b(khieu dam|porn|kich duc|anh nong|noi dung tinh duc|explicit sexual)\b",
        @"\b(xúc phạm|xuc pham|đe dọa|de doa|quấy rối|quay roi|lăng mạ|lang ma)\b.{0,50}\b(tên|ten|người|nguoi|cá nhân|ca nhan|đồng nghiệp|dong nghiep)\b",
        @"\b(kích động|kich dong|tiêu diệt|tieu diet|đuổi khỏi|duoi khoi)\b.{0,50}\b(chủng tộc|chung toc|tôn giáo|ton giao|dân tộc|dan toc)\b",
        @"\b(phân biệt|phan biet|kỳ thị|ky thi|thù ghét|thu ghet)\b.{0,50}\b(chủng tộc|chung toc|tôn giáo|ton giao|dân tộc|dan toc|người nhập cư|nguoi nhap cu)\b",
        @"\b(đảng phái|dang phai|vận động bầu cử|van dong bau cu|ủng hộ ứng viên|ung ho ung vien|tranh luận chính trị|tranh luan chinh tri|quan điểm chính trị|quan diem chinh tri|đảng nào|dang nao|nên bầu|nen bau|bầu cho ai|bau cho ai)\b"
    ];

    public static bool IsDisallowed(string text)
    {
        string normalized = Normalize(text);
        if (RefusalPatterns.Any(pattern => Regex.IsMatch(normalized, pattern,
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100))))
            return true;

        bool containsProfanity = Regex.IsMatch(normalized,
            @"\b(dit|dcm|dm|fuck|fucking|shit|asshole|do ngu|thang ngu|do mat day)\b",
            RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
        bool isUsingProfanityToAbuse = Regex.IsMatch(normalized,
            @"\b(chui|noi tuc|si nhuc|xuc pham|lang ma|goi ai la|viet loi chui)\b",
            RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
        if (containsProfanity && isUsingProfanityToAbuse)
            return true;

        bool asksAboutPolitics = Regex.IsMatch(normalized,
            @"\b(chinh tri|chinh phu|bau cu|ung vien|dang cong san|quoc hoi|tong thong|bo truong)\b",
            RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
        bool clearlyHistoricalOrAcademic = Regex.IsMatch(normalized,
            @"\b(lich su|thoi ky|tieu su|khai niem|dinh nghia|bai hoc|sach giao khoa|mon hoc)\b",
            RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
        return asksAboutPolitics && !clearlyHistoricalOrAcademic;
    }

    private static string Normalize(string text)
    {
        string decomposed = (text ?? "").ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var result = new StringBuilder(decomposed.Length);
        foreach (char c in decomposed)
            if (c == 'đ')
                result.Append('d');
            else if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                result.Append(c);
        return Regex.Replace(result.ToString().Normalize(NormalizationForm.FormC), @"\s+", " ").Trim();
    }
}

