# Personal Goal Tracker

## 📖 Description
Personal Goal Tracker is an initial version of a goal management application developed as a C# console application.  
It allows users to create, manage, and track personal objectives and their associated tasks, following a layered architecture approach in .NET.

This project was developed as part of an academic assignment at ITLA and represents the foundation for a more advanced application.

---

## 🚀 Features

- Create new personal goals
- Edit existing goals
- Delete goals
- View all goals with progress percentage
- Add multiple tasks to each goal
- Mark tasks as completed
- Automatic progress calculation based on completed tasks

---

## 🧠 How It Works

Each goal can contain multiple tasks.  
The system calculates progress dynamically based on how many tasks have been completed.

Example:
- 5 tasks → 3 completed = 60% progress

---

## 🏗️ Architecture

The application follows a **layered architecture**, separating responsibilities into different projects:

- **Entities**
  - Contains core models such as `Objetivo` and `Tarea`
  - Includes business logic like progress calculation

- **Data**
  - Handles data storage (in-memory list)
  - Implements CRUD operations for goals

- **Services**
  - Contains business logic layer
  - Acts as an intermediary between UI and Data

- **UI**
  - Console-based user interface
  - Handles user interaction and menu navigation

---

## 🛠️ Technologies Used

- C#
- .NET 8
- Console Application
- Object-Oriented Programming (OOP)

---

## 📂 Project Structure

The solution is organized into multiple layers, each with a specific responsibility:

**SeguimientoDeObjetivos.sln**  
│  
├── 📁 **SeguimientoDeObjetivos.Entities** 

│   ├── Objetivo.cs        # Goal entity with progress calculation  
│   ├── Tarea.cs           # Task entity  
│   └── DBconexion.cs      # (Reserved for future database connection)  
│  
├── 📁 **SeguimientoDeObjetivos.Data** 

│   └── RepositorioObjetivo.cs   # In-memory repository for managing goals  
│  
├── 📁 **SeguimientoDeObjetivos.Services** 

│   └── ServicioObjetivo.cs      # Business logic layer  
│  
├── 📁 **SeguimientoDeObjetivos.UI**  

│   └── Program.cs               # Console user interface (main menu & interaction)  
│  
└── **SeguimientoDeObjetivos.sln**   # Solution file

---

## ▶️ How to Run

1. Clone the repository:

git clone https://github.com/your-username/your-repo.git


2. Open the solution in Visual Studio

3. Set `SeguimientoDeObjetivos.UI` as Startup Project

4. Run the application

---

## 📌 Notes

- This version uses an in-memory data structure (no database)
- Designed as a foundational project to be later expanded into a web application
- Demonstrates separation of concerns using layered architecture

---

## 🚀 Future Improvements

- Convert to Web Application (ASP.NET Core)
- Add database integration (SQL Server)
- Implement user authentication
- Add habits and financial tracking
- Improve UI/UX with a modern interface

---

## 👩‍💻 Author

Developed by Candy **Angelis Mejia** Soriano 
Software Development Student at ITLA
