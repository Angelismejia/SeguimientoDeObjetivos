# Progreso del proyecto — dónde me quedé

_Última actualización: 2026-07-03_

## Estado del Backend — COMPLETO

El backend está 100% terminado y listo. No hace falta tocar nada más ahí.

### Qué tiene el backend:
- Clean Architecture en 4 capas (Domain, Application, Infrastructure, Api)
- Autenticación local con JWT + BCrypt (se quitó Keycloak porque requería Docker)
- CRUD completo para: Users, Categories, Objectives, Tasks, DiaryEntries, Notifications, Badges
- Todos los controllers protegidos con [Authorize] — requieren token JWT
- AuthController público (register y login no requieren token)
- Manejo global de errores con GlobalExceptionHandler
- Migraciones de EF Core listas

### Para correr el backend:
1. Configurar connection string en `Backend/SeguimientoDeObjetivos.Api/appsettings.json`
2. Correr migraciones:
   ```
   dotnet ef database update --project SeguimientoDeObjetivos.Infrastructure --startup-project SeguimientoDeObjetivos.Api
   ```
3. Correr la API:
   ```
   cd Backend/SeguimientoDeObjetivos.Api
   dotnet run
   ```

---

## Commits pendientes (hacer antes de empezar el frontend)

### Commit 1 — autenticación JWT:
```
git add Backend/SeguimientoDeObjetivos.Api/Program.cs
git add Backend/SeguimientoDeObjetivos.Api/SeguimientoDeObjetivos.Api.csproj
git add Backend/SeguimientoDeObjetivos.Api/appsettings.json
git add Backend/SeguimientoDeObjetivos.Api/Controllers/AuthController.cs
git add Backend/SeguimientoDeObjetivos.Api/Controllers/UsersController.cs
git add Backend/SeguimientoDeObjetivos.Api/Controllers/ObjectivesController.cs
git add Backend/SeguimientoDeObjetivos.Api/Controllers/CategoriesController.cs
git add Backend/SeguimientoDeObjetivos.Api/Controllers/TasksController.cs
git add Backend/SeguimientoDeObjetivos.Api/Controllers/DiaryEntriesController.cs
git add Backend/SeguimientoDeObjetivos.Api/Controllers/NotificationsController.cs
git add Backend/SeguimientoDeObjetivos.Api/Controllers/BadgesController.cs
git add Backend/SeguimientoDeObjetivos.Application/DTOs/Users/
git add Backend/SeguimientoDeObjetivos.Application/Interfaces/
git add Backend/SeguimientoDeObjetivos.Application/Services/
git add Backend/SeguimientoDeObjetivos.Application/DependencyInjection.cs
git add Backend/SeguimientoDeObjetivos.Application/SeguimientoDeObjetivos.Application.csproj
git add Backend/SeguimientoDeObjetivos.Domain/Entities/User.cs
git add Backend/SeguimientoDeObjetivos.Infrastructure/
```
Mensaje: `feat: replace Keycloak with local JWT authentication`

### Commit 2 — README:
```
git add README.md
```
Mensaje: `docs: update README`

---

## Estado del Frontend (Angular) — EN PROGRESO

### Ya está hecho:
- Proyecto Angular creado e instalado (`Frontend/`)
- `core/` completo:
  - `guards/auth.guard.ts` — bloquea rutas si no hay token
  - `interceptors/jwt.interceptor.ts` — agrega el token JWT a cada petición
  - `models/` — interfaces para User, Category, Objective, Task, DiaryEntry, Notification
  - `services/` — uno por entidad (auth, category, diary-entry, notification, objective, task)
- `app.routes.ts` configurado — todas las rutas protegidas con `authGuard` excepto landing/login/register
- `features/landing/` — landing page hecha
- `features/auth/` — login y register con formularios reales, toggle de mostrar contraseña, imágenes, "forgot password"
- `shared/components/navbar/` — navbar con lógica de autenticación y logout (último commit: `835ee53`)
- `shared/components/confirm-dialog/` — creado

### Pendiente — son placeholders "under construction" (~10-20 líneas, sin lógica real ni conexión a sus services):
- `features/dashboard/`
- `features/objectives/`
- `features/tasks/`
- `features/categories/`
- `features/diary/`
- `features/notifications/`
- `features/badges/`
- `features/profile/`

### Estructura actual:
```
Frontend/src/app/
├── core/
│   ├── guards/auth.guard.ts
│   ├── interceptors/jwt.interceptor.ts
│   ├── models/
│   └── services/
├── features/
│   ├── landing/          <- hecho
│   ├── auth/
│   │   ├── login/        <- hecho
│   │   └── register/     <- hecho
│   ├── dashboard/        <- placeholder
│   ├── objectives/       <- placeholder
│   ├── tasks/            <- placeholder
│   ├── categories/       <- placeholder
│   ├── diary/            <- placeholder
│   ├── notifications/    <- placeholder
│   ├── badges/           <- placeholder
│   └── profile/          <- placeholder
└── shared/
    └── components/
        ├── navbar/          <- hecho
        └── confirm-dialog/  <- hecho
```

### Conceptos clave de Angular que ya entendemos:
- Componente = pieza de pantalla (HTML + TypeScript + CSS)
- Service = el que habla con el backend, el componente nunca llama directo a la API
- Rutas = mapa de URLs, cada URL muestra un componente diferente
- Interceptor = agrega el token JWT automáticamente a cada petición HTTP
- Guard = protege rutas, si no estás logueado te manda al login
- Data binding = {{ }} en HTML se actualiza solo cuando cambia el TypeScript

### Siguiente paso — orden sugerido:
1. Conectar el layout (navbar + router-outlet) a las páginas reales
2. Construir el **dashboard** con datos reales del backend
3. Construir el CRUD de **objectives** (feature principal de la app)
4. Ir agregando el resto de pantallas: tasks, categories, diary, notifications, badges, profile

### Nota:
- Angular Material y Chart.js seguían en el stack decidido pero aún no se han instalado/usado — confirmar si se siguen queriendo antes de construir dashboard/objectives.

---

## Nota sobre Keycloak
- Se decidió NO usar Keycloak porque requiere Docker y hace difícil que otros corran el proyecto
- Si en el futuro se quiere implementar, se hará en una rama separada: `feature/keycloak-auth`
- La autenticación actual (JWT local + BCrypt) es suficiente para uso personal y portfolio
