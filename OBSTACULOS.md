# Obstáculos y decisiones técnicas

Este documento registra los problemas reales que aparecieron construyendo BetterMe:
qué se rompió, por qué, cómo se arregló y qué quedó aprendido. No es una lista de
tareas ni un changelog; es el razonamiento detrás de las decisiones que hoy están en
el código, para que quien lo lea (o yo misma en seis meses) no tenga que volver a
descubrirlo.

Cada sección cita los commits donde vive el cambio.

---

## 1. Los datos de cada usuario estaban abiertos a cualquier usuario logueado

**El problema (IDOR — Insecure Direct Object Reference).**
Los controladores recibían el `userId` por query string y confiaban en él. Pero ese
número lo elige quien llama. Estar autenticado como cualquier usuario alcanzaba para
cambiar un id en la URL y operar sobre los datos de otro.

No fue un descuido puntual: fue el mismo error repetido en casi todo el backend, y
salió en tandas a medida que se auditaba controller por controller.

| Commit | Qué estaba expuesto |
|---|---|
| `637a90e` | Tareas, objetivos y conversaciones de chat ajenas. Además, el secreto del JWT estaba commiteado en `appsettings.json` de un repo público: cualquiera podía firmar un token válido para cualquier `userId` sin siquiera loguearse. |
| `abacbee` | `UsersController` entero: borrar la cuenta de otro sin pedir contraseña, cambiarle nombre/email, pisarle la foto, y leer su export completo de datos incluido el diario privado. |
| `9139ddc` | El diario, las categorías, los follows y las invitaciones de racha compartida. También se quitó `POST badges/assign`, que dejaba regalarse cualquier insignia. |
| (2026-09-30) | Las notificaciones: leer, marcar y borrar las alertas de cualquiera, y `POST /api/notifications` permitía fabricarle una alerta falsa a cualquier usuario. |

**La solución.**
La identidad sale siempre del claim del token (`ClaimTypes.NameIdentifier`), nunca de
la URL. Cada endpoint que opera sobre un recurso por id comprueba la propiedad antes
de leer, editar o borrar, y devuelve `403` si no corresponde. Los endpoints que
existían solo como superficie de ataque y que el frontend nunca llamaba se eliminaron
en vez de protegerse.

Lo que queda deliberadamente abierto está documentado con un comentario en el propio
controller: las listas de seguidores son información social, y `GetById` /
`GetByUsername` siguen públicos porque se usan para buscar perfiles — pero ocultan el
email cuando el perfil consultado no es el propio.

**Lo aprendido.**

Un endpoint que recibe un id por la URL tiene que contrastarlo contra el token
*siempre*, aunque el frontend propio nunca mande un id ajeno. El frontend no es la
frontera de seguridad: cualquiera puede hablarle a la API directo.

Y una segunda lección, más cara: **un test que mira la forma del código no prueba el
comportamiento.** `ControllersProtegenPropiedadTests` se escribió justamente para que
esto no volviera a pasar en silencio, y verifica que todo controller que reciba
`userId` por query defina una propiedad `RequesterId`. `NotificationsController` la
definía —la usaba un solo endpoint, `read-all`— así que **pasaba el test con cinco
endpoints desprotegidos**. El test daba verde falso.

Por eso la protección de notificaciones se cubrió con `NotificacionesSonPrivadasTests`,
que no mira la forma: llama a cada endpoint haciéndose pasar por otro usuario y exige
un `403`. Y no se conforma con el código de respuesta — comprueba además que la acción
**no se haya ejecutado**, porque un `403` devuelto después de borrar el dato no sirve
de nada.

---

## 2. La racha se perdía sola

Este fue el problema más largo de cerrar, porque no era un bug sino una decisión de
modelado que se filtró a seis lugares distintos.

**El problema.**
Una tarea recurrente es **una sola fila con una única `ScheduledDate`**. No había
historial por día. Así que "¿en qué días se completó algo?" —la pregunta de la que
dependen la racha y los mapas de calor— se respondía leyendo esa única fecha. Una
tarea recurrente marcada lunes, martes y miércoles aportaba **un solo día**. Las
rachas se cortaban solas y los mapas de calor mostraban huecos donde sí había habido
actividad.

**La cadena de arreglos.**

- `d4c6afa` — Al completar una recurrente, el backend movía su `ScheduledDate` a hoy
  para que la racha viera el día como completado. En el frontend, una recurrente pasa
  a contar como "hecha" solo si su fecha es exactamente el día que se está mostrando,
  así que al otro día vuelve a verse pendiente en vez de quedar marcada para siempre.
- `45819dc` — Marcar una recurrente desde el calendario en un día pasado la movía
  igual a hoy, dejando el día pasado sin marcar y rompiendo el conteo. Ahora se guarda
  el día que se está marcando. En el mismo commit: un objetivo con tareas recurrentes
  dejó de completarse solo al llegar a 100%, porque un hábito no "termina" por estar al
  día hoy.
- `ec90ef4` — Se agregó el historial real por día (`TaskCompletion`, con índice único
  `(TaskId, Date)`), que es lo que faltaba desde el principio.
- `7244cd3` — El arreglo anterior se había quedado corto. La misma pregunta se
  respondía con **seis copias del mismo bucle escritas a mano en cuatro pantallas**:
  el mapa de calor de estadísticas, la racha y el mapa de 364 días del perfil, el
  listado de un día concreto, la racha del amigo, y la racha y el mapa de cada
  objetivo. Por eso arreglar una no arreglaba las otras, y por eso el perfil y el
  dashboard mostraban rachas distintas para el mismo usuario. Las seis pasan ahora por
  `completedDayKeys()` / `isTaskDoneOn()` en `core/utils/task-status.util.ts`.

**Lo aprendido.**
Cuando la misma pregunta se responde en varios lugares, arreglar uno da la falsa
sensación de haber terminado. Antes de dar por cerrado un bug de cálculo conviene
buscar todas las copias de ese cálculo: el arreglo real fue centralizar la decisión en
un helper, no corregir el bucle.

---

## 3. La hora del servidor no es la hora del usuario

**El problema, primera mitad: los cálculos.**
Varios servicios comparaban `DateTime.UtcNow` / `DateTime.Today` —la hora del
servidor— contra fechas que en realidad representan el **día local** del usuario. La
racha para las insignias podía quedar corta durante la noche, retrasando el otorgado;
lo mismo con la racha compartida; y los recordatorios llegaban desfasados.

**La solución** (`713eddb`): se agregó `IAppClock` (Application) con su implementación
`AppClock` (Infrastructure), que traduce `DateTime.UtcNow` a la zona configurada en
`AppTimeZone` (`appsettings.json`). Acepta tanto el id de Windows como el IANA, y si no
resuelve cae a UTC en vez de tirar una excepción al arrancar. Los servicios reciben
"hoy" ya calculado en vez de preguntárselo a `DateTime`.

**El problema, segunda mitad: lo que se muestra en pantalla.**
Todas las marcas de tiempo de las entidades nacen como `DateTime.UtcNow`, pero las
columnas son `timestamp without time zone`. Npgsql las devuelve con
`DateTimeKind.Unspecified` y `System.Text.Json` serializa eso **sin la `Z` final**: al
frontend llega `"2026-09-30T20:40:05"`.

El navegador interpreta una fecha sin zona como **hora local**. Como el valor guardado
es UTC, cada hora que muestra la app queda corrida por el desfase de la zona: un
mensaje enviado a las 16:40 se ve como enviado a las 20:40.

**El estado actual.** El contrato quedó fijado por tests (`FechasQueViajanAlFrontTests`)
y el frontend tiene `fechaDelBackend()` en `core/utils/fecha.util.ts`, que agrega la
`Z` solo si el texto no trae ya una zona —así sigue funcionando el día que el backend
empiece a mandarla—. **Por ahora lo usa únicamente el chat**; el resto de las pantallas
todavía formatea la fecha cruda con el `DatePipe` y sigue mostrando las horas corridas.
Aplicarlo en todas es trabajo pendiente.

La alternativa de fondo —pasar las columnas a `timestamptz`— exige una migración sobre
la base de producción, y por eso no se tomó a la ligera (ver sección 6).

**Lo aprendido.**
"Guardar en UTC" solo es media decisión. La otra mitad es **decir que es UTC** en el
borde del sistema. Un `timestamp` sin zona viaja como un texto ambiguo, y el navegador
resuelve esa ambigüedad de la peor manera posible: en silencio y mal.

---

## 4. Una regla de negocio nueva rompe los datos viejos

**El problema.**
Se agregó una validación de rango de fechas en los objetivos (`d3a3a5a`). Correcta,
pero llegó **después** de que la base ya hubiera aceptado objetivos con las fechas
dadas vuelta. Esas filas quedaron imposibles de actualizar por cualquier motivo,
incluso por uno que no tocaba las fechas.

Y se notó de la peor forma: marcar una tarea mostraba *"La fecha de fin no puede ser
anterior a la de inicio"*. El mensaje venía del **objetivo**, no de la tarea — al
marcar una tarea el frontend recalcula el progreso del objetivo ligado y le manda un
`PUT` completo, que reenvía `startDate` y `endDate` tal como estaban guardados.

**La solución** (`8493651`): el rango se valida **solo si el `PUT` lo está cambiando**,
comparando por día para no confundir un cambio de hora con un cambio de rango. Quien
edite las fechas sigue obligado a dejarlas en orden. `TaskService` tenía la misma
trampa con los horarios y se arregló igual, mirando cada regla por separado para que
corregir el horario no obligara a arreglar también el fin de repetición.

El mismo patrón se aplicó después a la validación de recurrencia (`92619f9`):
`ValidarRecurrenciaQueCambio` en el `PUT`, para no bloquear las filas ya guardadas mal.

**Lo aprendido.**
Toda regla de negocio nueva que se aplique en un `PUT` completo necesita preguntarse
qué pasa con las filas que ya están guardadas sin cumplirla. El patrón que quedó:
**validar solo lo que el request está cambiando.**

---

## 5. El arranque en frío de la base le mostraba EF Core al usuario

**El problema.**
Sondeando producción, un login contestó:

> HTTP 400 — "An exception has been raised that is likely due to a transient failure.
> Consider enabling transient error resiliency by adding 'EnableRetryOnFailure' to the
> 'UseSqlServer' call."

Dos fallos encadenados, y el segundo es el grave.

**El primero:** la base era Azure SQL serverless con auto-pause, así que se apagaba
sola y la primera consulta que llegaba mientras despertaba fallaba. Tardó 26 segundos.
No es un incidente raro: es el arranque en frío de todos los días. Faltaba
`EnableRetryOnFailure`, justo lo que EF sugiere en el mensaje. Era seguro ponerlo
porque no hay transacciones explícitas en el código; si las hubiera, habría que
reintentarlas enteras dentro de la estrategia.

**El segundo:** `GlobalExceptionHandler` convertía en `400` **cualquier**
`InvalidOperationException`, dando por hecho que solo las lanzaban las reglas de
negocio. Pero EF Core lanza una cuando pierde la conexión, así que un fallo de
infraestructura se presentaba como un error de formulario, con el texto interno de EF
metido en el `Detail` — y ese `Detail` se muestra tal cual en pantalla.

**La solución** (`508e11a`): las reglas de negocio pasaron a tener su propia excepción,
`BusinessRuleException`, y solo el mensaje de esa se le devuelve al cliente, porque
está escrito para leerse. Todo lo demás cae en el `500` genérico. Fueron 23 sitios. El
único `InvalidOperationException` que se dejó como estaba es *"JWT Secret not
configured"*, que no es culpa del usuario y debe ser un `500`.

**Lo aprendido.**
Un tipo de excepción no es una categoría de error. Mapear `InvalidOperationException` a
`400` parecía razonable hasta que una librería de terceros la usó para otra cosa. Las
reglas de negocio necesitan **su propio tipo**, y el mensaje que ve el usuario tiene
que estar escrito a propósito para él, nunca ser el texto que venga adentro de una
excepción cualquiera.

---

## 6. Migrar la base con la app en producción

**El problema.** La cuota mensual gratuita de Azure se agotó y la base quedó caída
hasta el mes siguiente.

**La solución** (`9ae2b4e`): migración de Azure SQL a **PostgreSQL en Supabase**. Trajo
su propia cola de detalles:

- Hay que conectarse por el **session pooler** de la región; el host directo es solo
  IPv6 y no resuelve desde cualquier lado.
- PostgreSQL rechaza escribir un `DateTime` con `Kind = Utc` en columnas
  `timestamp without time zone`, lo que obligó a normalizar las fechas al guardar
  (`fde6521`).

**Dos cosas que hay que tener presentes siempre en este proyecto:**

1. **Nada aplica las migraciones de EF solo.** Ni el deploy ni el CI. Si la base va
   atrás respecto del modelo, **todos los endpoints de esa entidad devuelven 500**. La
   migración se aplica a mano.
2. **Al regenerar una migración hay que borrar también el `ModelSnapshot`.** Si no, EF
   cree que el cambio ya está aplicado y genera una migración vacía, que falla en
   silencio: el `dotnet ef` termina bien y la columna nunca se crea.

---

## Qué queda abierto

- Aplicar `fechaDelBackend()` en todas las pantallas, no solo en el chat (sección 3).
- La preferencia de tema claro/oscuro se guarda en la base y se lee de vuelta, pero
  **ningún CSS reacciona a ella**: el interruptor del perfil no cambia nada en
  pantalla.
- El proyecto de tests apunta a `net9.0` mientras el resto apunta a `net8.0`. El CI
  instala el SDK 8 y compila igual solo porque el runner de GitHub trae el 9
  preinstalado.
- `Message.ReadAt` se guarda y se mapea al DTO, pero nada lo setea nunca: no existe
  forma de marcar un mensaje como leído.
- Angular Material figura entre las dependencias pero solo lo importa
  `features/auth/register/`, que es código muerto: ninguna ruta lo carga.
- La página **Comunidad** sigue siendo un placeholder.
