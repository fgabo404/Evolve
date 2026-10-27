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
| `GEvolve.Definitions` | Catálogos y contratos declarativos. `WorldGeneration/Configuration` contiene `WorldSettings`, `TipoMundo`, `EdadGeologica` y `WorldPresets`: parámetros y perfiles existentes, compartidos por Engine y State sin crear un ciclo. Los JSON siguen siendo referencias de diseño; Engine aún no activa los catálogos guía como reglas. |
| `GEvolve.Engine` | Única implementación de geometría, análisis y generación procedural. `Geometry/` contiene `HexCoord`, `HexGeometry` y `HexDemoWorld`; `World/Generation/` conserva los generadores, `WorldSystem`, `WorldGenerator` y los tipos de superficie/bioma; `World/Analysis/` contiene `AnalizadorRelieve`; `World/Model/` contiene `WorldMap`, `WorldCell`, `WorldChunk` y la clasificación de relieve. `SimulationEngine` genera el mundo desde los ajustes del estado y, por ahora, solo incrementa el turno. |
| `GEvolve.State` | `GameState` guarda versión de formato, versión declarada de definiciones, turno, `WorldSettings` y los estados existentes de civilizaciones y regiones. `GameSaveStore` persiste JSON. Todavía no guarda un snapshot del mundo. |
| `GEvolve.Game` | Aplicación ejecutable OpenTK/OpenGL 3D. Conserva shaders, controles, cámaras y renderizado; cuando el usuario genera un mundo, obtiene el `WorldMap` de `SimulationEngine`. Usa tipos del Engine para construir la representación gráfica, sin implementar generación propia. |
| `GEvolve.Simulator` | Servidor web local y cliente 2D futuro. La página actual es un lienzo blanco: no crea partida, no genera mundo ni muestra controles. `StrategicMap2D` permanece como exportador legado sin invocación desde el servidor actual. |
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

No hay referencias circulares. Definitions, State y Engine no dependen de OpenTK, OpenGL, Canvas ni HTTP. La referencia de Simulator a Engine queda preparada para cuando exista un contrato de consulta 2D aprobado; el servidor blanco no instancia Engine.

## Ejecutar

Desde la raíz del repositorio:

```sh
dotnet build GEvolve.sln
dotnet run --project GEvolve.Tests
dotnet run --project GEvolve.Game
dotnet run --project GEvolve.Simulator
```

Game abre la ventana 3D y conserva la generación bajo el control existente del usuario. Simulator escucha en <http://127.0.0.1:8766/>; muestra solo un lienzo blanco y se detiene con `Ctrl+C`.

## Persistencia y trabajo pendiente

State conserva la semilla dentro de `WorldSettings` y una versión provisional de definiciones. Aún no guarda una versión del generador ni un snapshot del mundo. La misma semilla y configuración no garantizan el mismo resultado si cambia el algoritmo; las partidas duraderas requerirán versionado o persistencia suficiente del resultado generado. Los futuros `WorldState`, `HexState`, `EntityState` y `WorldSnapshot` siguen sin modelo aprobado.

También quedan pendientes la selección declarativa de algoritmos, los contratos espaciales definitivos, las fases de turno, la carga validada de definiciones operativas y la integración 2D del Simulator. Ninguna de esas decisiones se implementó en esta migración.
