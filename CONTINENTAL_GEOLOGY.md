# Continentes: corteza, subducción y relieve

> Estado vigente: **Continentes** genera una superficie nueva desde grupos de placas; véase [CONTINENTS_SURFACE.md](CONTINENTS_SURFACE.md). La descripción de ensamblaje cortical y costas que sigue documenta una implementación histórica eliminada. El clasificador geológico, el generador de relieve, la subducción, los volcanes y la lectura de guardados geológicos anteriores permanecen en el código, pero no se ejecutan aún en la nueva ruta.

`continents-tectonic-1` fue la ruta activa de **Continentes** en el commit inicial de geología. Es una reconstrucción procedural estática inspirada en procesos geológicos, sin simulación temporal. Pangea, Continentes pequeños y Archipiélagos conservan sus algoritmos y resultados. `tectonic-plates-2` y `tectonic-motion-1` tampoco cambian.

## Orden y responsabilidades

1. `WorldGenerationService` construye la cuadrícula y las 16 placas originales.
2. `TectonicMotionGenerator` calcula sus movimientos, interacciones y campos originales.
3. `ContinentalCrustGenerator` distribuye corteza, provincias geológicas, cratones y terrenos acrecionados usando esas mismas placas y movimientos.
4. `ContinentsGenerator` selecciona un único nivel del mar global y conserva las masas conectadas a sus cratones.
5. `GeologicalContactClassifier` distingue colisión, subducción, rift, dorsal y transformación según corteza y movimiento relativo.
6. `ContinentalReliefGenerator` construye cordilleras, depresiones, fallas, fosas y dorsales; después sitúa los volcanes de subducción.
7. Se publican superficie, elevación y `ContinentalGeologyState`.

Clima, precipitación, pendientes y biomas continúan desactivados en este pipeline. La corteza se decide antes de la superficie; las clasificaciones tectónicas utilizan corteza y velocidades, nunca un bioma ni una costa que todavía no existe. La emersión puntual de los arcos oceánicos se realiza al final y no reclasifica retrospectivamente la corteza original.

Definitions contiene la calibración; Engine calcula; State almacena; Game visualiza. No hay una dependencia de State hacia Engine.

## Cratones y ensamblaje

La cantidad de continentes sigue siendo `LandmassProfiles.ContinentCountForSeed(seed)`, entre 2 y 6, idéntica en las cuatro resoluciones. Se eligen interiores de placas distintas, separando los cratones mediante distancia hexagonal; se evita favorecer raíces pegadas a los bordes polares.

Una propagación multiorigen determina **provincias de procedencia**, sin reclamar presupuestos de tierra para cada continente. Sus costos positivos dependen de la alineación con los vectores oficiales, compresión, extensión y una perturbación espacial periódica secundaria. Cada provincia conserva un árbol de conexión con su cratón.

Se forman cuatro ramas de terrenos acrecionados por cratón, orientadas respecto al movimiento de su placa y a sus contactos compresivos reales. Los recorridos siguen el árbol de la provincia, no el borde de una elipse. Una segunda propagación desde esos ejes produce el potencial de emersión. La deformación y el ruido secundarios dan variación costera; los cratones y sus terrenos adjuntos proporcionan la estructura principal. Las provincias pueden cruzar varias placas y una placa puede contener ambas cortezas.

La corteza continental ocupa una envolvente más amplia que la tierra emergida, inicialmente el 55 % superior del potencial. Esto deja plataformas continentales sumergidas. El nivel del mar es **único para todo el mapa**: se evalúan datums globales y se elige el que más se aproxima al objetivo existente del 35 % de tierra, conservando el número de masas principales y el perfil original (27–43 %, al menos dos masas grandes y dominancia menor de 75 %). No hay cuotas de área por continente ni cambios en esos perfiles para hacer pasar pruebas.

Los márgenes entre provincias permanecen sumergidos, con un corredor de al menos dos hexágonos, para representar separación oceánica entre los ancestros seleccionados. Es una condición de diseño para conservar el número de continentes, no una deducción de una historia tectónica física. Las islas nuevas de esta ruta provienen de los arcos de subducción, no de un rociado de islas aleatorias.

## Contactos y subducción

Se reutilizan las interacciones y normales de `TectonicMotionState`. La convergencia/divergencia y cizalla mantienen sus unidades de cm/año. `ContinentalGeologySettings` centraliza el umbral normal de 0,5 cm/año y la relación mínima normal/cizalla de 0,5 para considerar actividad axial; los contactos más laterales se tratan como transformantes.

| Movimiento y corteza | Tratamiento |
| --- | --- |
| Continental–continental convergente | Colisión y cinturón orogénico; ninguna placa subduce y no se crean volcanes por este contacto |
| Oceánica–continental convergente | Subduce el lado oceánico; fosa en el lado inferior y arco en la placa superior |
| Oceánica–oceánica convergente | Subduce la litosfera oceánica de mayor edad modelada; fosa y arco insular en la placa superior |
| Divergencia con corteza continental | Rift y subsidencia |
| Divergencia oceánica | Dorsal y menor profundidad relativa |
| Predominio de cizalla | Fallas y deformación localizada |
| Movimiento inferior al umbral | Contacto inactivo, sin subducción ni volcanes |

La edad oceánica heredada (20–180 Ma) es un parámetro sintético fijo por placa y semilla, persistido y generado en un flujo separado del movimiento. La litosfera de mayor edad representa la más fría y densa; esa preferencia se usa de forma consistente en todas las aristas OO de la pareja. En edades iguales, se desempata por ID menor. No se elige una placa inferior aleatoria en cada hexágono. No se simula todavía un campo de edad producido por expansión del fondo oceánico: esta simplificación debe revisarse cuando exista ese proceso.

Referencias de los principios físicos: [USGS — Dynamic Planet](https://volcanoes.usgs.gov/about/edu/dynamicplanet/nutshell.php), [USGS — Aleutian volcanic arc](https://pubs.usgs.gov/of/2000/ofr-00-0365/report.htm) y [NPS — Accreted terranes](https://home.nps.gov/subjects/geology/plate-tectonics-accreted-terranes.htm). La calibración procedural y las decisiones para fijar el número de continentes pertenecen al modelo de juego.

## Relieve

Las fuentes de levantamiento, arco, fosa, rift, cizalla y dorsal se obtienen de los valores numéricos de las interacciones originales, filtrados por el tipo de contacto. No hay velocidades ni estimaciones de convergencia privadas. Los campos de compresión y extensión originales también intervienen en el ensamblaje cortical.

Las fuentes se extienden mediante la envolvente gaussiana existente `exp(−(d/r)²)` sobre distancias hexagonales, incluida la unión este–oeste. Colisiones y arcos usan una influencia de 1,3 veces el radio original; rifts y fallas usan 0,5 veces ese radio (mínimo un paso). Los campos conservan canales separados. La variación de intensidades y la geometría de los contactos producen cinturones de anchura variable; la zona transformante es más localizada.

En tierra se usa una plataforma cratónica baja (8–28 niveles), detalle secundario de hasta ±2 niveles y una respuesta `sqrt(clamp(intensidad/8, 0, 1))`. La calibración suma hasta 48 niveles por colisión y 35 por arco y resta hasta 20 por rift y 8 por falla. Se limita el resultado a 1–100. **No se normaliza cada mapa para alcanzar 100** ni para imponer una proporción de montañas. Los valores máximos dependen de los contactos de cada mundo.

La elevación oceánica permanece 0. `OceanDepthByCellId` almacena un índice relativo 1–100 separado: fondo aproximado 55, mayor profundidad en fosas y menor en dorsales, con detalle secundario. No representa metros ni una batimetría físicamente calibrada. En tierra la profundidad es 0.

## Volcanes

Solo las aristas de subducción activa proporcionan candidatos. Desde el extremo superior se recorren vecinos de la **misma placa superior** y se sitúa el arco unos pasos hacia su interior, con la dirección marcada por la normal real y un desplazamiento proporcional al radio tectónico. Esto evita colocar el arco sobre la placa inferior o directamente en la fosa.

Los candidatos se espacian con una separación hexagonal proporcional al tamaño del mapa (4 % de su dimensión menor, mínimo tres pasos), con desempates deterministas. Cada volcán persiste ID de celda, tipo (arco continental o insular), placa inferior, placa superior e índice de la interacción que lo originó. Si varias fuentes alcanzan una ubicación, se conserva una referencia estable de la primera interacción. Los conos oceánicos pueden emerger como islas pequeñas; se rechazan ubicaciones adyacentes a tierra para que no formen puentes entre masas ya separadas. La corteza oceánica de una isla volcánica no se convierte artificialmente en corteza continental.

No se generan volcanes por colisiones continentales puras, por límites inactivos ni por cizalla pura. Tampoco se implementan hotspots o volcanismo de dorsal en esta fase. La subducción no implica una erupción actual; estos objetos representan centros volcánicos geológicos del mapa.

## Estado y compatibilidad

`LandmassState.ContinentalGeology` es opcional y guarda versión, corteza y provincia por ID, cratones, edades oceánicas, contactos, volcanes, accidente geológico y profundidad oceánica. El nivel del mar efectivo también queda registrado. El formato de guardado permanece en 2; los mapas antiguos sin estos campos se cargan sin regeneración. Se validan dimensiones, IDs, tipos, lados de subducción, alturas/profundidades y la referencia de cada volcán a una subducción activa en su placa superior.

La versión global histórica `landmass-12` se conserva para no cambiar la configuración efectiva ni las huellas aprobadas de Pangea. Los nuevos mapas de Continentes quedan identificados por `ContinentalGeology.Version = continents-tectonic-1`; los mapas anteriores no se convierten automáticamente.

## Ruta anterior retirada

- `ContinentalCrustGenerator`: se elimina la asignación privada de velocidades, sus campos sintéticos de colisión/rift y el ensamblaje mediante elipses superpuestas.
- `ContinentsGenerator`: se sustituye la selección anterior de fragmentos y el rociado de islas por el nuevo ensamblaje, un nivel del mar global y los arcos volcánicos.
- `PlateAwareElevationGenerator`: rechaza explícitamente Continentes; su lógica histórica queda aislada para Continentes pequeños y Archipiélagos, con resultados intactos.
- Tanto `WorldGenerationService` como las firmas compatibles de `ContinentsGenerator` usan el mismo procedimiento nuevo. Si el llamador antiguo no aporta placas/movimiento, se reconstruyen los originales oficiales; no existe un segundo algoritmo continental contradictorio.

## Vista del juego

**RELIEVE: GEOLOGÍA** colorea cordilleras, rifts, fallas, fosas, dorsales, arcos y plataformas sumergidas. Los límites de subducción se marcan en naranja y los centros volcánicos con triángulos. La selección muestra corteza, accidente, profundidad relativa y placas del volcán cuando lo hay. La cámara, zoom, cuadrícula y selección se conservan. Las capas posteriores siguen pendientes. Los controles se compactan en ventanas bajas para mantenerlos accesibles.

## Validación

`ContinentalGeologyTests` cubre las cuatro resoluciones y semillas 2026, 42, −731, 9 y 2, correspondientes a 2, 3, 4, 5 y 6 continentes. Comprueba perfiles sin cambios, masas principales, ambas cortezas, plataformas sumergidas, altura/profundidad, contactos controlados, determinismo, volcanes ligados a subducción, ausencia de volcanes en un mundo de colisiones continentales puras, subsidencia de rifts/fallas, falta de levantamiento sin movimiento y persistencia compatible (incluidos datos malformados).

`ApprovedNonContinentalBaseline.txt` se capturó de `master` en `6e95dca` antes de integrar estos cambios: cuatro resoluciones × tres semillas × Pangea/Continentes pequeños/Archipiélagos. Sus **36 mundos** se comparan por SHA-256 de superficie, elevación y placas y por intento elegido; las huellas históricas anteriores tampoco se modifican.

Las pruebas de la ruta continental anterior se adaptan al contrato nuevo: alturas válidas 1–100, sin obligar al máximo 100, y cálculo con `ContinentalReliefGenerator`. El resto de tipos conserva sus comprobaciones anteriores. Los escenarios históricos de Continentes se validan por su nuevo contrato en vez de exigir el fallo antiguo de Enorme/2026.

```powershell
dotnet build GEvolve.sln --no-restore
dotnet run --project GEvolve.Tests --no-build -- --continental-geology
dotnet run --project GEvolve.Tests --no-build -- --all-suites
dotnet run --project GEvolve.Game --no-build -- --graphics-smoke
git diff --check
```

### Resultados ejecutados

- Compilación Debug: 0 errores y 0 advertencias.
- Suite completa `--all-suites`: **12 grupos aprobados, 1 fallido**, código de salida 1. Pasan World, Grid, Landmass, Biomes, Slope, Temperature, Precipitation, PlateBoundary, TectonicPlate, Game, TectonicMotion y ContinentalGeology.
- StagedLandmass se detiene en Pangea Pequeño/2026: el resultado aprobado contiene **cuatro celdas terrestres con elevación 0**, mientras esa prueba exige valores mayores que cero para toda tierra. Su huella sigue idéntica a la capturada antes del cambio. No se altera el generador ni se relaja esa expectativa de Pangea para ocultar el fallo.
- Los 20 escenarios de Continentes pasan: cuatro resoluciones y cinco semillas (2026, 42, −731, 9 y 2), cubriendo las cinco cantidades posibles de continentes. La tierra emergida queda entre 35,06 % y 35,25 %, con 53–89 centros volcánicos y alturas máximas entre 66 y 100 según el mundo. Son resultados, no proporciones objetivo de montañas o volcanes.
- Enorme/2026 ya genera dos continentes y cumple el perfil geográfico original.
- Las 36 huellas de los otros tipos permanecen idénticas, incluidas las 12 de Pangea. Ningún archivo de huellas históricas existente se modifica.
- Pruebas de persistencia y compatibilidad aprobadas, incluyendo volcanes en una placa incorrecta y arreglos de corteza malformados rechazados.
- OpenGL verifica cambios de vista, colores reales y selección; se revisaron capturas de superficie, altura y geología. La prueba gráfica incluye las cuatro resoluciones de Continentes y semillas adicionales 42 y −731.
- `git diff --check`: sin errores al finalizar.

Ejemplo de la semilla 2026 después del ajuste de separación volcánica:

| Tamaño | Continentes | Tierra | Volcanes | Altura máxima |
| --- | ---: | ---: | ---: | ---: |
| Pequeño | 2 | 35,20 % | 53 | 71 |
| Mediano | 2 | 35,19 % | 79 | 70 |
| Grande | 2 | 35,14 % | 82 | 75 |
| Enorme | 2 | 35,06 % | 64 | 66 |

La suite global no se declara completamente aprobada mientras permanezca la discrepancia histórica de Pangea. Su corrección necesita una decisión separada sobre ese resultado aprobado.

## Archivos afectados

Las rutas siguientes son relativas al proyecto indicado:

| Área | Archivos |
| --- | --- |
| Definitions | `WorldGeneration/Configuration/ContinentalGeologySettings.cs` |
| Engine | `World/LandmassGeneration/ContinentsGenerator.cs`, `ContinentalCrustGenerator.cs`, `ContinentalReliefGenerator.cs`, `PlateAwareElevationGenerator.cs`; `World/Tectonics/GeologicalContactClassifier.cs`; `World/WorldGenerationService.cs` |
| State | `Game/ContinentalGeologyState.cs`, `Game/LandmassState.cs`, `Persistence/ContinentalGeologyValidation.cs`, `Persistence/GameSaveStore.cs` |
| Game | `Graphics/ContinentalGeologyPalette.cs`, `Graphics/HexGridRenderer.cs`, `Graphics/MapViewMode.cs`, `Screens/ScreenLayout.cs`, `Program.cs` |
| Tests | `ContinentalGeologyTests.cs`, `ApprovedNonContinentalBaseline.txt`, `StagedLandmassTests.cs`, `TectonicMotionTests.cs`, `LandmassTests.cs`, `Program.cs`, `GEvolve.Tests.csproj` |
| Documentación raíz | `CONTINENTAL_GEOLOGY.md`, `WORLD_GENERATION_PIPELINE.md` |
