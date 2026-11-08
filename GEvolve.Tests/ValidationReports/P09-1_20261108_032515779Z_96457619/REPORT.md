# P09.1 — Integración inicial de biomas de Pangea

**Integración implementada; pendiente de calibración y aprobación visual.** No se aprueba la distribución de biomas ni P03-A. No se genera flora, fauna ni minerales.

**Commit de código validado:** `fa3bf8b0a591ae8e4ed1fd201135d4cf6c9ebc5a`, `feat(worldgen): integrate catalog-driven Pangaea biomes`, rama `worldgeneration_pangea`. Base anterior `a53a754a02363d0b26dc347f7e2c5f53db6475c2`. Fecha UTC, SDK, comandos exactos, códigos de salida y duraciones: [SUMMARY.md](SUMMARY.md). La ejecución comenzó sin diferencias de fuente rastreada; la documentación de evidencia se añade posteriormente.

## Arquitectura y alcance

- La misma sesión oficial conserva las siete operaciones geológicas y añade FINAL → Temperature (incluye pendientes) → Precipitation (lluvia/humedad) → Biomes. `Generate` automático completa las diez operaciones para Pangea; los otros tipos conservan su ruta.
- Se reutilizan sin modificar TerrainSlope, TemperatureGenerator (`temperature-2`), PrecipitationGenerator (`precipitation-2`), BiomeClassifier (`biomes-4`) y el JSON original: 8 biomas, 22 reglas, SHA256 `0A78770E8281178EA848EE4838696FF4EC2DFF813BB63CF669E3F864B05E8254`. No se cambian clima, prioridades, IDs, rangos ni asociaciones de recursos.
- FINAL geológico se conserva separado de los resultados ambientales. Repetir una etapa invalida inmediatamente sus descendientes; snapshots y exportaciones se clonan. `GenerateGeology` recorre las siete operaciones del mismo orquestador y permite conservar referencias geológicas y pruebas aisladas. No duplica generadores.
- Game conserva 01–05 y añade 06 temperatura, 07 precipitación/humedad, 08 biomas. Vistas ecológicas disponibles solo con datos vigentes; BIOMAS usa la paleta por categoría existente y el inspector muestra IDs, clasificación y campos reales. Humedad es índice 0–100, no humedad relativa atmosférica. Pendiente es salto de niveles por paso hexagonal, no grados.
- No cambian geología, placas/H04, forma/costas, BASE/FINAL, cordilleras, topología, baselines ni contratos persistibles. Los generadores climáticos consumen FINAL. Se comprueba guardado/carga de estados nuevos completos y estados históricos sin campos ambientales.
- No se modifican PangaeaHeightLabels, UiRenderer, cámara ni renderer. Se conservan los controles de legibilidad P03-A.7.2.3. Las pruebas de geometría y color son funciones puras; no se abrió Game, creó contexto GL, capturó mapas ni evaluó apariencia.

Auditoría previa y reglas: [PANGAEA_ECOLOGY_INTEGRATION.md](../../../GEvolve.Engine/PANGAEA_ECOLOGY_INTEGRATION.md). Uso manual: [guía](../../../GEvolve.Game/PANGAEA_LABORATORY.md).

## Evidencia y límites de las pruebas

Los 16 casos cubren cuatro tamaños × semillas 2026, −1387338765, −2024691550 y −174327693. Verifican composición byte exacta con los componentes originales, geología intacta, determinismo, elegibilidad JSON, IDs/tierra/océano y persistencia. Los 16 distinguen clima calculado con FINAL frente a BASE. Sesión manual y controlador se verifican con semilla 2026 en cada tamaño: dependencias, repetición, invalidación, vistas, inspector, aislamiento y etiquetas. La prueba por etapas conserva sus 28 Pangea + 12 controles de otros tipos y su referencia independiente original; proyecta solo campos ecológicos nuevos al comparar el automático completo con la referencia geológica. No se alteran hashes esperados.

Los logs son completos por stdout/stderr, con rutas locales privadas sustituidas explícitamente. No se publican ejecutables ni bin/obj. Se conservan el [precheck fallido](../P09-1-precheck_20261108_031829732Z_2fb2b6e8/REPORT.md) y el [precheck posterior](../P09-1-precheck_20261108_031921018Z_3bb7ecf6/REPORT.md), ambos con fuente aún sin commit; no certifican el SHA de integración. Sus resultados no se sobrescriben.

## Resultados técnicos

| Validación | Estado | Código | Duración s | Log completo |
|---|---|---:|---:|---|
| Build | PASS; 0 advertencias, 0 errores | 0 | 3.470 | [build.log](build.log) |
| Ecología Pangea | PASS; 16 mundos, cuatro tamaños | 0 | 57.103 | [pangaea-ecology.log](pangaea-ecology.log) |
| Laboratorio, código sin ventana/GL | PASS; cuatro tamaños | 0 | 13.165 | [pangaea-laboratory.log](pangaea-laboratory.log) |
| Equivalencia por etapas | PASS; 28 Pangea + 12 otros tipos | 0 | 64.397 | [pangaea-staged.log](pangaea-staged.log) |
| Todas las suites | **FAIL; 17 PASS / 2 FAIL** | 1 | 390.738 | [all-suites.log](all-suites.log) |

Fallos: TectonicMotion (4 escenarios) y ContinentalGeology (12 escenarios), ambos por elevaciones históricas pendientes. Se compararon con [el informe anterior](../P03-A723_20261108_025945937Z_c306be7e/REPORT.md): mismos escenarios y mismos hashes esperados/obtenidos, sin diferencias en los registros de baseline. Las demás capas no aparecen como diferencias nuevas. Los baselines y las referencias no se modificaron. El script termina con código agregado 1 y conserva los fallos; esta entrega no presenta la suite general como aprobada.

Las 28 líneas `STAGED_EQ` y los 12 controles `STAGED_OTHER_PASS` son idénticos a los del informe anterior. Una comparación Git desde la base confirma cero modificaciones en los seis archivos de baseline/referencia, catálogos, State, generadores geológicos/climáticos, clasificador y lógica de etiquetas/cámara/renderer. No se repitieron pruebas tras añadir solo documentación de evidencia.

## Distribución inicial observada

Cada fila corresponde a un mundo, no a una cuota. Porcentajes sobre tierra; `marine` es 0% terrestre en todos los casos. En la muestra, todos los océanos resultan `marine_open_ocean`; los demás IDs marinos no reciben celdas. Es el comportamiento actual de elegibilidad/prioridad, sin nueva batimetría.

| Tamaño / semilla | Tierra | Desert % | Forest % | Plains % | Mountain % | Tundra % | Polar N % | Polar S % | Árido % tierra | Mediana lluvia mm/año |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Small/2026 | 10650 | 35.89 | 2.28 | 19.79 | 7.29 | 21.48 | 7.30 | 5.97 | 90.32 | 30 |
| Small/-1387338765 | 10995 | 43.60 | 3.13 | 26.25 | 5.35 | 14.12 | 3.00 | 4.56 | 91.44 | 36 |
| Small/-2024691550 | 10640 | 33.34 | 3.17 | 23.47 | 5.73 | 21.30 | 5.17 | 7.83 | 89.49 | 29 |
| Small/-174327693 | 11046 | 39.44 | 2.83 | 24.61 | 9.39 | 14.60 | 2.93 | 6.20 | 90.91 | 34 |
| Medium/2026 | 16700 | 40.97 | 2.53 | 21.58 | 6.71 | 18.47 | 9.74 | 0.00 | 91.87 | 29 |
| Medium/-1387338765 | 16700 | 32.68 | 2.50 | 31.28 | 5.54 | 18.95 | 2.48 | 6.58 | 93.13 | 32 |
| Medium/-2024691550 | 16706 | 37.28 | 2.60 | 26.91 | 8.31 | 15.73 | 0.00 | 9.16 | 93.50 | 31 |
| Medium/-174327693 | 16700 | 46.97 | 2.12 | 42.48 | 2.80 | 4.43 | 1.21 | 0.00 | 92.08 | 47 |
| Large/2026 | 27543 | 49.50 | 1.18 | 32.83 | 3.60 | 10.31 | 0.00 | 2.58 | 92.87 | 38 |
| Large/-1387338765 | 24022 | 39.09 | 1.82 | 26.18 | 7.35 | 16.64 | 7.71 | 1.22 | 92.09 | 34 |
| Large/-2024691550 | 24000 | 42.72 | 1.41 | 20.28 | 7.74 | 17.10 | 6.01 | 4.74 | 94.96 | 27 |
| Large/-174327693 | 24881 | 37.36 | 2.53 | 21.63 | 6.65 | 19.91 | 1.62 | 10.28 | 94.63 | 28 |
| Enormous/2026 | 42720 | 33.74 | 1.50 | 20.31 | 4.45 | 26.21 | 8.62 | 5.17 | 93.77 | 20 |
| Enormous/-1387338765 | 42722 | 39.18 | 1.18 | 21.41 | 3.81 | 19.30 | 8.51 | 6.60 | 94.84 | 26 |
| Enormous/-2024691550 | 43749 | 43.19 | 1.15 | 24.84 | 3.39 | 17.56 | 6.82 | 3.07 | 95.65 | 33 |
| Enormous/-174327693 | 42721 | 31.24 | 1.05 | 21.66 | 6.17 | 25.55 | 5.23 | 9.09 | 96.01 | 23 |

**Sesgo seco medido:** 89.49–96.01% de tierra cumple `lluvia<250 mm && humedad<=25`; 95.29–98.47% tiene lluvia<500 mm/año. Las medianas terrestres son 20–47 mm/año en estos casos. Es evidencia de una distribución climática muy seca bajo las reglas actuales, no una justificación para cambiar fórmulas en esta tarea. No se midió realismo físico ni se aprobó geográficamente.

**Clasificación:** cero celdas sin asignar y cero océanos incorrectamente clasificados en los 16 mundos. El cumplimiento del catálogo no certifica que la distribución sea natural.

**Prioridades a revisar posteriormente:** las condiciones climáticas son preferencias, no exclusiones. Por ejemplo Small/2026, celda 11365: `forest_tropical`, FINAL 58, pendiente 26, latitud 13.636°, temperatura 13.6 °C, lluvia 332 mm/año, humedad 30. Cumple JSON pero no la preferencia tropical ≥20 °C, ≥1800 mm, humedad≥58. El fallback de prioridades permite ese resultado. Otras observaciones cuantificadas son tierras áridas sin desierto elegible y montaña con vegetación elegible desplazada. No se corrige ni fuerza una clasificación nueva.

## Informes por mundo

Fuente: registros `ECOLOGY_WORLD` en [pangaea-ecology.log](pangaea-ecology.log). Cada registro conserva datos no redondeados, hash geológico, medias climáticas por bioma/subbioma y bandas, cuantiles, todas las observaciones y hasta cinco ejemplos por motivo con IDs de candidatos elegibles. Aquí se redondea a dos decimales. IDs ausentes en una tabla tienen cero celdas en ese grupo. Los porcentajes de subbiomas marinos se dan sobre mundo; el porcentaje terrestre es cero. Las bandas de latitud absoluta de 15° y elevación de 20 niveles son agrupaciones diagnósticas; no se usan para clasificar.

### Small / 2026

26600 celdas; 10650 terrestres. Áridas: 9619 (90.32% tierra); lluvia<500: 95.29%. Lluvia terrestre min/P10/mediana/P90/max: 2/8/30/187/7147 mm/año. Sin clasificación: 0; océanos incorrectos: 0.

| Bioma terrestre | % tierra | FINAL medio | Pendiente media | °C media | Lluvia media mm/año | Humedad media |
|---|---:|---:|---:|---:|---:|---:|
| desert | 35.89 | 21.53 | 5.55 | 11.07 | 55.93 | 7.31 |
| forest | 2.28 | 20.80 | 7.03 | 9.31 | 1400.63 | 59.15 |
| mountain | 7.29 | 69.19 | 15.18 | -3.31 | 392.60 | 21.49 |
| plains | 19.79 | 34.42 | 8.08 | 8.94 | 120.28 | 11.03 |
| polar_north | 7.30 | 45.08 | 4.79 | -22.77 | 39.10 | 6.53 |
| polar_south | 5.97 | 36.04 | 4.77 | -20.31 | 26.10 | 4.59 |
| tundra | 21.48 | 41.09 | 5.73 | -16.77 | 59.05 | 8.04 |

| Subbioma | Celdas | % tierra | % mundo |
|---|---:|---:|---:|
| desert_rocky | 2922 | 27.44 | 10.98 |
| desert_sandy | 900 | 8.45 | 3.38 |
| forest_boreal | 114 | 1.07 | 0.43 |
| forest_subtropical | 41 | 0.38 | 0.15 |
| forest_temperate | 50 | 0.47 | 0.19 |
| forest_tropical | 38 | 0.36 | 0.14 |
| marine_open_ocean | 15950 | 0.00 | 59.96 |
| mountain_alpine | 443 | 4.16 | 1.67 |
| mountain_cliffs | 87 | 0.82 | 0.33 |
| mountain_rocky | 246 | 2.31 | 0.92 |
| plains_prairie | 48 | 0.45 | 0.18 |
| plains_savanna | 1074 | 10.08 | 4.04 |
| plains_steppe | 986 | 9.26 | 3.71 |
| polar_north_ice | 777 | 7.30 | 2.92 |
| polar_south_ice | 636 | 5.97 | 2.39 |
| tundra_tundra | 2288 | 21.48 | 8.60 |

| Latitud absoluta ° | Desert % | Forest % | Plains % | Mountain % | Tundra % | Polar N % | Polar S % |
|---|---:|---:|---:|---:|---:|---:|---:|
| 0–15 | 14.89 | 2.59 | 75.00 | 7.52 | 0.00 | 0.00 | 0.00 |
| 15–30 | 73.47 | 2.79 | 11.14 | 12.60 | 0.00 | 0.00 | 0.00 |
| 30–45 | 80.04 | 2.81 | 6.24 | 10.90 | 0.00 | 0.00 | 0.00 |
| 45–60 | 44.37 | 4.94 | 41.04 | 9.65 | 0.00 | 0.00 | 0.00 |
| 60–75 | 0.31 | 0.43 | 0.12 | 3.27 | 95.87 | 0.00 | 0.00 |
| 75–90 | 0.00 | 0.00 | 0.00 | 0.83 | 33.87 | 35.91 | 29.39 |

| FINAL niveles | Desert % | Forest % | Plains % | Mountain % | Tundra % | Polar N % | Polar S % |
|---|---:|---:|---:|---:|---:|---:|---:|
| 1–20 | 58.57 | 4.12 | 21.35 | 2.17 | 9.77 | 2.87 | 1.15 |
| 21–40 | 31.03 | 1.27 | 14.90 | 0.00 | 30.69 | 6.77 | 15.34 |
| 41–60 | 29.12 | 1.17 | 22.34 | 0.00 | 27.78 | 13.62 | 5.98 |
| 61–80 | 0.00 | 1.15 | 22.63 | 32.63 | 29.25 | 11.34 | 3.00 |
| 81–100 | 0.00 | 0.00 | 1.67 | 98.33 | 0.00 | 0.00 | 0.00 |

| Observación de prioridad | Celdas | Ejemplo (celda, subbioma, FINAL, latitud, °C, lluvia, humedad) |
|---|---:|---|
| AridWithoutCatalogEligibleDesert | 4127 | 73, polar_north_ice, 14, 90.00, -12.90, 6, 2 |
| ReliefPriorityOverEligibleVegetation | 663 | 2296, mountain_rocky, 66, 75.00, -26.70, 19, 5 |
| TropicalOutsidePreferredClimate | 9 | 11365, forest_tropical, 58, 13.64, 13.60, 332, 30 |

### Small / -1387338765

26600 celdas; 10995 terrestres. Áridas: 10054 (91.44% tierra); lluvia<500: 96.81%. Lluvia terrestre min/P10/mediana/P90/max: 2/10/36/152/7565 mm/año. Sin clasificación: 0; océanos incorrectos: 0.

| Bioma terrestre | % tierra | FINAL medio | Pendiente media | °C media | Lluvia media mm/año | Humedad media |
|---|---:|---:|---:|---:|---:|---:|
| desert | 43.60 | 22.00 | 4.43 | 12.19 | 42.27 | 5.56 |
| forest | 3.13 | 51.94 | 4.55 | 10.49 | 1326.71 | 52.22 |
| mountain | 5.35 | 73.01 | 16.69 | 0.55 | 85.56 | 9.56 |
| plains | 26.25 | 39.14 | 5.28 | 11.59 | 100.05 | 10.66 |
| polar_north | 3.00 | 17.20 | 3.14 | -14.89 | 22.35 | 4.30 |
| polar_south | 4.56 | 40.07 | 3.85 | -21.09 | 26.20 | 4.54 |
| tundra | 14.12 | 25.31 | 4.06 | -11.89 | 105.70 | 12.50 |

| Subbioma | Celdas | % tierra | % mundo |
|---|---:|---:|---:|
| desert_rocky | 3959 | 36.01 | 14.88 |
| desert_sandy | 835 | 7.59 | 3.14 |
| forest_boreal | 48 | 0.44 | 0.18 |
| forest_subtropical | 127 | 1.16 | 0.48 |
| forest_temperate | 27 | 0.25 | 0.10 |
| forest_tropical | 142 | 1.29 | 0.53 |
| marine_open_ocean | 15605 | 0.00 | 58.67 |
| mountain_alpine | 372 | 3.38 | 1.40 |
| mountain_cliffs | 65 | 0.59 | 0.24 |
| mountain_rocky | 151 | 1.37 | 0.57 |
| plains_prairie | 56 | 0.51 | 0.21 |
| plains_savanna | 1723 | 15.67 | 6.48 |
| plains_steppe | 1107 | 10.07 | 4.16 |
| polar_north_ice | 330 | 3.00 | 1.24 |
| polar_south_ice | 501 | 4.56 | 1.88 |
| tundra_tundra | 1552 | 14.12 | 5.83 |

| Latitud absoluta ° | Desert % | Forest % | Plains % | Mountain % | Tundra % | Polar N % | Polar S % |
|---|---:|---:|---:|---:|---:|---:|---:|
| 0–15 | 13.83 | 6.42 | 70.85 | 8.90 | 0.00 | 0.00 | 0.00 |
| 15–30 | 77.80 | 5.89 | 11.55 | 4.76 | 0.00 | 0.00 | 0.00 |
| 30–45 | 78.00 | 1.10 | 15.28 | 5.61 | 0.00 | 0.00 | 0.00 |
| 45–60 | 54.43 | 1.81 | 36.55 | 7.21 | 0.00 | 0.00 | 0.00 |
| 60–75 | 2.16 | 1.08 | 2.16 | 1.99 | 92.61 | 0.00 | 0.00 |
| 75–90 | 0.00 | 0.00 | 0.00 | 0.08 | 34.44 | 26.00 | 39.48 |

| FINAL niveles | Desert % | Forest % | Plains % | Mountain % | Tundra % | Polar N % | Polar S % |
|---|---:|---:|---:|---:|---:|---:|---:|
| 1–20 | 56.70 | 0.49 | 17.83 | 1.38 | 16.85 | 5.63 | 1.12 |
| 21–40 | 53.12 | 1.19 | 19.43 | 0.00 | 15.64 | 1.98 | 8.64 |
| 41–60 | 29.68 | 9.86 | 42.66 | 0.00 | 12.67 | 0.32 | 4.82 |
| 61–80 | 0.00 | 8.50 | 56.33 | 21.33 | 5.01 | 0.00 | 8.84 |
| 81–100 | 0.00 | 0.00 | 2.51 | 97.49 | 0.00 | 0.00 | 0.00 |

| Observación de prioridad | Celdas | Ejemplo (celda, subbioma, FINAL, latitud, °C, lluvia, humedad) |
|---|---:|---|
| AridWithoutCatalogEligibleDesert | 3553 | 155, polar_north_ice, 13, 90.00, -12.20, 6, 2 |
| ReliefPriorityOverEligibleVegetation | 413 | 2341, mountain_alpine, 75, 75.00, -25.70, 111, 23 |
| TropicalOutsidePreferredClimate | 142 | 11282, forest_tropical, 47, 13.64, 16.50, 320, 28 |

### Small / -2024691550

26600 celdas; 10640 terrestres. Áridas: 9522 (89.49% tierra); lluvia<500: 95.35%. Lluvia terrestre min/P10/mediana/P90/max: 1/8/29/208/5725 mm/año. Sin clasificación: 0; océanos incorrectos: 0.

| Bioma terrestre | % tierra | FINAL medio | Pendiente media | °C media | Lluvia media mm/año | Humedad media |
|---|---:|---:|---:|---:|---:|---:|
| desert | 33.34 | 25.43 | 4.93 | 9.14 | 52.42 | 7.32 |
| forest | 3.17 | 38.36 | 4.42 | 10.97 | 1674.22 | 61.00 |
| mountain | 5.73 | 71.63 | 14.67 | -9.73 | 120.97 | 10.92 |
| plains | 23.47 | 45.57 | 5.18 | 7.73 | 130.86 | 13.26 |
| polar_north | 5.17 | 16.11 | 5.53 | -14.45 | 29.77 | 5.27 |
| polar_south | 7.83 | 19.89 | 3.09 | -16.38 | 18.76 | 3.61 |
| tundra | 21.30 | 27.61 | 7.88 | -12.52 | 52.63 | 7.73 |

| Subbioma | Celdas | % tierra | % mundo |
|---|---:|---:|---:|
| desert_rocky | 3402 | 31.97 | 12.79 |
| desert_sandy | 145 | 1.36 | 0.55 |
| forest_boreal | 73 | 0.69 | 0.27 |
| forest_subtropical | 112 | 1.05 | 0.42 |
| forest_temperate | 42 | 0.39 | 0.16 |
| forest_tropical | 110 | 1.03 | 0.41 |
| marine_open_ocean | 15960 | 0.00 | 60.00 |
| mountain_alpine | 375 | 3.52 | 1.41 |
| mountain_cliffs | 72 | 0.68 | 0.27 |
| mountain_rocky | 163 | 1.53 | 0.61 |
| plains_prairie | 62 | 0.58 | 0.23 |
| plains_savanna | 1407 | 13.22 | 5.29 |
| plains_steppe | 1028 | 9.66 | 3.86 |
| polar_north_ice | 550 | 5.17 | 2.07 |
| polar_south_ice | 833 | 7.83 | 3.13 |
| tundra_tundra | 2266 | 21.30 | 8.52 |

| Latitud absoluta ° | Desert % | Forest % | Plains % | Mountain % | Tundra % | Polar N % | Polar S % |
|---|---:|---:|---:|---:|---:|---:|---:|
| 0–15 | 6.30 | 6.83 | 84.77 | 2.10 | 0.00 | 0.00 | 0.00 |
| 15–30 | 59.72 | 7.80 | 21.43 | 11.04 | 0.00 | 0.00 | 0.00 |
| 30–45 | 81.67 | 3.06 | 9.75 | 5.52 | 0.00 | 0.00 | 0.00 |
| 45–60 | 51.98 | 3.52 | 42.06 | 2.44 | 0.00 | 0.00 | 0.00 |
| 60–75 | 1.54 | 0.05 | 0.36 | 10.04 | 88.00 | 0.00 | 0.00 |
| 75–90 | 0.00 | 0.00 | 0.00 | 3.53 | 27.70 | 27.35 | 41.42 |

| FINAL niveles | Desert % | Forest % | Plains % | Mountain % | Tundra % | Polar N % | Polar S % |
|---|---:|---:|---:|---:|---:|---:|---:|
| 1–20 | 38.72 | 2.38 | 7.93 | 1.71 | 24.99 | 10.59 | 13.68 |
| 21–40 | 46.77 | 2.02 | 23.98 | 0.00 | 21.92 | 1.88 | 3.42 |
| 41–60 | 28.56 | 7.62 | 35.69 | 0.00 | 22.38 | 0.79 | 4.95 |
| 61–80 | 0.00 | 1.94 | 58.29 | 23.33 | 10.08 | 1.78 | 4.57 |
| 81–100 | 0.00 | 0.00 | 1.18 | 92.94 | 1.57 | 4.31 | 0.00 |

| Observación de prioridad | Celdas | Ejemplo (celda, subbioma, FINAL, latitud, °C, lluvia, humedad) |
|---|---:|---|
| AridWithoutCatalogEligibleDesert | 4341 | 0, polar_north_ice, 23, 90.00, -14.90, 11, 3 |
| ReliefPriorityOverEligibleVegetation | 415 | 2336, mountain_alpine, 76, 75.00, -25.40, 3, 1 |
| TropicalOutsidePreferredClimate | 59 | 13418, forest_tropical, 19, -1.36, 25.40, 1664, 60 |

### Small / -174327693

26600 celdas; 11046 terrestres. Áridas: 10042 (90.91% tierra); lluvia<500: 96.90%. Lluvia terrestre min/P10/mediana/P90/max: 2/10/34/153/9911 mm/año. Sin clasificación: 0; océanos incorrectos: 0.

| Bioma terrestre | % tierra | FINAL medio | Pendiente media | °C media | Lluvia media mm/año | Humedad media |
|---|---:|---:|---:|---:|---:|---:|
| desert | 39.44 | 23.98 | 5.03 | 11.13 | 39.53 | 5.39 |
| forest | 2.83 | 34.92 | 5.82 | 9.35 | 1367.70 | 53.80 |
| mountain | 9.39 | 46.02 | 9.62 | -1.37 | 55.27 | 7.33 |
| plains | 24.61 | 43.41 | 5.75 | 14.86 | 106.20 | 10.53 |
| polar_north | 2.93 | 42.02 | 5.14 | -20.36 | 59.64 | 9.20 |
| polar_south | 6.20 | 33.47 | 6.86 | -20.29 | 20.58 | 4.18 |
| tundra | 14.60 | 28.14 | 9.24 | -13.14 | 94.11 | 12.24 |

| Subbioma | Celdas | % tierra | % mundo |
|---|---:|---:|---:|
| desert_rocky | 3495 | 31.64 | 13.14 |
| desert_sandy | 861 | 7.79 | 3.24 |
| forest_boreal | 117 | 1.06 | 0.44 |
| forest_subtropical | 73 | 0.66 | 0.27 |
| forest_temperate | 34 | 0.31 | 0.13 |
| forest_tropical | 89 | 0.81 | 0.33 |
| marine_open_ocean | 15554 | 0.00 | 58.47 |
| mountain_alpine | 390 | 3.53 | 1.47 |
| mountain_cliffs | 439 | 3.97 | 1.65 |
| mountain_rocky | 208 | 1.88 | 0.78 |
| plains_prairie | 24 | 0.22 | 0.09 |
| plains_savanna | 2133 | 19.31 | 8.02 |
| plains_steppe | 561 | 5.08 | 2.11 |
| polar_north_ice | 324 | 2.93 | 1.22 |
| polar_south_ice | 685 | 6.20 | 2.58 |
| tundra_tundra | 1613 | 14.60 | 6.06 |

| Latitud absoluta ° | Desert % | Forest % | Plains % | Mountain % | Tundra % | Polar N % | Polar S % |
|---|---:|---:|---:|---:|---:|---:|---:|
| 0–15 | 6.15 | 3.71 | 81.64 | 8.50 | 0.00 | 0.00 | 0.00 |
| 15–30 | 64.65 | 4.16 | 22.89 | 8.31 | 0.00 | 0.00 | 0.00 |
| 30–45 | 88.79 | 1.59 | 5.08 | 4.53 | 0.00 | 0.00 | 0.00 |
| 45–60 | 59.07 | 5.51 | 21.29 | 14.13 | 0.00 | 0.00 | 0.00 |
| 60–75 | 1.07 | 0.94 | 0.73 | 18.44 | 78.82 | 0.00 | 0.00 |
| 75–90 | 0.00 | 0.00 | 0.00 | 3.67 | 28.92 | 21.64 | 45.76 |

| FINAL niveles | Desert % | Forest % | Plains % | Mountain % | Tundra % | Polar N % | Polar S % |
|---|---:|---:|---:|---:|---:|---:|---:|
| 1–20 | 50.18 | 2.93 | 9.41 | 10.54 | 19.27 | 0.14 | 7.54 |
| 21–40 | 49.36 | 1.80 | 28.74 | 0.00 | 12.59 | 4.24 | 3.28 |
| 41–60 | 33.78 | 2.60 | 35.27 | 0.00 | 12.02 | 8.45 | 7.89 |
| 61–80 | 0.00 | 5.57 | 47.32 | 31.10 | 11.14 | 0.28 | 4.58 |
| 81–100 | 0.00 | 0.00 | 0.00 | 81.77 | 0.52 | 0.00 | 17.71 |

| Observación de prioridad | Celdas | Ejemplo (celda, subbioma, FINAL, latitud, °C, lluvia, humedad) |
|---|---:|---|
| AridWithoutCatalogEligibleDesert | 3969 | 0, polar_north_ice, 25, 90.00, -14.60, 2, 1 |
| ReliefPriorityOverEligibleVegetation | 919 | 2206, mountain_alpine, 78, 75.00, -27.10, 8, 2 |
| TropicalOutsidePreferredClimate | 61 | 11275, forest_tropical, 57, 13.64, 14.60, 432, 35 |

### Medium / 2026

41750 celdas; 16700 terrestres. Áridas: 15342 (91.87% tierra); lluvia<500: 96.13%. Lluvia terrestre min/P10/mediana/P90/max: 1/8/29/146/8709 mm/año. Sin clasificación: 0; océanos incorrectos: 0.

| Bioma terrestre | % tierra | FINAL medio | Pendiente media | °C media | Lluvia media mm/año | Humedad media |
|---|---:|---:|---:|---:|---:|---:|
| desert | 40.97 | 19.59 | 3.53 | 12.07 | 47.17 | 6.36 |
| forest | 2.53 | 34.13 | 9.47 | 11.80 | 1869.51 | 63.75 |
| mountain | 6.71 | 66.88 | 13.05 | -8.31 | 214.32 | 14.53 |
| plains | 21.58 | 32.59 | 4.56 | 9.27 | 119.98 | 11.45 |
| polar_north | 9.74 | 48.63 | 3.40 | -24.43 | 18.28 | 3.49 |
| tundra | 18.47 | 44.74 | 4.28 | -18.13 | 24.82 | 4.58 |

| Subbioma | Celdas | % tierra | % mundo |
|---|---:|---:|---:|
| desert_rocky | 4479 | 26.82 | 10.73 |
| desert_sandy | 2363 | 14.15 | 5.66 |
| forest_boreal | 52 | 0.31 | 0.12 |
| forest_subtropical | 113 | 0.68 | 0.27 |
| forest_temperate | 142 | 0.85 | 0.34 |
| forest_tropical | 115 | 0.69 | 0.28 |
| marine_open_ocean | 25050 | 0.00 | 60.00 |
| mountain_alpine | 542 | 3.25 | 1.30 |
| mountain_cliffs | 213 | 1.28 | 0.51 |
| mountain_rocky | 365 | 2.19 | 0.87 |
| plains_prairie | 84 | 0.50 | 0.20 |
| plains_savanna | 1821 | 10.90 | 4.36 |
| plains_steppe | 1699 | 10.17 | 4.07 |
| polar_north_ice | 1627 | 9.74 | 3.90 |
| tundra_tundra | 3085 | 18.47 | 7.39 |

| Latitud absoluta ° | Desert % | Forest % | Plains % | Mountain % | Tundra % | Polar N % | Polar S % |
|---|---:|---:|---:|---:|---:|---:|---:|
| 0–15 | 18.68 | 4.53 | 76.70 | 0.09 | 0.00 | 0.00 | 0.00 |
| 15–30 | 81.34 | 5.55 | 5.24 | 7.86 | 0.00 | 0.00 | 0.00 |
| 30–45 | 82.20 | 3.64 | 6.31 | 7.85 | 0.00 | 0.00 | 0.00 |
| 45–60 | 42.09 | 1.45 | 42.68 | 13.78 | 0.00 | 0.00 | 0.00 |
| 60–75 | 0.00 | 0.00 | 0.43 | 5.11 | 94.47 | 0.00 | 0.00 |
| 75–90 | 0.00 | 0.00 | 0.00 | 0.87 | 28.11 | 71.02 | 0.00 |

| FINAL niveles | Desert % | Forest % | Plains % | Mountain % | Tundra % | Polar N % | Polar S % |
|---|---:|---:|---:|---:|---:|---:|---:|
| 1–20 | 65.32 | 2.03 | 20.28 | 3.25 | 6.41 | 2.70 | 0.00 |
| 21–40 | 41.04 | 2.50 | 24.11 | 0.00 | 24.45 | 7.90 | 0.00 |
| 41–60 | 24.85 | 4.47 | 25.83 | 0.00 | 25.33 | 19.51 | 0.00 |
| 61–80 | 0.00 | 1.35 | 17.41 | 22.16 | 37.67 | 21.41 | 0.00 |
| 81–100 | 0.00 | 0.00 | 4.04 | 93.74 | 1.82 | 0.40 | 0.00 |

| Observación de prioridad | Celdas | Ejemplo (celda, subbioma, FINAL, latitud, °C, lluvia, humedad) |
|---|---:|---|
| AridWithoutCatalogEligibleDesert | 6098 | 0, polar_north_ice, 80, 90.00, -31.90, 5, 1 |
| ReliefPriorityOverEligibleVegetation | 757 | 3638, mountain_rocky, 67, 74.82, -27.00, 17, 4 |
| TropicalOutsidePreferredClimate | 51 | 17651, forest_tropical, 45, 14.10, 16.40, 392, 32 |

### Medium / -1387338765

41750 celdas; 16700 terrestres. Áridas: 15552 (93.13% tierra); lluvia<500: 97.74%. Lluvia terrestre min/P10/mediana/P90/max: 2/9/32/113/6426 mm/año. Sin clasificación: 0; océanos incorrectos: 0.

| Bioma terrestre | % tierra | FINAL medio | Pendiente media | °C media | Lluvia media mm/año | Humedad media |
|---|---:|---:|---:|---:|---:|---:|
| desert | 32.68 | 26.63 | 3.58 | 8.84 | 38.66 | 5.83 |
| forest | 2.50 | 61.98 | 2.97 | 7.53 | 1225.95 | 52.88 |
| mountain | 5.54 | 37.41 | 5.92 | -2.04 | 96.19 | 10.04 |
| plains | 31.28 | 48.20 | 4.65 | 10.52 | 81.47 | 8.95 |
| polar_north | 2.48 | 34.63 | 3.82 | -19.05 | 41.07 | 6.50 |
| polar_south | 6.58 | 29.61 | 6.77 | -18.22 | 24.82 | 4.12 |
| tundra | 18.95 | 18.05 | 3.52 | -10.09 | 65.74 | 8.59 |

| Subbioma | Celdas | % tierra | % mundo |
|---|---:|---:|---:|
| desert_rocky | 4579 | 27.42 | 10.97 |
| desert_sandy | 878 | 5.26 | 2.10 |
| forest_boreal | 58 | 0.35 | 0.14 |
| forest_subtropical | 145 | 0.87 | 0.35 |
| forest_temperate | 39 | 0.23 | 0.09 |
| forest_tropical | 176 | 1.05 | 0.42 |
| marine_open_ocean | 25050 | 0.00 | 60.00 |
| mountain_alpine | 294 | 1.76 | 0.70 |
| mountain_cliffs | 523 | 3.13 | 1.25 |
| mountain_rocky | 108 | 0.65 | 0.26 |
| plains_prairie | 53 | 0.32 | 0.13 |
| plains_savanna | 3506 | 20.99 | 8.40 |
| plains_steppe | 1664 | 9.96 | 3.99 |
| polar_north_ice | 414 | 2.48 | 0.99 |
| polar_south_ice | 1099 | 6.58 | 2.63 |
| tundra_tundra | 3164 | 18.95 | 7.58 |

| Latitud absoluta ° | Desert % | Forest % | Plains % | Mountain % | Tundra % | Polar N % | Polar S % |
|---|---:|---:|---:|---:|---:|---:|---:|
| 0–15 | 4.84 | 5.57 | 86.76 | 2.82 | 0.00 | 0.00 | 0.00 |
| 15–30 | 53.38 | 5.33 | 36.71 | 4.58 | 0.00 | 0.00 | 0.00 |
| 30–45 | 80.02 | 1.27 | 14.97 | 3.73 | 0.00 | 0.00 | 0.00 |
| 45–60 | 53.48 | 1.81 | 32.43 | 12.28 | 0.00 | 0.00 | 0.00 |
| 60–75 | 0.00 | 0.07 | 1.54 | 5.21 | 93.18 | 0.00 | 0.00 |
| 75–90 | 0.00 | 0.00 | 0.00 | 3.73 | 25.80 | 19.28 | 51.19 |

| FINAL niveles | Desert % | Forest % | Plains % | Mountain % | Tundra % | Polar N % | Polar S % |
|---|---:|---:|---:|---:|---:|---:|---:|
| 1–20 | 39.35 | 0.11 | 6.01 | 8.23 | 36.17 | 1.42 | 8.72 |
| 21–40 | 36.01 | 1.51 | 38.15 | 0.00 | 15.06 | 3.59 | 5.68 |
| 41–60 | 42.30 | 0.93 | 40.59 | 0.00 | 5.60 | 4.89 | 5.70 |
| 61–80 | 0.00 | 12.37 | 72.46 | 12.14 | 0.67 | 0.32 | 2.05 |
| 81–100 | 0.00 | 0.00 | 3.80 | 59.49 | 0.00 | 0.00 | 36.71 |

| Observación de prioridad | Celdas | Ejemplo (celda, subbioma, FINAL, latitud, °C, lluvia, humedad) |
|---|---:|---|
| AridWithoutCatalogEligibleDesert | 6757 | 109, polar_north_ice, 7, 90.00, -10.20, 4, 1 |
| ReliefPriorityOverEligibleVegetation | 830 | 5915, mountain_cliffs, 7, 65.06, -3.70, 79, 17 |
| TropicalOutsidePreferredClimate | 176 | 17676, forest_tropical, 63, 14.10, 12.00, 259, 27 |

### Medium / -2024691550

41750 celdas; 16706 terrestres. Áridas: 15620 (93.50% tierra); lluvia<500: 96.61%. Lluvia terrestre min/P10/mediana/P90/max: 2/8/31/122/9498 mm/año. Sin clasificación: 0; océanos incorrectos: 0.

| Bioma terrestre | % tierra | FINAL medio | Pendiente media | °C media | Lluvia media mm/año | Humedad media |
|---|---:|---:|---:|---:|---:|---:|
| desert | 37.28 | 24.88 | 4.97 | 10.87 | 42.86 | 5.78 |
| forest | 2.60 | 43.94 | 9.07 | 12.01 | 1821.95 | 61.30 |
| mountain | 8.31 | 76.43 | 12.29 | -0.04 | 140.63 | 11.72 |
| plains | 26.91 | 42.13 | 6.48 | 12.29 | 86.02 | 8.74 |
| polar_south | 9.16 | 25.53 | 5.73 | -18.09 | 16.02 | 3.22 |
| tundra | 15.73 | 30.99 | 6.02 | -14.25 | 40.27 | 5.67 |

| Subbioma | Celdas | % tierra | % mundo |
|---|---:|---:|---:|
| desert_rocky | 5720 | 34.24 | 13.70 |
| desert_sandy | 508 | 3.04 | 1.22 |
| forest_boreal | 60 | 0.36 | 0.14 |
| forest_subtropical | 174 | 1.04 | 0.42 |
| forest_temperate | 39 | 0.23 | 0.09 |
| forest_tropical | 162 | 0.97 | 0.39 |
| marine_open_ocean | 25044 | 0.00 | 59.99 |
| mountain_alpine | 779 | 4.66 | 1.87 |
| mountain_cliffs | 29 | 0.17 | 0.07 |
| mountain_rocky | 581 | 3.48 | 1.39 |
| plains_prairie | 77 | 0.46 | 0.18 |
| plains_savanna | 3179 | 19.03 | 7.61 |
| plains_steppe | 1239 | 7.42 | 2.97 |
| polar_south_ice | 1531 | 9.16 | 3.67 |
| tundra_tundra | 2628 | 15.73 | 6.29 |

| Latitud absoluta ° | Desert % | Forest % | Plains % | Mountain % | Tundra % | Polar N % | Polar S % |
|---|---:|---:|---:|---:|---:|---:|---:|
| 0–15 | 6.76 | 3.75 | 74.13 | 15.36 | 0.00 | 0.00 | 0.00 |
| 15–30 | 71.38 | 6.37 | 12.14 | 10.11 | 0.00 | 0.00 | 0.00 |
| 30–45 | 89.16 | 1.36 | 5.31 | 4.17 | 0.00 | 0.00 | 0.00 |
| 45–60 | 51.85 | 2.21 | 39.82 | 6.13 | 0.00 | 0.00 | 0.00 |
| 60–75 | 0.00 | 0.00 | 0.09 | 8.37 | 91.54 | 0.00 | 0.00 |
| 75–90 | 0.00 | 0.00 | 0.00 | 0.89 | 27.74 | 0.00 | 71.38 |

| FINAL niveles | Desert % | Forest % | Plains % | Mountain % | Tundra % | Polar N % | Polar S % |
|---|---:|---:|---:|---:|---:|---:|---:|
| 1–20 | 50.69 | 0.66 | 12.59 | 0.48 | 21.46 | 0.00 | 14.12 |
| 21–40 | 49.38 | 2.76 | 33.44 | 0.00 | 9.37 | 0.00 | 5.05 |
| 41–60 | 29.63 | 6.59 | 36.18 | 0.00 | 17.22 | 0.00 | 10.39 |
| 61–80 | 0.00 | 2.30 | 42.60 | 36.26 | 14.19 | 0.00 | 4.65 |
| 81–100 | 0.00 | 0.00 | 1.90 | 97.68 | 0.42 | 0.00 | 0.00 |

| Observación de prioridad | Celdas | Ejemplo (celda, subbioma, FINAL, latitud, °C, lluvia, humedad) |
|---|---:|---|
| AridWithoutCatalogEligibleDesert | 6902 | 4851, tundra_tundra, 48, 69.40, -16.10, 51, 12 |
| ReliefPriorityOverEligibleVegetation | 1088 | 5103, mountain_rocky, 74, 68.31, -22.70, 127, 25 |
| TropicalOutsidePreferredClimate | 119 | 17510, forest_tropical, 41, 14.10, 17.60, 265, 23 |

### Medium / -174327693

41750 celdas; 16700 terrestres. Áridas: 15378 (92.08% tierra); lluvia<500: 96.60%. Lluvia terrestre min/P10/mediana/P90/max: 2/16/47/153/6215 mm/año. Sin clasificación: 0; océanos incorrectos: 0.

| Bioma terrestre | % tierra | FINAL medio | Pendiente media | °C media | Lluvia media mm/año | Humedad media |
|---|---:|---:|---:|---:|---:|---:|
| desert | 46.97 | 24.65 | 4.02 | 14.24 | 47.34 | 5.63 |
| forest | 2.12 | 41.83 | 3.59 | 11.77 | 1555.08 | 56.99 |
| mountain | 2.80 | 60.06 | 10.97 | 5.09 | 190.78 | 13.89 |
| plains | 42.48 | 42.05 | 4.29 | 16.30 | 99.89 | 8.92 |
| polar_north | 1.21 | 22.25 | 7.86 | -15.54 | 73.32 | 12.07 |
| tundra | 4.43 | 35.31 | 4.56 | -13.08 | 152.58 | 18.74 |

| Subbioma | Celdas | % tierra | % mundo |
|---|---:|---:|---:|
| desert_rocky | 6812 | 40.79 | 16.32 |
| desert_sandy | 1032 | 6.18 | 2.47 |
| forest_boreal | 53 | 0.32 | 0.13 |
| forest_subtropical | 141 | 0.84 | 0.34 |
| forest_temperate | 41 | 0.25 | 0.10 |
| forest_tropical | 119 | 0.71 | 0.29 |
| marine_open_ocean | 25050 | 0.00 | 60.00 |
| mountain_alpine | 217 | 1.30 | 0.52 |
| mountain_cliffs | 107 | 0.64 | 0.26 |
| mountain_rocky | 143 | 0.86 | 0.34 |
| plains_prairie | 98 | 0.59 | 0.23 |
| plains_savanna | 5553 | 33.25 | 13.30 |
| plains_steppe | 1443 | 8.64 | 3.46 |
| polar_north_ice | 202 | 1.21 | 0.48 |
| tundra_tundra | 739 | 4.43 | 1.77 |

| Latitud absoluta ° | Desert % | Forest % | Plains % | Mountain % | Tundra % | Polar N % | Polar S % |
|---|---:|---:|---:|---:|---:|---:|---:|
| 0–15 | 9.00 | 1.84 | 86.26 | 2.90 | 0.00 | 0.00 | 0.00 |
| 15–30 | 75.48 | 3.26 | 18.98 | 2.29 | 0.00 | 0.00 | 0.00 |
| 30–45 | 77.30 | 1.22 | 19.30 | 2.18 | 0.00 | 0.00 | 0.00 |
| 45–60 | 60.92 | 1.93 | 32.36 | 4.79 | 0.00 | 0.00 | 0.00 |
| 60–75 | 0.00 | 2.48 | 4.14 | 4.41 | 88.97 | 0.00 | 0.00 |
| 75–90 | 0.00 | 0.00 | 0.00 | 0.00 | 31.76 | 68.24 | 0.00 |

| FINAL niveles | Desert % | Forest % | Plains % | Mountain % | Tundra % | Polar N % | Polar S % |
|---|---:|---:|---:|---:|---:|---:|---:|
| 1–20 | 65.46 | 1.78 | 25.78 | 1.70 | 3.37 | 1.91 | 0.00 |
| 21–40 | 48.65 | 0.75 | 44.84 | 0.00 | 4.63 | 1.12 | 0.00 |
| 41–60 | 47.91 | 3.19 | 40.55 | 0.00 | 7.37 | 0.98 | 0.00 |
| 61–80 | 0.00 | 4.13 | 81.34 | 11.26 | 3.26 | 0.00 | 0.00 |
| 81–100 | 0.00 | 0.00 | 4.55 | 95.45 | 0.00 | 0.00 | 0.00 |

| Observación de prioridad | Celdas | Ejemplo (celda, subbioma, FINAL, latitud, °C, lluvia, humedad) |
|---|---:|---|
| AridWithoutCatalogEligibleDesert | 4641 | 92, polar_north_ice, 10, 90.00, -11.30, 3, 1 |
| ReliefPriorityOverEligibleVegetation | 428 | 4856, mountain_rocky, 68, 69.40, -23.10, 61, 14 |
| TropicalOutsidePreferredClimate | 83 | 18197, forest_tropical, 6, 11.93, 28.10, 1645, 59 |

### Large / 2026

60000 celdas; 27543 terrestres. Áridas: 25578 (92.87% tierra); lluvia<500: 97.65%. Lluvia terrestre min/P10/mediana/P90/max: 2/12/38/126/7885 mm/año. Sin clasificación: 0; océanos incorrectos: 0.

| Bioma terrestre | % tierra | FINAL medio | Pendiente media | °C media | Lluvia media mm/año | Humedad media |
|---|---:|---:|---:|---:|---:|---:|
| desert | 49.50 | 23.28 | 3.04 | 11.21 | 43.36 | 5.79 |
| forest | 1.18 | 28.14 | 2.91 | 12.27 | 1854.98 | 61.69 |
| mountain | 3.60 | 62.81 | 7.21 | 0.19 | 72.18 | 8.84 |
| plains | 32.83 | 39.58 | 4.28 | 14.26 | 107.84 | 9.79 |
| polar_south | 2.58 | 66.97 | 3.49 | -28.49 | 32.74 | 5.25 |
| tundra | 10.31 | 38.13 | 3.91 | -14.25 | 79.75 | 10.37 |

| Subbioma | Celdas | % tierra | % mundo |
|---|---:|---:|---:|
| desert_rocky | 11250 | 40.85 | 18.75 |
| desert_sandy | 2383 | 8.65 | 3.97 |
| forest_boreal | 80 | 0.29 | 0.13 |
| forest_subtropical | 117 | 0.42 | 0.20 |
| forest_temperate | 41 | 0.15 | 0.07 |
| forest_tropical | 87 | 0.32 | 0.15 |
| marine_open_ocean | 32457 | 0.00 | 54.10 |
| mountain_alpine | 510 | 1.85 | 0.85 |
| mountain_cliffs | 230 | 0.84 | 0.38 |
| mountain_rocky | 252 | 0.91 | 0.42 |
| plains_prairie | 140 | 0.51 | 0.23 |
| plains_savanna | 6275 | 22.78 | 10.46 |
| plains_steppe | 2627 | 9.54 | 4.38 |
| polar_south_ice | 710 | 2.58 | 1.18 |
| tundra_tundra | 2841 | 10.31 | 4.74 |

| Latitud absoluta ° | Desert % | Forest % | Plains % | Mountain % | Tundra % | Polar N % | Polar S % |
|---|---:|---:|---:|---:|---:|---:|---:|
| 0–15 | 8.01 | 0.87 | 87.20 | 3.93 | 0.00 | 0.00 | 0.00 |
| 15–30 | 77.51 | 2.44 | 17.26 | 2.79 | 0.00 | 0.00 | 0.00 |
| 30–45 | 78.28 | 0.64 | 17.64 | 3.43 | 0.00 | 0.00 | 0.00 |
| 45–60 | 67.69 | 1.60 | 25.36 | 5.35 | 0.00 | 0.00 | 0.00 |
| 60–75 | 0.00 | 0.22 | 3.67 | 1.98 | 94.13 | 0.00 | 0.00 |
| 75–90 | 0.00 | 0.00 | 0.00 | 3.91 | 26.69 | 0.00 | 69.40 |

| FINAL niveles | Desert % | Forest % | Plains % | Mountain % | Tundra % | Polar N % | Polar S % |
|---|---:|---:|---:|---:|---:|---:|---:|
| 1–20 | 68.10 | 1.59 | 22.54 | 2.09 | 5.69 | 0.00 | 0.00 |
| 21–40 | 53.40 | 0.67 | 32.98 | 0.00 | 12.94 | 0.00 | 0.00 |
| 41–60 | 43.07 | 1.25 | 35.03 | 0.00 | 16.75 | 0.00 | 3.90 |
| 61–80 | 0.00 | 0.97 | 62.57 | 12.50 | 10.72 | 0.00 | 13.25 |
| 81–100 | 0.00 | 0.00 | 17.99 | 75.06 | 1.20 | 0.00 | 5.76 |

| Observación de prioridad | Celdas | Ejemplo (celda, subbioma, FINAL, latitud, °C, lluvia, humedad) |
|---|---:|---|
| AridWithoutCatalogEligibleDesert | 7307 | 5667, tundra_tundra, 67, 73.72, -23.80, 97, 20 |
| ReliefPriorityOverEligibleVegetation | 778 | 6273, mountain_alpine, 76, 71.91, -25.00, 1102, 74 |
| TropicalOutsidePreferredClimate | 17 | 25657, forest_tropical, 42, 13.12, 17.20, 496, 37 |

### Large / -1387338765

60000 celdas; 24022 terrestres. Áridas: 22122 (92.09% tierra); lluvia<500: 96.65%. Lluvia terrestre min/P10/mediana/P90/max: 1/9/34/140/8574 mm/año. Sin clasificación: 0; océanos incorrectos: 0.

| Bioma terrestre | % tierra | FINAL medio | Pendiente media | °C media | Lluvia media mm/año | Humedad media |
|---|---:|---:|---:|---:|---:|---:|
| desert | 39.09 | 26.10 | 3.13 | 10.78 | 47.46 | 6.31 |
| forest | 1.82 | 31.00 | 9.01 | 10.57 | 2027.40 | 67.19 |
| mountain | 7.35 | 67.98 | 11.68 | -3.84 | 72.02 | 9.75 |
| plains | 26.18 | 40.15 | 4.34 | 11.93 | 102.48 | 9.93 |
| polar_north | 7.71 | 34.36 | 5.15 | -19.30 | 34.11 | 5.66 |
| polar_south | 1.22 | 47.43 | 5.51 | -21.77 | 75.14 | 10.96 |
| tundra | 16.64 | 28.50 | 4.90 | -12.86 | 63.99 | 8.74 |

| Subbioma | Celdas | % tierra | % mundo |
|---|---:|---:|---:|
| desert_rocky | 7799 | 32.47 | 13.00 |
| desert_sandy | 1590 | 6.62 | 2.65 |
| forest_boreal | 129 | 0.54 | 0.22 |
| forest_subtropical | 122 | 0.51 | 0.20 |
| forest_temperate | 66 | 0.27 | 0.11 |
| forest_tropical | 121 | 0.50 | 0.20 |
| marine_open_ocean | 35978 | 0.00 | 59.96 |
| mountain_alpine | 1020 | 4.25 | 1.70 |
| mountain_cliffs | 306 | 1.27 | 0.51 |
| mountain_rocky | 439 | 1.83 | 0.73 |
| plains_prairie | 121 | 0.50 | 0.20 |
| plains_savanna | 4237 | 17.64 | 7.06 |
| plains_steppe | 1930 | 8.03 | 3.22 |
| polar_north_ice | 1851 | 7.71 | 3.09 |
| polar_south_ice | 293 | 1.22 | 0.49 |
| tundra_tundra | 3998 | 16.64 | 6.66 |

| Latitud absoluta ° | Desert % | Forest % | Plains % | Mountain % | Tundra % | Polar N % | Polar S % |
|---|---:|---:|---:|---:|---:|---:|---:|
| 0–15 | 15.66 | 2.04 | 78.01 | 4.29 | 0.00 | 0.00 | 0.00 |
| 15–30 | 69.31 | 3.54 | 17.38 | 9.77 | 0.00 | 0.00 | 0.00 |
| 30–45 | 78.92 | 1.25 | 10.27 | 9.57 | 0.00 | 0.00 | 0.00 |
| 45–60 | 54.02 | 3.08 | 32.73 | 10.17 | 0.00 | 0.00 | 0.00 |
| 60–75 | 0.00 | 0.26 | 4.17 | 7.83 | 87.74 | 0.00 | 0.00 |
| 75–90 | 0.00 | 0.00 | 0.00 | 1.22 | 29.82 | 59.54 | 9.42 |

| FINAL niveles | Desert % | Forest % | Plains % | Mountain % | Tundra % | Polar N % | Polar S % |
|---|---:|---:|---:|---:|---:|---:|---:|
| 1–20 | 51.45 | 1.87 | 23.70 | 3.46 | 15.09 | 4.43 | 0.00 |
| 21–40 | 42.45 | 1.87 | 13.41 | 0.00 | 26.77 | 13.39 | 2.10 |
| 41–60 | 42.27 | 2.80 | 27.00 | 0.00 | 16.90 | 9.03 | 2.00 |
| 61–80 | 0.00 | 0.47 | 66.13 | 24.93 | 2.87 | 4.76 | 0.84 |
| 81–100 | 0.00 | 0.00 | 5.16 | 90.68 | 0.00 | 0.00 | 4.16 |

| Observación de prioridad | Celdas | Ejemplo (celda, subbioma, FINAL, latitud, °C, lluvia, humedad) |
|---|---:|---|
| AridWithoutCatalogEligibleDesert | 9113 | 0, polar_north_ice, 53, 90.00, -24.80, 2, 1 |
| ReliefPriorityOverEligibleVegetation | 1306 | 5379, mountain_rocky, 72, 74.62, -24.70, 376, 50 |
| TropicalOutsidePreferredClimate | 39 | 24957, forest_tropical, 39, 14.92, 17.80, 581, 40 |

### Large / -2024691550

60000 celdas; 24000 terrestres. Áridas: 22791 (94.96% tierra); lluvia<500: 98.12%. Lluvia terrestre min/P10/mediana/P90/max: 2/8/27/91/9608 mm/año. Sin clasificación: 0; océanos incorrectos: 0.

| Bioma terrestre | % tierra | FINAL medio | Pendiente media | °C media | Lluvia media mm/año | Humedad media |
|---|---:|---:|---:|---:|---:|---:|
| desert | 42.72 | 22.60 | 3.92 | 12.32 | 42.31 | 5.61 |
| forest | 1.41 | 41.91 | 4.83 | 10.11 | 1804.07 | 60.99 |
| mountain | 7.74 | 67.89 | 11.13 | -10.57 | 53.59 | 6.52 |
| plains | 20.28 | 37.48 | 5.16 | 9.63 | 91.97 | 9.58 |
| polar_north | 6.01 | 40.74 | 4.60 | -20.86 | 23.44 | 4.14 |
| polar_south | 4.74 | 26.67 | 2.31 | -18.14 | 23.10 | 4.34 |
| tundra | 17.10 | 35.44 | 4.40 | -14.77 | 42.24 | 6.26 |

| Subbioma | Celdas | % tierra | % mundo |
|---|---:|---:|---:|
| desert_rocky | 8142 | 33.93 | 13.57 |
| desert_sandy | 2110 | 8.79 | 3.52 |
| forest_boreal | 69 | 0.29 | 0.12 |
| forest_subtropical | 97 | 0.40 | 0.16 |
| forest_temperate | 54 | 0.23 | 0.09 |
| forest_tropical | 119 | 0.50 | 0.20 |
| marine_open_ocean | 36000 | 0.00 | 60.00 |
| mountain_alpine | 890 | 3.71 | 1.48 |
| mountain_cliffs | 329 | 1.37 | 0.55 |
| mountain_rocky | 638 | 2.66 | 1.06 |
| plains_prairie | 100 | 0.42 | 0.17 |
| plains_savanna | 2643 | 11.01 | 4.41 |
| plains_steppe | 2124 | 8.85 | 3.54 |
| polar_north_ice | 1443 | 6.01 | 2.41 |
| polar_south_ice | 1138 | 4.74 | 1.90 |
| tundra_tundra | 4104 | 17.10 | 6.84 |

| Latitud absoluta ° | Desert % | Forest % | Plains % | Mountain % | Tundra % | Polar N % | Polar S % |
|---|---:|---:|---:|---:|---:|---:|---:|
| 0–15 | 16.22 | 3.43 | 75.08 | 5.27 | 0.00 | 0.00 | 0.00 |
| 15–30 | 88.12 | 2.06 | 2.30 | 7.51 | 0.00 | 0.00 | 0.00 |
| 30–45 | 88.94 | 1.44 | 5.39 | 4.23 | 0.00 | 0.00 | 0.00 |
| 45–60 | 45.98 | 1.54 | 43.79 | 8.69 | 0.00 | 0.00 | 0.00 |
| 60–75 | 0.00 | 0.08 | 1.01 | 18.78 | 80.14 | 0.00 | 0.00 |
| 75–90 | 0.00 | 0.00 | 0.00 | 1.89 | 27.34 | 39.57 | 31.20 |

| FINAL niveles | Desert % | Forest % | Plains % | Mountain % | Tundra % | Polar N % | Polar S % |
|---|---:|---:|---:|---:|---:|---:|---:|
| 1–20 | 58.59 | 0.73 | 11.37 | 3.45 | 15.29 | 2.92 | 7.65 |
| 21–40 | 49.94 | 0.95 | 25.84 | 0.00 | 13.99 | 7.19 | 2.09 |
| 41–60 | 29.63 | 3.34 | 31.06 | 0.00 | 24.41 | 9.85 | 1.71 |
| 61–80 | 0.00 | 1.63 | 24.89 | 34.79 | 23.21 | 6.39 | 9.08 |
| 81–100 | 0.00 | 0.00 | 2.45 | 89.43 | 0.78 | 7.34 | 0.00 |

| Observación de prioridad | Celdas | Ejemplo (celda, subbioma, FINAL, latitud, °C, lluvia, humedad) |
|---|---:|---|
| AridWithoutCatalogEligibleDesert | 9296 | 54, polar_north_ice, 51, 90.00, -21.20, 2, 1 |
| ReliefPriorityOverEligibleVegetation | 1202 | 5169, mountain_alpine, 78, 74.62, -27.80, 6, 1 |
| TropicalOutsidePreferredClimate | 103 | 25107, forest_tropical, 43, 14.92, 17.30, 293, 25 |

### Large / -174327693

60000 celdas; 24881 terrestres. Áridas: 23545 (94.63% tierra); lluvia<500: 98.06%. Lluvia terrestre min/P10/mediana/P90/max: 2/8/28/86/6803 mm/año. Sin clasificación: 0; océanos incorrectos: 0.

| Bioma terrestre | % tierra | FINAL medio | Pendiente media | °C media | Lluvia media mm/año | Humedad media |
|---|---:|---:|---:|---:|---:|---:|
| desert | 37.36 | 26.88 | 3.99 | 9.35 | 39.11 | 5.73 |
| forest | 2.53 | 28.15 | 2.17 | 8.54 | 899.77 | 47.01 |
| mountain | 6.65 | 46.24 | 7.78 | 0.46 | 45.16 | 7.35 |
| plains | 21.63 | 54.75 | 4.54 | 10.90 | 95.39 | 8.89 |
| polar_north | 1.62 | 40.76 | 3.52 | -20.69 | 45.31 | 6.95 |
| polar_south | 10.28 | 22.03 | 2.92 | -17.81 | 15.49 | 3.27 |
| tundra | 19.91 | 22.15 | 3.50 | -12.37 | 43.03 | 6.18 |

| Subbioma | Celdas | % tierra | % mundo |
|---|---:|---:|---:|
| desert_rocky | 7908 | 31.78 | 13.18 |
| desert_sandy | 1388 | 5.58 | 2.31 |
| forest_boreal | 319 | 1.28 | 0.53 |
| forest_subtropical | 120 | 0.48 | 0.20 |
| forest_temperate | 53 | 0.21 | 0.09 |
| forest_tropical | 138 | 0.55 | 0.23 |
| marine_open_ocean | 35119 | 0.00 | 58.53 |
| mountain_alpine | 617 | 2.48 | 1.03 |
| mountain_cliffs | 711 | 2.86 | 1.19 |
| mountain_rocky | 327 | 1.31 | 0.55 |
| plains_prairie | 93 | 0.37 | 0.16 |
| plains_savanna | 3918 | 15.75 | 6.53 |
| plains_steppe | 1372 | 5.51 | 2.29 |
| polar_north_ice | 404 | 1.62 | 0.67 |
| polar_south_ice | 2559 | 10.28 | 4.27 |
| tundra_tundra | 4954 | 19.91 | 8.26 |

| Latitud absoluta ° | Desert % | Forest % | Plains % | Mountain % | Tundra % | Polar N % | Polar S % |
|---|---:|---:|---:|---:|---:|---:|---:|
| 0–15 | 8.45 | 3.73 | 81.96 | 5.86 | 0.00 | 0.00 | 0.00 |
| 15–30 | 60.79 | 2.86 | 27.17 | 9.17 | 0.00 | 0.00 | 0.00 |
| 30–45 | 84.53 | 1.11 | 9.45 | 4.91 | 0.00 | 0.00 | 0.00 |
| 45–60 | 61.74 | 7.33 | 17.97 | 12.97 | 0.00 | 0.00 | 0.00 |
| 60–75 | 0.00 | 0.49 | 0.58 | 6.88 | 92.05 | 0.00 | 0.00 |
| 75–90 | 0.00 | 0.00 | 0.00 | 0.24 | 28.22 | 9.75 | 61.78 |

| FINAL niveles | Desert % | Forest % | Plains % | Mountain % | Tundra % | Polar N % | Polar S % |
|---|---:|---:|---:|---:|---:|---:|---:|
| 1–20 | 38.66 | 3.36 | 3.07 | 7.22 | 30.64 | 0.07 | 16.98 |
| 21–40 | 55.00 | 0.42 | 17.13 | 0.00 | 17.68 | 2.94 | 6.84 |
| 41–60 | 41.70 | 3.90 | 26.09 | 0.00 | 16.24 | 4.20 | 7.87 |
| 61–80 | 0.00 | 2.21 | 75.95 | 18.84 | 0.88 | 0.14 | 1.98 |
| 81–100 | 0.00 | 0.00 | 10.32 | 89.68 | 0.00 | 0.00 | 0.00 |

| Observación de prioridad | Celdas | Ejemplo (celda, subbioma, FINAL, latitud, °C, lluvia, humedad) |
|---|---:|---|
| AridWithoutCatalogEligibleDesert | 10161 | 112, polar_north_ice, 46, 90.00, -20.60, 2, 1 |
| TropicalOutsidePreferredClimate | 138 | 25066, forest_tropical, 52, 14.92, 16.00, 357, 30 |
| ReliefPriorityOverEligibleVegetation | 1454 | 27150, mountain_alpine, 76, 8.59, 12.00, 47, 6 |

### Enormous / 2026

106800 celdas; 42720 terrestres. Áridas: 40058 (93.77% tierra); lluvia<500: 97.68%. Lluvia terrestre min/P10/mediana/P90/max: 2/8/20/111/6807 mm/año. Sin clasificación: 0; océanos incorrectos: 0.

| Bioma terrestre | % tierra | FINAL medio | Pendiente media | °C media | Lluvia media mm/año | Humedad media |
|---|---:|---:|---:|---:|---:|---:|
| desert | 33.74 | 21.02 | 1.86 | 9.52 | 49.24 | 6.76 |
| forest | 1.50 | 17.85 | 1.66 | 11.79 | 1738.36 | 60.21 |
| mountain | 4.45 | 48.28 | 7.80 | -14.27 | 30.40 | 4.67 |
| plains | 20.31 | 39.62 | 2.80 | 6.91 | 116.12 | 10.87 |
| polar_north | 8.62 | 44.51 | 3.01 | -23.30 | 15.99 | 3.26 |
| polar_south | 5.17 | 45.84 | 2.55 | -23.54 | 21.96 | 3.94 |
| tundra | 26.21 | 35.10 | 2.75 | -15.78 | 25.56 | 4.47 |

| Subbioma | Celdas | % tierra | % mundo |
|---|---:|---:|---:|
| desert_rocky | 11769 | 27.55 | 11.02 |
| desert_sandy | 2646 | 6.19 | 2.48 |
| forest_boreal | 174 | 0.41 | 0.16 |
| forest_subtropical | 82 | 0.19 | 0.08 |
| forest_temperate | 248 | 0.58 | 0.23 |
| forest_tropical | 137 | 0.32 | 0.13 |
| marine_open_ocean | 64080 | 0.00 | 60.00 |
| mountain_alpine | 478 | 1.12 | 0.45 |
| mountain_cliffs | 882 | 2.06 | 0.83 |
| mountain_rocky | 539 | 1.26 | 0.50 |
| plains_prairie | 167 | 0.39 | 0.16 |
| plains_savanna | 4168 | 9.76 | 3.90 |
| plains_steppe | 4341 | 10.16 | 4.06 |
| polar_north_ice | 3684 | 8.62 | 3.45 |
| polar_south_ice | 2210 | 5.17 | 2.07 |
| tundra_tundra | 11195 | 26.21 | 10.48 |

| Latitud absoluta ° | Desert % | Forest % | Plains % | Mountain % | Tundra % | Polar N % | Polar S % |
|---|---:|---:|---:|---:|---:|---:|---:|
| 0–15 | 13.75 | 2.14 | 84.11 | 0.00 | 0.00 | 0.00 | 0.00 |
| 15–30 | 83.49 | 2.16 | 14.35 | 0.00 | 0.00 | 0.00 | 0.00 |
| 30–45 | 85.43 | 4.18 | 10.03 | 0.36 | 0.00 | 0.00 | 0.00 |
| 45–60 | 48.50 | 1.84 | 39.73 | 9.93 | 0.00 | 0.00 | 0.00 |
| 60–75 | 0.00 | 0.00 | 0.52 | 8.84 | 90.64 | 0.00 | 0.00 |
| 75–90 | 0.00 | 0.00 | 0.00 | 1.55 | 33.40 | 40.66 | 24.39 |

| FINAL niveles | Desert % | Forest % | Plains % | Mountain % | Tundra % | Polar N % | Polar S % |
|---|---:|---:|---:|---:|---:|---:|---:|
| 1–20 | 49.88 | 2.91 | 14.26 | 5.16 | 22.27 | 5.31 | 0.21 |
| 21–40 | 39.74 | 0.44 | 17.36 | 0.00 | 27.12 | 6.31 | 9.04 |
| 41–60 | 18.81 | 0.66 | 30.16 | 0.00 | 31.12 | 10.52 | 8.73 |
| 61–80 | 0.00 | 0.72 | 31.94 | 6.59 | 32.17 | 20.27 | 8.32 |
| 81–100 | 0.00 | 0.00 | 3.65 | 78.80 | 5.54 | 12.01 | 0.00 |

| Observación de prioridad | Celdas | Ejemplo (celda, subbioma, FINAL, latitud, °C, lluvia, humedad) |
|---|---:|---|
| AridWithoutCatalogEligibleDesert | 20360 | 0, polar_north_ice, 78, 90.00, -32.30, 3, 1 |
| ReliefPriorityOverEligibleVegetation | 1238 | 9537, mountain_alpine, 76, 74.44, -29.10, 7, 2 |
| WarmWetNoTropicalCatalogEligibility | 1 | 38796, plains_savanna, 4, 25.04, 22.50, 2181, 68 |
| TropicalOutsidePreferredClimate | 6 | 39193, forest_tropical, 4, 24.36, 22.90, 1718, 63 |

### Enormous / -1387338765

106800 celdas; 42722 terrestres. Áridas: 40516 (94.84% tierra); lluvia<500: 98.03%. Lluvia terrestre min/P10/mediana/P90/max: 1/8/26/93/8842 mm/año. Sin clasificación: 0; océanos incorrectos: 0.

| Bioma terrestre | % tierra | FINAL medio | Pendiente media | °C media | Lluvia media mm/año | Humedad media |
|---|---:|---:|---:|---:|---:|---:|
| desert | 39.18 | 23.78 | 2.69 | 11.13 | 43.99 | 5.93 |
| forest | 1.18 | 36.13 | 3.79 | 12.94 | 1531.30 | 56.61 |
| mountain | 3.81 | 64.53 | 8.04 | 1.68 | 220.80 | 13.77 |
| plains | 21.41 | 44.95 | 2.33 | 10.86 | 105.51 | 9.82 |
| polar_north | 8.51 | 29.07 | 2.55 | -18.76 | 13.40 | 2.84 |
| polar_south | 6.60 | 47.74 | 2.98 | -23.02 | 18.51 | 3.59 |
| tundra | 19.30 | 27.15 | 2.01 | -13.45 | 31.22 | 5.43 |

| Subbioma | Celdas | % tierra | % mundo |
|---|---:|---:|---:|
| desert_rocky | 13924 | 32.59 | 13.04 |
| desert_sandy | 2816 | 6.59 | 2.64 |
| forest_boreal | 74 | 0.17 | 0.07 |
| forest_subtropical | 185 | 0.43 | 0.17 |
| forest_temperate | 72 | 0.17 | 0.07 |
| forest_tropical | 171 | 0.40 | 0.16 |
| marine_open_ocean | 64078 | 0.00 | 60.00 |
| mountain_alpine | 927 | 2.17 | 0.87 |
| mountain_cliffs | 328 | 0.77 | 0.31 |
| mountain_rocky | 373 | 0.87 | 0.35 |
| plains_prairie | 110 | 0.26 | 0.10 |
| plains_savanna | 5589 | 13.08 | 5.23 |
| plains_steppe | 3448 | 8.07 | 3.23 |
| polar_north_ice | 3637 | 8.51 | 3.41 |
| polar_south_ice | 2821 | 6.60 | 2.64 |
| tundra_tundra | 8247 | 19.30 | 7.72 |

| Latitud absoluta ° | Desert % | Forest % | Plains % | Mountain % | Tundra % | Polar N % | Polar S % |
|---|---:|---:|---:|---:|---:|---:|---:|
| 0–15 | 17.19 | 2.62 | 73.66 | 6.53 | 0.00 | 0.00 | 0.00 |
| 15–30 | 71.16 | 2.22 | 20.87 | 5.75 | 0.00 | 0.00 | 0.00 |
| 30–45 | 79.18 | 1.05 | 14.74 | 5.03 | 0.00 | 0.00 | 0.00 |
| 45–60 | 67.39 | 1.11 | 27.77 | 3.73 | 0.00 | 0.00 | 0.00 |
| 60–75 | 0.00 | 0.17 | 1.07 | 2.38 | 96.38 | 0.00 | 0.00 |
| 75–90 | 0.00 | 0.00 | 0.00 | 0.13 | 29.13 | 39.84 | 30.90 |

| FINAL niveles | Desert % | Forest % | Plains % | Mountain % | Tundra % | Polar N % | Polar S % |
|---|---:|---:|---:|---:|---:|---:|---:|
| 1–20 | 52.13 | 0.93 | 13.17 | 1.96 | 22.20 | 9.50 | 0.11 |
| 21–40 | 44.29 | 1.31 | 14.89 | 0.00 | 24.35 | 7.90 | 7.25 |
| 41–60 | 31.02 | 1.60 | 21.56 | 0.00 | 16.82 | 11.54 | 17.46 |
| 61–80 | 0.00 | 1.05 | 68.09 | 17.99 | 3.22 | 1.58 | 8.06 |
| 81–100 | 0.00 | 0.00 | 21.60 | 78.40 | 0.00 | 0.00 | 0.00 |

| Observación de prioridad | Celdas | Ejemplo (celda, subbioma, FINAL, latitud, °C, lluvia, humedad) |
|---|---:|---|
| AridWithoutCatalogEligibleDesert | 18042 | 0, polar_north_ice, 10, 90.00, -14.50, 2, 1 |
| ReliefPriorityOverEligibleVegetation | 1369 | 10393, mountain_rocky, 65, 73.08, -23.60, 12, 3 |
| TropicalOutsidePreferredClimate | 122 | 44449, forest_tropical, 18, 14.89, 22.50, 1696, 63 |

### Enormous / -2024691550

106800 celdas; 43749 terrestres. Áridas: 41847 (95.65% tierra); lluvia<500: 98.47%. Lluvia terrestre min/P10/mediana/P90/max: 2/9/33/84/9045 mm/año. Sin clasificación: 0; océanos incorrectos: 0.

| Bioma terrestre | % tierra | FINAL medio | Pendiente media | °C media | Lluvia media mm/año | Humedad media |
|---|---:|---:|---:|---:|---:|---:|
| desert | 43.19 | 23.20 | 2.47 | 14.74 | 49.15 | 6.00 |
| forest | 1.15 | 26.59 | 1.70 | 8.76 | 1655.26 | 59.81 |
| mountain | 3.39 | 65.16 | 11.55 | -4.23 | 49.35 | 6.56 |
| plains | 24.84 | 42.80 | 3.64 | 8.81 | 68.20 | 6.98 |
| polar_north | 6.82 | 28.50 | 1.62 | -17.83 | 21.80 | 4.06 |
| polar_south | 3.07 | 16.89 | 1.39 | -15.40 | 32.70 | 5.81 |
| tundra | 17.56 | 36.83 | 2.20 | -15.60 | 45.46 | 6.85 |

| Subbioma | Celdas | % tierra | % mundo |
|---|---:|---:|---:|
| desert_rocky | 14138 | 32.32 | 13.24 |
| desert_sandy | 4756 | 10.87 | 4.45 |
| forest_boreal | 219 | 0.50 | 0.21 |
| forest_subtropical | 168 | 0.38 | 0.16 |
| forest_temperate | 63 | 0.14 | 0.06 |
| forest_tropical | 51 | 0.12 | 0.05 |
| marine_open_ocean | 63051 | 0.00 | 59.04 |
| mountain_alpine | 782 | 1.79 | 0.73 |
| mountain_cliffs | 279 | 0.64 | 0.26 |
| mountain_rocky | 420 | 0.96 | 0.39 |
| plains_prairie | 118 | 0.27 | 0.11 |
| plains_savanna | 5817 | 13.30 | 5.45 |
| plains_steppe | 4934 | 11.28 | 4.62 |
| polar_north_ice | 2982 | 6.82 | 2.79 |
| polar_south_ice | 1341 | 3.07 | 1.26 |
| tundra_tundra | 7681 | 17.56 | 7.19 |

| Latitud absoluta ° | Desert % | Forest % | Plains % | Mountain % | Tundra % | Polar N % | Polar S % |
|---|---:|---:|---:|---:|---:|---:|---:|
| 0–15 | 26.99 | 0.63 | 67.73 | 4.65 | 0.00 | 0.00 | 0.00 |
| 15–30 | 91.53 | 1.81 | 4.73 | 1.93 | 0.00 | 0.00 | 0.00 |
| 30–45 | 82.90 | 0.87 | 14.19 | 2.04 | 0.00 | 0.00 | 0.00 |
| 45–60 | 33.26 | 2.31 | 56.41 | 8.02 | 0.00 | 0.00 | 0.00 |
| 60–75 | 0.00 | 1.03 | 1.25 | 3.90 | 93.82 | 0.00 | 0.00 |
| 75–90 | 0.00 | 0.00 | 0.00 | 0.00 | 32.99 | 46.23 | 20.79 |

| FINAL niveles | Desert % | Forest % | Plains % | Mountain % | Tundra % | Polar N % | Polar S % |
|---|---:|---:|---:|---:|---:|---:|---:|
| 1–20 | 53.00 | 1.08 | 17.26 | 1.58 | 13.92 | 7.56 | 5.58 |
| 21–40 | 56.35 | 1.84 | 19.22 | 0.00 | 15.10 | 5.05 | 2.43 |
| 41–60 | 35.23 | 1.02 | 23.31 | 0.00 | 26.28 | 13.41 | 0.76 |
| 61–80 | 0.00 | 0.13 | 61.88 | 12.95 | 24.02 | 1.02 | 0.00 |
| 81–100 | 0.00 | 0.00 | 0.71 | 97.88 | 1.42 | 0.00 | 0.00 |

| Observación de prioridad | Celdas | Ejemplo (celda, subbioma, FINAL, latitud, °C, lluvia, humedad) |
|---|---:|---|
| AridWithoutCatalogEligibleDesert | 16632 | 0, polar_north_ice, 29, 90.00, -19.00, 2, 1 |
| ReliefPriorityOverEligibleVegetation | 1216 | 10855, mountain_alpine, 75, 71.73, -26.50, 15, 4 |
| TropicalOutsidePreferredClimate | 21 | 44599, forest_tropical, 35, 14.89, 19.00, 4543, 83 |

### Enormous / -174327693

106800 celdas; 42721 terrestres. Áridas: 41017 (96.01% tierra); lluvia<500: 98.08%. Lluvia terrestre min/P10/mediana/P90/max: 1/8/23/78/7855 mm/año. Sin clasificación: 0; océanos incorrectos: 0.

| Bioma terrestre | % tierra | FINAL medio | Pendiente media | °C media | Lluvia media mm/año | Humedad media |
|---|---:|---:|---:|---:|---:|---:|
| desert | 31.24 | 25.30 | 2.90 | 13.24 | 46.22 | 5.70 |
| forest | 1.05 | 51.67 | 9.37 | 7.74 | 1966.70 | 62.99 |
| mountain | 6.17 | 54.52 | 10.13 | -1.58 | 201.88 | 16.38 |
| plains | 21.66 | 45.72 | 3.47 | 9.80 | 70.77 | 6.97 |
| polar_north | 5.23 | 36.17 | 2.67 | -20.19 | 18.88 | 3.50 |
| polar_south | 9.09 | 23.27 | 2.44 | -18.10 | 14.29 | 3.04 |
| tundra | 25.55 | 30.21 | 3.76 | -14.62 | 27.82 | 4.72 |

| Subbioma | Celdas | % tierra | % mundo |
|---|---:|---:|---:|
| desert_rocky | 9710 | 22.73 | 9.09 |
| desert_sandy | 3635 | 8.51 | 3.40 |
| forest_boreal | 90 | 0.21 | 0.08 |
| forest_subtropical | 167 | 0.39 | 0.16 |
| forest_temperate | 83 | 0.19 | 0.08 |
| forest_tropical | 110 | 0.26 | 0.10 |
| marine_open_ocean | 64079 | 0.00 | 60.00 |
| mountain_alpine | 1245 | 2.91 | 1.17 |
| mountain_cliffs | 838 | 1.96 | 0.78 |
| mountain_rocky | 555 | 1.30 | 0.52 |
| plains_prairie | 102 | 0.24 | 0.10 |
| plains_savanna | 5770 | 13.51 | 5.40 |
| plains_steppe | 3382 | 7.92 | 3.17 |
| polar_north_ice | 2233 | 5.23 | 2.09 |
| polar_south_ice | 3884 | 9.09 | 3.64 |
| tundra_tundra | 10917 | 25.55 | 10.22 |

| Latitud absoluta ° | Desert % | Forest % | Plains % | Mountain % | Tundra % | Polar N % | Polar S % |
|---|---:|---:|---:|---:|---:|---:|---:|
| 0–15 | 18.13 | 1.56 | 73.28 | 7.02 | 0.00 | 0.00 | 0.00 |
| 15–30 | 79.87 | 2.71 | 11.64 | 5.79 | 0.00 | 0.00 | 0.00 |
| 30–45 | 84.60 | 1.28 | 5.35 | 8.77 | 0.00 | 0.00 | 0.00 |
| 45–60 | 36.38 | 1.38 | 49.56 | 12.68 | 0.00 | 0.00 | 0.00 |
| 60–75 | 0.00 | 0.07 | 0.33 | 5.97 | 93.63 | 0.00 | 0.00 |
| 75–90 | 0.00 | 0.00 | 0.00 | 0.30 | 34.25 | 23.89 | 41.56 |

| FINAL niveles | Desert % | Forest % | Plains % | Mountain % | Tundra % | Polar N % | Polar S % |
|---|---:|---:|---:|---:|---:|---:|---:|
| 1–20 | 39.33 | 0.17 | 8.17 | 5.35 | 31.17 | 1.12 | 14.69 |
| 21–40 | 39.63 | 0.86 | 17.46 | 0.00 | 24.22 | 11.49 | 6.34 |
| 41–60 | 26.25 | 1.65 | 35.02 | 0.00 | 22.02 | 7.03 | 8.02 |
| 61–80 | 0.00 | 3.14 | 47.44 | 27.27 | 20.75 | 0.73 | 0.67 |
| 81–100 | 0.00 | 0.00 | 5.14 | 94.86 | 0.00 | 0.00 | 0.00 |

| Observación de prioridad | Celdas | Ejemplo (celda, subbioma, FINAL, latitud, °C, lluvia, humedad) |
|---|---:|---|
| AridWithoutCatalogEligibleDesert | 21342 | 0, polar_north_ice, 21, 90.00, -16.80, 2, 1 |
| ReliefPriorityOverEligibleVegetation | 2348 | 9214, mountain_alpine, 77, 74.44, -28.50, 10, 3 |
| TropicalOutsidePreferredClimate | 103 | 44903, forest_tropical, 74, 14.21, 9.70, 2457, 79 |

## Archivos de implementación modificados

- `GEvolve.Engine/PANGAEA_ECOLOGY_INTEGRATION.md`
- `GEvolve.Engine/PANGAEA_ROADMAP.md`
- `GEvolve.Engine/PANGAEA_STAGED_GENERATION.md`
- `GEvolve.Engine/WORLD_GENERATION_ARCHITECTURE.md`
- `GEvolve.Engine/WORLD_GENERATION_ROADMAP.md`
- `GEvolve.Engine/World/PangaeaGenerationSession.cs`
- `GEvolve.Engine/World/PangaeaGenerationStage.cs`
- `GEvolve.Engine/World/WorldGenerationService.cs`
- `GEvolve.Game/Graphics/MapViewMode.cs`
- `GEvolve.Game/Graphics/PangaeaMapData.cs`
- `GEvolve.Game/PANGAEA_LABORATORY.md`
- `GEvolve.Game/PangaeaLaboratory.cs`
- `GEvolve.Game/Screens/PangaeaLaboratoryLayout.cs`
- `GEvolve.Game/Screens/ScreenLayout.cs`
- `GEvolve.Tests/BiomeTests.cs`
- `GEvolve.Tests/ContinentalGeologyTests.cs`
- `GEvolve.Tests/GameTests.cs`
- `GEvolve.Tests/PangaeaEcologyTests.cs`
- `GEvolve.Tests/PangaeaElevationTests.cs`
- `GEvolve.Tests/PangaeaLaboratoryTests.cs`
- `GEvolve.Tests/PangaeaMountainRangeTests.cs`
- `GEvolve.Tests/PangaeaStagedGenerationTests.cs`
- `GEvolve.Tests/PangaeaTectonicAuditTests.cs`
- `GEvolve.Tests/PangaeaUpliftTests.cs`
- `GEvolve.Tests/PrecipitationTests.cs`
- `GEvolve.Tests/Program.cs`
- `GEvolve.Tests/StagedLandmassTests.cs`
- `GEvolve.Tests/TectonicMotionTests.cs`
- `GEvolve.Tests/TemperatureTests.cs`
- `GEvolve.Tests/ValidationReports/commands.ps1`

El commit posterior de evidencia añade este directorio, los dos prechecks conservados y sus explicaciones, referencias en documentación/roadmaps/memoria y el README de reportes. El swap ajeno `.BiomeTests.cs.swp` se conserva sin versionar.

## Repetición y revisión del usuario

Desde la raíz:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\GEvolve.Tests\ValidationReports\commands.ps1 -ValidationSet PangaeaEcology -Label P09-1 -IsolatedArtifacts
```

Inspección visual exclusivamente manual del usuario: ejecutar Evolve normalmente, completar 01–08, revisar BIOMAS y capas de clima en los cuatro tamaños, seleccionar ejemplos de prioridades y repetir FINAL para comprobar la experiencia. Esta entrega no certifica apariencia, legibilidad real, fluidez ni distribución natural. La calibración de lluvia o reglas requiere autorización posterior y revisión individual; no se inicia aquí.

Tras terminar, se eliminaron únicamente los tres directorios propios de artefactos aislados P09.1, verificando rutas y ausencia de enlaces. Se revisaron los 20 archivos de reportes: sin coincidencias de rutas personales/credenciales en la búsqueda estática y sin enlaces Markdown relativos ausentes. No se eliminó ningún informe histórico ni el swap ajeno.
