# 🎓 Student Management System

A full-featured student management web application built with **ASP.NET Core 8 MVC**, **Azure Cosmos DB**, **Azure Blob Storage**, and **OAuth 2.0** (Google & GitHub) authentication.

---

## 📋 Table of Contents
- [Overview](#overview)
- [Features](#features)
- [Technology Stack](#technology-stack)
- [Prerequisites](#prerequisites)
- [Setup & Configuration](#setup--configuration)
  - [1. Clone the Repository](#1-clone-the-repository)
  - [2. Configure Azure Services](#2-configure-azure-services)
  - [3. Configure OAuth Providers](#3-configure-oauth-providers)
  - [4. Run the Application](#4-run-the-application)
- [Default Admin Credentials](#default-admin-credentials)
- [Project Structure](#project-structure)
- [Testing](#testing)
- [Security Considerations](#security-considerations)
- [Contributing](#contributing)
- [License](#license)

---

## 📖 Overview

The **Student Management System** is a web application that allows educational institutions to manage student records efficiently. It supports:

- **CRUD operations** for students.
- **Recycle Bin** – soft-delete and restore functionality.
- **Image uploads** for student profiles (stored in Azure Blob Storage).
- **Secure authentication** using OAuth 2.0 (Google, GitHub) or admin login.
- **Role-based authorization** – Admin and User roles.
- **Analytics dashboard** with charts and trends.
- **Search and pagination**.

---

## ✨ Features

### 👤 User Management
- OAuth login with **Google** and **GitHub**.
- Fallback **admin login** with email/password.
- Session-based cookie authentication.

### 👨‍🎓 Student Management
- **Create, Read, Update, Delete** (soft-delete).
- **Recycle Bin** – restore permanently deleted records.
- **Profile image upload** (resized to 400x400, stored in Azure Blob).
- **Search** by name, email, or ID.
- **Pagination** and sorting.

### 📊 Analytics & Dashboard
- Admin dashboard showing:
  - Total / Active / Inactive / Archived students.
  - Monthly registration trends.
  - Status distribution charts.
  - Recent activities.

### ☁️ Azure Integration
- **Cosmos DB** (NoSQL) for student and user data.
- **Blob Storage** for profile images.
- Automatic container creation on startup.

### 🔐 Security
- HTTPS enforced.
- Secure cookies with `HttpOnly`, `Secure`, `SameSite`.
- Admin-only policies.

---

## 🛠 Technology Stack

| Component               | Technology                              |
|-------------------------|-----------------------------------------|
| **Framework**           | ASP.NET Core 8 MVC                      |
| **Language**            | C# 12                                   |
| **Database**            | Azure Cosmos DB (NoSQL)                 |
| **Blob Storage**        | Azure Blob Storage                      |
| **Authentication**      | Cookie + OAuth 2.0 (Google, GitHub)    |
| **Image Processing**    | SixLabors.ImageSharp                    |
| **Testing**             | xUnit, Moq, FluentAssertions            |
| **Frontend**            | Bootstrap 5, Razor views, jQuery       |
| **Cloud Services**      | Azure (Cosmos DB, Blob Storage)         |
| **Package Manager**     | NuGet                                   |

---

## 📦 Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- [Visual Studio 2022+](https://visualstudio.microsoft.com/) or VS Code
- **Azure Subscription** (for Cosmos DB and Blob Storage) – free tier available
- **Git** (for cloning)
- **Google OAuth credentials** and/or **GitHub OAuth app**

> If you don't have Azure, you can modify the code to use in-memory or local alternatives (but this guide assumes Azure).

---

## ⚙️ Setup & Configuration

### 1. Clone the Repository

```bash
git clone https://github.com/gracemaluleke65-svg/StudentManagementSystem.git
cd StudentManagementSystem
