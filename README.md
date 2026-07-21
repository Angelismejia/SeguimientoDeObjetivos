# BetterMe — Seguimiento de Objetivos

## 📖 Descripción

BetterMe es una aplicación full-stack de seguimiento de objetivos personales: permite crear metas, dividirlas en tareas, llevar un diario, ganar insignias por logros y compartir el progreso con amigos (seguidores, rachas compartidas y chat en tiempo real).

El proyecto arrancó como una app de consola en C# hecha para una asignatura en ITLA, y evolucionó a una aplicación web completa con frontend en Angular y backend en ASP.NET Core siguiendo Clean Architecture.

---

## 🚀 Funcionalidades

**Objetivos y tareas**
- Crear, editar y eliminar objetivos, organizados por categorías
- Tareas asociadas a cada objetivo, con fecha programada y estado
- Cálculo automático de progreso según tareas completadas
- Dashboard con resumen del día y objetivos recientes

**Hábitos y seguimiento**
- Diario personal con entradas por día
- Racha de días consecutivos completando tareas
- Insignias/logros que se desbloquean según el progreso

**Social**
- Seguir a otros usuarios (seguidores/seguidos)
- Perfil propio y perfil público de cada usuario (foto, racha, insignias, objetivos)
- Rachas compartidas: invitá a un amigo y mantengan la racha juntos
- Chat en tiempo real vía SignalR, con notificaciones de mensajes nuevos
- Búsqueda de usuarios por nombre o username

**Cuenta**
- Registro e inicio de sesión con JWT
- Foto de perfil

---

## 🏗️ Arquitectura

**Backend — Clean Architecture (.NET 8)**

```
Backend/
├── SeguimientoDeObjetivos.Domain          # Entidades y reglas de negocio puras
├── SeguimientoDeObjetivos.Application     # Casos de uso, DTOs, interfaces de servicios
├── SeguimientoDeObjetivos.Infrastructure  # EF Core, acceso a datos, implementaciones
└── SeguimientoDeObjetivos.Api             # Controllers, SignalR hubs, autenticación JWT
```

**Frontend — Angular 21 (standalone components)**

```
Frontend/
└── src/app/
    ├── core/        # Servicios, modelos, guards, interceptors
    ├── features/    # Una carpeta por pantalla (dashboard, objectives, chat, profile, ...)
    └── shared/      # Componentes reutilizables (navbar, confirm-dialog, ...)
```

---

## 🛠️ Tecnologías

**Backend**: ASP.NET Core 8, Entity Framework Core, SQL Server, SignalR, JWT Bearer Auth

**Frontend**: Angular 21, Angular Material, Chart.js / ng2-charts, SignalR client, RxJS

---

## ▶️ Cómo correrlo localmente

### Backend

```bash
cd Backend
dotnet restore
dotnet ef database update --project SeguimientoDeObjetivos.Infrastructure --startup-project SeguimientoDeObjetivos.Api
dotnet run --project SeguimientoDeObjetivos.Api
```

Configurá la cadena de conexión a SQL Server en `SeguimientoDeObjetivos.Api/appsettings.json` antes de correr las migraciones.

### Frontend

```bash
cd Frontend
npm install
npm start
```

La app queda disponible en `http://localhost:4200`. El dev server proxea `/api` y `/hubs` hacia `http://localhost:5015` (ver `Frontend/proxy.conf.json`), así que el backend tiene que estar corriendo en paralelo.

---

## 👩‍💻 Autora

Desarrollado por **Angelis Mejía Soriano**
Estudiante de Desarrollo de Software en ITLA
