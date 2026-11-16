# RuteaYa — Plataforma de transporte colaborativo
 
Trabajo práctico integrador realizado por el Grupo N°2 para **Programación Web 3** (2026, 2.º cuatrimestre) - **UNLaM - Universidad Nacional de La Matanza**.
 
RuteaYa permite aprovechar viajes habituales en auto para transportar paquetes. Los conductores registran sus recorridos, el sistema busca envíos compatibles y un optimizador elige la combinación de paquetes que maximiza la ganancia respetando capacidad, peso y desvío.
 
---
 
## Tecnologías utilizadas
 
- .NET 10 — ASP.NET Core MVC + ASP.NET Core Web API
- Entity Framework Core 10 + SQL Server
- AutoMapper
- OpenRouteService para rutas y mapas, con alternativa *mock* sin API key
- xUnit + Moq para tests unitarios

## Estructura de la solución
 
| Proyecto | Responsabilidad |
|---|---|
| `TpMiercoles.Web` | Aplicación MVC: vistas, controllers y view models |
| `TpMiercoles.Api` | Web API REST |
| `TpMiercoles.Servicios` | Lógica de negocio, DTOs, optimizador e integración de rutas |
| `TpMiercoles.Repositorios` | Acceso a datos |
| `TpMiercoles.Entidades` | Entidades, `ViajesDbContext` y migraciones de EF Core |
| `TpMiercoles.Tests` | Tests unitarios |
 

## Arquitectura

La solución separa la aplicación en capas:

    Navegador
       │
       ▼
    TpMiercoles.Web (MVC) ──── HttpClient ───▶ TpMiercoles.Api (Web API REST)
       │                                         │
       ▼                                         ▼
    TpMiercoles.Servicios (lógica de negocio, DTOs, optimizador, rutas)
       │
       ▼
    TpMiercoles.Repositorios (acceso a datos)
       │
       ▼
    TpMiercoles.Entidades (EF Core) ──▶ SQL Server

- Los controllers de la Web y de la Api **no acceden al `DbContext`**: llaman a la capa de servicios, y los servicios usan los repositorios.
- La Web y la Api **comparten la misma capa de servicios**, así la lógica de negocio no se duplica.
- La Api devuelve **DTOs**, nunca entidades, y usa los códigos HTTP correspondientes (200, 201, 204, 400, 401, 404, 409, 502).

### Consumo de la Web API desde la aplicación MVC

La pantalla **Mis viajes** obtiene sus datos llamando a la Web API (`GET /api/viajes`) mediante un `HttpClient` configurado en `TpMiercoles.Web/Program.cs`:

- La dirección de la API se configura en `Api:BaseUrl` (en desarrollo, `http://localhost:5069/`, en `appsettings.Development.json`).
- La Web reenvía la cookie de sesión del usuario en cada llamada, así la API sabe quién es y devuelve solo sus viajes.
- Si la API responde 401, la Web redirige al login.

Por eso la Api tiene que estar corriendo para que *Mis viajes* funcione.

---

## Requisitos previos
 
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server: alcanza con **SQL Server Express**, con la instancia `.\SQLEXPRESS`
- Visual Studio 2026, o la línea de comandos con `dotnet`
- Para usar migraciones desde la terminal, la herramienta de EF:

```bash
dotnet tool install --global dotnet-ef
```
 
## 1. Clonar el repositorio
 
```bash
git clone <https://github.com/MarcelaGonzalezz/pw3-app_viajes.git>
cd pw3-app_viajes
```
 
## 2. Configurar la base de datos
 
La cadena de conexión está en `appsettings.json`, tanto en `TpMiercoles.Web` como en `TpMiercoles.Api`:
 
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=.\\SQLEXPRESS;Database=AppViajes;Trusted_Connection=True;TrustServerCertificate=True;"
}
```
Si tu instancia de SQL Server tiene otro nombre, por ejemplo `(localdb)\MSSQLLocalDB` o `localhost`, cambiala en **los dos** archivos.

## 3. Crear la base con las migraciones
 
Las migraciones están en el proyecto `TpMiercoles.Entidades`.
 
**Desde la terminal**, parado en la carpeta de la solución:
 
```bash
dotnet ef database update --project TpMiercoles.Entidades --startup-project TpMiercoles.Web
```

**Desde Visual Studio**, en *Herramientas → Administrador de paquetes NuGet → Consola del Administrador de paquetes*:
 
1. Elegí `TpMiercoles.Entidades` como **Proyecto predeterminado**.
2. Dejá `TpMiercoles.Web` como proyecto de inicio.
3. Ejecutá:
```powershell
Update-Database
```
 
Esto crea la base `AppViajes` con todas las tablas.
 
## 4. Ejecutar la aplicación
 
Hay que levantar **la Web y la Api al mismo tiempo**, porque la Web consume la API.
 
 ### Visual Studio
 
1. Clic derecho sobre la solución → **Configurar proyectos de inicio**.
2. Elegí **Varios proyectos de inicio** y poné `TpMiercoles.Api` y `TpMiercoles.Web` en **Iniciar**.
3. Presioná **Iniciar** (F5).
### Terminal
 
En dos consolas distintas:
 
```bash
dotnet run --project TpMiercoles.Api --launch-profile https
```
 
```bash
dotnet run --project TpMiercoles.Web --launch-profile https
```

### Direcciones
 
| Proyecto | HTTPS | HTTP |
|---|---|---|
| Web | https://localhost:7219 | http://localhost:5223 |
| Api | https://localhost:7192 | http://localhost:5069 |
 
En desarrollo, la Web llama a la API en `http://localhost:5069/`, configurado en `TpMiercoles.Web/appsettings.Development.json` bajo `Api:BaseUrl`.
 
> **Sesión compartida:** la Web y la Api comparten la cookie de login. Para eso guardan sus claves en la carpeta `shared-keys/`, en la raíz de la solución. Esa carpeta se crea sola al ejecutar y **no se sube al repositorio** porque está en el `.gitignore`.
 
## 5. Datos de prueba
 
Al iniciar la Web en entorno **Development**, si la base no tiene datos, se cargan automáticamente usuarios, rutas, viajes, envíos, oportunidades e historial de ejemplo.
 
 | Usuario | Email | Contraseña |
|---|---|---|
| Ana Duarte (conductora, con historial y oportunidades) | `conductor@ruteaya.test` | `Ruteaya123!` |
| Bruno Ferrari (otro conductor) | `otro@ruteaya.test` | `Ruteaya123!` |
| Carla Mendez (remitente de envíos) | `cliente@ruteaya.test` | `Ruteaya123!` |
 
Para volver a cargar los datos desde cero, borrá la base, volvé a correr las migraciones del paso 3 e iniciá la Web.
 
## 6. Servicio de rutas: mock u OpenRouteService
 
El cálculo de rutas, distancias y desvíos está encapsulado detrás de la interfaz `IServicioRutas`, y la implementación se elige según la configuración:
 
- **Sin API key (por defecto):** se usa `MockServicioRutas`. Calcula la distancia en línea recta, con un factor de calle de ×1,3 y una velocidad promedio de 40 km/h. No hace falta configurar nada.
- **Con API key:** se usa OpenRouteService.
Para usar OpenRouteService, conseguí una API key gratuita en <https://openrouteservice.org/> y guardala como *user secret* en los dos proyectos, para no subirla al repo:

```bash
dotnet user-secrets set "Rutas:OrsApiKey" "TU_API_KEY" --project TpMiercoles.Web
dotnet user-secrets set "Rutas:OrsApiKey" "TU_API_KEY" --project TpMiercoles.Api
```
 
En Visual Studio también podés hacer clic derecho en el proyecto → **Administrar secretos de usuario** y agregar:
 
```json
{
  "Rutas": {
    "OrsApiKey": "TU_API_KEY"
  }
}
```
 
## 7. Documentación de la API (Swagger)

Con la Api corriendo en Development, la documentación interactiva está en:

**https://localhost:7192/swagger**

Ahí se ven todos los endpoints agrupados por recurso (Historial, Oportunidades, Transportes y Viajes), con su descripción, sus parámetros y los códigos HTTP que puede devolver cada uno.

Para probarlos con **Try it out**:

1. Iniciá la Web y la Api.
2. Iniciá sesión en la Web (https://localhost:7219) con alguno de los usuarios de prueba.
3. Abrí Swagger en otra pestaña del mismo navegador: la sesión se comparte, así que los endpoints responden con los datos de ese usuario.

La especificación OpenAPI en formato JSON está en `https://localhost:7192/openapi/v1.json`.
 
## 8. Tests
 
```bash
dotnet test
```
 
También se pueden correr desde el **Explorador de pruebas** de Visual Studio (*Prueba → Explorador de pruebas*).
 
---

## Problemas comunes
 
| Problema | Solución |
|---|---|
| `Cannot open database "AppViajes"` o un aviso de que no se pudieron sembrar los datos de prueba | No se corrieron las migraciones: ejecutá el paso 3. |
| `A network-related or instance-specific error` | El nombre de la instancia de SQL Server no coincide con la cadena de conexión: revisá el paso 2. |
| *Mis viajes* muestra un error al consultar la API | La Api no está corriendo: iniciá los dos proyectos (paso 4). |
| Te pide login de nuevo al pasar entre Web y Api | Borrá la carpeta `shared-keys/` y reiniciá los dos proyectos. |