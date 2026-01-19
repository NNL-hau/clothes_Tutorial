# Clothes E-Commerce — Hướng dẫn chạy dự án (BE + FE)

Hướng dẫn ngắn để chạy backend (.NET Microservices) và frontend (Blazor WebAssembly) trong workspace này.

## Yêu cầu trước
- .NET SDK 9.0 hoặc cao hơn
- SQL Server (hoặc dùng Docker)
- Docker Desktop (tuỳ chọn, để chạy toàn bộ services)
- Visual Studio 2022 / VS Code / Rider (tuỳ chọn)

## Backend - Identity.API Service

Files tham chiếu:
- Cấu hình: [src/Services/Identity/Identity.API/appsettings.json](src/Services/Identity/Identity.API/appsettings.json)
- Entrypoint: [src/Services/Identity/Identity.API/Program.cs](src/Services/Identity/Identity.API/Program.cs)
- Controllers: [src/Services/Identity/Identity.API/Controllers/AuthController.cs](src/Services/Identity/Identity.API/Controllers/AuthController.cs)
- Database Context: [src/Services/Identity/Identity.API/Data/IdentityDbContext.cs](src/Services/Identity/Identity.API/Data/IdentityDbContext.cs)

### 1. Chuẩn bị database (SQL Server)
- Tạo database `IdentityDb` hoặc dùng Docker nhanh:
```sh
docker run --name sqlserver -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourStrong@Password123" -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest
```
- Mặc định cấu hình DB đang nằm ở [src/Services/Identity/Identity.API/appsettings.json](src/Services/Identity/Identity.API/appsettings.json):
  - Server: `localhost`
  - Database: `IdentityDb`
  - User Id: `sa`
  - Password: `YourStrong@Password123`
- Bạn có thể chỉnh trực tiếp file trên hoặc override bằng biến môi trường:
  - `ConnectionStrings__DefaultConnection`

### 2. Chế độ tạo bảng
- Ứng dụng dùng Entity Framework Core nên cần chạy migration để tạo database schema:
```sh
cd src/Services/Identity/Identity.API
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### 3. Chạy backend
```sh
cd src/Services/Identity/Identity.API
dotnet run
```
- Server mặc định lắng nghe ở port `5001` (HTTPS) và `5000` (HTTP)
- Swagger UI: `https://localhost:5001/swagger`

### 4. Kiểm tra kết nối DB khi lỗi
- Kiểm tra logs .NET để xem chi tiết lỗi kết nối (host, port, credentials).
- Đảm bảo SQL Server đang chạy và có thể truy cập từ máy dev.
- Kiểm tra `TrustServerCertificate=True` trong connection string nếu dùng SQL Server local.

## Frontend - Blazor WebAssembly (ClothesShop.Web)

Files tham chiếu:
- Project file: [src/Web/ClothesShop.Web/ClothesShop.Web.csproj](src/Web/ClothesShop.Web/ClothesShop.Web.csproj)
- Entry point: [src/Web/ClothesShop.Web/Program.cs](src/Web/ClothesShop.Web/Program.cs)
- Pages: 
  - [src/Web/ClothesShop.Web/Pages/Login.razor](src/Web/ClothesShop.Web/Pages/Login.razor)
  - [src/Web/ClothesShop.Web/Pages/Register.razor](src/Web/ClothesShop.Web/Pages/Register.razor)
  - [src/Web/ClothesShop.Web/Pages/Home.razor](src/Web/ClothesShop.Web/Pages/Home.razor)
- Services: [src/Web/ClothesShop.Web/Services/AuthService.cs](src/Web/ClothesShop.Web/Services/AuthService.cs)

### 1. Chạy frontend
```sh
cd src/Web/ClothesShop.Web
dotnet run
```
- Blazor WebAssembly thường chạy ở `https://localhost:5002` (kiểm tra output terminal).

### 2. Build production
```sh
cd src/Web/ClothesShop.Web
dotnet build -c Release
dotnet publish -c Release -o ./publish
```

### 3. Ghi chú
- Frontend dùng Blazor WebAssembly với C# full-stack.
- API backend URL được cấu hình trong [src/Web/ClothesShop.Web/Program.cs](src/Web/ClothesShop.Web/Program.cs): `https://localhost:5001/`
- JWT token được lưu trong LocalStorage sử dụng Blazored.LocalStorage.

## Chạy toàn bộ với Docker Compose (tùy chọn)

Files tham chiếu:
- [docker-compose.yml](docker-compose.yml)

```sh
docker-compose up -d
```

Services sẽ được khởi động:
- SQL Server (port 1433)
- Redis (port 6379)
- RabbitMQ (ports 5672, 15672)
- Identity.API (port 5001)
- Catalog.API (port 5002) - coming soon
- Basket.API (port 5003) - coming soon
- Ordering.API (port 5004) - coming soon
- Payment.API (port 5005) - coming soon
- API Gateway (port 5000) - coming soon

## Cấu trúc dự án

```
ClothesECommerce/
├── src/
│   ├── Services/
│   │   ├── Identity/Identity.API/          ✅ Completed
│   │   ├── Catalog/Catalog.API/            ⏳ Pending
│   │   ├── Basket/Basket.API/              ⏳ Pending
│   │   ├── Ordering/Ordering.API/          ⏳ Pending
│   │   ├── Payment/Payment.API/            ⏳ Pending
│   │   └── Review/Review.API/              ⏳ Pending
│   ├── ApiGateways/ApiGateway/             ⏳ Pending
│   ├── BuildingBlocks/EventBus/            ✅ Created
│   └── Web/ClothesShop.Web/                ✅ Completed
├── ClothesECommerce.sln
├── docker-compose.yml
└── README.md
```

## Troubleshooting nhanh

### Backend issues
- **Lỗi kết nối DB**: kiểm tra SQL Server đang chạy, thông tin trong [src/Services/Identity/Identity.API/appsettings.json](src/Services/Identity/Identity.API/appsettings.json) hoặc các biến môi trường đã set đúng.
- **Port conflict**: thay port trong `appsettings.json` hoặc dùng biến môi trường `ASPNETCORE_URLS`.
- **Migration error**: chạy `dotnet ef database update` trước khi start service.

### Frontend issues
- **CORS error**: đảm bảo Identity.API có cấu hình CORS cho phép origin của Blazor app.
- **API connection failed**: kiểm tra base URL trong [src/Web/ClothesShop.Web/Program.cs](src/Web/ClothesShop.Web/Program.cs) trỏ đúng về backend.
- **LocalStorage error**: clear browser cache/storage và thử lại.

## API Endpoints

### Identity.API
- `POST /api/auth/register` - Đăng ký tài khoản mới
- `POST /api/auth/login` - Đăng nhập và nhận JWT token
- `GET /api/auth/me` - Lấy thông tin người dùng hiện tại (yêu cầu authentication)

### Testing với Swagger
1. Truy cập `https://localhost:5001/swagger`
2. Test endpoint `/api/auth/register` để tạo tài khoản
3. Test endpoint `/api/auth/login` để nhận token
4. Copy token và click "Authorize" button ở Swagger UI
5. Paste token (với prefix "Bearer ") để test các endpoint cần authentication

---

## Commit Convention

Sử dụng chuẩn Conventional Commits để commit rõ ràng, dễ đọc và dễ tạo changelog.

**Format:**
```
type(scope?): subject
[BLANK LINE]
body (tuỳ chọn)
[BLANK LINE]
footer (tuỳ chọn, ví dụ: BREAKING CHANGE: ... hoặc closes #123)
```

**Common types:**
- `feat`: thêm tính năng
- `fix`: sửa lỗi
- `docs`: tài liệu
- `style`: format/code style không ảnh hưởng logic
- `refactor`: refactor code (không thêm tính năng, không sửa lỗi)
- `perf`: tối ưu hiệu năng
- `test`: thêm/sửa test
- `chore`: công việc không ảnh hưởng src (build, config)
- `ci`: thay đổi cấu hình CI

**Ví dụ:**
```
feat(identity): add JWT refresh token
fix(auth): handle null pointer in AuthController
docs: update README for database setup
chore: upgrade Entity Framework to 9.0.0
```

**Ghi chú:**
- Viết subject ở dạng câu lệnh (imperative), tối đa ~72 ký tự.
- Nếu cần mô tả chi tiết, thêm body.
- Đóng issue sử dụng footer: `closes #<issue>`.

## Branching (quy ước tạo nhánh)

**Branch chính:**
- `main`: mã production luôn ổn định
- `develop`: tích hợp feature, chuẩn bị release

**Branch tạm thời:**
- `feature/<your_name>/short-desc`
  - Ví dụ: `feature/khank/catalog-service`
- `fix/<your_name>/short-desc`
  - Ví dụ: `fix/khank/null-user`
- hotfix/<your_name>/short-desc
  - Dùng khi sửa gấp trên main

**Quy trình cơ bản:**
1. Tạo branch từ `develop` (hoặc từ `main` cho hotfix):
   ```sh
   git checkout develop
   git pull origin develop
   git checkout -b feature/yourname/short-desc
   ```

2. Làm việc, commit theo convention ở trên.
   ```sh
   git add .
   git commit -m "feat(catalog): add product CRUD endpoints"
   ```

3. Push và tạo Pull Request vào `develop` (hoặc `main` for hotfix/release).
   ```sh
   git push origin feature/yourname/short-desc
   ```

4. PR phải có mô tả, liên kết issue (nếu có) và review trước khi merge.

5. Sau merge feature vào `develop`, xóa branch remote khi xong:
   ```sh
   git push origin --delete feature/yourname/short-desc
   ```

---

## Tech Stack

### Backend
- **.NET 9.0** - Framework
- **ASP.NET Core** - Web API
- **Entity Framework Core 9.0** - ORM
- **SQL Server** - Database
- **JWT Bearer** - Authentication
- **BCrypt.Net** - Password hashing
- **Swagger/OpenAPI** - API documentation

### Frontend
- **Blazor WebAssembly** - SPA framework
- **C#** - Language
- **Blazored.LocalStorage** - Browser storage
- **Bootstrap Icons** - Icons

### Infrastructure (Docker Compose)
- **SQL Server 2022**
- **Redis** - Caching
- **RabbitMQ** - Message broker

---

## Tài liệu tham khảo

- [ASP.NET Core Documentation](https://docs.microsoft.com/en-us/aspnet/core/)
- [Blazor WebAssembly Documentation](https://docs.microsoft.com/en-us/aspnet/core/blazor/)
- [Entity Framework Core Documentation](https://docs.microsoft.com/en-us/ef/core/)
- [Microservices Architecture](https://docs.microsoft.com/en-us/dotnet/architecture/microservices/)
- [Docker Documentation](https://docs.docker.com/)

---

## Contributors

- Your Name

## License

MIT License
