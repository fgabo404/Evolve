# Continentes: selección tectónica con garantía de cordillera

La cuadrícula, las 16 placas, sus vectores y sus campos de esfuerzo se calculan **una sola vez**. `OrogenicOpportunityAnalyzer` recorre los límites originales y agrupa tramos convergentes vecinos del mismo par de placas. Descarta tramos cortos y contactos dominados por cizalla; la puntuación de cada margen combina longitud e intensidad de convergencia. Las divergencias y transformantes no cuentan como oportunidad de compresión.

`PlateGroupSurfaceGenerator` mantiene de dos a seis grupos de placas conectadas. Para cada intento determinista, combina separación entre núcleos, área de placas y potencial orogénico. La acreción de placas vecinas favorece contactos convergentes sin exigir que todos emerjan. El nivel del mar global conserva aproximadamente 35 % de tierra, grandes océanos, costas irregulares y tamaños continentales distintos. El número de intento solo cambia la selección continental; no cambia semilla, geometría, movimiento ni elevación numérica de las placas.

`WorldGenerationService` evalúa **como máximo ocho** agrupaciones. Rechaza un candidato si un grupo no posee una masa terrestre principal reconocible (al menos 1 % del mapa y 50 % de la tierra de ese grupo en su componente mayor). El generador orogénico existente calcula entonces el relieve. Un sistema principal exige margen convergente conectado de al menos ocho aristas, suficiente levantamiento terrestre y **al menos 24 hexágonos de montaña efectiva** (elevación 44 o mayor). Pequeño y Mediano aceptan el primer candidato válido. Grande y Enorme buscan dos sistemas principales; si no aparecen en ocho intentos, aceptan el mejor candidato con uno. Si no aparece ninguno, la generación falla con un mensaje explícito: no se altera la semilla ni se inventan montañas.

`ContinentalSurfaceState.Selection` conserva número de candidatos evaluados, intento elegido, márgenes disponibles, cantidad de sistemas principales y secundarios y motivo de cada rechazo. Cada `TectonicReliefSystem` guarda la extensión terrestre afectada, cantidad de hexágonos montañosos, longitud del eje montañoso sobre tierra y elevación máxima y promedio. Los guardados anteriores sin estos diagnósticos siguen siendo legibles.

## Validación de 48 mundos

La tabla indica **intento elegido / agrupaciones evaluadas / cordilleras principales**. El intento inicial es 0.

| Semilla | Pequeño | Mediano | Grande | Enorme |
| ---: | :---: | :---: | :---: | :---: |
| 2026 | 0/1/2 | 0/1/1 | 0/1/2 | 0/1/3 |
| 42 | 0/1/2 | 0/1/1 | 0/1/2 | 0/1/3 |
| -731 | 0/1/1 | 0/1/3 | 4/5/3 | 0/1/3 |
| 7 | 0/1/1 | 0/1/2 | 3/4/3 | 0/1/3 |
| 99 | 0/1/3 | 0/1/1 | 0/1/3 | 0/1/2 |
| 12345 | 0/1/2 | 0/1/3 | 1/2/2 | 1/2/2 |
| 314159 | 0/1/3 | 0/1/2 | 4/5/3 | 0/1/3 |
| -2026 | 0/1/3 | 0/1/1 | 0/1/3 | 1/2/2 |
| 8675309 | 0/1/1 | 0/1/3 | 0/1/2 | 0/1/2 |
| 0 | 0/1/2 | 0/1/1 | 1/2/2 | 4/8/1 |
| 1 | 0/1/1 | 1/2/1 | 1/2/3 | 0/1/2 |
| 123456789 | 0/1/2 | 0/1/2 | 1/2/2 | 0/1/2 |

Los 48 mundos generaron al menos una cordillera principal; 23 de 24 Grandes o Enormes tienen dos o tres. Diez selecciones necesitaron un intento posterior. Enorme promedió **1,826 ms** por generación en el entorno de prueba; el caso que agotó ocho candidatos tardó **3,704 ms**. Estos tiempos dependen del equipo y son solo una referencia.

La huella histórica `ApprovedContinentalSurfaceBaseline.txt` se conserva sin modificaciones y sigue comprobando los IDs de las placas. Tras validar las nuevas superficies, `ApprovedContinentalSelectionBaseline.txt` fija superficie, agrupación, corteza e identidad para cuatro tamaños y tres semillas. Entre ambas huellas, **11 de 12** superficies, agrupaciones, cortezas e identidades cambiaron por la nueva selección; **0 de 12** mapas de placas cambiaron. Las huellas de los otros tres tipos de mundo no cambian.

La garantía es **condicional al éxito de generación**. No existe prueba de que cualquier conjunto imaginable de placas y movimientos permita una cordillera terrestre con ocho agrupaciones; esas configuraciones fallan explícitamente. No se regeneran placas, se alteran velocidades ni se crean montañas por cuota. El modelo sigue siendo estático y no incorpora volcanes, clima ni biomas.
