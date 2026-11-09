# P09.1 — Auditoría e integración inicial de ecología de Pangea

## Auditoría anterior a la integración

Antes de P09.1, el orquestador terminaba Pangea en FINAL y dejaba pendientes las capas ambientales. La sesión temporal conservaba siete operaciones geológicas y exportaba snapshots aislados. Los componentes ambientales ya existían y no requieren duplicación:

- `TerrainSlope.Calculate`: máximo salto entre alturas de vecinos terrestres, océano cero; unidades de nivel por paso hexagonal, no grados.
- `TemperatureGenerator` (`temperature-2`): media anual por latitud, altura FINAL, distancia al océano y ruido periódico determinista. Perfil vigente: ecuador 32 °C, polos −12 °C, referencia oceánica 29/−6 °C, 40 m/nivel, gradiente 6.5 °C/km, influencia costera .65 y ruido regional 1.5 °C. No se ajusta.
- `PrecipitationGenerator` (`precipitation-2`): transporte 2D con 72 iteraciones y promedio de las últimas 12; vientos suaves por latitud, evaporación según superficie/temperatura, orografía según FINAL y circulación existente. Humedad es índice 0–100 de lluvia/demanda térmica, no humedad relativa atmosférica medida. No se ajusta.
- `BiomeCatalog.Default`: recurso original `GEvolve.Definitions/Biomas/biomes.json`, hash del contenido, IDs y rangos. `WorldDefinitions` modela IDs, altitud y latitud; flora/fauna/minerales siguen en JSON y no se pueblan.
- `BiomeClassifier` (`biomes-4`): primero exige elegibilidad JSON por tierra/océano, elevación y latitud/hemisferio. Ordena por prioridad existente, amplitud de latitud e ID ordinal. Solo con temperatura/lluvia/humedad disponibles usa la prioridad climática; no crea categorías. Sin coincidencia conserva estado explícito.
- `LandmassState`, `ClimateState`, `BiomeState` y `GameSaveStore` ya admiten pendientes, clima y tabla compacta de biomas. No hacen falta nuevos esquemas persistibles.

## Reglas reutilizadas, sin calibración

La prioridad climática utiliza aridez `lluvia<250 mm && humedad<=25`, seco `lluvia<500 || humedad<38`, húmedo `lluvia>=1000 && humedad>=45`, muy húmedo `lluvia>=1800 && humedad>=58` y frío `<−4 °C`. Desiertos arenoso/rocoso se prefieren solo áridos y no fríos; salares no se deducen de sequedad. La preferencia tropical utiliza muy húmedo y ≥20 °C, subtropical húmedo y ≥12 °C, templado húmedo y ≥2 °C; boreal ≥500 mm y −12..12 °C. Son prioridades, no exclusiones climáticas: el fallback puede asignar esos IDs fuera de sus condiciones preferidas. Las reglas anteriores de montaña/polar/marino pueden tener precedencia. Esto ordena únicamente candidatos que cumplen el JSON; no impone cuotas ni bandas de bioma nuevas. Los diagnósticos registran casos inesperados y sesgo seco sin modificar estas fórmulas.

## Diseño acotado

Se conserva el estado geológico FINAL separado de los resultados ecológicos. Temperatura (con pendientes automáticas) depende de FINAL; precipitación/humedad de temperatura; biomas de precipitación. Cambiar FINAL invalida transitivamente las tres. Automático y manual usan esas mismas operaciones de la sesión. `WorldGenerationService.GenerateGeology` recorre únicamente `PangaeaGenerationSession.GeologicalStages` dentro del mismo pipeline oficial. Permite comparar referencias históricas sin añadir clima y probar componentes ambientales aislados; no implementa otro generador. Los fixtures, sus hashes y las aserciones geológicas permanecen intactos. La prueba por etapas también ejecuta `Generate` completo y proyecta únicamente sus campos ecológicos fuera de la comparación histórica. La salida normal de Pangea incluye la continuación ecológica. Los otros tipos conservan su ruta.

El laboratorio mantiene 01–05 y añade 06 temperatura, 07 precipitación/humedad, 08 biomas y vistas TEMPERATURA, PRECIPITACIÓN, HUMEDAD y BIOMAS. Reutiliza las paletas térmica, de lluvia y de biomas; el gradiente del índice de humedad es local. BIOMAS muestra el color de categoría incluso sobre cumbres; la regla blanca de SUPERFICIE sigue intacta. Snapshots ecológicos son copias aisladas, sin fabricar resultados pendientes; el controlador retira inmediatamente las capas invalidadas antes de terminar el worker. Las alturas legibles P03-A.7.2.3 permanecen sin cambios. No se ejecuta Game ni validación visual desde Codex.

Los estados persistibles y `GameSaveStore` no cambian. Se verifican roundtrips tanto de mundos geológicos históricos sin campos ambientales como de mundos completos nuevos. Una sesión temporal puede exportar geología completa después de FINAL o su continuación ambiental vigente, nunca geología parcial.

## Evidencia reproducible

`--pangaea-ecology` cubre cuatro tamaños × cuatro semillas, composición exacta con los componentes originales, determinismo, elegibilidad JSON, guardado/carga y sensibilidad FINAL frente a BASE. La sesión manual y el controlador se comprueban en los cuatro tamaños; incluye invalidación y aislamiento de snapshots. Cada registro `ECOLOGY_WORLD` publica distribución por bioma/subbioma, bandas diagnósticas de latitud/elevación, medias climáticas, cuantiles de lluvia, aridez, errores y muestras concretas de prioridades. Las bandas solo agrupan el informe; no clasifican celdas. `PctWithinGroup`, `PctLand` y `PctWorld` explicitan los denominadores.

Desde la raíz: `powershell -NoProfile -ExecutionPolicy Bypass -File .\GEvolve.Tests\ValidationReports\commands.ps1 -ValidationSet PangaeaEcology -Label P09-1 -IsolatedArtifacts`. Incluye compilación, ecología, laboratorio, equivalencia por etapas y suite general. Las salidas y el análisis se publican en `GEvolve.Tests/ValidationReports/`; los resultados históricos fallidos se conservan, sin modificar baselines.

P09.1 es integración inicial, pendiente de calibración y aprobación visual. No aprueba la distribución ni P03-A, ni inicia flora/fauna/minerales.

Evidencia P09.1: [informe técnico y distribución por mundo](../GEvolve.Tests/ValidationReports/P09-1_20261108_032515779Z_96457619/REPORT.md), [comandos, SHA y resultados](../GEvolve.Tests/ValidationReports/P09-1_20261108_032515779Z_96457619/SUMMARY.md). Código de integración: `fa3bf8b0a591ae8e4ed1fd201135d4cf6c9ebc5a`. El informe conserva los prechecks y mide el sesgo seco y los fallbacks climáticos sin modificar el clasificador. Pendiente de calibración y aprobación visual.

Validación del commit de integración: build (0 errores/advertencias), ecología (16 mundos), laboratorio (cuatro tamaños) y equivalencia geológica (28 Pangea + 12 controles) PASS. Suite general: 17 PASS / 2 FAIL históricos (TectonicMotion 4 y ContinentalGeology 12 discrepancias de elevación), con los mismos hashes del informe anterior; baselines intactos. Aridez terrestre 89.49–96.01%, cero celdas sin clasificación y cero océanos incorrectos en la muestra. No se recalibra. Resultados completos en el informe P09.1 enlazado.
