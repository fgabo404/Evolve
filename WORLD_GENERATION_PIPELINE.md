# Evolve — Pipeline de generación por capas

**Reestructuración acordada: 2026-10-28**

```text
[Infraestructura] Cuadrícula hexagonal con unión este-oeste
   ↓
01. Placas tectónicas (universales, independientes del tipo de mundo)  ✓
   ↓
02. Tipo de mapa: Pangea ← ACTIVA; otros tipos pendientes
    ├─ Tierra y océanos
    └─ Elevaciones
   ↓
03. Temperatura
   ↓
04. Precipitaciones y humedad
   ↓
05. Pendientes y clasificación de biomas
   ↓
06. Mundo completo
```

## Qué cambia ahora

1. **Las 16 placas se generan antes de elegir la forma continental.** Con igual semilla y tamaño, los cuatro tipos de mundo comparten el mismo `PlateByCellId`.
2. **Nueva geometría:** `TectonicPlateGenerator` utiliza 16 orígenes separados por muestreo de candidatos, crecimiento simultáneo sobre vecinos hexagonales, velocidades relativas de expansión diferentes y un coste de crecimiento suavemente variable (ruido periódico) **específico para cada placa**. Todas las placas son regiones conectadas y las fronteras son irregulares. Es una aproximación procedural a las grandes formas tectónicas de la Tierra, **no una simulación científica de cinemática tectónica**.
3. **Capas 01 y 02 activas para Pangea:** `WorldGenerationService.Generate` crea primero las placas, después genera una única masa continental con agua y elevaciones 0–100 reutilizando el algoritmo de expansión anterior. El juego muestra por defecto tierra y océanos, y permite cambiar entre **SUPERFICIE**, **VER ALTURAS** y **PLACAS**. No ejecuta temperatura, precipitación, pendientes ni biomas. Solo Pangea está habilitada en el selector.
4. **Código conservado:** `LandmassGenerationService.Generate`, `PangaeaGenerator`, los tres otros generadores, `TemperatureGenerator`, `PrecipitationGenerator`, `BiomeClassifier` y sus pruebas siguen en el repositorio. El nuevo `PangaeaFromPlatesGenerator` adapta el algoritmo de superficie/relieve de Pangea a los IDs `tectonic-plates-2`, sin regenerarlos. **Los viejos generadores no deben reemplazar los IDs de placas de la capa 01.**
5. **Vista de placas aislada conservada:** `WorldGenerationService.GeneratePlatePreview` sigue produciendo solo la capa 01, señalada con `LandmassState.TectonicPreviewOnly=true` y superficie provisional sin océanos reales. La ruta de juego normal `Generate` ya produce superficie y elevación de Pangea y mantiene `TectonicPreviewOnly=false`.

## Siguiente paso

Revisar visualmente cómo se asientan costas y relieves de Pangea sobre las 16 placas, que no se modifican. Después adaptar **Continentes**, **Continentes pequeños** y **Archipiélagos** al mismo contrato de placas sin alterar su distribución; más adelante se integrarán clima, lluvia y biomas.

## Comprobación

```powershell
git pull origin master
dotnet build
dotnet run --project GEvolve.Tests
dotnet run --project GEvolve.Game
```

La ejecución de .NET/OpenGL debe verificarse en un entorno compatible; no se ha ejecutado dentro de esta sesión.

## Placas más irregulares: `tectonic-plates-2`

La primera versión `tectonic-plates-1` utilizaba un **Dijkstra multiorigen**: cada placa crecía a través de hexágonos vecinos pagando su propio factor de velocidad y una resistencia espacial común a todas las placas. Aunque las fronteras variaban, la competencia todavía se parecía a un Voronoi suavizado.

La segunda versión mantiene el crecimiento conectado, pero añade tres cambios:

1. **Deformación de dominio (domain warp):** dos campos de ruido continuo de baja frecuencia desplazan virtualmente las coordenadas antes de evaluar los demás ruidos. No desplazan las celdas reales ni alteran el grid.
2. **Ruido fractal multiescala:** seis campos periódicos se agrupan en tres pares: grandes lóbulos (frecuencias 3/5), entrantes de escala mediana (9/13) y detalles menores (21/29), todos con 3 octavas. No se usa ruido blanco independiente por celda.
3. **Resistencia diferente por placa:** cada una mezcla los tres pares de ruidos con ángulos propios derivados de la semilla. El costo de conquistar un vecino se calcula como `costoBaseDePlaca × clamp(1 - BorderIrregularity × (1.65×ruidoGrande + 1.10×ruidoMediano + 0.44×ruidoFino), 0.27, 3.10)`. Los campos y sus ángulos varían por semilla.

`BorderIrregularity = 1.90` es el parámetro principal para calibrar la irregularidad en futuras revisiones. El costo siempre es **positivo**, de modo que el algoritmo nunca ocupa una celda desconectada del territorio de su placa. Se mantiene el cierre horizontal periódico y el número fijo de **16 placas**, independiente del tipo de continente.

Esta técnica **simula fronteras naturales de manera procedural**: no modela todavía la dinámica real de placas (convergencia, subducción, dorsales ni fallas activas). El número de placas, el nuevo aspecto de los bordes y la versión del algoritmo son decisiones de diseño del juego.

**Estado:** implementación y pruebas de invariantes codificadas, pendiente de ejecución de .NET 10, examen visual y aprobación.

## Capa 02: tierra y alturas de Pangea (landmass-6)

- **Entrada inmutable:** `HexTopology`, `LandmassConfiguration` y `byte[] PlateByCellId` generado una sola vez por `TectonicPlateGenerator`.
- **`PangaeaFromPlatesGenerator`** reutiliza la selección de regiones tectónicas adyacentes, el cálculo de compresión/extensión a partir de velocidades locales, distancia a costas, ruido periódico, crecimiento de una masa continua por cola de prioridad, eliminación de agua interior aislada del mar y normalización de elevaciones 0–100. En vez de centros Voronoi nuevos, calcula centros de placas con promedio circular de X (continua a través de la costura) y promedio de Y.
- **Resultado:** `SurfaceByCellId` (0 océano, 1 tierra), `ElevationByCellId` (byte 0–100, océano 0) y el mismo contenido de `PlateByCellId`. El objetivo inicial de Pangea sigue siendo aproximadamente **40 % tierra**. La conectividad, determinismo y perfil estadístico se validan en el pipeline.
- **Sin otras capas:** `Climate`, `SlopeByCellId` y `Biomes` siguen ausentes.
- **Compatibilidad:** `LandmassGenerationService` y los cuatro generadores históricos no se eliminaron. La versión de mapas nuevos es **`landmass-6`** para diferenciarla de las salidas de `landmass-5`.
- **Pruebas:** `StagedLandmassTests` y los tests de sesión comprueban superficie, relieve, placas inmutadas, mismos resultados por semilla, selección, persistencia y capas posteriores desactivadas. `TectonicPlateTests` usa `GeneratePlatePreview` para verificar universalidad sin invocar la capa 02.

**Pendiente:** ejecutar `dotnet build`, `dotnet run --project GEvolve.Tests`, revisar visualmente en el juego y aprobar la fase. No afirmar resultados de pruebas antes de ejecutarlas.
