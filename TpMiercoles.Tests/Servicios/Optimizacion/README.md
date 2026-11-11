# Pruebas del optimizador

Las pruebas usan xUnit y Moq, ya instalados en el proyecto. No requieren una API key, SQL Server ni acceso a Internet.

## Ejecutar

Desde la raíz de la solución:

```powershell
dotnet test TpMiercoles.Tests/TpMiercoles.Tests.csproj -m:1 -p:UseSharedCompilation=false --filter "FullyQualifiedName~Optimizacion|FullyQualifiedName~OptimizadorController"
```

Suite completa:

```powershell
dotnet test TpMiercoles.Tests/TpMiercoles.Tests.csproj -m:1 -p:UseSharedCompilation=false
```

Si los paquetes ya están restaurados, se puede agregar `--no-restore`. `UseSharedCompilation=false` evita depender del servidor de compilación compartido. En el entorno aislado de Codex, VSTest no logró conectar con testhost: la ejecución verificada se realizó fuera del sandbox.

## Cobertura de comportamiento

- Motor: conjuntos factibles, poda por peso/cantidad, límites exactos, entrada inválida, cancelación, resultados independientes y comparación con un oráculo de subconjuntos por máscaras binarias.
- Reglas: momento de evaluación, desvío cero, pesos/desvíos no finitos, habilitación y capacidad.
- Compromisos: snapshot del viaje, cancelados, pendientes, entregados, capacidad excedida y entidades no cargadas.
- Recorridos: extremos, orden de paradas, retiro antes de entrega, varios paquetes, estabilidad frente al orden de entrada y ausencia de modificaciones sobre la base.
- Candidatos y repositorio: selección individual por peso, momento de evaluación, propietario excluido, disponibilidad, asignaciones y límites de fechas inclusivos.
- Salidas: autorización, próximas fechas, viajes ya materializados, deduplicación y viajes inactivos.
- Servicio completo: componentes reales coordinados con SQL/mapas sustituidos; propuestas por ganancia/desvío/equilibrio, desempates, umbral del 80 %, totales, compromisos, desvíos compartidos, fallos del proveedor y ausencia de reservas.
- MVC: GET/POST, identidad de sesión, formato de selección, validación, mensajes, recarga del selector, cancelación y presencia de atributos de seguridad.
- Adaptador OpenRouteService: solicitud HTTP, orden longitud/latitud, unidades, geometría, respuestas fallidas y cancelación mediante un HttpMessageHandler en memoria.

Las pruebas del coordinador ejercitan `CalcularAsync`, no acceden a métodos privados mediante reflexión. Los resultados de mapas se controlan por el conjunto de envíos recibido, no por el orden de llamadas.

## Regresión corregida

`MotorDeCombinacionesTests.PesosFraccionarios_AdmiteCienYDosCientosGramosConLimiteDeTrescientos` verifica el siguiente caso:

- Paquete 1: 0,1 kg.
- Paquete 2: 0,2 kg.
- Capacidad: 0,3 kg.
- Esperado: ambos paquetes pueden combinarse.
- Antes de la corrección: la combinación se descartaba por la suma binaria de `double`.

El motor ahora acumula y compara los pesos con `decimal`. La corrección es interna a `MotorDeCombinaciones`: los parámetros públicos siguen usando `double`. No modifica la representación de pesos en entidades, DTOs ni otros cálculos de la aplicación. La prueba permanece activa y pasa.

Verificación del 11/11/2026: suite completa de 464 casos, 464 aprobados, ninguno fallido ni omitido. Incluye 44 casos nuevos para la aceptación de propuestas y la adaptación del constructor del controlador en sus pruebas anteriores.

## Aceptación de propuestas

- `Servicios/AceptacionPropuestaTests.cs` ejecuta el servicio real con repositorios simulados: aceptación completa en viaje nuevo o existente, creación de un único viaje, guardado individual, fallo parcial sin intentar los envíos posteriores y rechazo del primer envío sin crear un viaje.
- Comprueba entradas inválidas, IDs duplicados, autorización sobre el viaje, estados inactivos, capacidad acumulada, compromisos cancelados, pesos fraccionarios en el límite y exceso real de peso.
- Verifica que los viajes iniciados usen la fecha actual para la disponibilidad y los futuros su fecha programada, conservando el snapshot del viaje seleccionado.
- `Web/Controllers/AceptarPropuestaControllerTests.cs` cubre sesión, ModelState, delegación de los datos del formulario, éxito total/parcial, errores, redirecciones, TempData y validación del ViewModel. Los atributos POST y antiforgery se inspeccionan sin ejecutar middleware.
- El mock del repositorio simula la identidad y la navegación de la ruta al crear un viaje. Estas pruebas verifican las llamadas de guardado y los cambios en las entidades en memoria, no la persistencia real en SQL Server.

## Alcance pendiente

- Las pruebas de repositorio usan el contexto simulado existente: no verifican traducción SQL, restricciones ni concurrencia real en SQL Server.
- Las pruebas de controlador llaman a las acciones directamente. Verifican los atributos de autorización/antiforgery, pero no ejecutan el middleware, el model binding ni una solicitud HTTP autenticada real.
- No se automatizó el navegador ni la presentación visual Razor.
- La aceptación de propuestas tiene cobertura de servicio y controlador; no se verificó el envío del formulario Razor desde un navegador ni el flujo HTTP completo.
- La búsqueda usa el constructor heurístico actual: las pruebas no afirman optimalidad global del recorrido por calles ni rendimiento con muchos candidatos.
