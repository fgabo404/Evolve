# Evolve — arquitectura de la solución

## Flujo lógico

```text
Definitions ──> Engine <──> State
                  │
             ┌────┴────┐
             ▼         ▼
           Game     Simulator
            3D         2D
```

**Definitions define; Engine ejecuta; State almacena; Game y Simulator presentan.** El esquema indica responsabilidades; las referencias de compilación concretas aparecen abajo. Los dos visualizadores deben recibir los mismos resultados del Engine y no duplicar sus reglas.

## Proyectos y código actual

| Proyecto | Responsabilidad y estado |
| --- | --- |
| `GEvolve.Definitions` | Catálogos y contratos declarativos. `WorldGeneration/Configuration` contiene los parámetros del prototipo anterior y los tres perfiles oficiales `HexGridDefinition` de la Capa 01. Los JSON siguen siendo referencias de diseño; Engine aún no activa los catálogos guía como reglas. |
| `GEvolve.Engine` | Única implementación de geometría, análisis y generación. `Grid/` construye la Capa 01 flat-top sin terreno, con ID, posición, vecinos y distancia sobre un cilindro este-oeste. `Geometry/` y `World/` conservan aislados los algoritmos del prototipo anterior; `SimulationEngine` sigue disponible para ese prototipo. |
| `GEvolve.State` | `GameState` guarda versiones, turno, `WorldSettings`, estados existentes y un `HexGridState` opcional. Este conserva la configuración efectiva de la Capa 01; las celdas se reconstruyen exactamente por su ID fila-columna. `GameSaveStore` persiste JSON. Todavía no guarda un snapshot del mundo. |
| `GEvolve.Game` | Aplicación ejecutable OpenTK/OpenGL 3D. Conserva shaders, controles, cámaras y renderizado; cuando el usuario genera un mundo, obtiene el `WorldMap` de `SimulationEngine`. Usa tipos del Engine para construir la representación gráfica, sin implementar generación propia. |
| `GEvolve.Simulator` | Servidor web local que solicita la Capa 01 a Engine mediante `/api/grid`. Canvas 2D muestra solo el grid, con selector de tamaño, zoom, desplazamiento horizontal continuo y selección. No solicita el generador procedural anterior ni presenta terreno. `StrategicMap2D` permanece como exportador legado sin invocación desde el servidor actual. |
| `GEvolve.Tests` | Reúne las pruebas previas de turnos, persistencia y determinismo, más todos los casos de geometría, relieve, generación y perfiles que estaban en el antiguo proyecto de pruebas espaciales. |

La configuración `WorldSettings` y los perfiles están en Definitions porque describen elecciones y parámetros, no estado mutable ni cálculos. Las clases geométricas y todos los algoritmos permanecen en Engine. El comportamiento matemático no se modificó durante el traslado. Los JSON de `EvolveDoc` siguen siendo la referencia de diseño; no hay sincronización automática ni traducción completa a reglas operativas.

## Dependencias de compilación

```text
Definitions  (sin referencias a otros proyectos Evolve)
State        ──> Definitions
Engine       ──> Definitions, State
Game         ──> Definitions, Engine, State
Simulator    ──> Engine, State
Tests        ──> Definitions, Engine, State
```

No hay referencias circulares. Definitions, State y Engine no dependen de OpenTK, OpenGL, Canvas ni HTTP. Simulator transforma las celdas del Engine en una respuesta compacta; el navegador calcula solo posiciones de dibujo y detección de clics. La identidad, vecindad y continuidad pertenecen a Engine.

## Ejecutar

Desde la raíz del repositorio:

```sh
dotnet build GEvolve.sln
dotnet run --project GEvolve.Tests
dotnet run --project GEvolve.Game
dotnet run --project GEvolve.Simulator
```

Game abre la ventana 3D y conserva la generación bajo el control existente del usuario. Simulator escucha en <http://127.0.0.1:8766/> y se detiene con `Ctrl+C`. Al abrir muestra un lienzo blanco; el tamaño pequeño (100 × 70) está seleccionado, pero no se genera ningún grid hasta pulsar **Generar mundo**. `GET /api/grid/sizes` consulta los tres perfiles oficiales (100 × 70, 150 × 100 y 250 × 200). `POST /api/grid` con `{ "size": "medium" }` crea o sustituye la Capa 01. `GET /api/grid` devuelve su configuración y número de celdas, sin enviar IDs redundantes: el ID estable es `fila × columnas + columna`. `GET /api/grid/state` consulta su configuración efectiva y `GET /api/grid/cells/{column}/{row}` devuelve identidad, coordenadas y vecinos de una celda. Las consultas de grid, estado y celda responden `404` antes de generar. La vista general reutiliza una imagen interna; con zoom cercano solo se dibujan celdas visibles, sin procesar los 50 000 hexágonos en cada redibujado.

## Persistencia y trabajo pendiente

State conserva la semilla dentro de `WorldSettings` y una versión provisional de definiciones. Aún no guarda una versión del generador ni un snapshot del mundo. La misma semilla y configuración no garantizan el mismo resultado si cambia el algoritmo; las partidas duraderas requerirán versionado o persistencia suficiente del resultado generado. Los futuros `WorldState`, `HexState`, `EntityState` y `WorldSnapshot` siguen sin modelo aprobado.

También quedan pendientes la selección declarativa de algoritmos para las capas posteriores, los contratos espaciales definitivos, las fases de turno, la carga validada de definiciones operativas y las capas 02–04 del Simulator. La Capa 01 no añade atributos de terreno ni mecánicas.
