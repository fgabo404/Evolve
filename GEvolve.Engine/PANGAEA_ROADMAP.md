# Evolve — Roadmap operativo de Pangea

> **Rama:** `worldgeneration_pangea`  
> **Rama de origen:** `worldgenerator`  
> **Base limpia validada:** `a58d5689c17f1fea045066c10a400dbbe9a35976` — `refactor(worldgen): revert experimental continents generation and preserve pangaea`  
> **Documento general:** [WORLD_GENERATION_ROADMAP.md](WORLD_GENERATION_ROADMAP.md)  
> **Estado al 2026-11-07:** Laboratorio reorganizado en columna izquierda fija y mapa protagonista, cinco acciones secuenciales en Game, reinicio inmediato por semilla/tamaño y alturas integradas sin interruptor. Motor P03-A.7.1 y resultados conservados. Pendiente de aprobación visual del usuario. P03-A.7.3–7.4 planificados; P03-A.6 sigue pendiente de revisión visual. P03-A no aprobado; baselines históricos intactos.

## Objetivo

Completar **Pangea** antes de ampliar o adaptar las capas a Continents, Archipelagos y SmallContinents. Conservar la forma actual de Pangea, el rendimiento, los cuatro tamaños hexagonales, el wrap E/O, los límites abiertos N/S, las 16 placas y sus movimientos, y los catálogos JSON. Mantener **una sola generación individual por tipo, tamaño y semilla**, con etapas especializadas y contratos compartidos.

No restaurar los experimentos corticales H05 retirados. `Continents` continúa en estado `SurfaceGenerationPending`; los otros generadores históricos quedan protegidos.

## Cómo llevar el control

Cada hito puede estar en uno de estos estados:

- **Base funcional:** existe código previo; no implica que toda la capa esté finalizada.
- **Pendiente:** aún no comienza el trabajo específico de Pangea.
- **En desarrollo:** implementación autorizada, sin prueba local aprobada.
- **Implementado / pendiente de validación:** existe un commit, falta la comprobación del usuario.
- **Aprobado:** el usuario confirmó compilación, tests, integridad de baselines y revisión visual cuando corresponda.
- **Bloqueado:** hay un fallo documentado que impide continuar.

No marcar `[x]` por el solo hecho de crear una clase o publicar un commit. Para cerrar un hito consignar mensaje de commit, SHA completo, pruebas locales realizadas, resultados y decisión visual. Actualizar **este documento en el repositorio** en cada cierre o cambio de alcance; alinear las síntesis del roadmap general y `.agents/PROJECT_MEMORY.md` sin duplicar especificaciones detalladas.

## Matriz principal

| Hito | Alcance | Estado | Evidencia / próximo paso |
|---|---|---|---|
| P01 | Cuadrícula, 16 placas, límites y movimiento tectónico | **Base funcional aprobada** | H01–H04; conservar hashes y topología |
| P02 | Superficie actual de Pangea, costas y masa conectada | **Base funcional aprobada** | 36 baselines históricos y geometría valorada por usuario; no cambiar |
| P03 | Accidentes geográficos terrestres | **En desarrollo** | P03-A implementado; validación y aprobación pendientes |
| P04 | Accidentes submarinos y batimetría | Pendiente | Plataformas, taludes, llanuras abisales, dorsales, fosas |
| P05 | Consolidar elevaciones y pendientes | Pendiente | Tierra 1–100, océano 0 en contrato actual; profundidad separada |
| P06 | Temperatura sobre geografía definitiva | **Base funcional; integración/calibración pendiente** | Conservar módulos y validar latitud/altura/mar |
| P07 | Precipitaciones y humedad | **Base funcional; integración/calibración pendiente** | Revisar sesgo seco, vientos y sombra de lluvia |
| P08 | Hidrología | Pendiente | Escorrentía, cuencas, ríos, lagos y conectividad |
| P09 | Biomas y subbiomas | **Base funcional; integración/calibración pendiente** | Clasificación desde catálogo JSON, altitud, humedad, pendiente y clima |
| P10 | Recursos naturales | Pendiente | Flora, fauna y minerales desde catálogos y condiciones |
| P11 | Integración, persistencia y cierre | Pendiente | Rendimiento, 4 tamaños, semillas, guardado y auditoría visual |

## P03 — Accidentes geográficos terrestres

| Subhito | Resultado esperado | Estado |
|---|---|---|
| **P03-A — Cordilleras** | Sistemas montañosos regionales continuos, ejes conectados, anchuras y alturas variables, coherentes con evidencia convergente de H04 | **Implementado / pendiente de validación** |
| P03-B — Montañas aisladas | Macizos y cumbres diferenciables de cordilleras | Pendiente |
| P03-C — Mesetas y colinas | Relieve elevado amplio y ondulaciones regionales | Pendiente |
| P03-D — Valles y cuencas | Depresiones coherentes con el relieve circundante | Pendiente |
| P03-E — Rifts, escarpes y fallas | Deformación relacionada con extensión y cizalla verificadas | Pendiente |
| P03-F — Volcanes | Volcanismo coherente, sin inferir subducción sin evidencia cortical suficiente | Pendiente |
| P03-G — Consolidación | Resolver superposiciones y mantener contratos de altura y biomas | Pendiente |

### Contrato de aceptación P03-A

- [ ] Implementación publicada **solo en** `worldgeneration_pangea`, sin modificar los otros generadores.
- [ ] Identificar cordilleras con IDs y ejes conectados por vecinos hexagonales reales; preservar seam E/O y límites N/S.
- [ ] Evidencia tectónica de H04 utilizada sin afirmar colisiones continente–continente no verificables.
- [ ] Superficie de Pangea **idéntica** a la base por tamaño/semilla; ninguna celda pasa de tierra a océano o viceversa.
- [ ] Placas, velocidades, interacciones y campos de esfuerzo **idénticos** a la base.
- [ ] Elevaciones terrestres de 1–100; océano 0. Cumbres y laderas diferenciadas, sin saturación generalizada.
- [ ] No modificar catálogos JSON, clima, precipitación, biomas ni batimetría en este subhito.
- [ ] Documentar cambios previstos en hashes de elevación; **no actualizar baselines históricos sin aprobación explícita**.
- [ ] Compilación local, suite específica y pruebas generales aprobadas por el usuario.
- [ ] Inspección visual dentro de Evolve aprobada: cadenas regionales reconocibles, no líneas artificiales.
- [ ] Rendimiento registrado para los cuatro tamaños; sin solucionadores globales costosos.
- [ ] Commit y SHA documentados; no iniciar P03-B antes del cierre de P03-A.

### Registro de entregas

| Fecha | Hito | Estado / decisión | Commit | Evidencia |
|---|---|---|---|---|
| 2026-11-05 | Base | Limpieza H05 validada; retomamos Pangea | `a58d5689c17f1fea045066c10a400dbbe9a35976` | Build, suite y 36 baselines aprobados por usuario |
| 2026-11-05 | P03-A | Implementado; **ajustes pendientes tras revisión visual** | `feat(worldgen): add tectonic mountain range systems to Pangaea` (`c2f222cbfa0922d8626c71d10460657b94261cad`) | Build PASS; suite específica 12/12 PASS; P03-A 0–2 ms reportados; suite general con fallo de cumbres blancas y hashes de elevación anteriores; revisión visual de tres semillas no aprobada |

### Entrega P03-A pendiente de validación

Los ejes se extraen de contactos H04 convergentes o mixtos con convergencia de al menos 1 cm/año, dominante frente a la cizalla y con tierra a ambos lados. Se agrupan por par de placas y vecindad hexagonal; los componentes cortos se descartan. Cada sistema conserva ID, eje ordenado, anchura local e intensidad. El levantamiento se propaga localmente sobre tierra, se atenúa hacia las laderas y se suma al relieve inicial sin renormalizar el mapa ni alterar la superficie. El modo de geología muestra las áreas y ejes de Pangea.

Limitaciones: H04 no identifica corteza continental ni la frontera causante del máximo de compresión por celda; las cordilleras son sistemas favorecidos por convergencia, no colisiones continentales certificadas. La selección de ejes y su aspecto requieren revisión visual y medición de rendimiento local. Se esperan cambios en hashes de elevación de Pangea; los baselines históricos permanecen intactos hasta aprobación explícita. Los guardados anteriores sin metadatos de cordilleras siguen admitidos.

### Auditoría visual preliminar y diagnóstico técnico — 2026-11-05

Muestras vistas en Evolve: **Enormous 400×267**, semillas **1510325998**, **-251942972** y **1697481406**. La tercera muestra presenta sistemas largos, conectados y prometedores; otras presentan cadenas pequeñas y anillos compactos. No extrapolar frecuencias a todas las semillas.

**Fortalezas:** formación regional conectada, uso de contactos convergentes H04, preservación de la silueta de Pangea, coste específico muy bajo (0–2 ms reportados, limitado por resolución del cronómetro).

**Problemas por revisar antes de aprobar:** (1) ejes compactos o cerrados convertidos en cordilleras o anillos; (2) cadenas que siguen con demasiada fidelidad el perímetro costero, especialmente en la semilla 1697481406; (3) bandas de anchura visualmente regular y crestas excesivamente blancas; (4) sistemas secundarios con escala mal clasificada. **No filtrar todos los sistemas cortos sin auditar: la tercera semilla demuestra utilidad de variación regional.**

**Causas plausibles confirmadas por inspección de código:** eje extraído mediante dos barridos del grafo, que no modelan explícitamente circularidad/ramificación; anchura definida por seno periódico `baseWidth + 1.2*sin(...)`; levantamiento `27*intensity*taper` con intensidad acotada y elevación recortada a 100. Son mecanismos capaces de explicar los patrones, pero falta medición por celdas para confirmar cuántas alturas saturan.

**Pruebas locales reportadas:** compilación correcta; suite `--mountain-ranges` aprobada en 12 escenarios; suite general falló en `TemperatureTests` («Summits stay white in terrain views») y en comparaciones históricas de hashes de elevación de Pangea (4 escenarios en `TectonicMotionTests`, 12 en `ContinentalGeologyTests`). No cambiar baselines antes de aprobar la nueva topografía. La precipitación sigue extremadamente seca (~94,79% de celdas terrestres), asunto independiente para P07.

**Próximo paso recomendado:** validar local y visualmente P03-A.1 en los mismos casos, sin iniciar P03-B ni declarar cierre.

### P03-A.1 — Calibración implementada; aprobación pendiente

Implementación publicada: `refactor(worldgen): calibrate Pangaea mountain range morphology and uplift` (`7c4ad519f30fb006d90c4681b7ca50ea41d5d47f`). La prueba específica informa, por tamaño y semilla, alturas originales y finales en 75/85/95/100, celdas realmente elevadas, incremento medio y máximo, componente alta más grande y fracción terrestre afectada. Por sistema registra pareja de placas, longitud, celdas elevadas, anchuras, convergencia media y máxima, elongación, extremos, ciclos y ramificación. Estos contadores aún no tienen resultados locales reportados; no se atribuyen a P03-A alturas que ya existían en P02.

El clasificador combina longitud y elongación para excluir contactos compactos, incluidos anillos pequeños, sin descartar arcos regionales extensos. Los ejes pueden desplazarse un hexágono hacia compresión interior conservando conectividad, tierra y proximidad a la frontera. Los anchos e intensidades varían mediante anclas regionales interpoladas, sin periodicidad sinusoidal ni ruido por celda. La propagación reemplaza una primera llegada débil cuando encuentra una influencia local mayor. El aporte nuevo se limita a 94; las alturas originales superiores se conservan. Los guardados `pangaea-ranges-1` siguen admitidos; la salida nueva usa `pangaea-ranges-2`.

El fallo de `TemperatureTests` procedía de la expectativa de blanco para todas las vistas de terreno: la vista diagnóstica de cordilleras colorea deliberadamente también las cumbres. Se corrigió solo esa expectativa; el umbral, la paleta, el shader y la temperatura permanecen intactos. Los baselines históricos de elevación siguen sin actualizar. Validar compilación, suite P03-A, suite general, persistencia, tiempos y revisión en Evolve de Enormous para semillas `1510325998`, `-251942972`, `1697481406`, `1567286827` y `418579367`, además de las cuatro resoluciones y semillas oficiales.

### P03-A.2 — Diagnóstico y calibración de elevaciones terrestres

**Estado: Diagnóstico validado localmente**, según compilación y pruebas específicas confirmadas por el usuario tras la corrección nullable. P03-A.2 no modificó las fórmulas de elevación ni la geometría de las cordilleras. Sus resultados autorizan la calibración acotada P03-A.3; no constituyen aprobación de P03-A. La consolidación definitiva de alturas y pendientes sigue reservada a P05.

Implementación: `refactor(worldgen): diagnose and calibrate Pangaea terrestrial elevations` (`c9bcda8645ac53ef5a28593b4ab7ef5394dab15a`), corregida por `fix(tests): correct nullable GridCell access in Pangaea elevation diagnostics` (`515e74e5c02a96625c3ed4e48b296cdde6360a70`). El usuario decidió conservar la geometría actual de cordilleras y priorizar la distribución de alturas.

**Hechos comprobados en código:** el blanco de las vistas habituales empieza en 85, no en 100; la paleta y el shader ocultan diferencias dentro de 85–100. El potencial inicial es `2.5 * uplift aproximado + 1.3 * ruido regional + 0.45 * ruido fino`; sus cuantiles 0/0.34/0.78/0.955/1 se interpolan a 0/15/50/80/100 y la tierra se recorta a 1–100. Ese uplift utiliza los vectores históricos locales de Pangea, distintos del movimiento H04. P03-A combina influencias mediante el **máximo**, sin sumar sistemas superpuestos, y limita su composición a 94 conservando alturas heredadas superiores. Por tanto, puede crear celdas blancas sin crear nuevas celdas de altura 100. La extensión y procedencia de las concentraciones siguen pendientes de medición.

**Evidencia disponible para recopilar:** `dotnet run --project GEvolve.Tests -- --pangaea-elevations`. Captura BASE, APORTE y FINAL durante una sola generación por escenario. Informa mínimo/máximo/media/mediana, P10/P25/P50/P75/P90/P95/P99 (cuantil por posición redondeada), bandas 1–20/21–40/41–60/61–80/81–100, ceros del aporte, umbrales 75/85/95/100, componentes extremas conectadas por `HexTopology`, tamaños y composición heredada/nueva/mixta, fracción elevada, aporte medio/máximo y recorte efectivo. Para la mayor componente blanca muestra rango y media reales de alturas. Registra anclas del potencial, correlaciones y medias de la contribución tectónica aproximada y el ruido agregado; una correlación no certifica causalidad. Los hashes históricos no se actualizan.

**En Evolve:** el botón de alturas de Pangea recorre `ALTURAS: BASE` → `ALTURAS: APORTE CORDILLERAS` → `ALTURAS: FINAL`. BASE y FINAL muestran niveles numéricos 1–100 sin el reemplazo por blanco desde 85; APORTE usa escala +0 a +25. Superficie y cámara son compartidas; la selección muestra los tres valores y el zoom etiqueta el campo activo. La vista de superficie habitual conserva la clasificación de cumbres. Los campos diagnósticos viven solo en el resultado de Engine y en la sesión, fuera de `LandmassState`, del formato de guardado y de cualquier exportación de mapas. Un mundo cargado sin esa captura conserva sus vistas históricas; no se regenera para fabricar el campo base.

**Matriz de diagnóstico:** cuatro tamaños × semillas 2026/42/−731; Enormous con 1324918567, 418579367, 1697481406 y 1242668747; Large con −234020843 y −690013300. El usuario confirmó que compilación y suite específica funcionan; la suite general conserva diferencias de hashes históricos de elevación. No se cambian esas huellas sin aprobación. Los seis casos se mantienen para comparar la calibración siguiente.

### P03-A.3 — Calibración del relieve inicial de Pangea

**Estado: Implementado, rechazado visualmente por el usuario como resultado final.** Commit: `refactor(worldgen): calibrate Pangaea base elevation distribution` (`e0bfae2de8c56a36d8987c56c3ddb71b88288975`). La reducción de concentraciones extremas fue útil, pero cuatro mundos Large se percibieron pobres en relieve montañoso. P03-B y P05 no se inician.

**Evidencia de P03-A.2 aportada por el usuario (Enormous):**

| Semilla | BASE ≥85 | FINAL ≥85 | Mayor componente BASE | Mayor componente FINAL |
|---|---:|---:|---:|---:|
| 1324918567 | 876 | 988 | 568 | 568 |
| 418579367 | 764 | 947 | 282 | 319 |
| 1242668747 | 472 | 712 | 472 | 571 |
| 1697481406 | 674 | 1257 | No aportado | No aportado |

La componente principal de 1324918567 ya existía en BASE y abarcaba alturas 85–93. En varios casos se informó correlación con ruido regional >0.90, menor para la tectónica aproximada. El mecanismo verificable es la transformación monótona de un único potencial: sus superniveles conservan los máximos espaciales amplios del ruido dominante. La última banda de la transferencia lleva el tramo entre el cuantil 0.955 y el máximo a 80–100; no exige prominencia local ni proximidad tectónica para elevar una zona regional entera. El blanco desde 85 oculta las diferencias restantes, pero no causa la concentración numérica. La correlación por sí sola no se presenta como prueba causal.

**Cambio conceptual único: estructurar la banda superior de la envolvente regional.** La selección de tierra/océano termina antes de cualquier modificación. Se conservan los cuantiles originales como envolvente y como referencia comparativa. Las alturas 1–60 permanecen idénticas. Para H>60 se calcula `p=(H−60)*(0.25+0.75*max(d²,u))`, donde `d` es el detalle ya existente (frecuencia base 15, con sus octavas históricas) normalizado al rango terrestre, y `u` el levantamiento aproximado existente normalizado por su máximo terrestre. Se convierte solo esa banda mediante `H'=60+40*p/max(p)`, con redondeo al nivel entero. El nivel 60 protege zonas bajas y medias; el suelo 0.25 conserva la estructura regional, el detalle cuadrático concentra crestas suaves, y el máximo permite que la presión existente sostenga una región alta sin exigir un máximo de ruido simultáneo. La normalización de esta banda conserva cumbres de 100; no fija una cuota de montañas ni escala toda la tierra por un factor constante. No se añade ruido, RNG, simulación ni máscara de aceptación por semilla.

**Identidad y contratos:** `PangaeaFromPlatesGenerator.Version` pasa a `pangaea-from-plates-2`. Configuraciones persistidas y formato de guardado no cambian; las partidas existentes conservan sus alturas almacenadas. Regenerar una semilla con esta versión cambia las elevaciones de Pangea. No cambian máscara, intentos, placas o H04. El generador de cordilleras mantiene selección, ejes, IDs, clasificación, anchuras, intensidad y aporte solicitado; únicamente se extrae su composición vertical sin alterar la fórmula para aplicarla también a la BASE anterior. El aporte efectivo puede variar por la diferencia de altura base y el mismo límite de 94. `AffectedCells` puede variar porque cuenta elevaciones efectivamente incrementadas, no identidad geométrica.

**Comparación local:** la misma generación conserva BASE anterior, BASE calibrada, FINAL anterior y FINAL calibrada. FINAL anterior se compone con el mismo campo de levantamiento ya generado, sin otro mundo ni otra búsqueda de cordilleras en producción. La suite `--pangaea-elevations` informa las distribuciones de ambos estados, sus aportes, componentes ≥85, diferencias entre vecinos terrestres y perímetro/raíz del área de la mayor componente, además del coste de la calibración. Reproduce como referencia estricta los conteos anteriores aportados por el usuario y mantiene los hashes históricos en la BASE anterior; no los sustituye por huellas nuevas. Las vistas BASE/APORTE/FINAL y la paleta tradicional se conservan.

**Validación pendiente:** build, suite de elevaciones, suite de cordilleras y suite general; cuatro tamaños y semillas técnicas; revisión visual de los seis casos anteriores, especialmente Enormous/1697481406 y Large/−234020843, −690013300. Las pruebas verifican alturas válidas, océano 0, conservación de 1–60 y de cumbres 100, máscaras/placas/intentos históricos y equivalencia exacta de geometría de cordilleras entre ambas bases. No se impone una reducción universal de componentes extremas. Riesgos por evaluar: dependencia del máximo de prominencia terrestre, posible traslado de algunas cumbres dentro de regiones elevadas y aumento de contraste local. No se declaran medidas nuevas de calidad ni rendimiento sin ejecución del usuario.

### P03-A.4 — Recuperación del relieve montañoso y equilibrio de alturas

**Estado: Implementado, pendiente de validación local y visual.** Commit solicitado: `refactor(worldgen): restore Pangaea mountain relief balance` (SHA en la entrega). Se mantiene P03-A.1 implementado sin aprobación global, P03-A.2 técnicamente validado, P03-A.3 rechazado visualmente y P03-A sin aprobación. No se inicia P03-B ni la consolidación P05.

**Auditoría del cambio P03-A.2 → P03-A.3:**

1. **Sistemas tectónicos:** la selección y agrupación no cambiaron y no consumen la elevación BASE. No hay un mecanismo en P03-A.3 que reduzca sistemas; la comparación local verificará conteos, IDs, geometría y serialización en las mismas semillas. Un número bajo observado puede ser anterior a esa calibración y no autoriza alterar selección ni imponer cuotas.
2. **Ejes y anchuras:** las fórmulas de geometría permanecen idénticas. El diff de P03-A.3 en el generador de cordilleras solo añadió captura comparativa y extrajo la misma composición vertical, sin cambiarla. Se comprueba igualdad exacta entre las tres bases.
3. **Alturas de cresta:** sí pueden cambiar. La composición sigue siendo `max(BASE, min(94, round(BASE+aporte)))`. Reducir BASE reduce FINAL salvo donde el recorte ya domina; una cordillera puede perder color blanco o prominencia sin perder su eje.
4. **Relieve intermedio:** 1–60 permaneció idéntico; P03-A.3 pudo contraer 61–84 hacia 60 según el soporte local y el máximo de prominencia mundial. Su corrección alcanzaba alturas moderadas, además de concentraciones ≥85. Esta es la dependencia que se corrige, no la selección tectónica.
5. **Extensiones anteriores:** las carencias en 1–60 son anteriores porque esa banda no cambió. La extensión precisa de las áreas 61–84 y su relación con cordilleras se medirá por caso; no se atribuyen todas las áreas poco estructuradas a P03-A.3.
6. **Efecto visual:** el umbral blanco sigue en 85. La pérdida visual puede combinar descenso numérico de crestas y cruce de ese umbral; no existe evidencia para asignar porcentajes a cada efecto. La vista de superficie consulta la elevación FINAL y su condición de cumbre; las vistas diagnósticas siguen mostrando valores reales.

**Cambio conceptual único:** recuperar las alturas regionales moderadas que P03-A.3 redujo, sin retroceder su banda extrema. Sea H2 la envolvente regional anterior y H3 la altura estructurada de P03-A.3. Si H2>H3, `t=clamp((H2−75)/10,0,1)` y `H4=round(H3+(1−t²*(3−2t))*(H2−H3))`; en los demás casos H4=H3. Hasta 75 se recupera toda la pérdida; entre 75 y 85 la recuperación se desvanece suavemente. Las alturas nuevas nunca bajan respecto a H3; sus celdas BASE ≥85 conservan exactamente máscara y valor, incluidas cumbres 100. No se impone una proporción de montañas ni se añaden sistemas o ruido. Los límites 75 y 85 corresponden a la franja elevada observable y al contrato de cumbres; no son cuotas de aceptación. FINAL puede ampliar cumbres alrededor de cordilleras existentes al recibir una BASE moderada más alta, por lo que su concentración aún requiere revisión.

`PangaeaFromPlatesGenerator.Version` pasa a `pangaea-from-plates-3`. La superficie termina antes de estas operaciones y no cambia. Placas, H04, perfiles, intentos, ejes, IDs, anchuras, clasificación y aporte solicitado de P03-A quedan intactos. El formato de guardado no cambia ni se reinterpretan partidas: se conservan sus alturas almacenadas. La fórmula de P03-A.3 permanece como paso de estructuración de una sola implementación operativa; su captura intermedia y la referencia P03-A.2 son diagnósticos temporales, no estrategias seleccionables ni mundos candidatos.

**Comparación reproducible:** `dotnet run --project GEvolve.Tests -- --pangaea-elevations` usa cuatro tamaños × 2026/42/−731; Large/689804754, 1195510322, 1443340172, −185224545; referencias Large/−234020843, −690013300 y Enormous/1697481406; y los casos Enormous/1324918567, 418579367, 1242668747. Una generación captura BASE y FINAL P03-A.2, P03-A.3 y actuales, componiendo todas con el mismo campo de levantamiento. Las pruebas reaplican solo la fase de cordilleras a las bases capturadas para verificar esa equivalencia; el juego no vuelve a generar mundos ni cordilleras para comparar.

Se registran sistemas detectados y serializados, ejes, cobertura de influencia frente a celdas realmente elevadas, bandas <40/40–60/61–75/76–84/85–94/95–100 dentro y fuera de corredores, alturas de ejes, pendiente media del corredor y conectividad ≥61/≥75/≥85. La prominencia se define como diferencia del eje frente a sus vecinos terrestres inmediatos y frente al entorno exterior de la influencia (media ponderada por aristas); son indicadores locales, no prominencia física de una cumbre en metros. La relación tectónica se informa con compresión H04 media y cercanía a contactos convergentes en el radio oficial de influencia. Se mantienen estadísticas de fuentes aproximadas de BASE, hashes de las tres versiones, recortes, diferencias entre vecinos y forma de componentes extremas. Las huellas históricas no se actualizan ni se eliminan aserciones.

**Pendiente:** compilación, suite de elevaciones, suite de cordilleras, suite general y revisión visual de los mismos mundos con BASE/APORTE/FINAL, RELIEVE: CORDILLERAS y PLACAS. Las nuevas pruebas protegen la banda extrema de BASE P03-A.3 y la geometría de sistemas en las tres versiones; no exigen una prominencia mínima ni un número arbitrario de montañas. Se esperan hombros y elevaciones intermedias más continuos alrededor de los sistemas existentes en las cuatro semillas desfavorables; no se promete cubrir regiones tectónicamente vacías. Riesgos: exceso de relieve moderado, variación de contraste de crestas y expansión de componentes ≥85 en FINAL por el aporte ya existente. Las cifras nuevas y la mejora visual siguen sin confirmar hasta la ejecución del usuario.

### P03-A.5 — Auditoría tectónica y distribución de cordilleras

**Estado:** diagnóstico implementado y ejecutado, no aprobación de P03-A. No se implementa P03-A.6. La autorización específica de esta auditoría permite ejecutar compilación y pruebas, como excepción a la regla ordinaria de ejecución reservada al usuario.

Instrumentación publicada en `test(worldgen): trace Pangaea tectonic mountain selection` (`7ebd325fbd44e38b611a0c797ccc5770118f1a4a`) y ampliada en `test(worldgen): quantify sub-summit mountain visibility` (`74db94f6a4bc4f848245bb0bcf0e882aa71d7b1d`). La traza opcional registra las decisiones existentes y no forma parte de State ni de los guardados. No cambia filtros, RNG, superficie, tectónica, ejes, anchuras, alturas, paleta ni shader.

**Evidencia del 2026-11-06:** [informe y respuestas](../GEvolve.Tests/ValidationReports/P03-A5_20261106_143813456Z_ed0d8bb9/REPORT.md), [resumen y entorno](../GEvolve.Tests/ValidationReports/P03-A5_20261106_143813456Z_ed0d8bb9/SUMMARY.md), [log numérico completo](../GEvolve.Tests/ValidationReports/P03-A5_20261106_143813456Z_ed0d8bb9/tectonic-audit.log). Sobre el segundo commit: `dotnet build` PASS (0 errores/advertencias) y `dotnet run --project GEvolve.Tests -- --pangaea-tectonic-audit` PASS. Las cinco semillas solicitadas en Large conservan exactamente el estado generado con/sin traza, incluidos sistemas persistibles, BASE, aporte, superficie y H04. No se ejecutó el juego gráfico ni se extrapola a otros tamaños.

**Conclusiones medidas:** 143 componentes candidatos, 41 seleccionados; los 102 rechazos son por componente/eje corto, ninguno por filtros compactos tras superar longitud. No hay mayor descarte sistemático en las desfavorables. `-1979952984` tiene escasez convergente; las otras dos desfavorables conservan convergencia y longitud de eje comparables a una favorable. Sus ejes tienen FINAL media 52–55 frente a 61/83 de las favorables; BASE y su coincidencia espacial son relevantes. Sin biomas, 79–87 % de sus ejes recibe el mismo color terrestre sub85 que el terreno ordinario en superficie. Ese hecho no basta para explicar todas las etiquetas visuales: también ocurre en una favorable. El informe distingue hechos, limitaciones e hipótesis y conserva las nueve regiones por semilla.

**Siguiente paso recomendado, sin implementarlo:** comparar FINAL/RELIEVE/SUPERFICIE al mismo zoom. No relajar selección ni imponer cuotas. Si se autoriza P03-A.6 sobre alturas, ensayar únicamente composición vertical dentro de corredores existentes con envolvente dependiente de convergencia y continuidad, contrastando prominencia y pendientes; si el defecto se limita a superficie, tratar primero su representación sub85 por separado.

**Validaciones anteriores:** la [ejecución P03-A.4](../GEvolve.Tests/ValidationReports/P03-A4_20261106_140522345Z_f747ba65/SUMMARY.md) reemplaza el pendiente técnico de la sección anterior: build, elevaciones y cordilleras PASS; suite general FAIL (13 suites PASS, dos fallos de baselines históricos de elevación). No se corrigieron fallos ni actualizaron baselines. Se mantiene pendiente la aceptación visual y global por el usuario.

### P03-A.6 — Calibración tectónica de la elevación de cordilleras

**Estado:** implementado, pendiente de validación visual y aprobación del usuario. Commit `feat(worldgen): calibrate tectonic mountain uplift in Pangaea`, SHA `ab7c78914e0153ba946f7d5be5b6c17dc6c66ae7`. [Informe completo](../GEvolve.Tests/ValidationReports/P03-A6_20261106_213733272Z_a40eedf0/REPORT.md), [resumen de ejecución](../GEvolve.Tests/ValidationReports/P03-A6_20261106_213733272Z_a40eedf0/SUMMARY.md) y [comparación numérica](../GEvolve.Tests/ValidationReports/P03-A6_20261106_213733272Z_a40eedf0/pangaea-uplift.log).

**Único cambio de generación:** envolvente vertical dentro de los corredores ya existentes. Con continuidad `C=L/(L+0.08*filas)` y convergencia efectiva `c=sqrt(mediaSistema*mediaLocalSuavizada)`, la cresta objetivo es `94*sqrt(C)*(1-exp(-c/2))`. Se mezcla con BASE mediante `(0.6+0.4*perfilLongitudinal)*perfilTransversalAnterior`; el aporte anterior se conserva cuando es mayor y los corredores superpuestos usan máximo, no suma. El objetivo no es un mínimo universal ni fija 85 a los ejes. El suavizado longitudinal usa sumas prefijas y no consume RNG.

Se mantienen los filtros y la propagación previa como autoridad exclusiva de influencia/propietarios. La referencia capturada **antes de editar el generador** en `3c95bc7ca7afee362ab732efceb8fe86536aabc5` verifica por hashes los 20 casos: cuatro tamaños × 2026/42/−731, cinco Large de P03-A.5 y referencias Large/−234020843, Large/−690013300, Enormous/1697481406. BASE, superficie, placas, H04 completo, intentos, sistemas, IDs, parejas, clasificación, ejes, anchuras y propietarios permanecen idénticos. `AffectedCells` refleja el nuevo incremento real. No se alteran paleta, shader, catálogos ni otros generadores; los baselines históricos no se actualizan.

`AdditiveFinalByCellId` conserva la comparación P03-A.4 en la misma pasada, sin volver a generar mundos ni ofrecer estrategias alternativas al jugador. El esquema `pangaea-ranges-2` y sus lectores anteriores permanecen intactos; la composición se identifica solo en diagnósticos como `tectonic-envelope-1`. Los saves existentes conservan sus alturas almacenadas.

**Resultados destacados:** altura media de ejes desfavorables 53.02→71.47, 52.03→63.28 y 54.53→75.94; no aumenta su blanco. Las favorables pasan 60.59→70.56 y 83.29→85.73; en la segunda el blanco aumenta 629→645 y el mayor componente ≥85, 113→160, pendiente de evaluación visual. La pendiente media de corredor aumenta en varias semillas, por lo que no se interpreta una mejora numérica como aprobación morfológica. El informe conserva mínimos/máximos/percentiles, ganancias, prominencias, bandas, componentes, regiones y tiempos reales.

**Validación del 2026-11-06:** build PASS (0 errores/advertencias), elevaciones PASS, cordilleras PASS, auditoría tectónica PASS y comparación vertical de 20 casos PASS. Suite general **FAIL: 14 suites PASS, 2 FAIL** (`TectonicMotion`, cuatro escenarios, y `ContinentalGeology`, doce escenarios, por hashes históricos de elevación). Sin discrepancias reportadas de superficie, placas ni intentos; no se modifican baselines ni validadores. No se declara aprobada la suite general ni P03-A.

Repetir desde la raíz con `powershell -NoProfile -ExecutionPolicy Bypass -File .\GEvolve.Tests\ValidationReports\commands.ps1 -ValidationSet TectonicCalibration -Label P03-A6`. La tarea autoriza expresamente ejecución .NET, como excepción a las reglas ordinarias; no se ejecuta el juego gráfico.

### P03-A.7 — Laboratorio de generación manual de Pangea

La solicitud del 2026-11-07 reorganiza el próximo paso: primero observar el mismo mundo por etapas; la auditoría integral previamente prevista pasa a P03-A.7.4. La primera entrega implementó P03-A.7.1; la autorización posterior implementa exclusivamente la interfaz P03-A.7.2 y permite validación automatizada de código, prohibiendo inspección gráfica por Codex.

| Subfase | Alcance | Estado |
|---|---|---|
| **P03-A.7.1 — Motor por etapas** | Separación mínima, sesión temporal, DAG, invalidación, equivalencia | **Implementado; equivalencia técnica verificada; pendiente de revisión del usuario** |
| **P03-A.7.2 — Interfaz manual** | Pantalla con componentes pixel-art existentes, semilla/tamaño, botones, estados, vistas parciales y ejecución asíncrona | **Implementado y validado técnicamente; pendiente de aprobación visual del usuario** |
| **P03-A.7.2.1 — Ajustes de interfaz** | Cuadrícula automática, placas/H04 agrupados, cinco botones, semilla aleatoria copiable, alturas y blanco >90 solo en superficie del laboratorio | **Implementado y validado técnicamente; pendiente de aprobación visual** |
| **P03-A.7.2.3 — Etiquetas legibles** | Medición de glifos, ajuste y centrado dentro del hexágono, sin números espaciados a distancia | **Implementado y validado técnicamente; pendiente de aprobación visual** |
| P03-A.7.3 — Comparación y diagnósticos | ANTES/DESPUÉS, diferencias, hashes y estadísticas enriquecidas visibles | Planificado; no implementado |
| P03-A.7.4 — Auditoría integral | Topología, superficie/costas, 16 placas/H04, BASE, sistemas, FINAL, persistencia, rendimiento, determinismo, capas y representación | Planificado; no ejecutado |

Commit solicitado: `refactor(worldgen): introduce staged Pangaea generation session`, SHA `46483ca299175c80ea2be0890bda8455f93305f7`. [Auditoría inicial y API](PANGAEA_STAGED_GENERATION.md), [informe](../GEvolve.Tests/ValidationReports/P03-A71_20261107_204048086Z_e7e415c6/REPORT.md), [resumen](../GEvolve.Tests/ValidationReports/P03-A71_20261107_204048086Z_e7e415c6/SUMMARY.md) y [hashes e invalidaciones completos](../GEvolve.Tests/ValidationReports/P03-A71_20261107_204048086Z_e7e415c6/pangaea-staged.log).

Las siete operaciones comparten los mismos generadores y cálculos. Superficie conserva el intento elegido y su presión inicial; BASE reutiliza el sufijo existente sin volver a elegir mapa. Sistemas conserva geometría, propietarios y muestras de la propagación; FINAL compone P03-A.6 sin repetir selección. No cambian fórmulas, RNG, superficie, BASE, ejes, anchuras, influencia ni FINAL. State y Game no reciben contratos incompletos ni cambios de interfaz.

Dependencias: cuadrícula→placas; placas→H04 y superficie; superficie→BASE; H04+superficie→sistemas; BASE+sistemas→FINAL. Repetir BASE conserva sistemas e invalida FINAL; repetir H04 conserva superficie/BASE e invalida sistemas/FINAL. Los descendientes se eliminan al comenzar una repetición. Cambiar configuración limpia toda la sesión y descarta publicaciones de trabajos obsoletos. Los snapshots son copias aisladas y solo FINAL vigente permite exportar un mundo guardable.

La referencia independiente se capturó antes de refactorizar en `9220a1da789936c63b6fb143743d57ed9dbb48b5`. **PASS:** 28 mundos Pangea (las siete semillas solicitadas en cuatro tamaños) coinciden exactamente con automático y referencia original, más 12 controles de otros tipos. También pasan repetición, invalidación por grafo, snapshots/exportaciones aisladas, cancelación/reinicio asíncrono y guardado/carga. Medium/−174327693 conserva el rechazo de intento 0 y la aceptación de intento 1. No se actualizan los baselines históricos.

Repetir P03-A.7.1 con `powershell -NoProfile -ExecutionPolicy Bypass -File .\GEvolve.Tests\ValidationReports\commands.ps1 -ValidationSet StagedPangaea -Label P03-A71`. En esa entrega no se ejecutó Game ni se añadió pantalla. La interfaz posterior consume snapshots por revisión y campos realmente disponibles. P03-A permanece sin aprobación y P03-B no inicia.

#### P03-A.7.2 — Interfaz implementada

Commit `feat(game): add interactive Pangaea generation laboratory`, SHA `e231bbf26f9ba81865ea9bd2158838771c639780`. [Guía y arquitectura de Game](../GEvolve.Game/PANGAEA_LABORATORY.md), [informe técnico](../GEvolve.Tests/ValidationReports/P03-A72_20261107_220535936Z_c4ebdf57/REPORT.md) y [resumen de ejecución](../GEvolve.Tests/ValidationReports/P03-A72_20261107_220535936Z_c4ebdf57/SUMMARY.md).

Menú **LABORATORIO DE PANGEA**, configuración con confirmación de reinicio, siete operaciones reales y botones dependientes del DAG. El renderer existente admite snapshots parciales sin construir estados persistibles ficticios; muestra cuadrícula, placas/H04, superficie, BASE, sistemas y FINAL. Selección de vistas vigentes, inspección con pendiente/invalidado/cero real, duración y errores. La interfaz almacena las representaciones por revisión; generación y copia de snapshots se hacen fuera del hilo de ventana. Regenerar BASE conserva sistemas; editar configuración oculta capas hasta aplicar/cancelar; las terminaciones abandonadas no se publican.

Validación automatizada mediante `-ValidationSet LaboratoryPangaea -Label P03-A72`: build PASS (cero advertencias/errores), laboratorio PASS en cuatro tamaños, equivalencia por etapas PASS en 28 Pangea + 12 controles; general **16 PASS / 2 FAIL**. Las 16 diferencias históricas de elevación en TectonicMotion/ContinentalGeology coinciden exactamente con P03-A.7.1; baselines intactos. Codex no inicia Game, no crea contexto GL, capturas ni automatización visual, y no aprueba apariencia o calidad geográfica. El usuario ejecuta el juego e inspecciona cada capa. P03-A.7.3 queda planificado para comparación ANTES/DESPUÉS y diagnósticos visibles; P03-A.7.4 y P03-B no se ejecutan.

#### P03-A.7.2.1 — Controles y elevaciones del laboratorio

Commit `feat(game): simplify Pangaea laboratory controls and elevation display`, SHA `f862a0c265cd9c341a761c5c0606c6f107dc1f2b`. [Informe](../GEvolve.Tests/ValidationReports/P03-A721_20261107_223547253Z_a9387211/REPORT.md), [resumen](../GEvolve.Tests/ValidationReports/P03-A721_20261107_223547253Z_a9387211/SUMMARY.md), [guía de uso](../GEvolve.Game/PANGAEA_LABORATORY.md).

La cuadrícula oficial inicia automáticamente con cada sesión; conserva su etapa interna. Cinco acciones visibles: 01 placas/movimiento, 02 superficie, 03 BASE, 04 sistemas y 05 FINAL. La acción 01 ejecuta placas y H04 secuencialmente sin combinar generadores. Cancelar impide comenzar H04 si sigue en cola. El motor conserva las siete etapas, sus dependencias y resultados. Placas y direcciones tienen vistas separadas, además de compresión/extensión/cizalla.

ALEATORIA con dado crea un entero con signo de 32 bits usando aleatoriedad criptográfica de Game, sin tocar el RNG determinista ni iniciar un mundo completo. Utiliza el mismo borrador/confirmación; Ctrl+C copia la semilla y Ctrl+V con foco reemplaza su texto. MOSTRAR ALTURAS utiliza datos reales de la vista, solo con escala ≥13 y hasta 1000 etiquetas visibles. En SUPERFICIE se usa FINAL vigente o BASE en su ausencia; tierra >90 blanca, tierra ≤90 con color terrestre, océano azul. Sin alturas no se inventan números ni blanco; invalidar FINAL retira ese campo inmediatamente. El cambio de umbral es exclusivo del laboratorio: la paleta global y los guardados siguen intactos.

Solo validación automatizada de código; Codex no abre Game, no ejecuta pruebas visuales ni certifica apariencia o geografía. P03-A.7.2.1 queda pendiente de aprobación visual del usuario. P03-A.7.3 no comienza.

Evidencia: laboratorio PASS en cuatro tamaños; equivalencia PASS en 28 Pangea + 12 controles; general **16 PASS / 2 FAIL históricos**, con los mismos 16 registros de elevación que P03-A.7.2. La compilación inicial pasó con CS8602 en una prueba; se corrigió mediante guarda explícita en `5bfbfc96bcb896615679d78dabe091f60d308164`. La [comprobación posterior](../GEvolve.Tests/ValidationReports/P03-A721-nullable_20261107_224554334Z_1be5ccd6/SUMMARY.md) pasa build sin advertencias/errores y laboratorio. No se modifican baselines ni se presenta la general como aprobada.

#### Rediseño lateral y flujo secuencial — seguimiento de P03-A.7.2.1

La solicitud posterior sustituye las reglas de interacción anteriores: controles en columna izquierda fija; título, tipo mapa, tamaño, semilla, vista, generación manual, información de celda y salida en ese orden. Tipo Pangea explícitamente único. Mapa con viewport propio a la derecha, sin controles superpuestos. Inspector y salida fijos abajo; controles/detalles desplazables en ventanas bajas. Las cinco acciones visibles requieren todos los pasos previos en el controlador; Engine conserva sus siete etapas y DAG independiente.

El dado borra automáticamente sesión, capas, selección y trabajo en curso, y prepara únicamente la nueva cuadrícula, sin confirmación. Cambiar tamaño también reinicia; editar semilla limpia inmediatamente y espera iniciar con un texto válido. Esta entrega dibujaba alturas a escala ≥13 y espaciadas al alejarse; P03-A.7.2.3 sustituye esa regla por ajuste geométrico sin muestreo. Se mantienen BASE/APORTE/FINAL según vista y FINAL→BASE en otras, blanco de superficie solo en tierra >90 y guardados/paletas globales.

Implementación: `86a7effcf8d0425b4c669b588590cdb4a93397a3`, `feat(game): reorganize Pangaea laboratory sidebar and sequential flow`. Validación aislada preparada en `51ca6458b7445334df4961ce7903640959ca3490` (opción de artefactos por ejecución y prueba de anclaje periódico E/O). [Guía](../GEvolve.Game/PANGAEA_LABORATORY.md), [informe](../GEvolve.Tests/ValidationReports/P03-A721-sidebar_20261108_013348836Z_784fadcd/REPORT.md), [resumen](../GEvolve.Tests/ValidationReports/P03-A721-sidebar_20261108_013348836Z_784fadcd/SUMMARY.md). La compilación normal inicial encontró DLLs bloqueadas por el juego abierto: se conserva [su fallo](../GEvolve.Tests/ValidationReports/P03-A721-sidebar_20261108_013206845Z_e1030014/SUMMARY.md); Codex no cierra ni inspecciona esa instancia. La validación usa `--artifacts-path` bajo bin ignorado. No se cambian algoritmos ni baselines; aprobación visual pendiente, P03-A.7.3 no iniciado.

Resultado técnico: build PASS sin advertencias/errores; laboratorio cuatro tamaños PASS; equivalencia 28 Pangea + 12 controles PASS; general **16 PASS / 2 FAIL históricos**, con exactamente los mismos 16 registros de elevación del informe anterior. Se conserva el fallo de copia inicial y la ejecución aislada completa. Implementado y validado técnicamente, pendiente de aprobación visual exclusiva del usuario.

#### P03-A.7.2.3 — Etiquetas dentro de hexágonos legibles

Commit `fix(game): constrain elevation labels to readable hex cells`, SHA `d507714956546652e8d7dc640057e998ca41ec65`. [Informe](../GEvolve.Tests/ValidationReports/P03-A723_20261108_025945937Z_c306be7e/REPORT.md), [resumen](../GEvolve.Tests/ValidationReports/P03-A723_20261108_025945937Z_c306be7e/SUMMARY.md), [log del laboratorio](../GEvolve.Tests/ValidationReports/P03-A723_20261108_025945937Z_c306be7e/pangaea-laboratory.log).

Se elimina el stride/muestreo a distancia y el umbral fijo de escala. La dimensión de cada número procede de los píxeles encendidos de la misma fuente bitmap; el texto se centra horizontal y verticalmente por su caja real y se ajusta entre 1.25 y 1.5 px por píxel bitmap. Debe caber con 2 px de margen dentro del hexágono flat-top y del viewport; en caso contrario no se dibuja. Todas las celdas visibles que cumplen esa condición se incluyen, sin tope ni selección espaciada. Blanco/negro se elige por el fondo real de la capa. Valores, inspector, panel, botones, cámara y generadores se conservan. Solo comprobaciones técnicas, aprobación visual exclusiva del usuario pendiente; P03-A.7.3 no iniciado.

Resultado técnico: build PASS sin advertencias/errores; laboratorio y geometría de etiquetas PASS en cuatro tamaños; equivalencia 28 Pangea + 12 controles PASS; general **16 PASS / 2 FAIL históricos** con los mismos 16 registros de elevación. Los 28 hashes de comparación Pangea son idénticos al reporte anterior. No se actualizan baselines ni se declara aprobada la general o la apariencia.

## Arquitectura protegida y dependencias

```text
Configuración (tipo + tamaño + semilla)
  -> Cuadrícula / topología hexagonal
  -> 16 placas tectónicas
  -> Movimientos, fronteras e interacciones H04
  -> Estrategia de superficie de Pangea [CONSERVAR]
  -> Elevación inicial de Pangea [BASE]
  -> Accidentes geográficos terrestres P03 (en subhitos)
  -> Batimetría P04 y consolidación de relieve P05
  -> Temperatura P06
  -> Precipitaciones / humedad P07
  -> Hidrología P08
  -> Biomas del catálogo P09
  -> Recursos del catálogo P10
  -> Validación, persistencia y consumo por Game P11
```

El diagrama representa la **secuencia objetivo**, no afirma que todos los módulos estén integrados hoy en el orquestador. P03-A.7.1 separa superficie y elevación en etapas conservando su salida; temperatura, lluvia y biomas existen como módulos especializados separados. La introducción de P03 debe conservar la salida actual y evitar acoplar los demás tipos de mapa.

## Reglas de trabajo

1. Trabajar en entregas pequeñas: un subhito por autorización.
2. El **usuario** ejecuta `dotnet build`, `dotnet run --project GEvolve.Tests` y `dotnet run --project GEvolve.Game`; agentes de desarrollo no ejecutan compilaciones ni pruebas.
3. Los cambios de superficie y tectónica están prohibidos salvo aprobación expresa. Los hashes de elevación pueden cambiar con el relieve, pero deben auditarse antes de aceptar nuevos baselines.
4. No crear mapas PNG/SVG/CSV/HTML ni laboratorios externos. Inspección visual en Evolve.
5. La escala actual es tierra 1–100, océano 0; la batimetría tendrá representación separada sin redefinir el cero silenciosamente.
6. El cierre del roadmap requiere evidencia técnica **y** evaluación visual. Evitar adoptar un modelo solo porque pasa los tests.

Validación técnica P03-A.7.1: build y cinco suites específicas PASS; general 15 PASS / 2 FAIL históricos (TectonicMotion: 4 escenarios; ContinentalGeology: 12). Las 16 diferencias son de elevación; baselines intactos. Véase el informe enlazado para códigos y duraciones.

## P09.1 — Integración inicial de biomas

**Implementado; pendiente de calibración y aprobación visual.** Pangea continúa FINAL → pendientes/temperatura → precipitación/humedad → biomas/subbiomas del catálogo original. Automático y laboratorio comparten las mismas operaciones; el laboratorio conserva 01–05 y añade 06–08, vistas ambientales e inspector. Se reutilizan TerrainSlope, TemperatureGenerator, PrecipitationGenerator y BiomeClassifier sin modificar sus fórmulas, IDs, rangos ni prioridades. La geología permanece intacta; los otros tipos mantienen su ruta. Persistencia opcional existente sin cambios de esquema. Véase [auditoría y contrato](PANGAEA_ECOLOGY_INTEGRATION.md).

La validación técnica cubre cuatro tamaños, elegibilidad JSON, determinismo, sensibilidad FINAL, guardado/carga, invalidación, equivalencia geológica y regresión de otros mapas. Los informes publican aridez y resultados inesperados de prioridades para revisión posterior; no se imponen cuotas ni se recalibra. No se aprueba la distribución de biomas ni P03-A; baselines históricos intactos. No se inicia flora, fauna ni minerales. Validación visual exclusiva del usuario.
