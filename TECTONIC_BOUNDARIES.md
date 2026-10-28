# Evolve — Fronteras de placas tectónicas (fase 01)

**Modelo:** `plate-boundaries-1` · **Estado:** implementación pendiente de validación local.

## Alcance

La etapa inicial de accidentes tectónicos detecta las fronteras **reales entre IDs de placas**. No genera todavía volcanes ni infiere actividad, subducción, divergencia o movimiento.

### Cálculo

`PlateBoundaryMap.Analyze(HexTopology, LandmassState)` compara `PlateByCellId` de cada pareja de vecinos reales:

- Una frontera existe cuando `plates[a] != plates[b]`.
- Se crea una arista canónica solo si `a < b`, evitando duplicados.
- `BoundaryByCellId` marca los hexágonos adyacentes a alguna frontera.
- `DistanceToBoundaryByCellId` contiene la distancia mínima en pasos hexagonales, calculada por BFS multi-origen. Si el mundo contiene una sola placa, usa `ushort.MaxValue` para indicar ausencia de fronteras.
- Si faltan placas, el análisis devuelve `null` y no crea fronteras ficticias. Esto aplica a tipos de mundo sin tectónica persistida.

La unión este-oeste se preserva mediante `HexTopology`. La geometría de la frontera es un segmento sobre el **borde compartido** de dos hexágonos, no una unión entre centros. El renderer analiza el mapa una vez al preparar el mundo y solo dibuja aristas visibles durante la vista tectónica.

### Interfaz

Nuevo botón **PLACAS TECTÓNICAS** debajo de PRECIPITACIONES. Cada placa recibe un color determinista; los océanos tienen un tono más tenue. Las fronteras se muestran en amarillo. Al seleccionar una celda se visualizan ID de placa, si toca una frontera y su distancia al borde más cercano. El modo se desactiva al pulsar nuevamente el botón. En mundos sin placas, no se inventa la vista.

### Estado y compatibilidad

No se modifica `PangaeaGenerator`, ni `LandmassState`, ni guardados existentes, ni datos de superficie, placas, elevaciones, clima o biomas. El mapa de fronteras es derivable y no se persiste. La siguiente fase usará estas fronteras como **uno entre varios indicadores** de aptitud volcánica; no debe interpretar cualquier límite como volcánico.

### Validación pendiente

```powershell
git pull origin master
dotnet build
dotnet run --project GEvolve.Tests
dotnet run --project GEvolve.Game
```

Se añadieron pruebas para aristas únicas, vecinos válidos, costura horizontal, distancias, ausencia de falsos límites, color de placas, determinismo y botón de interfaz. La ejecución local de .NET y OpenGL **no se ha realizado en este entorno**. Tras validar, actualizar `ROADMAP_ACCIDENTES_GEOGRAFICOS.md` marcando la etapa como completada con las evidencias y el commit.

## Cambio de línea base en landmass-5

Pangea ahora usa 16 semillas de placa para todos los tamaños (Pequeño 200×133, Mediano 250×167, Grande 300×200, Enorme 400×267). La modificación de la cantidad de placas fue solicitada expresamente; el análisis derivado de fronteras no cambia. Las antiguas huellas de tectónica no son comparables entre versiones.

## Geometría irregular (tectonic-plates-2)

La forma de la placa ya no procede de una resistencia espacial compartida: cada placa combina seis campos de ruido periódicos con orientación diferente y coordenadas deformadas. El mapa de aristas `PlateBoundaryMap` no cambia: continúa calculando bordes únicos y distancias desde `PlateByCellId`. Por tanto, la capa visual se adapta automáticamente a las nuevas formas. Las placas permanecen conectadas y sus contornos incluyen más curvas, salientes y entrantes. Ver [WORLD_GENERATION_PIPELINE.md](WORLD_GENERATION_PIPELINE.md) para detalles y parámetro `BorderIrregularity`.

## Integración con la capa 02

La versión `landmass-6` produce Pangea a partir de `PlateByCellId` de `tectonic-plates-2`. El mapa de fronteras usa los mismos IDs; el generador continental no los sustituye ni modifica. Las líneas siguen visibles en la vista `CAPA 01: PLACAS`, mientras que `CAPA 02: SUPERFICIE` y `VER ALTURAS` presentan los datos de tierra, océano y relieve. La ruta de vista previa aislada de placas se mantiene disponible mediante `WorldGenerationService.GeneratePlatePreview`.

## Todos los tipos comparten tectónica (landmass-7)

La capa de placas se genera una sola vez por mundo, independientemente de la estrategia de superficie. Pangea utiliza sus placas en la elección y forma continental; Continentes, Continentes pequeños y Archipiélagos conservan sus distintas estrategias de costa y las usan para calcular el relieve mediante `PlateAwareElevationGenerator`. La vista de placas sigue usando los IDs originales en los cuatro casos.
