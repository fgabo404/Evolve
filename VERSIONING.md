# Versionado de Evolve

`Directory.Build.props` centraliza los identificadores de versión y los incorpora como metadatos de los ensamblados .NET. `GEvolve.Definitions.Versioning.EvolveVersion` los expone al código.

| Identificador | v0.001 | Uso |
| --- | --- | --- |
| Versión visible | `v0.001` | Hito de desarrollo mostrado en Game y usado como etiqueta Git. |
| Versión técnica .NET | `0.0.1` / ensamblado `0.0.1.0` | Compilación e identificación de los binarios. |
| Versión del generador | `landmass-1` | Contrato de reproducción de tierra y océano; se guarda con la configuración y la semilla. |
| Formato de guardado | `2` | Esquema de persistencia. El lector admite también el formato `1` sin Capa 02. |

Cada concepto evoluciona de forma independiente. Incrementar la versión del generador cuando una modificación altere el mapa determinista para los mismos parámetros y semilla. Incrementar el formato de guardado si cambia el esquema persistido y documentar la compatibilidad de lectura. La versión visible identifica un conjunto publicado de funciones, sin implicar un calendario de lanzamientos. No mover ni reutilizar etiquetas Git publicadas.

## Revisión en desarrollo

El generador activo ahora se identifica como `landmass-2` debido al nuevo algoritmo Pangea 2.0. No se reescribe la etiqueta `v0.001` ni su resultado histórico. Los demás algoritmos conservan su implementación anterior.

La siguiente iteración experimental usa `landmass-3` por los cambios deterministas en Pangea. La versión visual del juego sigue siendo `v0.001` hasta un futuro hito publicado; la etiqueta Git original es inmutable.

La revisión activa de Pangea usa `landmass-4` con alturas tectónicas 0–100. La clasificación posterior de biomas usa `biomes-1` y registra el hash del catálogo en su estado; no cambia `landmass-4` porque conserva exactamente el terreno. El campo opcional `LandmassState.Biomes` amplía el formato 2 manteniendo la lectura de guardados anteriores sin ese campo. Véase [BIOMES.md](BIOMES.md).
