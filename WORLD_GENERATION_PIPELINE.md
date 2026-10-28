# Evolve — Pipeline de generación por capas

**Reestructuración acordada: 2026-10-28**

```text
[Infraestructura] Cuadrícula hexagonal con unión este-oeste
   ↓
01. Placas tectónicas (universales, independientes del tipo de mundo)  ✓
   ↓
02. Tipo de mapa: Pangea / Continentes / Pequeños / Archipiélagos  ← ACTIVOS
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
3. **Capas 01 y 02 activas para los cuatro mundos:** `WorldGenerationService.Generate` crea primero las placas, después genera tierra, océanos y elevaciones 0–100 siguiendo el tipo seleccionado. El juego muestra por defecto tierra y océanos y permite cambiar entre **SUPERFICIE**, **VER ALTURAS** y **PLACAS**. No ejecuta todavía temperatura, precipitación, pendientes ni biomas. Los cuatro tipos están disponibles en el selector.
4. **Código conservado:** `LandmassGenerationService.Generate`, `PangaeaGenerator`, `TemperatureGenerator`, `PrecipitationGenerator`, `BiomeClassifier` y sus pruebas siguen en el repositorio. `PangaeaFromPlatesGenerator` recibe placas para formar su supercontinente. `ContinentsGenerator`, `SmallContinentsGenerator` y `ArchipelagosGenerator` conservan sus algoritmos distintos de superficie, **ahora también influidos suavemente por las placas existentes**; `PlateAwareElevationGenerator` calcula alturas por tipo con influencia tectónica y devuelve el mismo formato para las etapas posteriores. Ningún generador de superficie sustituye las placas de la capa 01.
5. **Vista de placas aislada conservada:** `WorldGenerationService.GeneratePlatePreview` sigue produciendo solo la capa 01, señalada con `LandmassState.TectonicPreviewOnly=true` y superficie provisional sin océanos reales. La ruta de juego normal `Generate` ya produce superficie y elevación de Pangea y mantiene `TectonicPreviewOnly=false`.

## Siguiente paso

Revisar visualmente las **cuatro siluetas terrestres** y su relieve sobre las 16 placas inmutables. Después podremos reactivar en una única ruta común el clima (temperatura, precipitaciones y humedad), las pendientes y la clasificación de biomas.

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

## Integración de los cuatro tipos: landmass-7

`WorldGenerationService` coordina dos capas y publica el mismo contrato (`SurfaceByCellId`, `ElevationByCellId`, `PlateByCellId`) para **todos** los tipos:

| Tipo | Algoritmo de superficie | Relieve | Tierra objetivo |
|---|---|---|---|
| Pangea | `PangaeaFromPlatesGenerator`: selección de regiones tectónicas vecinas, crecimiento de una masa única y costas con ruido | Modelo propio: compresión/extensión sobre fronteras y cuantiles de altura | ~40 % |
| Continentes | `ContinentsGenerator`: varios núcleos distantes que crecen con cuotas distintas; islas secundarias | `PlateAwareElevationGenerator`: énfasis en interior continental y orogenia | ~35 % |
| Continentes pequeños | `SmallContinentsGenerator`: radios y cuotas locales, corredores de mar | `PlateAwareElevationGenerator`: pendientes y alturas distribuidas en territorios menores | ~30 % |
| Archipiélagos | `ArchipelagosGenerator`: cadenas curvas y estampado de islas, conservando canales oceánicos | `PlateAwareElevationGenerator`: relieves más localizados, con potencial de islas volcánicas | ~20 % |

**Contrato tectónico:** todos reciben `TectonicPlateGenerator.Generate` con la misma semilla y tamaño; `PlateByCellId` no se vuelve a calcular al crear un continente. La distribución base de tierra de los tres tipos no-Pangea conserva los métodos anteriores (con nucleación y costas independientes de placas); en ellos la **influencia tectónica ya está presente en las elevaciones**, y la integración más fuerte de la costa con los límites tectónicos puede calibrarse en otra revisión. Esto evita afirmar que todas las costas obedecen las fronteras tectónicas.

**Relieve reutilizable:** `PlateAwareElevationGenerator` calcula un indicador aproximado de convergencia mediante velocidades por placa obtenidas determinísticamente, lo difunde dentro de tierra, combina distancia al mar y ruido periódico en tres escalas y normaliza a alturas 1–100 en tierra y 0 en océanos. No constituye aún una simulación geológica exacta de movimientos, volcanes o subducción. Los pesos de interior/compresión/ruido son distintos por tipo.

**Etapas compartidas aún desconectadas:** cuando se activen, `TemperatureGenerator`, `PrecipitationGenerator`, `BiomeClassifier` y el cálculo de pendientes consumirán el mismo `LandmassState` independientemente del tipo de mapa, sin generar tectónica propia.

**Versión:** `landmass-7`; se conservan los generadores y pruebas de `landmass-6` como referencia histórica. La UI permite seleccionar los cuatro tipos y ver superficie, elevación o placas. Las pruebas `StagedLandmassTests` verifican las cuatro morfologías, los cuatro tamaños, la conservación de IDs de placa, las elevaciones, el determinismo, la persistencia y la ausencia de capas climáticas/biomas. **Build y pruebas .NET pendientes de ejecución local.**

## Integración tectónica de costas en los cuatro tipos (landmass-8)

La versión anterior `landmass-7` ya ofrecía las cuatro siluetas y elevaciones, pero **Continentes, Continentes pequeños y Archipiélagos usaban las placas sobre todo para la altura**, no para construir la forma de tierra. Esta revisión añade guía tectónica también a sus costas, sin cambiar la tectónica de entrada.

- **Pangea:** conserva `PangaeaFromPlatesGenerator` (supercontinente único, con selección de placas contiguas, expansión conectada, costas irregulares y relieve tectónico).
- **Continentes:** conserva los grandes núcleos distantes y las cuotas de expansión, pero crecer a través de un límite de placa tiene un costo ligeramente mayor que continuar dentro de la misma placa; pequeñas preferencias regionales generan costas distintas. Una placa no equivale a un continente.
- **Continentes pequeños:** mantiene numerosos núcleos, radios limitados y corredores marítimos. Un factor suave dependiente de las placas modifica los radios locales y evita que sean formas totalmente independientes de la tectónica.
- **Archipiélagos:** conserva las trayectorias curvas de cadenas insulares y las islas pequeñas. Algunas cadenas comienzan cerca de límites tectónicos, sin forzar a que todas estén en ellos; cerca de límites el radio de estampado aumenta ligeramente. Se permiten islas interiores de placa.

`PlateSurfaceInfluence` deriva distancias a fronteras mediante BFS sobre vecinos hexagonales y preferencias regionales estables por semilla. Solo lee `PlateByCellId`: no vuelve a generar placas ni altera su contenido. Los tres generadores tienen un nuevo método `Generate(topology, configuration, attempt, plates)` y conservan intacta la firma original de tres argumentos por compatibilidad. `IPlateAwareLandmassGenerator` facilita que `WorldGenerationService` pase la misma matriz de placas a todos.

**Contrato de la capa 02:** `PlateByCellId` (16 placas) + `SurfaceByCellId` (tierra y océano) + `ElevationByCellId` (0–100) para Pangea, Continentes, Continentes pequeños y Archipiélagos. Todas las capas climáticas futuras podrán consumir los mismos campos sin lógica distinta por tipo. Las capas 03–05 (temperatura, precipitación/humedad, pendiente y biomas) siguen **desactivadas en el pipeline nuevo**, aunque su código anterior se preserva.

**Versión:** `landmass-8` para distinguir los nuevos resultados de `landmass-7`. Las huellas de terreno anteriores no deben compararse con esta revisión. Las pruebas incluyen las cuatro resoluciones y tipos, conservación de placas, determinismo, perfiles y diferencias entre la ruta original y la ruta guiada por placas. **Compilación, suite .NET y revisión OpenGL pendientes de validación local.**

## Selector de tamaño y tipo (interfaz OpenGL)

La pantalla **Crear mundo** y la vista **Mundo** usan dos controles desplegables: **Tamaño** (Pequeño 200×133, Mediano 250×167, Grande 300×200, Enorme 400×267) y **Tipo de mundo** (Continentes, Pangea, Continentes pequeños, Archipiélagos). Las opciones se dibujan por encima de los otros controles y se pueden cerrar al hacer clic fuera o pulsar Escape.

En la vista de mundo, seleccionar una opción distinta invoca `GameSession.BeginReconfigure(size, type)`: **conserva la semilla**, modifica solo el parámetro elegido y vuelve a generar tierra/océanos/elevaciones con las mismas 16 placas tectónicas para la combinación de tamaño y semilla. No se genera nada al seleccionar la opción actual. `BeginResize` sigue funcionando como ruta compatible y `BeginChangeType` permite alterar solo el tipo. La ruta de generación de la pantalla Crear mundo conserva el campo de semilla editable.

El cambio es de interfaz y sesión: **no altera la versión `landmass-8` ni el algoritmo tectónico o continental**. Las comprobaciones de menú y reconfiguración se extendieron en `GEvolve.Tests/GameTests.cs`; la ejecución .NET/OpenGL queda pendiente de validación local.
