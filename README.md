# 🧩 MiniDashboard

**MiniDashboard** is a lightweight desktop and web solution built with **WPF (.NET 8)** and an **ASP.NET Core Web API** backend.  
It allows users to load, view, edit, and search dynamic JSON-based datasets through a modern, MVVM-structured interface.

---

## 🚀 Features

- 🪟 **WPF Frontend**
  - Dynamic data grids that adapt to JSON schema
  - Inline editing and data management
  - Search functionality
  - Responsive MVVM architecture using `CommunityToolkit.Mvvm`

- 🌐 **ASP.NET Core Web API**
  - CRUD operations (`GET`, `POST`, `PUT`, `DELETE`) on JSON datasets
  - Dataset discovery and structured data handling
  - Text-based search across all fields
  - File-based data storage (no database required)

- 🧪 **Testing**
  - Unit tests for the WPF ViewModel using **xUnit**, **Moq**, and **FluentAssertions**
  - Integration tests for the API using **WebApplicationFactory**
  - Temporary data isolation in tests (no pollution between runs)

---

## 📁 Project Structure

MiniDashboard.sln
|
├── MiniDashboard.Api
│   ├── Controllers
│   │   └── DataController.cs
│   ├── Program.cs
│   ├── appsettings.json
│   └── MiniDashboard.Api.csproj
|
├── MiniDashboard.App
│   ├── ViewModels
│   │   ├── ItemsViewModel.cs
│   │   ├── DynamicItem.cs
│   │   └── RelayCommand.cs
│   ├── Services
│   │   └── DataApiService.cs
│   ├── Views
│   │   └── MainWindow.xaml
│   ├── App.xaml
│   └── MiniDashboard.App.csproj
|
├── MiniDashboard.Tests
│   ├── ViewModels
│   │   └── ItemsViewModelTests.cs
│   ├── Services
│   │   └── DataApiServiceTests.cs
│   └── MiniDashboard.Tests.csproj
|
├── MiniDashboard.Api.IntegrationTests
│   ├── CustomWebApplicationFactory.cs
│   └── DataControllerIntegrationTests.cs
|
└── README.md

---

## 🧰 Technologies Used

| Layer | Technologies |
|-------|---------------|
| Frontend | WPF, MVVM, CommunityToolkit.Mvvm |
| Backend | ASP.NET Core 8 Web API |
| Data Storage | Local JSON files |
| Testing | xUnit, Moq, FluentAssertions, Microsoft.AspNetCore.Mvc.Testing |

---

## ⚙️ Getting Started

## Prerequisites

Before building or running the solution, make sure you have the following installed:

1. **.NET SDK**
   - Version: 8.0
   - Download from: [https://dotnet.microsoft.com/en-us/download](https://dotnet.microsoft.com/en-us/download)

2. **IDE / Editor**
   - Visual Studio 2022/2023 (with **.NET desktop development** and **ASP.NET and web development** workloads installed),  
     or Visual Studio Code with the C# extension.

3. **NuGet Packages**
   - The solution uses several NuGet packages such as:
     - `CommunityToolkit.Mvvm`
     - `Moq`
     - `FluentAssertions`
   - Running `dotnet restore` will automatically install all required packages.
   
---

## 🧪 Running Tests

### Unit Tests
- Run the tests in the `MiniDashboard.Tests` project to verify ViewModel logic.

### Integration Tests
- Run the tests in the `MiniDashboard.Api.IntegrationTests` project to verify API endpoints.

---

## 🧱 Example API Endpoints

| Method | Endpoint                                  | Description            |
|--------|-------------------------------------------|------------------------|
| GET    | /api/data/datasets                        | List all datasets      |
| GET    | /api/data/{dataset}                       | Get items in dataset   |
| POST   | /api/data/{dataset}                       | Add a new item         |
| PUT    | /api/data/{dataset}/{id}                  | Update an existing item|
| DELETE | /api/data/{dataset}/{id}                  | Delete an item         |
| GET    | /api/data/{dataset}/search?query=term     | Search dataset          |

---

## 🧑‍💻 Author
Matthew Jack

---

## 📄 License
MIT License
