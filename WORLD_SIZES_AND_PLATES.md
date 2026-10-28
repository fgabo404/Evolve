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
