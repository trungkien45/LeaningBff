# LearningBff

Ứng dụng web hỗ trợ quản lý và học tập theo môn học. Giáo viên có thể quản lý môn học, chương trình học, ngân hàng câu hỏi và đề thi; học viên có thể xem nội dung học, làm bài thi và xem kết quả. Dự án được xây dựng trên ABP Framework theo mô hình single-layer.

## Công nghệ

- .NET 10
- ABP Framework 10.6
- ASP.NET Core MVC / Razor Pages
- Entity Framework Core và PostgreSQL
- Docker Compose (tùy chọn)

## Chạy bằng Docker Compose

Yêu cầu đã cài Docker Desktop hoặc Docker Engine có Docker Compose.

Từ thư mục gốc repository, chạy:

```powershell
docker compose up --build
```

Compose sẽ khởi động PostgreSQL, chờ database sẵn sàng, chạy migration, sau đó khởi động ứng dụng. Mở [http://localhost:8080](http://localhost:8080) để truy cập. Endpoint kiểm tra sức khỏe là [http://localhost:8080/health-status](http://localhost:8080/health-status).

Dừng các container:

```powershell
docker compose down
```

Dữ liệu PostgreSQL được lưu trong thư mục `postgres-data` ở repository root nên vẫn còn sau khi dừng container. Chỉ xóa dữ liệu này khi bạn thực sự muốn khởi tạo lại database.

## Chạy local

Yêu cầu:

- .NET 10 SDK
- PostgreSQL đang chạy và có database mà ứng dụng được phép sử dụng

1. Cấu hình connection string qua biến môi trường (hoặc trong `LearningBff/appsettings.json`). Thay giá trị mẫu bên dưới bằng thông tin database local của bạn:

   ```powershell
   $env:ConnectionStrings__Default = "Host=localhost;Port=5432;Database=LearningBff;Username=postgres;Password=<your-password>"
   ```

2. Chạy ứng dụng từ thư mục gốc:

   ```powershell
   dotnet run --project LearningBff/LearningBff.csproj
   ```

Ứng dụng tự áp dụng migration và dữ liệu seed khi khởi động. Để chạy migration riêng:

```powershell
.\migrate-database.ps1
```

Địa chỉ local mặc định được cấu hình trong `LearningBff/appsettings.json` và `etc/run-profiles/Default.abprun.json`. Nếu thay đổi URL hoặc cổng, hãy cập nhật các giá trị URL liên quan như `App:SelfUrl` và `AuthServer:Authority` cho đồng nhất.

## Cấu hình đăng nhập bằng Google

1. Trong [Google Cloud Console](https://console.cloud.google.com/), tạo hoặc chọn một project, cấu hình màn hình đồng ý OAuth (OAuth consent screen / Google Auth Platform) và thêm tài khoản của bạn vào danh sách test users nếu ứng dụng đang ở chế độ Testing.
2. Tạo OAuth client ID loại **Web application**. Thêm callback URL vào **Authorized redirect URIs**:

   | Cách chạy ứng dụng | Authorized redirect URI |
   | --- | --- |
   | Local theo cấu hình mặc định | `https://localhost:44368/signin-google` |
   | Docker Compose ở repository root | `http://localhost:8080/signin-google` |

   Nếu đổi hostname, scheme hoặc cổng, dùng URL bên ngoài mà trình duyệt truy cập được, nối thêm `/signin-google`. Đây là callback mặc định của Google authentication middleware trong ứng dụng. Với cấu hình production, đăng ký callback HTTPS của domain production; không dùng URL localhost.
3. Cấu hình Client ID và Client Secret. Khi chạy local bằng PowerShell, đặt biến môi trường trong terminal trước khi khởi động ứng dụng:

   ```powershell
   $env:Authentication__Google__ClientId = "<Google OAuth Client ID>"
   $env:Authentication__Google__ClientSecret = "<Google OAuth Client Secret>"
   dotnet run --project LearningBff/LearningBff.csproj
   ```

   .NET chuyển `__` trong tên biến môi trường thành `:` trong khóa cấu hình. Có thể dùng user-secrets hoặc secret store thay cho biến môi trường; tránh ghi Client Secret vào `appsettings*.json` hoặc Git.
4. Khi chạy bằng Docker Compose, hai giá trị cũng phải được truyền vào service `web` dưới các khóa `Authentication__Google__ClientId` và `Authentication__Google__ClientSecret`. Ví dụ, thêm vào `environment` của service `web` trong `docker-compose.yml`:

   ```yaml
   Authentication__Google__ClientId: ${GOOGLE_CLIENT_ID}
   Authentication__Google__ClientSecret: ${GOOGLE_CLIENT_SECRET}
   ```

   Sau đó cung cấp `GOOGLE_CLIENT_ID` và `GOOGLE_CLIENT_SECRET` qua môi trường triển khai hoặc file `.env` không được commit, rồi khởi động lại service.

Google login chỉ được đăng ký khi cả Client ID và Client Secret đều có giá trị. Nếu không thấy tùy chọn đăng nhập Google hoặc bị lỗi redirect, kiểm tra lại thông tin OAuth, callback URL, URL ứng dụng và log của ứng dụng.

## Cấu hình và bảo mật

- Các thiết lập có thể được ghi đè bằng biến môi trường theo quy ước cấu hình của .NET, ví dụ `ConnectionStrings__Default`.
- Không đưa mật khẩu database, Google OAuth client secret, passphrase hoặc private certificate vào Git. Dùng user-secrets, biến môi trường hoặc secret store của môi trường triển khai.
- `openiddict.pfx` và chứng chỉ HTTPS phục vụ phát triển không nên được xem là chứng chỉ production. Xem phần [Chứng chỉ OpenIddict](#chứng-chỉ-openiddict) khi chuẩn bị triển khai thật.

## Cấu trúc chính

- `LearningBff/` — ứng dụng ASP.NET Core và các module ABP.
- `LearningBff/Pages/` — các trang Razor cho học viên, giáo viên và quản trị.
- `LearningBff/Services/` — dịch vụ nghiệp vụ cho môn học, bài học, đề thi và ngân hàng câu hỏi.
- `LearningBff/Entities/` và `LearningBff/Data/` — entity, DbContext và migration database.
- `etc/run-profiles/` — cấu hình run profile cho ABP Studio.
- `Dockerfile`, `docker-compose.yml` — đóng gói và chạy ứng dụng cùng PostgreSQL.

## Thông tin từ template ABP

Các hướng dẫn dưới đây giữ lại thông tin nền tảng của template ABP single-layer và được điều chỉnh để khớp với cấu hình hiện tại của repository.

### Về solution

Đây là ứng dụng single-layer, không chia thành nhiều project layer. Các module nền tảng của ABP đã được cài đặt. Tham khảo tài liệu [Application (Single Layer) Startup Template](https://abp.io/docs/latest/solution-templates/single-layer-web-application) để biết thêm.

### Cài thư viện client-side

Sau khi clone repository hoặc thêm package client-side mới, chạy lệnh sau từ thư mục chứa solution nếu cần khôi phục các thư viện UI:

```bash
abp install-libs
```

Lệnh này cần ABP CLI và Node.js. Dockerfile hiện tại cài Node.js 24 trong bước build và chạy lệnh này tự động.

### Chứng chỉ OpenIddict

Ở môi trường production, nên cấu hình chứng chỉ ký và mã hóa phù hợp; không dùng chứng chỉ phát triển hay passphrase mẫu. ABP/OpenIddict đọc file `openiddict.pfx` cùng cấu hình `AuthServer:CertificatePassPhrase` khi cần chứng chỉ production. Chứng chỉ và passphrase phải được cung cấp an toàn cho môi trường triển khai, không commit vào source control.

Ví dụ tạo chứng chỉ HTTPS phát triển (không thay thế quy trình cấp chứng chỉ production):

```bash
dotnet dev-certs https -v -ep openiddict.pfx -p <your-local-password>
```

Nên dùng hai RSA certificate riêng — một cho ký và một cho mã hóa — tách biệt với chứng chỉ HTTPS. Tham khảo tài liệu [OpenIddict về chứng chỉ ký và mã hóa](https://documentation.openiddict.com/configuration/encryption-and-signing-credentials.html#registering-a-certificate-recommended-for-production-ready-scenarios) và [cấu hình OpenIddict trên ABP](https://abp.io/docs/latest/deployment/configuring-openIddict#production-environment).

### Triển khai

Ứng dụng ABP được triển khai theo quy trình của ứng dụng .NET/ASP.NET Core. Repository này cung cấp Dockerfile và `docker-compose.yml` tại thư mục gốc để chạy ứng dụng cùng PostgreSQL; xem phần [Chạy bằng Docker Compose](#chạy-bằng-docker-compose). Hướng dẫn triển khai chung có tại [tài liệu deployment của ABP](https://abp.io/docs/latest/deployment).

### Tài liệu tham khảo

- [Application (Single Layer) Startup Template](https://abp.io/docs/latest/solution-templates/single-layer-web-application)
- [ABP Framework](https://abp.io/docs/latest/)
