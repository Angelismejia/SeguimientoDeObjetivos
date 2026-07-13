# Plan — Módulo Social: Amigos, Rachas y Chat

_Creado: 2026-07-10_
_Estado: planeando, sin código todavía_

## 1. Resumen

Agregar una capa social a la app: seguir a otros usuarios, rachas personales estilo Duolingo, rachas compartidas con amigos (estilo Snapchat: invitás a alguien puntual a mantener una racha juntos), chat en tiempo real, un perfil más completo, y un rediseño de la navegación para que quede más limpia. Este documento es la referencia para ir marcando qué está hecho, fase por fase.

## 2. Alcance de esta fase

- Seguir / seguidores (como Instagram: asimétrico, no requiere aceptación)
- Racha personal (completar ≥1 tarea al día la mantiene)
- Racha compartida con un amigo puntual (invitación que ambos aceptan; se mantiene si AMBOS completan ≥1 tarea ese día)
- Chat en tiempo real
- Perfil: foto, seguidores/seguidos, agregar por username, estadísticas/racha, insignias
- Rediseño de navegación: sin navbar lateral, categorías dentro del flujo de crear tarea, notificaciones y diario como íconos arriba, insignias dentro de perfil
- Responsive en todo el frontend

## 3. Decisiones de diseño que ya tomamos

- **"Seguir" ≠ "racha compartida".** Son dos cosas separadas. Podés seguir a mucha gente (como Instagram), pero la racha compartida es una relación puntual de a dos que se arma con una invitación explícita — igual que Snapchat, donde seguir a alguien no significa que tengan racha.
- **No hay ligas ni rankings semanales.** Se descartó explícitamente por el usuario.
- **Regla de racha personal:** completar al menos 1 tarea en el día la mantiene. No hace falta completar todas las tareas programadas.
- **Regla de racha compartida:** se mantiene solo si AMBOS usuarios completaron al menos 1 tarea ese mismo día.

## 4. Decisiones ya resueltas

- **Sin freeze por ahora.** MVP simple: si faltás un día, la racha se corta. El perdón de racha queda anotado como mejora futura, no entra en las 7 fases de abajo.
- **Racha guardada como contador**, no calculada al vuelo. Se actualiza en el mismo momento en que se completa una tarea (mismo patrón que ya usa el proyecto para recalcular el progreso de un objetivo cuando cambia una tarea).
- **Chat 1 a 1 únicamente.** Grupal queda fuera de alcance, se podría agregar después reusando la misma base de SignalR.
- **Foto de perfil: subida de archivo real**, no una URL pegada. Esto implica resolver almacenamiento en el backend (carpeta estática servida por la API, o un blob storage) — lo marcamos como punto técnico a definir en la Fase 1 cuando lleguemos ahí.

## 5. Modelo de datos nuevo (Backend)

### 5.1 Follow (seguir)
```
Follow
- Id
- FollowerId (User que sigue)
- FollowingId (User seguido)
- CreatedAt
```
Endpoints: `POST /follows`, `DELETE /follows/{id}`, `GET /users/{id}/followers`, `GET /users/{id}/following`, `GET /users/search?username=`

### 5.2 Racha personal
```
UserStreak
- UserId
- CurrentStreak
- LastCompletedDate
```
Se actualiza en el mismo lugar donde hoy se marca una tarea como completada (`toggleTaskComplete` / `submitTask` en el dashboard, del lado backend en `TaskService`).

### 5.3 Racha compartida
```
FriendStreakInvitation
- Id, FromUserId, ToUserId, Status (Pending/Accepted/Rejected), CreatedAt

FriendStreak
- Id, UserAId, UserBId, CurrentStreak, LastBothCompletedDate
```

### 5.4 Chat
```
Message
- Id, SenderId, ReceiverId, Content, SentAt, ReadAt
```
Tiempo real: un Hub de SignalR (`ChatHub`) con método `SendMessage` y evento `ReceiveMessage`. Esto es infraestructura nueva — hoy el proyecto no tiene WebSockets, solo HTTP REST.

### 5.5 Perfil
```
User (agregar campo)
- ProfilePhotoUrl
```

## 6. Rediseño de navegación (Frontend)

- Sacar el navbar lateral actual (`shared/components/navbar`) con sus links de texto.
- Reemplazar por una barra superior de íconos: Dashboard, Perfil (avatar), Amigos, Chat, 🔔 Notificaciones, 📔 Diario.
- **Categorías** deja de ser una página propia — se gestiona embebida dentro del modal de crear/editar tarea en el dashboard (ej. un botón "+ nueva categoría" al lado del selector de categoría).
- **Insignias** deja de ser una página propia — pasa a ser una sección/tab dentro de Perfil.
- Responsive en todos los breakpoints ya usados en el proyecto (480px / 650px / 900px).

## 7. Fases sugeridas (orden de implementación)

1. **Perfil base** — foto de perfil + página de perfil con estadísticas personales (sin nada social todavía)
2. **Racha personal** — contador visible en dashboard/perfil
3. **Seguir/seguidores** — buscar usuario por username, seguir, ver listas de seguidores/seguidos
4. **Racha compartida** — invitación + cálculo diario entre dos usuarios
5. **Rediseño de navegación** — top bar de íconos, categorías embebidas, insignias a perfil (se puede hacer en paralelo a las fases 2-4)
6. **Chat en tiempo real** — al final, es la pieza técnicamente más pesada (SignalR)
7. **Pulido responsive** — revisión general en todas las pantallas nuevas y viejas

## 8. Registro de avance

_(ir tachando acá a medida que se completa cada fase)_

- [ ] Fase 1 — Perfil base
- [ ] Fase 2 — Racha personal
- [ ] Fase 3 — Seguir/seguidores
- [ ] Fase 4 — Racha compartida
- [ ] Fase 5 — Rediseño de navegación
- [ ] Fase 6 — Chat en tiempo real
- [ ] Fase 7 — Pulido responsive
