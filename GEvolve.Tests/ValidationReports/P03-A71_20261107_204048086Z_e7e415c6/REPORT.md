# P03-A.7.1 — Motor de generación manual de Pangea

**Alcance:** siete operaciones reales, sesión temporal, dependencias, invalidación y equivalencia. No se implementa interfaz de laboratorio, ANTES/DESPUÉS visual ni auditoría integral. P03-A no aprobado; P03-B no iniciado.

**Código validado:** `46483ca299175c80ea2be0890bda8455f93305f7`, `refactor(worldgen): introduce staged Pangaea generation session`, rama `worldgeneration_pangea`. HEAD inicial local/remoto verificado: `0da9da7546f811f320fde5fe8b6ba399e7a6c811`. La auditoría inicial se escribió antes de modificar generadores y se conserva en [PANGAEA_STAGED_GENERATION.md](../../../GEvolve.Engine/PANGAEA_STAGED_GENERATION.md).

## Orquestador anterior y nueva separación

El servicio validaba el perfil oficial, creaba cuadrícula, placas y H04, y probaba hasta cuatro intentos Pangea. Cada llamada calculaba superficie y BASE juntas antes de validar la superficie. La selección elegía el primer intento con tierra 30–55 %, una componente y dominio 100 %. No había evaluación de altura o aspecto para sustituir mapas desfavorables.

La superficie termina antes del suavizado de presión y normalización de BASE. El RNG mutable de ese intento ya se consumió; el resto usa ruido determinista por coordenadas/semilla/intento. Se extrae ese corte, conservando la superficie y presión inicial del intento aceptado. BASE reutiliza exactamente el sufijo anterior, sin volver a elegir superficie, placas o intento. Las BASE de intentos rechazados dejan de calcularse porque no intervenían en la aceptación ni en el siguiente RNG.

En cordilleras se extraen selección/propagación y composición. La prioridad de propagación, filtros, ejes, perfiles, anchuras y propietarios siguen intactos. Se almacenan todas sus muestras vigentes `(celda, aporte anterior, cresta, mezcla)` en el orden original. FINAL aplica las mismas operaciones P03-A.6 a BASE con esas muestras. No se aproxima una envolvente con una sola muestra por celda: la dominante puede depender de BASE. No se repite el recorrido para cambiar de BASE ni se duplica una fórmula.

El automático y el manual usan esta misma sesión a través de `WorldGenerationService`. Las APIs `Generate`/`Apply` anteriores de los generadores conservan su funcionalidad componiendo las operaciones extraídas. No cambian las constantes, perfiles públicos, esquemas de State, Game, paleta, shader, catálogos ni las rutas de otros tipos.

## Etapas, dependencias e invalidación

| Etapa | Dependencias directas | Resultado real | Repetir invalida |
|---|---|---|---|
| 01 Cuadrícula | Configuración oficial | HexGrid y HexTopology | 02–07 |
| 02 Placas | 01 | 16 placas | 03–07 |
| 03 Movimiento/H04 | 02 | Movimientos, contactos y esfuerzos | 06–07; conserva 04–05 |
| 04 Superficie | 02 | Intento aceptado, tierra/océano, estadísticas e historial | 05–07; conserva 03 |
| 05 BASE | 04 | Alturas P03-A.4 y fuentes | 07; conserva 06 |
| 06 Sistemas | 03 y 04 | Ejes, anchuras, influencia, decisiones y muestras | 07 |
| 07 FINAL | 05 y 06 | APORTE, FINAL, metadatos y mundo completo | Solo 07 |

El grafo es explícito y consultable. Sistemas puede ejecutarse antes de BASE. Superficie usa sus vectores históricos, no el H04 oficial; por eso puede preceder a H04. La prueba manual utiliza deliberadamente esos órdenes para demostrar las dependencias reales.

Los descendientes se borran al **comenzar** una repetición. Los estados distinguen pendiente, en ejecución, completado, invalidado y fallido; disponibilidad, hashes, revisiones de entradas, tiempo y error son metadatos reales. Un snapshot no contiene alturas ni sistemas ficticios cuando falta su etapa. Los intentos rechazados registran su perfil real, sin presentarlos como superficie aceptada.

Cambiar semilla/tamaño mediante `Reset` elimina todas las capas e incrementa la revisión. Un trabajo anterior puede acabar su cálculo, pero su identificador ya no permite publicar datos ni errores. La cancelación se comprueba antes del cálculo y al publicar; no modifica los bucles geográficos. `ExecuteAsync` trabaja fuera del hilo solicitante. `GetStages` es ligero; `GetSnapshot` y `GetCompletedWorld` devuelven copias independientes, incluida la cuadrícula y los diagnósticos anidados. La exportación de un mundo persistible requiere FINAL completado y vigente.

## Evidencia independiente de equivalencia

Antes de refactorizar se publicó localmente el capturador `9220a1da789936c63b6fb143743d57ed9dbb48b5`, con generación todavía idéntica al HEAD inicial. Su [resumen](../P03-A71-reference_20261107_203038633Z_bec46dbb/SUMMARY.md) y [log](../P03-A71-reference_20261107_203038633Z_bec46dbb/staged-reference.log) contienen **40 referencias**:

- Las siete semillas solicitadas en Small, Medium, Large y Enormous: 28 mundos Pangea.
- Continents, Archipelagos y SmallContinents, semilla 2026, en los cuatro tamaños: 12 controles.

`PangaeaStagedReference.txt` conserva esos hashes sin reemplazar los baselines históricos. Incluye cuadrícula, intento, estado persistible completo, estadísticas, superficie, placas, H04, BASE, APORTE, FINAL y sistemas completos. La comprobación del automático contra esta referencia evita que un cambio compartido por automático/manual pase inadvertido. El manual compara además los bytes JSON completos de LandmassState y los arrays BASE/APORTE/FINAL; la igualdad de sistemas incluye IDs, ejes, anchuras, clasificación, influencia y celdas afectadas.

Medium/-174327693 acepta el intento **1**, por lo que la muestra cubre rechazo de intento 0 seguido de aceptación. No se generaron semillas alternativas para sustituir resultados desfavorables. El camino de agotamiento de los cuatro intentos se conserva y revisa estáticamente; no se afirma haberlo ejercitado con esta muestra.

Las pruebas repiten BASE en los 28 mundos y verifican que las revisiones/hashes de cuadrícula, placas, H04, superficie y sistemas no cambian. En cada tamaño repiten también H04, superficie, placas y cuadrícula, comprueban qué capas desaparecen y reconstruyen exactamente el mismo mundo. Añaden guardado/carga real del mundo manual completo, mutación de snapshots/exportaciones, configuración inválida, orden inválido, cancelación y reinicio de semilla+tamaño con trabajo asíncrono anterior.

## Reproducción y límites

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\GEvolve.Tests\ValidationReports\commands.ps1 -ValidationSet StagedPangaea -Label P03-A71
```

El bypass es exclusivo del proceso; no se cambia la política del sistema. Los comandos nativos, códigos, entorno y duraciones se conservan en [SUMMARY.md](SUMMARY.md). Los hashes, dependencias, tiempos por etapa, historial de intentos e invalidaciones están en [pangaea-staged.log](pangaea-staged.log). La captura anterior solo se reproduce desde su commit y snapshot originales.

- Los snapshots copian O(N) datos para aislar consumidores. P03-A.7.2 debe almacenarlos por revisión, no pedir copias completas en cada frame.
- Las muestras de propagación añaden memoria proporcional a los pasos que ya visitaba el algoritmo. No se introduce un solucionador ni una simulación temporal.
- La cancelación no mata una operación a mitad de su bucle; descarta su resultado al finalizar. Mientras tanto se conserva la disponibilidad de la futura interfaz y la integridad de la configuración nueva.
- Los tiempos registrados son observaciones de una ejecución, con JIT/caché/carga; no un benchmark controlado ni una garantía de igualdad de coste respecto a P03-A.6.
- No hay botón ni pantalla nueva todavía. El usuario seguirá evaluando el resultado automático dentro de Evolve; la interfaz manual pertenece exclusivamente a P03-A.7.2.

## Plan de P03-A.7.2 y posteriores

P03-A.7.2 integrará una pantalla pixel-art accesible desde Evolve, panel de semilla/tamaño, botones según `CanExecute`, estados y tiempos, ejecución asíncrona y vistas adaptadas a los campos disponibles. Debe usar snapshots de la revisión vigente y no fabricar LandmassState incompletos para los renderers actuales. No requiere otro generador.

P03-A.7.3 añadirá ANTES/DESPUÉS, diferencias numéricas y diagnósticos enriquecidos sobre los hashes/estadísticas existentes. P03-A.7.4 realizará la auditoría integral con el laboratorio funcional. Ninguna de estas fases se implementó en esta entrega.

## Resultados de la ejecución

Los resultados siguientes proceden de los logs completos de esta ejecución, sin actualizar expectativas históricas.

### Equivalencia de los 28 mundos de aceptación

En cada fila, automático y manual coincidieron con el hash previo a la refactorización y entre sí. `STAGED_EQ` conserva también los hashes separados de superficie, placas, H04, BASE, APORTE, FINAL, sistemas y estadísticas.

| Tamaño / semilla | Intento aceptado | SHA-256 del estado persistible completo |
|---|---:|---|
| Small/-1387338765 | 0 | E10739D08DC71995BB6CF72D7B119090376D2563B98C84085B98D78ECC879B5F |
| Small/-132790047 | 0 | 7D3DF1BA3F6AD1C6A83C0B65E76C47900BA95BAD9DC369708D9440EBAABEBAE9 |
| Small/-714216669 | 0 | 74D65E351E099FFA42A0B85CA865D60F6A2925915661991A00B43A36E0B1D9AB |
| Small/-2024691550 | 0 | E0D6BEEFAF26C534F1069C944D458F1373FBCC6BE4F1B1E29A549E8973F4488A |
| Small/-174327693 | 0 | 093EE9ABAA01333B9DF3A3D97D4585E8B7035B9B9771955E44097E356068DC6E |
| Small/-1979952984 | 0 | 209A381DF68706304C1AF2EE0AD03C42FC8CA6F52CB5A88CCC227ECD562C9F70 |
| Small/2076374193 | 0 | 208116414B80709145DC91DCB3FC12384FE36C2C90753FF5EFF53D840A1EE030 |
| Medium/-1387338765 | 0 | AF582F34DA90B4060EE19321C3A93D2824901F917A2FFA18CD0E8A23F9783324 |
| Medium/-132790047 | 0 | F1783F97DF71BE0EB5F94E4DE1E1AF201B5FA6C70616F243FAD261F0273D0055 |
| Medium/-714216669 | 0 | 3A8C014CC9226EE3405969E66CBDBE0D15B967E0B5113C26806B269568716B26 |
| Medium/-2024691550 | 0 | DAF33B7855383E6C920237B67A62512FEA1B10CA93919AA8C443B58D6485E80D |
| Medium/-174327693 | 1 | 69EAD7048B9AD44C8BD521CE4914BDE0307B3BA63BC023AA74C96354352B5AFC |
| Medium/-1979952984 | 0 | 2E54CE928DFF4ACB3C9FC43C7C8CBD0346C5084F3D7F4D650838F489B8CBAC3E |
| Medium/2076374193 | 0 | 6FD320471E3FA0E8F41B03C4145FECAED7A08960BB95AD5353DE86DF14585267 |
| Large/-1387338765 | 0 | FC6AA5BF55D2213C4B7EAA689FF035E4B00169BE7BCDBEA59B0D3F437F825C9C |
| Large/-132790047 | 0 | EC512A51FE2F3951A32F2A3B212F00A04B6EE8E95A5AF9F8B5239279535B5D91 |
| Large/-714216669 | 0 | 4DE99925BD53AC0CEA826913F3F1AD424D90B817353543A6BE8FA3452352D92D |
| Large/-2024691550 | 0 | 4343C718DD1A4504CE61B3E23837A45A1590BEC4917A2671D5DDF0DF197F6EA2 |
| Large/-174327693 | 0 | 221507D0FB27E8364268FFF3C85FD752217AD18AFF7D2E34B2A9E5BDE45F2229 |
| Large/-1979952984 | 0 | C986886BB7F4D2EBDBCD628F85D305556D88819A79FE277A92CD869C43099C31 |
| Large/2076374193 | 0 | 6AFABA060F9FBB890A82EB0540C28DE6EC0A0D744CAA3CC84765407312286BC4 |
| Enormous/-1387338765 | 0 | F91B85E3A0BE51BA2EF11D1749684FE0D6F0247EC37FBD722781D4BF24FF5044 |
| Enormous/-132790047 | 0 | D43775D0EE80CAC777ED60E59A4887CA7DC41690E5F299D5F0E6A7B30409DF6F |
| Enormous/-714216669 | 0 | 1F277BF68A5C93A90A5C279EEF4C9176A0EBD9F31127C605E745BAD528090A93 |
| Enormous/-2024691550 | 0 | C4667A3D19EFEC348EEC44AE4AED092C7898B8E79DC82A5F5CBB98932AACC6B9 |
| Enormous/-174327693 | 0 | 083A21E50C9CD18D51715D8AD880973DB98D8C25ACB1ADCB916BFE7849DADAC0 |
| Enormous/-1979952984 | 0 | 33BF5D06539F199027D30C48CE598C0044BE7B303103C19D4C4B07BAE94E89D6 |
| Enormous/2076374193 | 0 | AD214965AD767DA69DA33738D50C5B585652D025CCE91E8EC6620D0E98D10C38 |

Los 12 registros `STAGED_OTHER_PASS` confirman igualdad con la referencia en Continents, Archipelagos y SmallContinents a los cuatro tamaños. Las 28 repeticiones de BASE tienen registro `STAGED_INVALIDATION`; las cuatro pruebas del grafo completo y guardado/carga, `STAGED_GRAPH_PASS`. La prueba asíncrona y de aislamiento finalizó con `STAGED_ASYNC_PASS`.

### Observaciones de ejecución por etapa

Las duraciones manuales incluyen el hash de la etapa, pero excluyen copiar snapshots/exportaciones y renderizar una interfaz (no implementada). La generación automática no calcula esos hashes ni realiza las copias de exportación. No comparar ambas cifras como si midieran el mismo trabajo.

| Etapa | Mínimo ms | Máximo ms |
|---|---:|---:|
| Cuadrícula | 42.718 | 240.352 |
| Placas | 120.483 | 657.595 |
| Movimiento/H04 | 98.310 | 2014.026 |
| Superficie | 50.582 | 380.126 |
| BASE | 35.287 | 293.203 |
| Sistemas | 2.200 | 19.202 |
| FINAL | 22.945 | 112.984 |

| Tamaño | Casos | Generación automática ms (mín.–máx.) | Muestras retenidas (mín.–máx.) |
|---|---:|---:|---:|
| Small | 7 | 386.328–565.160 | 305–3019 |
| Medium | 7 | 558.504–726.620 | 1984–4832 |
| Large | 7 | 778.737–1024.868 | 1953–6744 |
| Enormous | 7 | 1359.684–2190.288 | 3312–15848 |

El máximo observado fue 15848 muestras; cada muestra contiene una celda y tres doubles. No se presenta ese contador como una medición de memoria total de la sesión. El tiempo máximo observado de H04 incluye serialización/hash y posibles pausas del entorno; no demuestra un cambio en su algoritmo.

En Medium/-174327693, intento 0: fracción terrestre 0.5524071856287425, una componente, rechazo por superar 0.55. Intento 1: fracción 0.4, una componente, aceptado. Se conservan los hashes de ambas superficies examinadas en `STAGED_STEPS`, sin promover la rechazada a resultado vigente.

### Resultado consolidado

Ejecución UTC: 2026-11-07 20:40:48–20:57:00; SDK .NET 10.0.402. Fuente validada limpia: `46483ca299175c80ea2be0890bda8455f93305f7`. Resumen y códigos reales en [SUMMARY.md](SUMMARY.md).

| Command | Status | Native exit code | Duration (seconds) | Complete log |
|---|---|---:|---:|---|
| `dotnet build` | PASS | 0 | 22.055 | [build.log](build.log) |
| `dotnet run --project GEvolve.Tests -- --pangaea-staged` | PASS | 0 | 134.734 | [pangaea-staged.log](pangaea-staged.log) |
| `dotnet run --project GEvolve.Tests -- --pangaea-elevations` | PASS | 0 | 56.682 | [pangaea-elevations.log](pangaea-elevations.log) |
| `dotnet run --project GEvolve.Tests -- --mountain-ranges` | PASS | 0 | 50.212 | [mountain-ranges.log](mountain-ranges.log) |
| `dotnet run --project GEvolve.Tests -- --pangaea-tectonic-audit` | PASS | 0 | 22.138 | [tectonic-audit.log](tectonic-audit.log) |
| `dotnet run --project GEvolve.Tests -- --pangaea-uplift` | PASS | 0 | 43.232 | [pangaea-uplift.log](pangaea-uplift.log) |
| `dotnet run --project GEvolve.Tests -- --all-suites` | FAIL | 1 | 641.101 | [all-suites.log](all-suites.log) |

La suite general termina con código 1: **15 suites PASS / 2 FAIL**. TectonicMotion falla en cuatro escenarios Pangaea/2026; ContinentalGeology en doce escenarios Pangaea (2026, 42, -731 por cada tamaño). Las 16 diferencias reportadas son hashes históricos de elevación, ya documentados en P03-A.6; los baselines permanecen intactos. No hubo errores de compilación ni advertencias. No se presenta la suite general como aprobada.

La ejecución específica y la incluida en all-suites confirman equivalencia original/automático/manual, invalidación, persistencia y aislamiento. No se ejecutó el juego interactivo; no hay validación visual ni aprobación de P03-A.
