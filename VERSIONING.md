# Versionado de Evolve

`Directory.Build.props` centraliza los identificadores de versión y los incorpora como metadatos de los ensamblados .NET. `GEvolve.Definitions.Versioning.EvolveVersion` los expone al código.

| Identificador | v0.001 | Uso |
| --- | --- | --- |
| Versión visible | `v0.001` | Hito de desarrollo mostrado en Game y usado como etiqueta Git. |
| Versión técnica .NET | `0.0.1` / ensamblado `0.0.1.0` | Compilación e identificación de los binarios. |
| Versión del generador | `landmass-1` | Contrato de reproducción de tierra y océano; se guarda con la configuración y la semilla. |
| Formato de guardado | `2` | Esquema de persistencia. El lector admite también el formato `1` sin Capa 02. |

Cada concepto evoluciona de forma independiente. Incrementar la versión del generador cuando una modificación altere el mapa determinista para los mismos parámetros y semilla. Incrementar el formato de guardado si cambia el esquema persistido y documentar la compatibilidad de lectura. La versión visible identifica un conjunto publicado de funciones, sin implicar un calendario de lanzamientos. No mover ni reutilizar etiquetas Git publicadas.
