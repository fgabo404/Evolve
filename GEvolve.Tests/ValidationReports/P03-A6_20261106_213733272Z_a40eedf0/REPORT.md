# P03-A.6 — Calibración tectónica de la elevación de cordilleras

**Estado:** implementado, pendiente de validación visual y aprobación del usuario. P03-A no aprobado. P03-A.7 solo planificado; P03-B no iniciado.

**Código ejecutado:** `ab7c78914e0153ba946f7d5be5b6c17dc6c66ae7`, `feat(worldgen): calibrate tectonic mountain uplift in Pangaea`, rama `worldgeneration_pangea`. El HEAD remoto se verificó y sincronizó antes de empezar: `adf69c545011e1cf18fdaaee897b6bb5f4500ff0`. El swap local ajeno no se modificó ni publicó.

## Validaciones ejecutadas

2026-11-06, **21:37:33–21:43:36 UTC**. Fuente limpia al iniciar. SDK 10.0.402, runtime .NET 10.0.13, Windows 10.0.26200 X64, PowerShell 5.1.26100.9711. El [resumen automático](SUMMARY.md) conserva fechas, entorno, SHA, comandos, estados y códigos reales.

| Comando | Estado | Salida | Segundos | Log completo |
|---|---|---:|---:|---|
| `dotnet build` | PASS | 0 | 6.935 | [build.log](build.log) |
| `dotnet run --project GEvolve.Tests -- --pangaea-elevations` | PASS | 0 | 31.567 | [pangaea-elevations.log](pangaea-elevations.log) |
| `dotnet run --project GEvolve.Tests -- --mountain-ranges` | PASS | 0 | 26.971 | [mountain-ranges.log](mountain-ranges.log) |
| `dotnet run --project GEvolve.Tests -- --pangaea-tectonic-audit` | PASS | 0 | 8.451 | [tectonic-audit.log](tectonic-audit.log) |
| `dotnet run --project GEvolve.Tests -- --pangaea-uplift` | PASS | 0 | 17.275 | [pangaea-uplift.log](pangaea-uplift.log) |
| `dotnet run --project GEvolve.Tests -- --all-suites` | FAIL | 1 | 270.867 | [all-suites.log](all-suites.log) |

Build: **0 errores y 0 advertencias**. Suite general: **14 suites PASS y 2 FAIL**. Fallan `TectonicMotion` (4 casos) y `ContinentalGeology` (12 casos), que comparan FINAL contra huellas históricas anteriores a P03-A. Son las mismas suites y escenarios pendientes en P03-A.4; los valores FINAL actuales cambian con la calibración. Sus 16 registros de campo discrepante son exclusivamente `elevation`; no se reportan discrepancias de `surface`, `plates` ni `attempt`. No se modificaron datos esperados ni se suprimieron aserciones. La suite nueva también pasa dentro de `--all-suites`; no se declara aprobada la suite general.

La auditoría P03-A.5 vuelve a comprobar identidad total con/sin traza para las cinco semillas. La comparación vertical de 20 casos pasa frente a la referencia independiente anterior. No se infiere un conteo de aserciones individuales que el runner no emite. La ejecución y sus logs recibieron revisión de privacidad antes de publicarse; no se incluyen binarios, bin/obj ni el swap ajeno.

## Causa atendida y diseño

La [auditoría P03-A.5](../P03-A5_20261106_143813456Z_ed0d8bb9/REPORT.md) no encontró exceso general de filtrado. Había ejes extensos en BASE baja cuya expresión altitudinal dependía de una suma limitada, mientras otras semillas tenían escasez local de convergencia. Esta fase cambia únicamente la composición vertical dentro de los corredores existentes; no añade sistemas en regiones vacías ni pretende corregir la representación verde uniforme bajo 85.

Antes, con altura BASE `B`, perfil longitudinal existente `p`, distancia hexagonal `d` y anchura existente `w`:

```text
t = (1 - d/(w+1))²
U = 25 × escalaAnterior × clamp(convergenciaLocal/4, 0.35, 1) × p × t
D4 = máximo de U alcanzado por la propagación anterior
FINAL4 = max(B, min(94, round(B + D4)))
```

La selección, orden y prioridad de esa propagación permanecen iguales, incluidos sus descartes de pasos superados y la asignación de propietario. La escala anterior conserva 1 para ejes de longitud al menos `0.20 × filas`, y 0.65 para los menores. No se retoca este mecanismo geométrico para cambiar alturas.

Ahora se calcula además una envolvente por eje:

```text
L = longitud del eje existente; R = filas del tamaño oficial
C = L / (L + 0.08 × R)
mᵢ = media de convergencia sobre el eje en [i-h, i+h], recortado a sus extremos
h = max(2, floor(L/12))
cᵢ = sqrt(convergenciaMediaDelSistema × mᵢ)
Tᵢ = 94 × sqrt(C) × (1 - exp(-cᵢ/2))
aᵢ,d = (0.6 + 0.4 × pᵢ) × t
Eᵢ,d = max(0, Tᵢ - B) × aᵢ,d
D6 = máximo de max(Uᵢ,d, Eᵢ,d) en los pasos vigentes de la propagación original
FINAL6 = max(B, min(94, round(B + D6)))
```

`T` es un objetivo condicionado, no una altura mínima: la mezcla conserva BASE y el perfil. La intensidad efectiva combina evidencia local suavizada y regional; la continuidad crece de manera gradual con la longitud relativa al tamaño. El divisor 2 fija una respuesta saturante a convergencia, sin el recorte duro de intensidad a 4 del aporte anterior. El factor 0.08 escala la transición de continuidad por resolución; la raíz evita eliminar sierras intermedias. El perfil existente modula la mezcla entre 0.74 y 1, aproximadamente, y el perfil transversal cuadrático se conserva. Son parámetros de esta calibración para validar visualmente, no nuevas leyes físicas ni categorías geológicas certificadas.

No se añaden sorteos ni ruido. La media móvil usa sumas prefijas, O(L), sin barridos completos por cada sistema. Cada paso ya visitado añade trabajo constante; un campo O(N) conserva el máximo vertical, separado del campo que determina propietarios. Los cruces de corredores usan máximo, nunca suma. Se conserva el aporte anterior cuando es mayor: no bajan crestas existentes ni se recortan cumbres BASE superiores a 94. La nueva envolvente no añade un plano objetivo fijo a 94, aunque el límite histórico puede seguir afectando al aporte anterior.

## Comparación independiente e invariantes

Antes de modificar el generador se creó `3c95bc7ca7afee362ab732efceb8fe86536aabc5`, `test(worldgen): capture P03-A.4 mountain elevation reference`. Solo añadió el capturador; la generación permanecía idéntica a P03-A.5/P03-A.4. Su [ejecución de referencia](../P03-A6-reference_20261106_213250767Z_2572f876/SUMMARY.md) pasó build y los 20 casos. El [log original](../P03-A6-reference_20261106_213250767Z_2572f876/uplift-reference.log) contiene hashes y métricas antes del cambio. No se derivaron los valores esperados del algoritmo nuevo.

`GEvolve.Tests/PangaeaUpliftReference.txt` conserva esos 20 registros inmutables. No sustituye ni actualiza baselines históricos. La comparación nueva exige identidad exacta de intento, superficie, placas, H04 completo, BASE, todos los campos geométricos de cada sistema (salvo el contador derivado `AffectedCells`) y el array completo de propietarios. También exige que el FINAL aditivo reconstruido en la misma pasada coincida con el hash original P03-A.4. No se introduce una segunda estrategia seleccionable en el juego ni se regenera un mundo para elegir un resultado.

En el log de captura previa, `Before` y `After` contienen el mismo FINAL P03-A.4: la etiqueta de plantilla `After/P03-A.6` aún no representa una calibración. En el log de esta entrega sí son composiciones diferentes. Se compararon además todos los objetos `Before` de ambas ejecuciones: cero diferencias en los 20 casos, incluidas métricas, sistemas y regiones. En la suite antigua de elevaciones, las líneas `SYSTEMS P03-A.2/P03-A.3` describen geometría reaplicada sobre esas BASE antiguas; su contador `final raised cells metadata` usa la composición activa al reaplicarla. Los registros `MOUNTAIN P03-A.2/P03-A.3 FINAL` y las aserciones históricas usan explícitamente el FINAL aditivo, y `UPLIFT_CASE.Before` es la referencia completa P03-A.4.

Casos: los cuatro tamaños × semillas 2026, 42 y -731; las cinco semillas Large de P03-A.5; referencias positivas Large/-234020843, Large/-690013300 y Enormous/1697481406. Son 20 casos únicos. Las pruebas de elevación anteriores conservan sus casos adicionales, sus cifras P03-A.2 aportadas por el usuario y sus aserciones históricas. Al reaplicar esa fase sobre BASE antigua comparan explícitamente el campo **aditivo histórico**, no el FINAL nuevo; ambos campos se informan con nombres diferentes.

La suite verifica además determinismo de la fase sin callback, metadatos de celdas realmente elevadas, ningún cambio fuera de influencia, océano cero y serialización de todo el estado. Las suites existentes conservan sus comprobaciones con el almacén real de partidas, consumo de FINAL por vistas y trazas activadas/desactivadas. Un contacto controlado comprueba respuesta a convergencia sobre BASE baja, ausencia de un suelo universal 85 y conservación de cumbres heredadas 97.

## Persistencia, integración y alcance

No hay cambio de esquema: `pangaea-ranges-2` sigue describiendo los mismos metadatos. Su lector y la compatibilidad previa con `pangaea-ranges-1` permanecen intactos. No se reinterpretan saves: retienen su FINAL almacenado. `tectonic-envelope-1` identifica la composición en diagnósticos, sin campos persistibles nuevos. `AffectedCells` puede aumentar porque cuenta celdas elevadas de verdad; no es geometría protegida.

`AdditiveFinalByCellId` es evidencia temporal de Engine, como BASE/APORTE/FINAL anteriores, y no entra en State ni en el guardado. Los arrays de APORTE y FINAL que consume Game contienen el incremento y la altura reales nuevos. Se mantienen todas las vistas, paleta, shader, umbral 85, catálogos y los generadores de otros tipos de mundo. No cambian topología, 16 placas ni H04.

## Ejecución reproducible

Desde la raíz, con el SDK del proyecto:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\GEvolve.Tests\ValidationReports\commands.ps1 -ValidationSet TectonicCalibration -Label P03-A6
```

El bypass afecta únicamente a ese proceso; no modifica la política del sistema. El script ejecuta build, elevaciones, cordilleras, auditoría tectónica, comparación de composición y suite general. Conserva stdout/stderr completos, salidas individuales y duraciones en [SUMMARY.md](SUMMARY.md) y sus logs. No inicia el juego gráfico. La captura histórica se repite solo con el commit de referencia y su snapshot, no con el generador calibrado.

El [primer intento de captura](../P03-A6-reference_20261106_213235533Z_2bb49306/SUMMARY.md) registró build FAIL por acceso denegado a NuGet.Config desde el aislamiento; se omitió su prueba dependiente. Se repitió con acceso suficiente, sin cambiar configuración NuGet. Los errores y la evidencia fallida se conservan.

## Riesgos y siguiente paso

- Se conserva toda altura previa: una concentración elevada existente no puede reducirse con esta intervención. Las cifras de componentes, blanco y altura exacta 94 permiten detectar expansiones o persistencias, sin prometer eliminarlas por reducir BASE.
- La elevación de corredores bajos puede aumentar pendientes o conectar zonas altas. El máximo entre envolventes evita acumulación, pero no garantiza por sí solo calidad morfológica. Las métricas son diagnósticas, no cuotas de aceptación.
- La propagación conserva sus prioridades anteriores para proteger la influencia; la envolvente no busca rutas nuevas ni es una solución global de distancia a todos los ejes. No representa historia tectónica temporal ni erosión.
- Los tiempos son ejecuciones puntuales con JIT, caché y carga del entorno; no un benchmark controlado. Se informa cada tamaño, sin atribuir aceleración a pequeñas diferencias entre ejecuciones.
- SUPERFICIE continúa sin sombreado continuo sub85 cuando no hay biomas. Una mejora numérica puede seguir oculta ahí. El usuario debe comparar ALTURAS BASE/APORTE/FINAL, RELIEVE: CORDILLERAS y PLACAS al mismo zoom, además de SUPERFICIE.

P03-A.7 queda planificado para auditoría integral de topología, superficie/costas, placas/H04, BASE, cordilleras, FINAL, persistencia, rendimiento, determinismo, integración de capas y representación. No se ejecuta en esta entrega ni se inicia P03-B.

## Resultados medidos

Las tablas siguientes se extraen de los logs completos de esta ejecución; las etiquetas favorable/desfavorable son las del usuario, no una aprobación visual nueva.

### Cinco semillas de la auditoría (Large)

| Semilla | Ejes: media FINAL A4 → A6 | Ganancia media eje | Prominencia inmediata | Pendiente media corredor | Blanco ≥85 | Mayor componente ≥85 |
|---|---:|---:|---:|---:|---:|---:|
| -714216669 | 60.59 → 70.56 | 9.65 → 19.62 | 3.77 → 5.11 | 9.25 → 10.12 | 135 → 135 | 58 → 58 |
| -2024691550 | 83.29 → 85.73 | 13.37 → 15.81 | 3.82 → 3.36 | 9.97 → 9.42 | 629 → 645 | 113 → 160 |
| -174327693 | 53.02 → 71.47 | 13.45 → 31.90 | 4.09 → 7.73 | 7.89 → 10.57 | 191 → 191 | 156 → 156 |
| -1979952984 | 52.03 → 63.28 | 9.03 → 20.28 | 2.12 → 4.64 | 5.36 → 6.98 | 175 → 175 | 143 → 143 |
| 2076374193 | 54.53 → 75.94 | 15.00 → 36.41 | 2.87 → 7.27 | 6.01 → 9.27 | 526 → 526 | 244 → 244 |

La prominencia inmediata es FINAL del eje menos media de sus vecinos terrestres, ponderada por celdas de eje. La referencia exterior es media de las alturas al otro lado del propietario del corredor, ponderada por aristas de borde; puede incluir otro sistema. Son niveles de altura diagnósticos, no metros ni prominencia topográfica física. La pendiente es el máximo salto a vecinos terrestres definido por TerrainSlope, promediado dentro de influencia.

### Veinte casos: bandas, concentraciones y celdas realmente elevadas

Las bandas son tierra 61–75 / 76–84 / 85–94 / 95–100. El blanco incluye ambas bandas ≥85. «Nuevas 94» cuenta FINAL=94 con BASE<94; permite separar el recorte heredado del nuevo método.

| Tamaño / semilla | Bandas A4 | Bandas A6 | Componentes ≥85 A4 → A6 | Mayor ≥85 A4 → A6 | Elevadas A4 → A6 | Nuevas 94 A4 → A6 | Salto máximo A4 → A6 |
|---|---|---|---:|---:|---:|---:|---:|
| Small/2026 | 959/512/95/4 | 1113/537/95/4 | 13 → 13 | 20 → 20 | 1663 → 1815 | 13 → 13 | 51 → 51 |
| Small/42 | 1138/367/158/35 | 1197/387/158/35 | 2 → 2 | 187 → 187 | 1089 → 1151 | 0 → 0 | 59 → 59 |
| Small/-731 | 800/382/159/11 | 893/389/159/11 | 5 → 5 | 82 → 82 | 1919 → 2061 | 25 → 25 | 49 → 43 |
| Medium/2026 | 1259/793/336/7 | 1357/794/336/7 | 9 → 9 | 103 → 103 | 2535 → 2871 | 102 → 102 | 46 → 43 |
| Medium/42 | 1309/657/212/21 | 1340/657/212/21 | 2 → 2 | 146 → 146 | 676 → 734 | 14 → 14 | 43 → 43 |
| Medium/-731 | 1554/670/88/13 | 1662/710/88/13 | 3 → 3 | 88 → 88 | 1422 → 1535 | 17 → 17 | 47 → 47 |
| Large/2026 | 2694/1105/164/19 | 2722/1105/164/19 | 13 → 13 | 65 → 65 | 1923 → 2220 | 40 → 40 | 29 → 28 |
| Large/42 | 1659/846/455/20 | 1795/846/455/20 | 12 → 12 | 253 → 253 | 4076 → 4596 | 164 → 164 | 50 → 50 |
| Large/-731 | 2398/1236/152/18 | 2599/1281/152/18 | 8 → 8 | 66 → 66 | 4274 → 4815 | 26 → 26 | 37 → 37 |
| Enormous/2026 | 3760/1674/618/17 | 3819/1674/618/17 | 3 → 3 | 573 → 573 | 2957 → 3281 | 294 → 294 | 57 → 46 |
| Enormous/42 | 3266/1516/579/5 | 3403/1522/579/5 | 12 → 12 | 109 → 109 | 4420 → 4774 | 111 → 111 | 54 → 44 |
| Enormous/-731 | 4112/1700/81/4 | 4166/1700/81/4 | 3 → 3 | 72 → 72 | 3220 → 3607 | 14 → 14 | 55 → 52 |
| Large/-714216669 | 1911/1140/132/3 | 2027/1142/132/3 | 16 → 16 | 58 → 58 | 2858 → 3339 | 15 → 15 | 50 → 46 |
| Large/-2024691550 | 1279/912/603/26 | 1385/950/619/26 | 14 → 11 | 113 → 160 | 2663 → 3013 | 125 → 125 | 41 → 41 |
| Large/-174327693 | 2313/1195/168/23 | 2440/1214/168/23 | 4 → 4 | 156 → 156 | 2628 → 2882 | 22 → 22 | 38 → 36 |
| Large/-1979952984 | 2180/507/173/2 | 2189/507/173/2 | 2 → 2 | 143 → 143 | 1375 → 1539 | 32 → 32 | 33 → 29 |
| Large/2076374193 | 2371/1138/487/39 | 2626/1224/487/39 | 10 → 10 | 244 → 244 | 2910 → 3274 | 85 → 85 | 26 → 24 |
| Large/-234020843 | 1447/867/271/8 | 1667/952/271/8 | 12 → 12 | 118 → 118 | 3247 → 3699 | 29 → 29 | 58 → 54 |
| Large/-690013300 | 1485/922/594/8 | 1790/1019/594/8 | 22 → 22 | 388 → 388 | 5524 → 6141 | 186 → 186 | 44 → 40 |
| Enormous/1697481406 | 3803/1927/469/6 | 4357/1997/469/6 | 18 → 18 | 238 → 238 | 10014 → 10981 | 62 → 62 | 59 → 51 |

### Sistemas que ilustran la respuesta diferenciada

| Semilla / ID | Longitud | Convergencia media | Eje FINAL A4 → A6 | Ganancia en eje A4 → A6 | Prominencia exterior A4 → A6 |
|---|---:|---:|---:|---:|---:|
| -174327693/6 | 81 | 3.00 | 57.09 → 66.32 | 12.49 → 21.73 | 41.81 → 50.88 |
| -174327693/7 | 73 | 5.56 | 29.51 → 72.75 | 18.34 → 61.59 | 18.88 → 62.00 |
| -1979952984/3 | 15 | 3.77 | 14.07 → 45.60 | 7.67 → 39.20 | 5.11 → 36.64 |
| -1979952984/5 | 13 | 1.29 | 25.77 → 32.62 | 4.08 → 10.92 | 2.75 → 7.91 |
| 2076374193/1 | 58 | 12.64 | 53.91 → 78.98 | 19.78 → 44.84 | 20.36 → 45.43 |
| 2076374193/5 | 18 | 11.20 | 26.89 → 60.89 | 10.67 → 44.67 | 4.34 → 37.38 |

El log contiene para **cada sistema de los 20 casos**, antes/después: mínimo, máximo, P10/P50/P90, media, ganancia del eje y del corredor, ambas prominencias, pendiente, longitud, convergencia, influencia y celdas elevadas. No se exige una ganancia uniforme: los sistemas sobre BASE ya alta pueden conservar su cresta.

### Interpretación de los resultados y referencias positivas

Las tres desfavorables aumentan media de eje y prominencia local manteniendo el blanco sin cambios. No se inventan sistemas donde faltaba convergencia: `-1979952984` conserva sus 7 sistemas, 117 celdas de eje y sus regiones tectónicamente vacías. El sistema débil de 13 celdas, convergencia 1.29, queda en media 32.62; no se convirtió en gran cordillera. El sistema de 73 celdas sobre BASE media 11.16 alcanza media 72.75, todavía muy por debajo de un suelo universal 85. La geometría es exactamente la misma.

En **19 de los 20 casos**, el número de celdas ≥85 no aumenta. La excepción favorable `-2024691550` pasa 629→645 (+16), y el mayor componente ≥85 pasa 113→160: unas pocas celdas pueden conectar áreas previamente separadas. Debe revisarse visualmente esa unión; no se oculta tras el porcentaje global. No se añadieron celdas exactamente 94 ni celdas 95–100 en ninguno de los 20 casos. No aumentó el salto máximo entre vecinos en esa muestra. Estos datos limitan el riesgo observado de saturación, pero no certifican forma visual ni garantizan ausencia de concentraciones en otras semillas.

La pendiente media aumenta en 17 casos y baja en tres. Por ejemplo, Small/42 pasa 6.73→13.04; en la favorable Large/-2024691550 pasa 9.97→9.42 y su prominencia inmediata baja 3.82→3.36 al elevarse parte del entorno. Enormous/2026 también reduce esa prominencia (3.75→3.34). No se declara mejora uniforme ni se confunde altura absoluta con prominencia.

Las tres referencias históricas positivas conservan sistemas, ejes, influencia, blanco y tamaño de su mayor componente alto:

| Caso | Sistemas / celdas eje | Media FINAL eje A4 → A6 | Mayor componente ≥85 |
|---|---:|---:|---:|
| Large/-234020843 | 12 / 323 | 68.32 → 80.24 | 118 → 118 |
| Large/-690013300 | 19 / 642 | 69.00 → 78.90 | 388 → 388 |
| Enormous/1697481406 | 18 / 696 | 60.36 → 72.95 | 238 → 238 |

**Tiempos puntuales de generación**, separados del coste diagnóstico, por tamaño y mismos casos:

| Tamaño | Casos | Rango anterior ms | Rango nuevo ms |
|---|---:|---:|---:|
| Small | 3 | 237.33–330.12 | 240.15–294.56 |
| Medium | 3 | 438.72–709.19 | 313.94–420.22 |
| Large | 10 | 617.92–741.03 | 409.47–448.61 |
| Enormous | 4 | 1354.63–1423.49 | 791.06–912.13 |

No se atribuye una aceleración a esta fórmula: las ejecuciones separadas no controlan calentamiento ni carga. Las suites y la captura de referencia no son un benchmark de sobrecoste. Se mantiene la estructura previa y se añade O(N) de memoria y O(longitud de ejes + pasos ya visitados) de cálculo; la prueba visual y una medición controlada futura siguen siendo relevantes.

### Distribución regional de las cinco semillas

Nueve cajas disjuntas de tierra por mapa: floor(fila*3/filas)*3+floor(columna*3/columnas). Los ejes que tocan varias cajas se cuentan en cada una; no se deben sumar sus conteos regionales. BASE, ejes y tierra permanecen idénticos. Las fracciones elevadas se normalizan por tierra de la caja.

| Semilla | Región | Tierra | Sistemas con eje | BASE media | FINAL media A4 → A6 | % tierra elevada A4 → A6 | ≥85 A4 → A6 |
|---|---:|---:|---:|---:|---:|---:|---:|
| -714216669 | 0 | 6700 | 3 | 33.45 | 33.96 → 34.55 | 11.07 → 13.16 | 25 → 25 |
| -714216669 | 1 | 4596 | 5 | 41.09 | 41.85 → 42.72 | 16.17 → 17.60 | 71 → 71 |
| -714216669 | 2 | 5820 | 4 | 22.80 | 22.95 → 23.10 | 8.04 → 9.64 | 10 → 10 |
| -714216669 | 3 | 3795 | 5 | 32.11 | 33.17 → 35.49 | 22.06 → 26.27 | 21 → 21 |
| -714216669 | 4 | 805 | 1 | 45.58 | 45.87 → 46.04 | 7.20 → 8.70 | 0 → 0 |
| -714216669 | 5 | 2302 | 0 | 27.13 | 27.13 → 27.15 | 0.43 → 0.87 | 8 → 8 |
| -714216669 | 6 | 0 | 0 | n/a | n/a → n/a | n/a → n/a | 0 → 0 |
| -714216669 | 7 | 0 | 0 | n/a | n/a → n/a | n/a → n/a | 0 → 0 |
| -714216669 | 8 | 0 | 0 | n/a | n/a → n/a | n/a → n/a | 0 → 0 |
| -2024691550 | 0 | 2115 | 2 | 54.27 | 55.56 → 56.22 | 27.99 → 29.98 | 113 → 114 |
| -2024691550 | 1 | 6193 | 1 | 36.15 | 37.37 → 38.02 | 17.12 → 19.41 | 203 → 218 |
| -2024691550 | 2 | 650 | 0 | 16.65 | 16.65 → 16.65 | 0.00 → 0.00 | 0 → 0 |
| -2024691550 | 3 | 25 | 0 | 2.88 | 2.88 → 2.88 | 0.00 → 0.00 | 0 → 0 |
| -2024691550 | 4 | 5789 | 2 | 24.44 | 24.62 → 24.74 | 3.64 → 4.40 | 97 → 97 |
| -2024691550 | 5 | 1933 | 1 | 44.03 | 44.69 → 44.97 | 11.33 → 13.55 | 96 → 96 |
| -2024691550 | 6 | 1688 | 0 | 32.89 | 32.89 → 32.89 | 0.00 → 0.00 | 0 → 0 |
| -2024691550 | 7 | 4958 | 2 | 23.01 | 23.65 → 24.05 | 11.21 → 12.51 | 118 → 118 |
| -2024691550 | 8 | 649 | 0 | 28.73 | 28.87 → 29.09 | 3.85 → 6.16 | 2 → 2 |
| -174327693 | 0 | 514 | 0 | 29.51 | 29.51 → 29.51 | 0.00 → 0.00 | 0 → 0 |
| -174327693 | 1 | 2613 | 0 | 25.63 | 25.63 → 25.63 | 0.00 → 0.00 | 0 → 0 |
| -174327693 | 2 | 0 | 0 | n/a | n/a → n/a | n/a → n/a | 0 → 0 |
| -174327693 | 3 | 1875 | 0 | 51.93 | 51.93 → 51.93 | 0.27 → 0.37 | 63 → 63 |
| -174327693 | 4 | 5796 | 4 | 48.17 | 48.70 → 48.85 | 12.37 → 12.97 | 107 → 107 |
| -174327693 | 5 | 483 | 0 | 72.14 | 72.15 → 72.15 | 0.62 → 0.62 | 0 → 0 |
| -174327693 | 6 | 3658 | 0 | 18.10 | 18.10 → 18.10 | 0.00 → 0.00 | 7 → 7 |
| -174327693 | 7 | 6600 | 4 | 17.17 | 18.45 → 20.96 | 21.79 → 24.24 | 0 → 0 |
| -174327693 | 8 | 3342 | 2 | 34.08 | 34.81 → 35.73 | 13.91 → 15.56 | 14 → 14 |
| -1979952984 | 0 | 377 | 0 | 40.64 | 40.64 → 40.64 | 0.00 → 0.00 | 0 → 0 |
| -1979952984 | 1 | 5696 | 0 | 39.67 | 39.67 → 39.67 | 0.00 → 0.00 | 89 → 89 |
| -1979952984 | 2 | 3401 | 2 | 39.93 | 40.45 → 40.75 | 12.73 → 13.41 | 86 → 86 |
| -1979952984 | 3 | 5788 | 4 | 27.56 | 28.02 → 28.88 | 12.65 → 15.01 | 0 → 0 |
| -1979952984 | 4 | 5837 | 0 | 21.14 | 21.14 → 21.14 | 0.00 → 0.00 | 0 → 0 |
| -1979952984 | 5 | 193 | 0 | 12.90 | 12.90 → 12.90 | 0.00 → 0.00 | 0 → 0 |
| -1979952984 | 6 | 2433 | 2 | 30.68 | 31.10 → 31.11 | 8.63 → 8.80 | 0 → 0 |
| -1979952984 | 7 | 275 | 0 | 33.64 | 33.64 → 33.64 | 0.00 → 0.00 | 0 → 0 |
| -1979952984 | 8 | 0 | 0 | n/a | n/a → n/a | n/a → n/a | 0 → 0 |
| 2076374193 | 0 | 3604 | 3 | 29.51 | 31.25 → 33.73 | 23.83 → 27.28 | 0 → 0 |
| 2076374193 | 1 | 146 | 1 | 56.05 | 58.14 → 60.16 | 25.34 → 26.03 | 6 → 6 |
| 2076374193 | 2 | 3727 | 1 | 37.59 | 38.02 → 38.77 | 6.57 → 7.06 | 11 → 11 |
| 2076374193 | 3 | 6700 | 3 | 32.47 | 33.15 → 34.24 | 12.48 → 14.07 | 147 → 147 |
| 2076374193 | 4 | 3092 | 1 | 38.92 | 39.90 → 40.41 | 15.52 → 16.30 | 313 → 313 |
| 2076374193 | 5 | 1981 | 0 | 32.10 | 32.10 → 32.10 | 0.10 → 0.25 | 0 → 0 |
| 2076374193 | 6 | 3964 | 1 | 15.23 | 15.36 → 15.89 | 3.25 → 3.96 | 0 → 0 |
| 2076374193 | 7 | 4813 | 2 | 40.23 | 40.54 → 41.15 | 6.69 → 7.92 | 47 → 47 |
| 2076374193 | 8 | 100 | 0 | 65.97 | 65.97 → 65.97 | 0.00 → 0.00 | 2 → 2 |
