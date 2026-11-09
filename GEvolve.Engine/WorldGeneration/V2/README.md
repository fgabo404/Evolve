# Motor de generación mundial V2

**Estado:** estructura y contratos por diseñar. Ningún algoritmo se ha sustituido.

La intención es una sola ejecución por configuración, con estados internos verificables y **una elevación final**. Esta carpeta está separada de `GEvolve.Engine/World/`, que continúa ejecutando V1. No crear un segundo orquestador público paralelo: V2 debe conectarse detrás de una interfaz experimental y migrar de forma controlada.

Subdirectorios: `Core`, `Grid`, `Tectonics`, `Surface`, `Geology`, `Terrain`, `Climate`, `Hydrology`, `Biomes`, `Resources`.

Consulta el único plan vigente: [WORLD_GENERATION_V2.md](../../../WORLD_GENERATION_V2.md).
