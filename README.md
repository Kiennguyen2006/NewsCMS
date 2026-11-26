# NewsCMS
# Hướng dẫn sử dụng ứng hệ thống quản trị tin tức đa vai trò và 1 phần luồng xây dưng
vai trò 4:đọc giả, khách vãng lai
->khách vãng lai(có thể xem các trang báo khi chưa đăng nhập)
-đ>ộc giả(có thể xem trang web, bình luận sau khi đã đăng nhập)
vai trò 2: biên tập viên(là tài khoản được quản trị viên tạo ra và đã được liên kết vs dữ liệu trong bảng nhân viên và vào phần quản lý tài khoản đã được đổi quyền truy cập 2 rồi, rồi đăng nhập)
->vs vai trò biên tập viên có thể truy cập vô quản lý bài viết có thể viết bài đăng bài rồi sửa bài viết nhưng thực chất mới thêm một phần vào bảng của csdl thôi(ko tác động được vô hoàn toàn giao diện thêm bài viết và sửa bài viết) và bài viết sẽ có trạng thái Chờ Duyệt và chưa được hiển thị lên ngay chờ chủ biên tập duyệt(có nghiên cứu thêm các công cụ giúp trình soạn thảo như quill, tinymcv, ckeditor)
vai trò 3:Chủ biên tập(là tài khoản được quản trị viên tạo ra và đã được liên kết vs dữ liệu trong bảng nhân viên và vào phần quản lý tài khoản đã được đổi quyền truy cập 3 rồi, rồi đăng nhập)
->chủ biên tập có thể quản lý bài viết có thể truy cập vô sửa bài viết vs toàn bộ giao diện và có thể sửa được dữ liệu của biên tập viên đã viết và sẽ tác động vô phần còn lại của giao diện mà biên tập viên ko dc tác động để điền nốt dữ liệu cho bảng, khi bảng đầy đủ bài viết nó sẽ lưu luôn lịch sử duyệt vào bảng lịch sử duyệt
vai trò 1: quản trị viên(là 1 tài khoản do bên phát triển phần mềm cấp(là nhóm mình tạo tài khoản có vai trò 1 addmin cho họ))
->có thể quản lý bài viết vs chức năng xoá để xoá bài viết nhưng vì khi xoá nó có liên quan đến bảng bình luận và lịch sử duyệt cx sẽ phải xoá đi theo bài viết đấy nên phải để quản trị viên xoá
->có thể quản lý tài khoản
+có thể sử dụng thêm,sửa 
+có thể xoá nhưng xoá phải dựa vào mã muốn xoá tìm ra mã tài khoản và vai trò xong từ mã vai trò tìm ra mã nhân viên vs mã nhân viên này sẽ tìm ra dòng nhân viên dòng nhân viên này ta sẽ phải kiểm tra xem có giá trị ko nếu ko có giá trị =>tài khoản do độc giả tạo chỉ lưu vào bảng tài khoản và đôc giả  ko có liên kết vs nhân viên rồi cx từ mã tài khoản đấy tìm ra dòng độc giả từ dòng độc giả tìm ra mã độc giả từ mã độc giả tìm ra các bình luận rồi từ các bình luận của độc giả đấy nữa rồi xoá độc giả đi cuối cùng là xoá tài khoản đi, còn nếu có giá trị dòng đấy tức !=null là chứng tỏ tài khoản do quan trị viên tạo và đã liên kết vs thông tin nhân viên trong bảng nhân viên=>lấy ra mã nhân viên rồi sau sẽ sẽ lấy ra mã vai trò kiểm tra nếu vai trò =2 ta sẽ tìm ra các bài viết vs mã nhân viên tác giả bẳng mã nhân viên kia rồi từ các bài viết đấy tìm ra các lịch sử duyệt và các bình luận liên quan rồi xoá đi các lịch sử duyệt và các bình luận liên quan rồi xoá đi dòng nhân viên, rồi cx từ mã tài khoản đấy tìm ra dòng độc giả từ dòng độc giả tìm ra mã độc giả từ mã độc giả tìm ra các bình luận rồi từ các bình luận của độc giả đấy nữa rồi xoá độc giả đi cuối cùng là xoá tài khoản đi còn nếu là vai trò 3 thì tìm ra các bài viết đưa thêm mã nhân viên duyệt bằng mã nhân viên này rồi từ các bài viết đấy tìm ra các lịch sử duyệt và bình luận liên quan để xoá đi xoá các bài viết rồi xoá nhân viên đi , sau đấy sẽ dựa vào mã tài khoản tìm ra dòng độc giả từ dòng độc giả tìm ra mã độc giả rồi từ mã độc giả tìm ra các bình luận liên quan đến bài viết đấy từ bài viết rồi xoá đi bài viết đấy rồi xoá độc giả và xoá độc giả đi sau đấy cuối cùng xoá độc giả
+có thể quản lý danh mục là thêm và sửa danh mục thôi nếu vô tình tạo ra nhiều quá hay danh mục ko còn sử dụng thì vào đổi trạng thái cho danh mục đấy, và sau muốn thêm danh mục mới thì bật lại danh mục kia và sửa nội dung bên trong đi 
+có quản lý lịch sử duyệt xem đc toàn bộ lịch sử duyệt
+có thể quản lý nhân viên có thể thêm, sửa nội dung nhân viên bình thưởng(lưu ý nhân viên phải liên kết luôn vs tài khoản của nhân viên),có thể xoá nhưng phải lấy ra mã nhân viên từ mã nhân viên tìm ra mã tk rồi vai trò rồi nếu vai trò =2 ta sẽ tìm ra các bài viết vs mã nhân viên tác giả bẳng mã nhân viên kia rồi từ các bài viết đấy tìm ra các lịch sử duyệt và các bình luận liên quan rồi xoá đi các lịch sử duyệt và các bình luận liên quan rồi xoá đi dòng nhân viên, rồi cx từ mã tài khoản đấy tìm ra dòng độc giả từ dòng độc giả tìm ra mã độc giả từ mã độc giả tìm ra các bình luận rồi từ các bình luận của độc giả đấy nữa rồi xoá độc giả đi cuối cùng là xoá nhân viên đi còn nếu là vai trò 3 thì tìm ra các bài viết dưa them mã nhân viên duyệt bằng mã nhân viên này rồi từ các bài viết đấy tìm ra các lịch sử duyệt và bình luận liên quan để xoá đi xoá các bài viết rồi xoá nhân viên đi là đc
+có thể quản lý bình luận và chỉ có thể xoá bình luận đi th và o liên quan đến bảng nào
+có  thể thực hiện chức năng thống kê là dùng giao diện có sẵn của admilite và chỉ việc tải nó về và sau đấy sẽ đưa nó vào chương trình cần dùng rồi controller sẽ gửi dữ liệu đến một view view đấy nhân được dữ liệu sẽ gửi vào một trang view nữa mà ở trang view đấy sẽ gọi view giao diện và xử lý của trang adminlite cung cấp sẵn và sẽ có giao diện thống kê sẽ hiển thị lên 




---Hướng dẫn cách để các máy có thể sử dụng ai chatbot(lưu ý rất rễ lỗi lên nếu ko thành công hoặc sai bước nào tự dùng ai sửa :)))
# NewsCMS – Hệ thống quản lý tin tức

## 1. Giới thiệu dự án

NewsCMS là hệ thống quản lý tin tức được xây dựng bằng ASP.NET Core MVC. Dự án hỗ trợ quản lý bài viết và tích hợp chatbot AI để hỗ trợ người dùng tra cứu, tìm kiếm và trao đổi thông tin liên quan đến nội dung website.

## 2. Công nghệ sử dụng

- **Backend:** ASP.NET Core MVC (.NET 10)
- **Ngôn ngữ:** C#
- **Cơ sở dữ liệu:** SQL Server
- **ORM:** Entity Framework Core
- **AI Chatbot:** Ollama
- **Mô hình AI:** qwen2.5:7b
- **Tìm kiếm thông tin:** Tavily API
- **Quản lý mã nguồn:** Git và GitHub

## 3. Yêu cầu cài đặt

Trước khi chạy dự án, thành viên cần cài đặt:

1. .NET 10 SDK.
2. Visual Studio hoặc Visual Studio Code.
3. SQL Server Express và SQL Server Management Studio (SSMS).
4. Ollama.
5. Git.

## 4. Tải dự án về máy

Mở Terminal hoặc PowerShell và chạy:

```bash
git clone https://github.com/Kiennguyen2006/NewsCMS.git
```

Sau đó mở thư mục dự án bằng Visual Studio hoặc Visual Studio Code.

## 5. Cấu hình cơ sở dữ liệu

Dự án sử dụng SQL Server.

- Cài đặt SQL Server Express.
- Mở SQL Server Management Studio.
- Kết nối đến SQL Server trên máy tính.
- Khôi phục cơ sở dữ liệu từ file backup hoặc chạy file SQL được nhóm cung cấp.
- Kiểm tra tên cơ sở dữ liệu và cấu hình kết nối tương ứng với môi trường đã được chuẩn bị.

**Lưu ý:** Cơ sở dữ liệu không tự động được tải về khi clone GitHub. Thành viên cần nhận file backup hoặc script SQL từ nhóm nếu chưa có dữ liệu.

## 6. Cấu hình Ollama

Chatbot sử dụng Ollama để chạy mô hình AI trên máy tính.

### Bước 1: Cài đặt Ollama

Tải Ollama tại:

https://ollama.com/

### Bước 2: Tải mô hình AI

Mở Terminal và chạy:

```bash
ollama pull qwen2.5:7b
```

### Bước 3: Khởi chạy mô hình

```bash
ollama run qwen2.5:7b
```

Ollama sử dụng địa chỉ mặc định:

```text
http://localhost:11434
```

Giữ Ollama hoạt động khi sử dụng chức năng chatbot.

## 7. Cấu hình Tavily API Key

Chatbot sử dụng Tavily API để hỗ trợ tìm kiếm thông tin trên Internet.

Mỗi thành viên cần tự tạo API Key tại:

https://tavily.com/

**Không chia sẻ API Key trong mã nguồn hoặc đẩy API Key lên GitHub.**

Mở Terminal tại thư mục chứa file `.csproj` của dự án và chạy:

```powershell
dotnet user-secrets init
```

Nếu dự án đã có `UserSecretsId` trong file `.csproj`, không cần chạy lại lệnh khởi tạo.

Tiếp theo, cấu hình API Key:

```powershell
dotnet user-secrets set "Chatbot:TavilyApiKey" "API_KEY_CUA_BAN"
```

Thay `API_KEY_CUA_BAN` bằng API Key riêng của thành viên.

Mỗi thành viên chỉ cần thực hiện cấu hình này một lần trên máy của mình.

## 8. Chạy dự án

Mở Terminal tại thư mục chứa file `.csproj`, sau đó thực hiện:

```bash
dotnet restore
```

Tiếp theo:

```bash
dotnet build
```

Nếu quá trình build thành công, chạy:

```bash
dotnet run
```

Hoặc chạy trực tiếp dự án bằng Visual Studio.

## 9. Kiểm tra chatbot

Trước khi sử dụng chatbot, cần đảm bảo:

- Ollama đã được cài đặt và đang hoạt động.
- Mô hình `qwen2.5:7b` đã được tải về.
- Tavily API Key đã được cấu hình bằng User Secrets.
- SQL Server và cơ sở dữ liệu đã sẵn sàng.

Sau đó truy cập website và thử gửi câu hỏi cho chatbot.

## 10. Lưu ý khi làm việc nhóm

- Không tự ý xóa hoặc thay đổi các tệp trong `Services/Chatbot` nếu chưa trao đổi với nhóm.
- Không đưa API Key, mật khẩu hoặc thông tin bí mật lên GitHub.
- Không chia sẻ thư mục `bin`, `obj` hoặc `.vs` khi gửi mã nguồn.
- Mỗi thành viên sử dụng API Key riêng trên máy cá nhân.
- Khi chỉnh sửa mã nguồn, cần trao đổi với nhóm để tránh xung đột khi cập nhật GitHub.
- Không tự ý thay đổi cấu hình đang được nhóm sử dụng nếu chưa thống nhất.

## 11. Repository

GitHub: https://github.com/Kiennguyen2006/NewsCMS.git

**NewsCMS – Dự án thực tập chuyên đề đồ án 2.**
