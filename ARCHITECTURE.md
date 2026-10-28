# Evolve — Arquitectura de v0.001

La primera versión oficial implementa las capas 01 (grid) y 02 (tierra y océano). Las capas de elevación, clima, biomas, recursos y civilizaciones, así como la vista 3D, siguen pendientes.

```text
GEvolve.Definitions ──→ GEvolve.Engine ──→ GEvolve.State
        │                       │                 │
        └───────────────────────┴─────────────────┴──→ GEvolve.Game

GEvolve.Tests verifica los módulos anteriores.
```

Las flechas representan dependencias de compilación. `Definitions` contiene los tamaños, tipos y perfiles oficiales; `Engine` ejecuta la geometría y los cuatro algoritmos; `State` conserva la configuración efectiva y el resultado; `Game` presenta los controles y dibuja el mundo con OpenTK/OpenGL. No hay referencias circulares. El antiguo Simulator web y el prototipo 3D no forman parte de esta solución.

## Generación

`GameSession` comienza en el menú sin mundo. **Crear mundo** permite elegir Pequeño (100 × 70), Mediano (150 × 100) o Grande (250 × 200), uno de los cuatro tipos y una semilla entera editable o aleatoria. Los valores iniciales son Pequeño y Continentes. Al generar, `Engine` construye el grid flat-top `odd-q` con ID `fila × columnas + columna` y cierre horizontal este–oeste. Luego `LandmassGenerationService` selecciona el generador específico, aplica un máximo de cuatro intentos deterministas y analiza el resultado. La generación se ejecuta fuera del hilo gráfico; la creación de buffers OpenGL ocurre en él.

- `ContinentsGenerator`: núcleos separados y crecimiento territorial irregular de varios frentes.
- `PangaeaGenerator`: expansión conectada desde una región principal, con varios frentes de crecimiento.
- `SmallContinentsGenerator`: núcleos de alcance limitado y corredores oceánicos entre regiones.
- `ArchipelagosGenerator`: trayectorias curvas que colocan grupos y cadenas de islas de distintos tamaños.

Los generadores comparten vecindad hexagonal, aleatoriedad determinista, ruido coherente multiescala periódico en el eje horizontal y análisis de componentes. Cada uno conserva su estrategia espacial. `LandmassAnalysis` calcula cobertura, componentes, tamaños, masa dominante e islas usando la vecindad real, incluida la unión este–oeste. Los perfiles y porcentajes de tierra de `LandmassProfiles` son **experimentales de v0.001**, no reglas definitivas del juego; se detallan en [lineamientos generacion de mundo.md](lineamientos%20generacion%20de%20mundo.md).

## Estado y versiones

`HexGridState` guarda la definición de la Capa 01. `LandmassState` conserva la configuración de la Capa 02 (tipo, parámetros efectivos, semilla y versión del generador), el intento determinista elegido y un byte por ID de celda: `0` océano, `1` tierra. El mapa se recupera del estado sin regeneración. `GameSaveStore` escribe formato de guardado 2 y sigue leyendo los guardados de formato 1, que no tienen Capa 02.

`Directory.Build.props` es la fuente central del nombre Evolve, la versión visible `v0.001`, la versión técnica .NET, `landmass-1` para el generador y `2` para el formato de guardado. `EvolveVersion` expone estos datos desde los metadatos del ensamblado. La versión visible, la del generador y la del formato de guardado evolucionan por separado; véase [VERSIONING.md](VERSIONING.md).

## Game y renderizado

El menú conserva **Crear mundo**, **Opciones** y **Salir**. La pantalla de generación evita bloquear la interfaz. La vista 2D dibuja cada hexágono con relleno verde para tierra, azul para océano y contorno discreto. `HexGridRenderer` usa un hexágono base, un atributo de superficie por instancia y dos llamadas instanciadas para rellenos y contornos. Solo envía las instancias visibles; mover la cámara, cambiar zoom o seleccionar no ejecuta de nuevo los algoritmos. `MapCamera2D` mantiene el cierre horizontal visual, el límite norte–sur y la selección de IDs canónicos. La ficha de selección muestra ID, columna, fila y superficie.

Volver al menú libera el mundo de la sesión y sus recursos GPU. Los shaders y buffers restantes se liberan al cerrar la ventana. Opciones sigue siendo informativa; no hay controles de guardado/carga en el menú.

## Ejecutar y comprobar

```sh
dotnet build GEvolve.sln
dotnet run --project GEvolve.Tests
dotnet run --project GEvolve.Game
dotnet run --project GEvolve.Game -- --graphics-smoke
```

En el mapa: rueda para zoom, arrastre para desplazar, clic para seleccionar y **Menú principal** o `Esc` para volver. `--graphics-smoke` recorre los tres tamaños y los cuatro tipos en OpenGL, comprueba errores y registra tiempos e instancias visibles. Sus tiempos de fotograma son muestras aisladas, no una medición sostenida de FPS.
