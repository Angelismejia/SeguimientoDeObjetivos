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

---

---

# Backend — API REST (v2)

Esta sección documenta la migración del proyecto original (app de consola) a una **API REST con ASP.NET Core 8** siguiendo los principios de **Clean Architecture**.

---

## Tabla de contenidos

1. [¿Qué es Clean Architecture?](#qué-es-clean-architecture)
2. [Estructura del proyecto](#estructura-del-proyecto)
3. [Capa Domain](#capa-domain)
4. [Capa Application](#capa-application)
5. [Capa Infrastructure](#capa-infrastructure)
6. [Capa API](#capa-api)
7. [Inyección de Dependencias](#inyección-de-dependencias)
8. [Migraciones y base de datos](#migraciones-y-base-de-datos)
9. [Flujo completo de una petición HTTP](#flujo-completo-de-una-petición-http)
10. [Endpoints disponibles](#endpoints-disponibles)
11. [Cómo ejecutar el proyecto](#cómo-ejecutar-el-proyecto)
12. [Decisiones técnicas tomadas en el camino](#decisiones-técnicas-tomadas-en-el-camino)
13. [Próximos pasos](#próximos-pasos)

---

## ¿Qué es Clean Architecture?

Clean Architecture es una forma de organizar el código en capas concéntricas donde **las capas internas no saben nada de las capas externas**.

```
         ┌──────────────────────────────┐
         │           API                │  ← Capa más externa (controllers, HTTP)
         │  ┌────────────────────────┐  │
         │  │     Infrastructure     │  │  ← Acceso a datos (EF Core, SQL Server)
         │  │  ┌──────────────────┐  │  │
         │  │  │   Application    │  │  │  ← Lógica de negocio (DTOs, interfaces)
         │  │  │  ┌────────────┐  │  │  │
         │  │  │  │   Domain   │  │  │  │  ← Núcleo (entidades puras)
         │  │  │  └────────────┘  │  │  │
         │  │  └──────────────────┘  │  │
         │  └────────────────────────┘  │
         └──────────────────────────────┘
```

**Regla de dependencia:** las flechas de dependencia apuntan siempre hacia adentro.
- Domain no depende de nadie.
- Application solo depende de Domain.
- Infrastructure depende de Application y Domain.
- API depende de Infrastructure y Application.

**¿Por qué esto importa?**
Si mañana cambias de SQL Server a PostgreSQL, solo tocas Infrastructure. Si mañana cambias de REST a GraphQL, solo tocas API. El Domain y Application permanecen intactos.

---

## Estructura del proyecto

```
Backend/
├── SeguimientoDeObjetivos.Domain/
│   └── Entities/               ← Las 10 entidades del negocio
│
├── SeguimientoDeObjetivos.Application/
│   ├── DTOs/                   ← Objetos de transferencia de datos
│   │   ├── Users/
│   │   ├── Categories/
│   │   ├── Objectives/
│   │   ├── Tasks/
│   │   ├── DiaryEntries/
│   │   ├── Notifications/
│   │   └── Badges/
│   └── Interfaces/
│       └── Repositories/       ← Contratos de acceso a datos
│
├── SeguimientoDeObjetivos.Infrastructure/
│   ├── Persistence/
│   │   └── ApplicationDbContext.cs   ← Contexto de EF Core
│   ├── Repositories/                 ← Implementaciones con EF Core
│   │   ├── UserRepository.cs
│   │   ├── CategoryRepository.cs
│   │   ├── ObjectiveRepository.cs
│   │   ├── TaskRepository.cs
│   │   ├── DiaryEntryRepository.cs
│   │   ├── NotificationRepository.cs
│   │   └── BadgeRepository.cs
│   └── DependencyInjection.cs        ← Registro de servicios
│
└── SeguimientoDeObjetivos.Api/
    ├── Controllers/            ← Endpoints HTTP
    │   ├── UsersController.cs
    │   ├── CategoriesController.cs
    │   ├── ObjectivesController.cs
    │   ├── TasksController.cs
    │   ├── DiaryEntriesController.cs
    │   ├── NotificationsController.cs
    │   └── BadgesController.cs
    ├── appsettings.json        ← Configuración (connection string)
    └── Program.cs              ← Punto de entrada de la aplicación
```

---

## Capa Domain

**Proyecto:** `SeguimientoDeObjetivos.Domain`
**Namespace:** `Domain.Entities`
**Dependencias externas:** ninguna — es código C# puro.

El Domain contiene solo las entidades del negocio. Una entidad es una clase que representa un concepto real de la aplicación. No tienen lógica de base de datos, no conocen EF Core, no conocen HTTP.

### Las 10 entidades

#### User
Representa al usuario de la aplicación. Tiene `KeycloakUserId` porque el sistema de autenticación externo que se planea usar (Keycloak) genera su propio ID. Tu base de datos guarda ese ID para vincular al usuario de Keycloak con el usuario de tu sistema.

```csharp
public class User
{
    public int Id { get; set; }
    public string KeycloakUserId { get; set; }  // ID generado por Keycloak
    public string Name { get; set; }
    public string Email { get; set; }
    public bool IsActive { get; set; } = true;  // soft delete: desactivar sin borrar
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }    // nullable: no fue editado aún
    // navegaciones hacia otras entidades...
}
```

#### Category
Las categorías organizan objetivos y tareas. Ejemplos: "Trabajo", "Salud", "Personal". Cada usuario crea las suyas. `IsDefault` permite marcar categorías que vienen precargadas al registrarse.

#### Objective
El objetivo es el centro de la app. Tiene un ciclo de vida definido por `Status`: `"Pending"` → `"InProgress"` → `"Completed"` (o `"Cancelled"`). `ProgressPercentage` permite mostrar una barra de progreso en la UI.

#### TaskItem
Se llama `TaskItem` y no `Task` porque `Task` es una clase reservada en C# para operaciones asíncronas (`System.Threading.Tasks.Task`). Nombrarla igual causaría conflictos de compilación.

Una tarea puede estar suelta o vinculada a un objetivo (`ObjectiveId` es nullable). Tiene soporte completo para recurrencia:
- `IsRecurring`: ¿se repite?
- `RecurrenceType`: "Daily", "Weekly", "Monthly", "None"
- `RepeatEveryWeeks`: cada cuántas semanas
- `EndRepeatDate`: hasta cuándo se repite
- `TaskRepeatDays`: en qué días de la semana (tabla separada)

#### TaskRepeatDay
Tabla auxiliar para los días de la semana en que se repite una tarea recurrente. Por ejemplo, una tarea que se repite lunes, miércoles y viernes genera 3 registros en esta tabla.

#### DiaryEntry
Entradas de diario personal. El campo `EntryDate` es la fecha a la que pertenece la entrada (elegida por el usuario), no `CreatedAt` (cuando se guardó). Esto permite escribir hoy sobre algo que pasó ayer.

#### Streak
Rastrea la racha de días consecutivos en que el usuario completó al menos una tarea. `CurrentStreak` es la racha actual y `BestStreak` es el récord histórico. Es un motivador para mantener consistencia.

#### Badge
Los badges son logros desbloqueables del sistema. Ejemplos: "Primera semana completa", "10 objetivos cumplidos". Los crea el administrador, no el usuario. `BadgeType` clasifica de qué tipo es el logro.

#### UserBadge
Tabla intermedia entre `User` y `Badge`. Registra qué usuario ganó qué badge y cuándo. Una relación muchos-a-muchos: un usuario puede tener muchos badges, un badge puede ser ganado por muchos usuarios.

#### Notification
Notificaciones generadas por el sistema. Pueden ser recordatorios de tareas, avisos de logros, etc. `IsRead` permite marcarlas como leídas. `SendEmail` indica si además de mostrarse en la app se envió por correo.

### Relaciones entre entidades

```
User ──────────┬── tiene 1 ──── UserSetting
               ├── tiene 1 ──── Streak
               ├── tiene N ──── Category
               ├── tiene N ──── Objective
               ├── tiene N ──── TaskItem
               ├── tiene N ──── DiaryEntry
               ├── tiene N ──── UserBadge ── apunta a ──── Badge
               └── tiene N ──── Notification

Objective ─────┬── pertenece a ─── User
               └── tiene N ──── TaskItem

TaskItem ──────┬── pertenece a ─── User
               ├── pertenece a ─── Objective (opcional)
               ├── pertenece a ─── Category (opcional)
               └── tiene N ──── TaskRepeatDay
```

### Por qué todas las fechas son `DateTime.UtcNow` y no `DateTime.Now`

`DateTime.Now` devuelve la hora local del servidor. `DateTime.UtcNow` devuelve la hora universal (UTC). Si el servidor está en España y el usuario en México, `DateTime.Now` guardaría la hora española en la base de datos. UTC garantiza que todos los registros tengan una referencia de tiempo consistente sin importar dónde está el servidor.

---

## Capa Application

**Proyecto:** `SeguimientoDeObjetivos.Application`
**Namespace:** `Application.*`
**Dependencias:** solo Domain

Esta capa tiene dos responsabilidades: definir los **DTOs** y los **contratos de repositorio (interfaces)**.

### DTOs (Data Transfer Objects)

Un DTO es un objeto que solo tiene propiedades, sin lógica. Su única función es transportar datos entre el cliente y la API.

**¿Por qué no usar las entidades directamente?**

Tres razones:
1. **Seguridad:** si expones la entidad directamente, el cliente ve campos internos que no debería ver.
2. **Protección:** si el cliente puede mandar la entidad directamente, podría cambiar campos que no debería (como el `UserId` de otro usuario).
3. **Evolución:** puedes cambiar la entidad internamente sin romper el contrato con el cliente, y viceversa.

**El patrón de tres DTOs:**

Para cada recurso hay hasta tres DTOs:

| DTO | Método HTTP | Propósito |
|-----|-------------|-----------|
| `XxxDto` | GET | Lo que la API devuelve |
| `CreateXxxDto` | POST | Lo que el cliente manda para crear |
| `UpdateXxxDto` | PUT | Lo que el cliente manda para editar |

`CreateXxxDto` nunca tiene: `Id` (lo genera la DB), `CreatedAt` (lo asigna el servidor), `UserId` (vendrá del token JWT cuando se implemente autenticación).

`UpdateXxxDto` puede tener `Status` y campos calculados si el usuario tiene permiso de cambiarlos (como `ProgressPercentage` en un objetivo).

### Interfaces de Repositorio

Una interfaz define un **contrato**: qué operaciones existen, sin decir cómo se implementan.

```csharp
// Application define el contrato
public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id);
    Task<User> CreateAsync(User user);
    // ...
}

// Infrastructure cumple el contrato usando EF Core
public class UserRepository : IUserRepository
{
    public async Task<User?> GetByIdAsync(int id)
        => await _context.Users.FindAsync(id);
}
```

**¿Por qué no usar `ApplicationDbContext` directamente en los controllers?**

Si los controllers conocen `ApplicationDbContext`, están acoplados a EF Core. Si mañana decides usar Dapper, o hacer un repositorio en memoria para tests, tendrías que modificar todos los controllers. Con la interfaz, cambias solo la implementación.

**¿Por qué los métodos retornan `Task<T>`?**

Porque las operaciones de base de datos son I/O (Input/Output). Mientras SQL Server procesa la consulta, el hilo de .NET queda libre para atender otras peticiones. `async/await` es la forma de escribir código asíncrono sin que se vea complicado.

```csharp
// Sin async: el hilo espera bloqueado mientras SQL responde
var user = _context.Users.Find(id);

// Con async: el hilo queda libre mientras espera a SQL Server
var user = await _context.Users.FindAsync(id);
```

**¿Por qué algunos métodos retornan `T?` (nullable) y otros `T`?**

- `GetByIdAsync` retorna `User?` porque el usuario puede no existir.
- `CreateAsync` retorna `User` (no nullable) porque si falla lanza una excepción, nunca retorna null.

---

## Capa Infrastructure

**Proyecto:** `SeguimientoDeObjetivos.Infrastructure`
**Namespace:** `Infrastructure.*`
**Dependencias:** Domain, Application, EF Core, SQL Server

Esta capa implementa todo lo que toca el mundo externo: base de datos, emails, servicios externos.

### ApplicationDbContext

El `DbContext` es el puente entre las entidades C# y las tablas SQL. Cada `DbSet<T>` representa una tabla.

```csharp
public class ApplicationDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<TaskItem> Tasks { get; set; }
    // etc...
}
```

EF Core lee estas propiedades para saber qué tablas crear en la migración.

### Repositorios

Cada repositorio implementa su interfaz usando el `ApplicationDbContext`. Reciben el contexto por constructor (inyección de dependencias):

```csharp
public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;

    public UserRepository(ApplicationDbContext context)  // ← DI lo inyecta automáticamente
    {
        _context = context;
    }
}
```

**Operaciones LINQ más comunes usadas:**

| Operación | Para qué |
|-----------|----------|
| `FindAsync(id)` | Buscar por clave primaria (más eficiente que Where) |
| `FirstOrDefaultAsync(x => x.Email == email)` | Buscar con condición, devuelve null si no existe |
| `Where(x => x.UserId == userId).ToListAsync()` | Filtrar y traer lista |
| `AnyAsync(x => x.Id == id)` | Verificar si existe sin traer el objeto |
| `Add(entity)` + `SaveChangesAsync()` | Insertar |
| `Update(entity)` + `SaveChangesAsync()` | Actualizar |
| `Remove(entity)` + `SaveChangesAsync()` | Eliminar |

**Decisiones de diseño en los repositorios:**
- `DiaryEntryRepository` ordena entradas por `EntryDate` descendente (las más recientes primero).
- `NotificationRepository` ordena por `CreatedAt` descendente.
- `BadgeRepository.AssignToUserAsync` verifica si el badge ya fue asignado antes de crear el registro, evitando duplicados.

### DependencyInjection.cs

Centraliza el registro de todos los servicios en un método de extensión:

```csharp
public static IServiceCollection AddInfrastructure(
    this IServiceCollection services,
    IConfiguration configuration)
{
    services.AddDbContext<ApplicationDbContext>(...);
    services.AddScoped<IUserRepository, UserRepository>();
    // etc...
    return services;
}
```

Un **método de extensión** agrega métodos a una clase existente sin modificarla. `this IServiceCollection services` significa "este método es una extensión de `IServiceCollection`". Por eso se puede llamar como `builder.Services.AddInfrastructure(...)`.

`AddScoped` crea **una instancia por request HTTP**. Es el ciclo de vida correcto para repositorios con `DbContext`.

---

## Capa API

**Proyecto:** `SeguimientoDeObjetivos.Api`
**Namespace:** `Api.Controllers`

### Controllers

Un controller es la puerta de entrada de cada petición HTTP. Recibe la petición, llama al repositorio, mapea la entidad al DTO, y devuelve la respuesta.

```csharp
[ApiController]
[Route("api/[controller]")]  // → api/users
public class UsersController : ControllerBase
{
    private readonly IUserRepository _userRepository;  // ← interfaz, NO la implementación

    public UsersController(IUserRepository userRepository)  // ← DI inyecta UserRepository
    {
        _userRepository = userRepository;
    }
}
```

`[ApiController]` activa la validación de modelos y el binding del body JSON.
`[Route("api/[controller]")]` genera la ruta a partir del nombre del controller. `UsersController` → `/api/users`.

### Códigos HTTP usados

| Código | Método | Cuándo |
|--------|--------|--------|
| `200 OK` | GET, PUT | Operación exitosa con datos |
| `201 Created` | POST | Recurso creado exitosamente |
| `204 No Content` | DELETE, PATCH | Operación exitosa sin datos que devolver |
| `404 Not Found` | GET, PUT, DELETE | El recurso no existe |
| `409 Conflict` | POST | El recurso ya existe (ej: badge ya asignado) |

### Mapping manual (entidad → DTO)

Cada controller tiene un método privado `ToDto` que convierte la entidad al DTO de respuesta:

```csharp
private static UserDto ToDto(User u) => new()
{
    Id = u.Id,
    Name = u.Name,
    // etc...
};
```

Es `static` porque no necesita acceso a ningún campo del controller. Para el sentido contrario (DTO → entidad), la creación se hace inline dentro de `Create` y `Update`.

### Endpoints especiales que merecen explicación

**`PATCH /api/notifications/{id}/read`** — Usa `PATCH` (modificación parcial) en lugar de `PUT` (modificación completa) porque solo cambia el campo `IsRead`. Es más semántico y correcto en REST.

**`GET /api/tasks/by-objective/{objectiveId}`** — Permite obtener las tareas de un objetivo específico, útil para mostrar el progreso detallado.

**`GET /api/notifications/unread?userId=1`** — Solo las no leídas, para el contador de notificaciones de la UI.

**`POST /api/badges/assign?userId=1&badgeId=2`** — Retorna `409 Conflict` si el usuario ya tiene ese badge.

**`TasksController.Update`** — Cuando el `Status` cambia a `"Completed"` y `CompletedAt` es null, se establece `CompletedAt = DateTime.UtcNow` automáticamente. La fecha de completado solo se fija una vez.

---

## Inyección de Dependencias

La **Inyección de Dependencias (DI)** es el mecanismo por el cual ASP.NET Core crea y entrega los objetos que cada clase necesita automáticamente.

### El flujo completo

1. Al arrancar la app, `Program.cs` registra los servicios:
```csharp
builder.Services.AddInfrastructure(builder.Configuration);
// esto registra: DbContext + 7 repositorios
```

2. Cuando llega una petición a `POST /api/users`, ASP.NET crea `UsersController`.

3. Para crear `UsersController`, necesita un `IUserRepository`. Busca en el registro: "¿quién implementa `IUserRepository`?" → `UserRepository`.

4. Para crear `UserRepository`, necesita `ApplicationDbContext`. Lo crea y lo inyecta.

5. Todo esto es automático — el código solo declara lo que necesita en el constructor.

```
ASP.NET Core (contenedor de DI)
│
├── "Necesito UsersController"
│   └── UsersController necesita IUserRepository
│       └── IUserRepository → UserRepository
│           └── UserRepository necesita ApplicationDbContext
│               └── ApplicationDbContext → creado con connection string
│
└── Entrega UsersController completamente construido
```

---

## Migraciones y base de datos

### ¿Qué es una migración?

Una migración es un archivo C# generado automáticamente por EF Core que contiene instrucciones SQL para crear o modificar la base de datos. En vez de escribir `CREATE TABLE` a mano, describes tu modelo en C# y EF genera el SQL.

### Comandos

Crear la migración (genera los archivos C#):
```bash
dotnet ef migrations add InitialCreate \
  --project SeguimientoDeObjetivos.Infrastructure \
  --startup-project SeguimientoDeObjetivos.Api
```

Aplicar la migración a la base de datos (ejecuta el SQL):
```bash
dotnet ef database update \
  --project SeguimientoDeObjetivos.Infrastructure \
  --startup-project SeguimientoDeObjetivos.Api
```

### Por qué `--project` y `--startup-project` son distintos

- `--project`: dónde está el `DbContext` (Infrastructure)
- `--startup-project`: el proyecto que arranca y lee la configuración (Api, donde está `appsettings.json` con la connection string)

### Connection string

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=ANGELIS-LAPTOP\\SQLEXPRESS;Database=SeguimientoObjetivosDB;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

- `Trusted_Connection=True`: usa autenticación de Windows (no necesitas usuario/contraseña)
- `TrustServerCertificate=True`: evita errores de certificado SSL en desarrollo local

---

## Flujo completo de una petición HTTP

Ejemplo: `POST /api/objectives?userId=1` con body `{ "title": "Aprender C#", "endDate": "2026-12-31" }`

```
1. HTTP Request llega a la API
   POST /api/objectives?userId=1
   Body: { "title": "Aprender C#", "endDate": "2026-12-31" }

2. ASP.NET Core enruta al ObjectivesController.Create()
   - Deserializa el JSON → CreateObjectiveDto
   - Extrae userId del query param

3. El controller crea la entidad Objective:
   var objective = new Objective
   {
       Title = "Aprender C#",       ← del DTO
       EndDate = 2026-12-31,        ← del DTO
       UserId = 1,                  ← del query param
       Status = "Pending",          ← valor por defecto en la entidad
       ProgressPercentage = 0,      ← valor por defecto en la entidad
       CreatedAt = DateTime.UtcNow  ← valor por defecto en la entidad
   };

4. Llama al repositorio:
   var created = await _objectiveRepository.CreateAsync(objective);

5. ObjectiveRepository.CreateAsync():
   _context.Objectives.Add(objective);   ← EF hace tracking del objeto
   await _context.SaveChangesAsync();    ← EF genera y ejecuta el INSERT SQL
   // La DB asigna el Id autoincrement → objective.Id = 42
   return objective;

6. El controller mapea la entidad al DTO:
   return CreatedAtAction(..., ToDto(created));

7. HTTP Response:
   Status: 201 Created
   Location: /api/objectives/42
   Body: {
     "id": 42,
     "title": "Aprender C#",
     "status": "Pending",
     "progressPercentage": 0,
     "userId": 1,
     "createdAt": "2026-06-18T..."
   }
```

---

## Endpoints disponibles

### Users
| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | `/api/users` | Todos los usuarios |
| GET | `/api/users/{id}` | Un usuario por ID |
| POST | `/api/users` | Crear usuario |
| PUT | `/api/users/{id}` | Editar usuario |
| DELETE | `/api/users/{id}` | Eliminar usuario |

### Categories
| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | `/api/categories?userId=1` | Categorías de un usuario |
| GET | `/api/categories/{id}` | Una categoría |
| POST | `/api/categories?userId=1` | Crear categoría |
| PUT | `/api/categories/{id}` | Editar categoría |
| DELETE | `/api/categories/{id}` | Eliminar categoría |

### Objectives
| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | `/api/objectives?userId=1` | Objetivos de un usuario |
| GET | `/api/objectives/{id}` | Un objetivo |
| POST | `/api/objectives?userId=1` | Crear objetivo |
| PUT | `/api/objectives/{id}` | Editar objetivo |
| DELETE | `/api/objectives/{id}` | Eliminar objetivo |

### Tasks
| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | `/api/tasks?userId=1` | Tareas de un usuario |
| GET | `/api/tasks/{id}` | Una tarea |
| GET | `/api/tasks/by-objective/{objectiveId}` | Tareas de un objetivo |
| POST | `/api/tasks?userId=1` | Crear tarea |
| PUT | `/api/tasks/{id}` | Editar tarea |
| DELETE | `/api/tasks/{id}` | Eliminar tarea |

### DiaryEntries
| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | `/api/diaryentries?userId=1` | Entradas de diario de un usuario |
| GET | `/api/diaryentries/{id}` | Una entrada |
| POST | `/api/diaryentries?userId=1` | Crear entrada |
| PUT | `/api/diaryentries/{id}` | Editar entrada |
| DELETE | `/api/diaryentries/{id}` | Eliminar entrada |

### Notifications
| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | `/api/notifications?userId=1` | Todas las notificaciones |
| GET | `/api/notifications/unread?userId=1` | Solo no leídas |
| GET | `/api/notifications/{id}` | Una notificación |
| POST | `/api/notifications` | Crear notificación |
| PATCH | `/api/notifications/{id}/read` | Marcar como leída |
| DELETE | `/api/notifications/{id}` | Eliminar |

### Badges
| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | `/api/badges` | Todos los badges |
| GET | `/api/badges/{id}` | Un badge |
| GET | `/api/badges/by-user/{userId}` | Badges ganados por un usuario |
| POST | `/api/badges/assign?userId=1&badgeId=2` | Asignar badge a usuario |

---

## Cómo ejecutar el proyecto

### Requisitos
- .NET 8 SDK
- SQL Server Express (LocalDB o instancia nombrada)
- Visual Studio 2022 o VS Code

### Pasos

1. Clonar el repositorio
2. Abrir `Backend/SeguimientoDeObjetivos.sln`
3. Verificar la connection string en `SeguimientoDeObjetivos.Api/appsettings.json`
4. Aplicar la migración:
   ```bash
   dotnet ef database update --project SeguimientoDeObjetivos.Infrastructure --startup-project SeguimientoDeObjetivos.Api
   ```
5. Ejecutar el proyecto `SeguimientoDeObjetivos.Api`
6. Abrir `https://localhost:{puerto}/swagger` para ver y probar todos los endpoints

---

## Decisiones técnicas tomadas en el camino

### TaskItem en lugar de Task
La entidad de negocio se llama `Task` en el dominio del problema, pero C# tiene `System.Threading.Tasks.Task` reservado. Se renombró a `TaskItem` para evitar conflictos de compilación.

### FK de Notification con atributo explícito
La entidad `Notification` tenía una propiedad FK llamada `TaskId` pero la navegación era `TaskItem`. Por convención EF Core busca `TaskItemId` como FK para una navegación llamada `TaskItem`. Se agregó `[ForeignKey("TaskId")]` para decirle explícitamente a EF qué propiedad usar.

```csharp
public int? TaskId { get; set; }

[ForeignKey("TaskId")]   // sin esto, EF buscaría TaskItemId y TaskId quedaría como columna huérfana
public TaskItem? TaskItem { get; set; }
```

### EF Core solo en Infrastructure
Domain y Application tenían referencias a `Microsoft.EntityFrameworkCore.SqlServer`. Esto viola Clean Architecture: Domain no debe conocer infraestructura. Se eliminaron esas dependencias de capas que no las necesitan.

### DateTime.UtcNow unificado
Algunas entidades usaban `DateTime.Now` y otras `DateTime.UtcNow`. Se unificó todo a `DateTime.UtcNow` para garantizar consistencia sin importar dónde se despliega el servidor.

### Renombre de carpeta Infrastructure
La carpeta original se llamaba `SeguimientoDeObjetivos.Infraestructure` (con typo). Se renombró a `SeguimientoDeObjetivos.Infrastructure`. No fue necesario rehacer la migración porque los archivos de migración se mueven con la carpeta y no contienen rutas absolutas.

### CompletedAt se fija automáticamente
En `TasksController.Update`, cuando el status cambia a `"Completed"` y `CompletedAt` aún es null, se establece `CompletedAt = DateTime.UtcNow` en ese momento. La fecha de completado solo se fija una vez y no se puede sobreescribir por accidente.

---

## Próximos pasos

En orden de prioridad:

1. **Autenticación con Keycloak** — proteger los endpoints con JWT tokens. Eliminar el `userId` de query params y sacarlo del token de autenticación.

2. **Capa de Servicios** — actualmente los controllers mapean directamente entidad ↔ DTO. Agregar una capa de servicios centraliza la lógica de negocio (por ejemplo, calcular `ProgressPercentage` automáticamente al completar tareas vinculadas).

3. **Validaciones** — agregar `[Required]`, `[MaxLength]`, etc. en los DTOs, o usar FluentValidation para reglas más complejas.

4. **Paginación** — los endpoints que devuelven listas deberían devolver páginas de resultados, no todos los registros de una vez.

5. **AutoMapper o Mapster** — reemplazar el mapeo manual `ToDto()` por una librería cuando el número de entidades crezca.

6. **Tests** — tests unitarios para los repositorios (usando base de datos en memoria) y tests de integración para los controllers.

7. **Lógica de Streak** — implementar la lógica que actualiza `CurrentStreak` y `BestStreak` automáticamente cuando el usuario completa tareas.

8. **Lógica de Badges** — implementar las reglas para asignar badges automáticamente según los logros del usuario.
