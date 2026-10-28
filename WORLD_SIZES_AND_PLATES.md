# Evolve — Tamaños del mundo y placas tectónicas

**Fecha:** 2026-10-28 · **Versión:** `landmass-5`

Se conservaron las anchuras solicitadas y se redondearon las filas al entero más cercano a 2/3 del ancho. Así la relación 3:2 se aplica al **número de hexágonos**, sin deformar su geometría.

| Tamaño | Original solicitado | Implementado | Hexágonos |
|---|---|---|---:|
| Pequeño | 200 × 150 | **200 × 133** | 26.600 |
| Mediano | 250 × 200 | **250 × 167** | 41.750 |
| Grande | 300 × 250 | **300 × 200** | 60.000 |
| Enorme | 400 × 300 | **400 × 267** | 106.800 |

Todos usan hexágonos de orientación plana, unión horizontal y polos norte/sur abiertos. El rectángulo **visual** tiene una proporción ligeramente distinta porque la separación entre centros hexagonales es 1,5 horizontal y √3 vertical.

## Placas de Pangea

Pangea genera **16 semillas de placa Voronoi independientemente de la resolución**; antes generaba entre 18 y 70 según el número de hexágonos. El cambio de placas y tamaño altera deliberadamente los nuevos continentes y alturas, así que las antiguas huellas geográficas dejan de ser regresiones válidas para `landmass-5`.

El cálculo de convergencia, extensión, superficie y relieve conserva su método, y las fronteras siguen detectándose por IDs vecinos. No se generan volcanes todavía.

Los perfiles de Continentes, Continentes pequeños y Archipiélagos se ajustaron para los nuevos tamaños. La nueva opción **Enorme** está disponible tanto al crear mundo como al cambiar su tamaño en la partida.

Los archivos de guardado anteriores contienen su propia definición de dimensiones. No se reinterpretan automáticamente como mapas nuevos, aunque al regenerar un mundo se aplica la nueva versión.

## Validación requerida

```powershell
git pull origin master
dotnet build
dotnet run --project GEvolve.Tests
dotnet run --project GEvolve.Game
```

Los snapshots `TerrainBaseline.txt` y `BiomeDistributionBaseline.json` permanecen como registros históricos. Se sustituyen sus comparaciones por pruebas actuales de determinismo, perfiles, topología, persistencia y distribución. La ejecución de .NET y el control visual quedan pendientes en el equipo del usuario.

## Nota posterior: landmass-6

La versión `landmass-5` de este documento queda como referencia histórica de tamaños y 16 placas. La nueva integración de Pangea con las placas independientes `tectonic-plates-2` usa `landmass-6`: produce nuevas superficies y elevaciones, sin cambiar los tamaños enumerados aquí. Véase [WORLD_GENERATION_PIPELINE.md](WORLD_GENERATION_PIPELINE.md).

## Actualización landmass-7

Los cuatro tipos de mundo comparten tamaños y placas `tectonic-plates-2`; ahora cada uno genera sus propias superficies y alturas en el pipeline activo. Los tamaños de la tabla anterior no cambian. `landmass-7` sustituye a `landmass-6` como versión de nuevos mundos.

## Actualización landmass-8

Las dimensiones 200×133, 250×167, 300×200 y 400×267 y las 16 placas continúan iguales. `landmass-8` añade influencia tectónica en las **masas terrestres** de Continentes, Continentes pequeños y Archipiélagos, además de la influencia que ya tenían las elevaciones. Pangea conserva su generador basado en placas. Las capas climáticas y de biomas permanecen separadas.
