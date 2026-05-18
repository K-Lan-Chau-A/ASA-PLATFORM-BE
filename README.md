# 🏪 ASA Platform — Backend Service (ASA-PLATFORM-BE)

> The **Platform Service** of the ASA microservice system — manages platform-level operations including shop registration, subscription plans, platform admin, reporting, and cross-service communication with the Tenant Service.

---

## 🏗️ System Architecture

ASA is a **multi-tenant SaaS platform** for retail shop management, built as two independent microservices that communicate via HTTP:

```
┌─────────────────────────────────────────────────────────┐
│                     ASA SYSTEM                          │
│                                                         │
│  ┌──────────────────┐        ┌──────────────────────┐  │
│  │  ASA-PLATFORM-BE │◄──────►│   ASA-TENANT-BE      │  │
│  │  (Port 8081)     │  HTTP  │   (Port 8080)        │  │
│  │                  │        │                      │  │
│  │  - Platform Admin│        │  - Shop Operations   │  │
│  │  - Shop Mgmt     │        │  - POS / Orders      │  │
│  │  - Subscriptions │        │  - Inventory         │  │
│  │  - Platform Rpts │        │  - Customers / NFC   │  │
│  │  - Feature Flags │        │  - Payments          │  │
│  └──────────────────┘        └──────────────────────┘  │
│           │                            │                │
│    PostgreSQL (Platform DB)    PostgreSQL (Tenant DB)   │
└─────────────────────────────────────────────────────────┘
```

---

## 📖 About This Service

**ASA-PLATFORM-BE** is the platform-level backend responsible for:
- Managing shop accounts and their subscription status
- Controlling feature flags available to each shop
- Platform-level reporting (weekly, monthly)
- Admin authentication and user management
- Cross-service calls to ASA-TENANT-BE for data aggregation
- AI Chatbot powered by Google Gemini

---

## ✨ Features & API Modules

### Authentication (`/api/Authentication`)
- Platform admin login with JWT
- Password reset via OTP email (Brevo/SMTP)
- Token-based authorization (Bearer JWT)

### Shop Management (`/api/Shop`)
- Register and manage shops on the platform
- Approve / reject shop registrations
- View shop details and subscription status
- Shop status expiry automation

### User Management (`/api/User`)
- Platform-level user CRUD
- Role-based access control
- Profile management with Cloudinary image upload

### Feature Flags (`/api/Feature`)
- Define platform features (e.g., NFC, AI Chatbot, Advanced Reports)
- Assign/revoke features per shop subscription plan

### Order Management (`/api/Order`)
- Platform-level order overview across all shops
- Cross-service order data aggregation

### Product Management (`/api/Product`)
- Platform-level product catalog view

### Promotion Management (`/api/Promotion`, `/api/PromotionProduct`)
- Platform-wide promotion management
- Promotion expiry automation via cron job

### Reporting (`/api/Report`)
- Platform-level revenue and activity reports
- Weekly and monthly automated report generation

### Payment (`/api/SepayWebhook`)
- SePay payment webhook handler for subscription payments

### AI Chatbot (`/api/Chatbot`)
- Google Gemini AI-powered chatbot
- Fallback to hardcoded responses if AI unavailable

### Activity Logs (`/api/LogActivity`)
- Platform-level audit trail for all admin actions

---

## ⚙️ Scheduled Jobs (Quartz.NET)

| Job | Schedule | Description |
|---|---|---|
| `WeeklyReportJob` | Every Monday 00:05 (VN time) | Generate weekly platform reports |
| `MonthlyReportJob` | 1st of month 00:30 (VN time) | Generate monthly platform reports |
| `CheckTrialJob` | Daily 01:00 (VN time) | Check and expire trial subscriptions |
| `PromotionExpiryJob` | Daily 00:00 (VN time) | Expire outdated promotions |
| `ShopStatusExpiryJob` | Daily 00:15 (VN time) | Expire shops with lapsed subscriptions |

---

## 🛠️ Tech Stack

| Technology | Details |
|---|---|
| Framework | ASP.NET Core 8.0 Web API |
| Language | C# (.NET 8) |
| Database | PostgreSQL (Aiven Cloud) |
| ORM | Entity Framework Core (Npgsql) |
| Authentication | JWT Bearer (HS256) |
| Object Mapping | AutoMapper 15 |
| Scheduler | Quartz.NET |
| Image Storage | Cloudinary |
| Email | Brevo (SMTP) |
| AI | Google Gemini API |
| Password Hashing | BCrypt.Net-Next |
| Excel Export | ClosedXML |
| API Docs | Swagger / Swashbuckle |
| Containerization | Docker |
| Deployment | Render (port 8080) |

---

## 🏛️ Project Structure

```
ASA-PLATFORM-BE/
├── ASA-PLATFORM-BE/          # API Layer (Controllers, Program.cs)
│   ├── Controllers/          # REST API endpoints
│   └── Program.cs            # DI registration, middleware pipeline
├── ASA-PLATFORM-SERVICE/     # Business Logic Layer
│   ├── Interface/            # Service interfaces
│   ├── Implenment/           # Service implementations
│   ├── DTOs/                 # Data Transfer Objects
│   ├── Mapping/              # AutoMapper profiles
│   ├── CronJobs/             # Quartz scheduled jobs
│   ├── Configurations/       # Cloudinary, Gemini, Email configs
│   └── Enums/                # Shared enumerations
└── ASA-PLATFORM-REPO/        # Data Access Layer
    ├── Models/               # EF Core entity models
    ├── DBContext/            # ASAPLATFORMDBContext
    ├── Repository/           # Repository classes
    └── Basic/                # Generic base repository
```

---

## 🚀 Getting Started

### Prerequisites
- .NET 8 SDK
- PostgreSQL database
- Docker (optional)

### Configuration

Create or update `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=...;Port=...;Database=ASA-PLATFORM-DB;Username=...;Password=..."
  },
  "JwtConfig": {
    "Issuer": "AsaPlatformBe",
    "Audience": "AsaPlatformUser",
    "Key": "<your-jwt-secret>",
    "ExpireMinutes": 180
  },
  "CloudinarySettings": {
    "CloudName": "<cloud-name>",
    "ApiKey": "<api-key>",
    "ApiSecret": "<api-secret>"
  },
  "GeminiSettings": {
    "ApiKey": "<gemini-api-key>",
    "Model": "gemini-2.0-flash-latest"
  },
  "BETenantUrl": {
    "Url": "https://<asa-tenant-be-url>"
  }
}
```

### Run Locally

```bash
cd ASA-PLATFORM-BE
dotnet restore ASA-PLATFORM-BE.sln
dotnet run --project ASA-PLATFORM-BE/ASA-PLATFORM-BE.csproj
```

API runs on `http://localhost:8081`
Swagger UI: `http://localhost:8081/swagger`

### Run with Docker

```bash
cd ASA-PLATFORM-BE
docker build -t asa-platform-be .
docker run -p 8081:8080 asa-platform-be
```

---

## 🔗 Service Communication

This service communicates with **ASA-TENANT-BE** via HTTP client:

```csharp
// Configured in Program.cs
builder.Services.AddHttpClient("BETenantUrl", client =>
{
    client.BaseAddress = new Uri(configuration["BETenantUrl:Url"]);
    client.Timeout = TimeSpan.FromSeconds(60);
});
```

---

## 📚 Course Information

- **Course**: EXE201 — Entrepreneurship Execution
- **Semester**: Semester 8
- **University**: FPT University

---

## 📝 License

This project is developed for educational and entrepreneurship purposes as part of the FPT University EXE201 program.
