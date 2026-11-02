# GEvolve.State — Estado de las partidas

Este proyecto **representa y conserva el estado de cada partida de Evolve**. No define reglas de juego ni calcula la generación procedural: las reglas están en `GEvolve.Definitions`, el cálculo en `GEvolve.Engine`, y los resultados mutables en `GEvolve.State`.

## Organización del código

- **`Game/`**: `GameState.cs`, objeto principal del estado en memoria. Contiene turno, ajustes del mundo, regiones, civilizaciones y referencias a los datos del mundo.
- **`World/`**: estructuras persistibles de cuadrícula, relieve y superficie, biomas, clima, tectónica y geología continental. Se han movido aquí sin cambiar sus espacios de nombres públicos para no romper las referencias existentes.
- **`Civilizations/`**: `CivilizationState.cs` y `RegionState` (misma unidad de código por ahora): estado de civilizaciones y propiedad de regiones. Sus tipos conservan el espacio de nombres `GEvolve.State.Game` por compatibilidad.
- **`Persistence/`**: `GameSaveStore.cs`, lectura/escritura y validación de estados JSON. **`Persistence/Validation/`** agrupa las validaciones tectónica y geológica.

## Partidas independientes: diseño de almacenamiento

**Objetivo arquitectónico (todavía pendiente de implementar):** cada nueva partida tendrá un ID único e inmutable y una carpeta propia, independiente del nombre visible de la partida:

```text
Saves/
  <save-id>/
    save.json           # ID, nombre, fechas, versión pública, turno y metadatos
    world.json          # Estado del mundo, clima, biomas y geología
    civilizations.json  # Estado de civilizaciones y regiones
    simulation.json     # Estado adicional necesario para continuar la simulación
```

**Situación actual:** `GameSaveStore.Save(path, state)` y `Load(path)` utilizan **un único JSON** en la ruta recibida. Todavía no crean carpetas `Saves/<save-id>/`, ni reparten el estado entre cuatro archivos, ni ofrecen una lista de partidas. La estructura anterior es el destino del siguiente desarrollo, no una funcionalidad completada.

Al implementar `SaveManager`, deberá crear/listar/cargar/borrar partidas por ID; elegir una ubicación de datos de usuario (en Windows, por ejemplo `%LOCALAPPDATA%/Evolve/Saves`) y evitar incluir partidas en Git. Para varios JSON se necesitará una estrategia de guardado transaccional o de snapshots por generación: **no deben mezclarse archivos de turnos diferentes** después de una interrupción.

## Flujo esperado

1. Se crea el estado inicial de la partida con su ID.
2. El motor genera el mundo y modifica el estado al avanzar los turnos.
3. El sistema de persistencia valida y guarda una instantánea coherente.
4. Al continuar, se leen los datos y se restaura el estado antes de reanudar el motor.

Los IDs del formato de guardado y de los algoritmos geográficos son **identificadores de compatibilidad**, distintos de la versión pública única de Evolve declarada en `Directory.Build.props`.

## Comprobación

```powershell
dotnet build GEvolve.sln
dotnet run --project GEvolve.Tests
```
