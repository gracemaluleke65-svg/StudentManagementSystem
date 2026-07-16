# 🎓 Student Management System

A full-featured student management web application built with **ASP.NET Core 8 MVC**, **Azure Cosmos DB**, **Azure Blob Storage**, and **OAuth 2.0** (Google & GitHub) authentication.  
Designed for educational institutions to manage student records, track enrolment status, and analyse trends – all backed by enterprise-grade Azure cloud services.

---

## 📖 Overview

The **Student Management System** is a production-ready web application that allows:

- **Admins** to manage student records (CRUD), archive inactive students, view analytics, and manage users.
- **Users** (staff/educators) to view student lists, search, and update profiles.
- **All users** to authenticate via Google or GitHub OAuth, with a fallback admin login.

The system uses **Azure Cosmos DB** for scalable NoSQL data storage and **Azure Blob Storage** for student profile images, with automatic container creation and image resizing.

---

## ✨ Key Features

### 👤 Authentication & Authorization
- OAuth 2.0 with **Google** and **GitHub**.
- Cookie-based authentication with 8‑hour sliding expiry.
- Role-based access: `Admin` vs `User`.
- Fallback admin login (email/password).

### 👨‍🎓 Student Management
- **CRUD** operations with server-side validation.
- **Soft delete** (moves to Recycle Bin) and **permanent delete**.
- **Recycle Bin** – restore deleted students or empty permanently.
- **Profile image upload** – automatic resize to 400x400, stored in Azure Blob, validation (JPEG/PNG, ≤5MB, ≤5000px).
- **Search** by name, email, or ID with pagination (10, 25, 50 per page).

### 📊 Analytics & Admin Dashboard
- Real-time metrics: total, active, inactive, archived students.
- Monthly registration trends (last 6 months).
- Status distribution chart (Active / Inactive / Archived).
- Recent activity feed (latest 5 additions and deletions).
- System health checks (Cosmos DB & Blob Storage status).

### ☁️ Azure Integration
- **Cosmos DB** – containers created automatically on startup.
- **Blob Storage** – secure, scalable image hosting.
- **SAS URLs** – temporary access to private images.

### 🛡️ Security
- HTTPS enforced in production.
- Secure cookies (`HttpOnly`, `Secure`, `SameSite=Lax`).
- Anti-forgery tokens on all POST forms.
- Input validation and sanitization.
- No sensitive data in logs.

### 🧪 Testing & Quality
- **Unit tests** for all controllers and services.
- **Mocked dependencies** (Moq) for isolated testing.
- **FluentAssertions** for readable assertions.
- Test coverage >80% on core logic.

---

## 🛠 Technology Stack

| Category               | Technology / Library                                                                 |
|------------------------|--------------------------------------------------------------------------------------|
| **Framework**          | ASP.NET Core 8 MVC                                                                   |
| **Language**           | C# 12                                                                                |
| **Database**           | Azure Cosmos DB (NoSQL, SQL API)                                                     |
| **Blob Storage**       | Azure Blob Storage                                                                   |
| **Authentication**     | Cookie + OAuth 2.0 (Google, GitHub) – `Microsoft.AspNetCore.Authentication.Google`   |
| **Image Processing**   | SixLabors.ImageSharp (v3.1.12)                                                       |
| **Testing**            | xUnit, Moq, FluentAssertions, Microsoft.AspNetCore.Mvc.Testing                       |
| **Frontend**           | Bootstrap 5, jQuery, Razor Views, Chart.js (for analytics)                           |
| **JSON Serialisation** | Newtonsoft.Json (v13.0.3)                                                            |
| **Logging**            | Built-in `ILogger`, Console + Debug sinks                                            |
| **CI/CD**              | GitHub Actions (optional, configurable)                                              |
| **Hosting**            | Azure App Service (recommended)                                                      |

---

## 📦 Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) (required)
- [Visual Studio 2022+](https://visualstudio.microsoft.com/) (with ASP.NET workload) or VS Code with C# extension
- **Azure Subscription** (free tier available) for Cosmos DB and Blob Storage
- **Git** (for cloning)
- **Google Cloud Project** (for OAuth) and/or **GitHub OAuth App**
- (Optional) **Azure CLI** for deployment

---

## ⚙️ Setup & Configuration

### 1. Clone the Repository

```bash
git clone https://github.com/gracemaluleke65-svg/StudentManagementSystem.git
cd StudentManagementSystem
