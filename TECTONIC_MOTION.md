# Movimiento e interacciones tectónicas

Modelo `tectonic-motion-1`, posterior a `tectonic-plates-2`. En la versión original el resultado era diagnóstico y no modificaba las elevaciones. La fase posterior `tectonic-relief-1` sí consume estos campos y deforma el relieve; véase [TECTONIC_RELIEF.md](TECTONIC_RELIEF.md). No anima placas ni genera montañas, volcanes, terremotos o rifts visibles.

## Arquitectura y orden

1. `TectonicPlateGenerator`: placas originales, sin cambios.
2. `PlateMotionGenerator`: movimientos independientes de tamaño y tipo de mundo.
3. `TectonicMotionGenerator`: fronteras, proyecciones y campos de influencia.
4. Generadores actuales de masas terrestres y elevaciones (`landmass-12`), sin consumir todavía estos campos nuevos.
5. Temperatura.
6. Precipitaciones y humedad.
7. Pendientes y biomas.

Los puntos 5–7 continúan desconectados en el pipeline activo `WorldGenerationService`, igual que antes. La ruta histórica `LandmassGenerationService` y sus capas permanecen intactas. Los indicadores sintéticos internos usados por el ensamblaje continental y el relieve tampoco se reemplazan: hacerlo cambiaría los resultados terrestres y requiere otra autorización.

Definitions centraliza parámetros en `TectonicMotionSettings`; Engine calcula; State persiste; Game dibuja. `GeneratePlatePreview` incluye la cinemática sin generar superficie real. En la generación completa se calcula una vez antes de los intentos de masas terrestres, reutilizando la misma matriz de placas.

## Movimientos

`PlateMotionGenerator` utiliza el generador determinista existente SplitMix64, con un flujo separado `0x4D4F5431`. Solo recibe semilla y configuración cinemática; no recibe tamaño ni tipo de mundo. Por orden de ID 0–15 obtiene un ángulo uniforme en [0,360) y una velocidad uniforme entre los límites configurables, inicialmente 1–8 cm/año (extremo superior excluido por el muestreo).

Convención del plano de juego: X hacia el este, Y hacia el sur; 0° este y 90° sur, sentido horario. Para ángulo θ y velocidad s:

```text
u = (cos θ, sin θ)
v = s × u
```

`PlateMotion` conserva ID, dirección y velocidad; expone `UnitVector` y `Velocity` como valores derivados, sin duplicarlos en JSON. La dirección no representa rumbo geodésico sobre una esfera.

## Fronteras e interacciones

Se reutilizan las aristas canónicas de `PlateBoundaryMap`, una por pareja de vecinos de diferentes placas (`FirstCellId < SecondCellId`). No se inventan nuevas fronteras.

`BoundaryNormal` calcula el vector entre centros flat-top odd-q. Corrige la diferencia horizontal al desplazamiento periódico más corto e incorpora el desplazamiento vertical de columnas impares. Las componentes geométricas son `x = 1,5 × deltaColumna` e `y = √3 × (deltaFila + 0,5 × deltaParidad)`, y después se normaliza. Así, una frontera entre columnas 0 y N−1 usa el borde vecino, no una línea que atraviesa todo el mapa. La normal n apunta de A a B; la tangente es `t = (−n.y, n.x)`.

```text
Δv = vB − vA
C = max(0, −Δv · n)      convergencia
D = max(0,  Δv · n)      divergencia
S = |Δv · t|            cizalla
```

Las magnitudes están en cm/año. Se conservan normal, IDs de celdas y placas, C/D/S y categoría. Intercambiar A y B e invertir n deja C/D/S iguales.

Clasificación configurable:

- `Quiet`: máximo de |Δv·n| y S ≤0,15 cm/año; no se fuerza actividad por ruido numérico.
- `Mixed`: ambos componentes superan o igualan 0,15 y el menor alcanza al menos la mitad del mayor.
- `Transform`: domina S.
- `Convergent` o `Divergent`: domina el componente normal, según su signo.

La categoría no elimina ni redondea los valores numéricos originales. Una placa puede tener interacciones distintas en diferentes bordes según la orientación local.

## Campos por hexágono

En ambos hexágonos adyacentes a una frontera se siembra el máximo C, D y S de las aristas incidentes. Cada fuente se propaga usando distancias por vecinos reales, incluida la unión horizontal:

`I(d) = I0 × exp(−(d/r)²)`

El radio inicial es `max(1, 0,02 × min(columnas, filas))`: 2,66 / 3,34 / 4,00 / 5,34 pasos hexagonales en los cuatro tamaños actuales. Mantiene aproximadamente la misma anchura relativa al cambiar resolución. No equivale a kilómetros.

Se toma el **máximo** de las influencias por canal, evitando amplificar la intensidad por tener más aristas o mayor resolución. Cada propagación BFS local se corta en `ceil(3r)` pasos; fuera de ahí la contribución es cero (en 3r ya es aproximadamente 0,012 % de I0). Se reutilizan buffers de recorrido; no hay búsquedas por celda durante renderizado. El coste crece con el número de fuentes y el área hexagonal de influencia, no con una simulación temporal.

Se conservan tres `float[]`, independientes y alineados por ID, y la distancia mínima `ushort[]` de `PlateBoundaryMap`. Las distancias valen cero en celdas fronterizas. Los tres campos son **intensidades cinemáticas**, no esfuerzos físicos medidos en pascales: faltan rigidez, espesor, viscosidad y tiempos de deformación. La distancia discreta da contornos hexagonales, nunca cuadrados; no se promete invariancia exacta entre mapas de distinta geometría.

## Estado, consultas y persistencia

`LandmassState.TectonicMotion` es opcional. `TectonicMotionState` guarda versión, movimientos, interacciones, campos, distancia a frontera, radio efectivo y un ID interior representativo por placa. Las consultas por celda son indexaciones directas. El punto representativo es la celda de mayor distancia a una frontera dentro de la placa, con desempate por ID: evita centroides fuera de placas cóncavas.

El formato de guardado sigue siendo 2. Los archivos antiguos sin `TectonicMotion` se cargan con valor nulo y no se recalcula automáticamente la tectónica. Se validan longitudes, valores finitos/no negativos, IDs, normales unitarias y anclas interiores. Guardar/cargar conserva los resultados sin regenerar terrenos. No se añaden dependencias de State hacia Engine.

## Diagnóstico visual

En **CAPA 01: PLACAS** se dibuja una flecha por placa en su punto interior. La longitud en pantalla depende de la velocidad (`22 + 3 × velocidad` píxeles), con contorno oscuro y punta blanca para legibilidad; no es una escala de desplazamiento físico. Las copias visibles del cilindro repiten la misma flecha. Las anclas interiores separan las flechas; al acercar la cámara solo se ven las que están dentro de la vista.

Tres botones seleccionan **ESFUERZO: COMPRESIÓN**, **EXTENSIÓN** y **CIZALLA**. Los colores pasan de oscuro a naranja, cian y violeta respectivamente. La escala común 0–16 cm/año-equivalente utiliza raíz cuadrada para mostrar influencias débiles; no se normaliza cada mapa a su propio máximo. Las cumbres conservan su condición aunque las vistas diagnósticas reemplacen su color.

La selección muestra ID de placa, dirección, velocidad en cm/año y distancia al límite en hexágonos. La cámara, cuadrícula, selección y controles existentes se conservan. Cambiar vista solo consulta datos persistidos.

## Fundamento y límites

Las tres componentes siguen la distinción geológica entre separación, acercamiento y deslizamiento descrita por [USGS — Understanding plate motions](https://pubs.usgs.gov/gip/dynamic/understanding.html) y [NOAA — Plate boundaries](https://oceanexplorer.noaa.gov/ocean-fact/plate-boundaries/). El rango 1–8 cm/año es una calibración simplificada solicitada, no una medición de las placas de la Tierra.

No se modelan rotaciones mediante polos de Euler, curvatura esférica, conservación global del momento, fuerzas del manto, edad o densidad cortical, subducción, viscosidad, deformación acumulada ni evolución temporal. Las normales siguen las aristas hexagonales discretas. Los futuros generadores podrán usar C/D/S para orientar zonas orogénicas, extensión o fallas; esta revisión no las genera ni adapta los algoritmos existentes.

## Validación

`TectonicMotionTests` prueba los 16 movimientos, configuración y determinismo, independencia del tamaño, vectores controlados, casos quietos y mixtos, inversión A/B, normales y costura horizontal, atenuación gaussiana, canales separados, finitud, cuatro resoluciones, geometría original, anclas y persistencia compatible.

`TectonicMotionTerrainBaseline.txt` captura el terreno de `11e784c` **antes de integrar** esta capa: cuatro tamaños × cuatro tipos, semilla 2026. Quince escenarios generan mapas y se comparan por SHA-256 de superficie, elevación y placas, además del intento elegido. Continentes/Enorme/2026 ya fallaba al cumplir su perfil geográfico; se registra como fallo previo, no como mapa aprobado ni como problema corregido. Las huellas históricas anteriores no se actualizan.

Comandos:

```powershell
dotnet build GEvolve.sln --no-restore
dotnet run --project GEvolve.Tests --no-restore -- --tectonic-motion
dotnet run --project GEvolve.Tests --no-restore -- --all-suites
dotnet run --project GEvolve.Game --no-restore -- --graphics-smoke
git diff --check
```

`--all-suites` ejecuta todos los grupos aun si alguno falla y devuelve un código distinto de cero cuando hay fallos. No cambia las expectativas ni silencia excepciones. La ejecución habitual mantiene su comportamiento de detenerse en el primer fallo.

Resultados ejecutados el 30 de octubre de 2026:

- Compilación Debug: 0 errores y 0 advertencias.
- Pruebas nuevas de movimiento: aprobadas en los cuatro tamaños; 15 huellas anteriores idénticas y el fallo de generación anterior reproducido.
- Suite completa por grupos: **9 aprobados, 3 fallidos**, salida 1. Aprobados: World, Grid, Slope, Temperature, Precipitation, PlateBoundary, TectonicPlate, Game y TectonicMotion.
- Landmass y Biomes fallan al generar Continentes/Enorme/2026: no satisface el perfil tras cuatro intentos (43,0 % tierra, 216 componentes, masa dominante 74,5 %). Este fallo se observó antes de integrar la cinemática.
- StagedLandmass falla en `Every world type has oceans, land and 0..100 elevation`. La comprobación depende de superficie y elevaciones que conservan sus hashes anteriores; no se alteran el relieve ni sus expectativas para hacer pasar la prueba.
- OpenGL: recorrido completo aprobado, incluidos los cuatro tamaños y las tres vistas de esfuerzo. Se revisaron capturas de placas/flechas, compresión, extensión y cizalla con selección visible. Los muestreos de relleno evitan las flechas y el panel superpuestos, que intencionadamente tienen otro color.
- `git diff --check`: sin errores al finalizar.

La suite global **no está completamente aprobada**. Los fallos de terreno requieren una tarea separada que autorice revisar los generadores o sus expectativas. No se modifica ningún resultado esperado histórico para ocultarlos.

## Archivos de esta revisión

| Área | Archivos añadidos o modificados |
| --- | --- |
| Definitions | `WorldGeneration/Configuration/TectonicMotionSettings.cs` |
| Engine | `World/Tectonics/PlateMotionGenerator.cs`, `World/Tectonics/TectonicMotionGenerator.cs`, `World/WorldGenerationService.cs` |
| State | `Game/TectonicMotionState.cs`, `Game/LandmassState.cs`, `Persistence/TectonicMotionValidation.cs`, `Persistence/GameSaveStore.cs` |
| Game | `Graphics/TectonicStressPalette.cs`, `Graphics/MapViewMode.cs`, `Graphics/HexGridRenderer.cs`, `Screens/ScreenLayout.cs`, `Program.cs` |
| Tests | `TectonicMotionTests.cs`, `TectonicMotionTerrainBaseline.txt`, `Program.cs`, `GEvolve.Tests.csproj` |
| Documentación | `TECTONIC_MOTION.md`, `TECTONIC_BOUNDARIES.md`, `WORLD_GENERATION_PIPELINE.md` |
