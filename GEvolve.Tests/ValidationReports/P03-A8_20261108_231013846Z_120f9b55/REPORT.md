# P03-A.8.1 + P03-A.8.2 — Comparación aislada del relieve regional

**Estado:** experimentación implementada; variante definitiva pendiente de selección y aprobación visual. No se adopta ninguna variante, no se aprueba P03-A ni se inicia P03-A.8.3. No se ejecutó Evolve gráficamente, abrió una ventana, produjo imágenes/CSV o modificó la interfaz.

Base publicada: `c5c8aa62bcfc08cf6eae09204a1ceff23b6ddf12`. Infraestructura: `393da16d604d7e8e12b4f3d1bb45d10c2eca4bfb`, `test(worldgen): introduce isolated Pangaea base calibration variants`. Comparador: `ef53b0b343e3ac158d5cc31569c8711b07db15af`, `test(worldgen): compare regional Pangaea base relief variants`. Corrección de import de Tests y **fuente validada final**: `8cd7a0604691e0f70fe59fc79e60c501716d5647`, `fix(tests): import generated world contract in base comparisons`. Rama `worldgeneration_pangea`.

Fecha UTC, SDK, códigos/duraciones y fuente limpia: [SUMMARY.md](SUMMARY.md). Datos completos: [base-variants.log](base-variants.log). Referencia histórica: [auditoría P03-A.7.4](../P03-A74_20261108_222619723Z_ca4e68da/REPORT.md), motor auditado `d1d4571d262a17ea4b1da0643c4bad0f254448c6`. **Cero archivos productivos de Engine/Game/State/Definitions modificados por esta entrega.**

## Recomendación principal

La retirada de presión heredada es la dirección más prometedora para continuar investigando, con V1 como control, **sin adoptar V1 como BASE definitiva**. Reduce pendientes medias en las doce configuraciones y las aristas terrestres con salto ≥20 pasan de 5969 a 7 en la suma de los doce mundos. Sin embargo, conserva el forzado estadístico de máximos 100 y empeora el salto costero medio en cinco configuraciones. Las demás variantes aíslan defectos adicionales; ninguna completa reúne evidencia suficiente para sustituir el flujo oficial.

V2/V3 reducen máximos y discontinuidades interiores, pero elevan sustancialmente el conjunto de tierras bajas y los saltos hacia el océano. V3 no arregla ese desplazamiento y no elimina en general los contornos cerrados de un campo continuo mediante una función monótona. Menos celdas claras no se interpreta como validación geográfica.

## Arquitectura experimental

Los archivos nuevos residen exclusivamente en GEvolve.Tests. El arnés obtiene una vez por configuración una superficie oficial y los campos A–H mediante la sesión existente con `captureBaseFields:true`; utiliza exactamente el tamaño, semilla, intento aceptado, costas y placas vigentes. V0 conserva la referencia oficial. Las variantes solo devuelven matrices y metadatos; no producen un segundo GeneratedWorld ni un estado guardable experimental, y nunca calculan FINAL/clima/biomas experimentales.

`PangaeaBaseVariants` reutiliza los métodos privados puros ShapeUpperRelief y RecoverRegionalShoulders con delegates enlazados por reflexión una sola vez en Tests. Esta dependencia explícita falla si cambian nombres/firmas, y V0 protege además F/G/H byte a byte. No se amplía el API productivo ni se copia el generador completo. Solo el pequeño mapeo inline de percentiles se reproduce en Tests, verificado contra F oficial. Las métricas reutilizan PangaeaBaseMorphology y las estadísticas/distancias de P03-A.7.4; en PangaeaBaseAuditTests solo cambia la visibilidad de dos helpers, no sus cuerpos.

No hay flags, menús, dependencias de Tests desde Game ni callbacks nuevos en el motor. Las ocho acciones visibles y el DAG oficial permanecen originales. Las matrices se retienen solo dentro de cada configuración; no existe una caché permanente de mapas. [Definición y contrato del módulo](../../PANGAEA_BASE_VARIANTS.md).

## Fórmulas exactas y cadena controlada

Se conservan C = ruido regional frecuencia 5, D = local frecuencia 15 y B = presión heredada suavizada cuatro pasadas, ya capturados; no se muestrea ruido/RNG nuevo. Tierra usa 1..100; océano continúa cero. Round es Math.Round, con empates al par; se limita únicamente al contrato 1..100 tras transformar.

Q(P) ordena el potencial terrestre y toma anclas en los índices Round(q*(N−1)), q=[0,.34,.78,.955,1], con niveles [0,15,50,80,100]. Interpola linealmente por tramo y redondea; una amplitud≤1e−12 usa fracción cero. Para V1 se recalculan anclas sobre el potencial sin presión, manteniendo cuantiles y niveles.

S(F,D,B) es ShapeUpperRelief oficial: no cambia F≤60. Para F>60 usa `support=.25+.75*max(local_normalizado²,B/Bmax)`, `prominence=(F−60)*support`, y reescala por el máximo global de prominencia hasta 100. Si B=0, la contribución de presión al soporte es cero. R(F,S) es RecoverRegionalShoulders oficial: cuando F>S, t=clamp((F−75)/10,0,1), recovery=1−t²*(3−2t), H=Round(S+recovery*(F−S)). No baja cumbres.

| Variante | Padre | Potencial | Normalización | Tratamiento posterior | Factor cambiado |
|---|---|---|---|---|---|
| V0 | Oficial | 2.5B+1.3C+.45D | Q oficial | R(F,S(F,D,B)) | Ninguno, referencia exacta |
| V1 | V0 | 1.3C+.45D | Q, mismas reglas | R(F,S(F,D,0)) | Retirada completa de presión, incluido soporte |
| V1T | V1 | Idéntico a V1 | Q idéntico a V1 | Identidad H=F | Solo tratamiento de cumbres |
| V2 | V1T | Idéntico a V1T | Escala fija de amplitud teórica | Identidad | Solo normalización |
| V3 | V2 | Idéntico a V2 | Escala fija idéntica | Compresión suave de banda superior | Solo tratamiento de cumbres |

V1T es el puente necesario: Q y S pueden forzar máximos por vías distintas. Cambiar Q y eliminar S simultáneamente en V2 sin ese control mezclaría dos hipótesis. V1 retira una sola fuente conceptual de ambas entradas por las que intervenía; este ensayo no separa el efecto de presión en el potencial del efecto en el soporte de cumbres. Se prueba además que perturbar B no cambia V1/V1T/V2/V3.

Para V2/V3, C,D∈[−1,1] implica P∈[−A,A], A=1.3+.45=1.75. La altura continua es `h=1+99*clamp((P+1.75)/3.5,0,1)`. No usa el máximo observado, tamaño ni semilla para ajustar la escala, ni cuotas. Los números 1/100 provienen de la escala terrestre vigente; 99 es su intervalo. P=0→50.5 antes de redondear: esta referencia continental es una hipótesis, no una altitud física calibrada. La amplitud teórica está justificada matemáticamente, pero el centrado vertical no está geográficamente validado y eleva tierras bajas/costas en la muestra.

V3 deja h≤60 intacto. Para h>60, e=h−60 y `g(h)=60+e/(1+e/99)`. El 60 es el hombro vigente; 99 es el intervalo terrestre, sin ajuste a porcentajes deseados. Es continua, monótona y C1 en 60; arriba su derivada es `1/(1+e/99)²`, siempre positiva. No hay recorte horizontal ni suavizado espacial. El máximo teórico en h∈[1,100] es 88.4892…; no fuerza alcanzarlo ni prohíbe ≥85 matemáticamente. V3 actúa sobre h continua y redondea al final, no sobre V2 ya cuantizada. Se verifica que todas las celdas V2≤60 conservan exactamente su valor en V3.

## Método, protección y límites

Se ejecutan −1387338765, −132790047 y 2026 en Small 200×133, Medium 250×167, Large 300×200 y Enormous 400×267: 12 configuraciones ×5 variantes =60. Los dos casos solicitados se detallan abajo.

- V0 reproduce E/F/G/H oficiales exactamente. En los ocho mundos históricos también coinciden State/BASE y las 48 morfologías H de P03-A.7.4, sin reemplazar referencias.
- Se compara generación normal antes/después, sesión manual completa y entrada congelada antes/después de las variantes. Los hashes incluyen geología, superficie, placas/H04, sistemas, BASE/FINAL, pendientes, clima y biomas. Guardado/carga se prueba con datos oficiales completos y un estado histórico compatible sin campos ambientales. No se certifican archivos históricos desconocidos de experimentos retirados.
- Se miden media/mediana/P90/P99, máximos y todos los umbrales solicitados. Las diferencias se calculan tanto frente a V0 como frente al padre inmediato.
- Se reutilizan vecinos hexagonales reales con unión E/O y N/S abierto. Componentes, recorrido inferior por doble BFS, inradio, anchura proxy, elongación, esqueletos/ramificaciones y cierres con contraste siguen los criterios de P03-A.7.4. No equivalen a una medición física ni visual. Los posibles anillos requieren cierre topológico no cilíndrico, un único borde alto, ≥3 celdas interiores, ≥90% tierra y contraste≥5; no basta contar ciclos del grafo.
- Además de los seis umbrales fijos, se analizan P90/P99 de la altura de cada variante. Son cortes diagnósticos, nunca cuotas del generador. El redondeo y los empates alteran el área seleccionada; dos cortes relativos no garantizan idéntico conjunto ni idéntica fracción. Se obtienen 464 máscaras distintas en total.
- La pendiente por celda es el mayor salto a un vecino terrestre; las aristas terrestres se cuentan una sola vez. Se separa el salto de aristas costa-océano porque una pendiente que omita océano escondería ese coste. También se publican alturas por distancia a costa, distancias a placas/ejes y correlación con compresión H04. Correlación no es causalidad ni una condición obligatoria para un relieve continental regional.
- Los máximos cuantizados son mesetas sin vecino terrestre más alto. No miden prominencia geológica. También se cuentan máximos del potencial continuo: las cuatro variantes sin presión conservan exactamente el mismo potencial, de modo que una compresión monótona no elimina por sí sola sus estructuras continuas. Puede cambiar máscaras y mesetas por cuantización.

## Clasificación de hallazgos

**Demostrado en estas configuraciones:** aislamiento completo del producto oficial; V0 exacta; descenso de pendiente media de V1 en 12/12; reducción de discontinuidades interiores; máximos 100 todavía forzados en V1/V1T; elevación general y costera por la escala V2; V3 mantiene toda altura baja/media y no resuelve el centrado de V2. No se detectan anillos candidatos en las máscaras muestreadas V1/V1T/V2/V3. Eso no demuestra ausencia de anillos a todos los niveles o con otros criterios.

**Probable:** las bandas de presión heredada son una contribución importante a las crestas estrechas de V0. La retirada simultánea de sus dos usos reduce aristas bruscas y candidatos alargados; no demuestra que toda cresta sea artificial o deba eliminarse.

**No determinado:** apariencia natural, aceptabilidad del nuevo reparto de elevaciones, comportamiento con otras semillas, realismo de pendientes/costas, pertinencia física de las escalas y variante definitiva. No se calculan consecuencias de un FINAL/clima/bioma experimental ni se aprueba su integración. La evaluación visual futura de variantes requerirá una decisión posterior para exponerlas en Evolve; esta entrega no añade esa capacidad ni exporta partidas experimentales.

## Resumen de rangos entre los doce mundos

Cada intervalo es mínimo–máximo entre configuraciones, no un porcentaje global ponderado ni una puntuación de calidad.

| Variante | Altura media | Máximo por mundo | Tierra ≥85 % | Pendiente terrestre media | Salto costero medio | Aristas tierra ≥20, suma | Detecciones de anillo |
|---|---|---|---|---|---|---:|---:|
| V0 | 31.193–32.341 | 100–100 | 0.073–1.546 | 2.283–5.606 | 26.040–41.720 | 5969 | 6 |
| V1 | 31.347–32.935 | 100–100 | 0.389–1.399 | 1.887–3.854 | 23.725–45.435 | 7 | 0 |
| V1T | 31.746–33.207 | 100–100 | 1.497–3.014 | 1.888–3.946 | 23.688–46.178 | 8 | 0 |
| V2 | 42.810–54.613 | 75–85 | 0.000–0.028 | 1.151–2.319 | 44.318–54.281 | 0 | 0 |
| V3 | 42.748–54.407 | 73–80 | 0.000–0.000 | 1.140–2.258 | 44.236–53.946 | 0 | 0 |

Las seis detecciones de V0 son eventos componente/umbral, no seis anillos físicos distintos; algunas máscaras son niveles del mismo entorno. Cinco aparecen en cortes fijos y una en un corte P90 relativo. Cero detecciones en otra variante tampoco implica que hayan desaparecido todas las familias de contornos cerrados.

## Small / -1387338765 — 200×133

Tierra 10995; mismo fixture oficial para las cinco variantes. BASE oficial `0068BB72CB8AA3B526702C274A7262BC32DCFF4C0D6028DD70B50513BBE43428`, FINAL `0F1C615E0DCD5DA290992E92D32996D3F3FB3C9BCE63C0343676A8A93E9D73C8`, estado completo `881BFF8D682CA53D75331BA9E15C22651C68AD25DD24C86104B8FDDA6674BD1B`. Referencia P03-A.7.4 disponible: True. Todas las comprobaciones de igualdad/persistencia pasaron.

| Variante | Media | Mediana | P90 | P99 | Máximo | Pendiente media | Salto costa medio | Δ medio padre | Δ absoluto medio V0 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| V0 | 31.193 | 25.000 | 65.000 | 87.000 | 100 | 5.167 | 41.720 | 0.000 | 0.000 |
| V1 | 31.347 | 26.000 | 67.000 | 80.000 | 100 | 3.556 | 45.435 | 0.153 | 6.451 |
| V1T | 31.746 | 26.000 | 67.000 | 89.000 | 100 | 3.561 | 46.178 | 0.400 | 6.775 |
| V2 | 42.810 | 42.000 | 60.000 | 74.000 | 85 | 2.127 | 50.488 | 11.063 | 16.179 |
| V3 | 42.748 | 42.000 | 60.000 | 72.000 | 80 | 2.103 | 50.389 | -0.062 | 16.241 |

| Variante | ≥60 celdas (%) | ≥75 celdas (%) | ≥85 celdas (%) | ≥90 celdas (%) | ≥95 celdas (%) | =100 celdas (%) | Aristas tierra ≥20 |
|---|---:|---:|---:|---:|---:|---:|---:|
| V0 | 1540 (14.006) | 676 (6.148) | 170 (1.546) | 75 (0.682) | 23 (0.209) | 1 (0.009) | 624 |
| V1 | 1543 (14.034) | 498 (4.529) | 49 (0.446) | 23 (0.209) | 12 (0.109) | 2 (0.018) | 0 |
| V1T | 1543 (14.034) | 706 (6.421) | 250 (2.274) | 83 (0.755) | 26 (0.236) | 2 (0.018) | 0 |
| V2 | 1153 (10.487) | 93 (0.846) | 3 (0.027) | 0 (0.000) | 0 (0.000) | 0 (0.000) | 0 |
| V3 | 1153 (10.487) | 46 (0.418) | 0 (0.000) | 0 (0.000) | 0 (0.000) | 0 (0.000) | 0 |

| Variante | Componentes ≥60 | Componentes ≥85 | P90 relativo: nivel / componentes / compactas / anillos | P99 relativo: nivel / componentes / crestas / anillos | Máximos cuantizados | Máximos potencial | r altura–H04 |
|---|---:|---:|---|---|---:|---:|---:|
| V0 | 10 | 11 | 65/9/3/0 | 87/11/2/0 | 104 | 150 | 0.307 |
| V1 | 7 | 2 | 67/8/3/0 | 80/4/0/0 | 96 | 134 | -0.060 |
| V1T | 7 | 3 | 67/8/3/0 | 89/3/0/0 | 85 | 134 | -0.062 |
| V2 | 7 | 1 | 60/7/3/0 | 74/3/0/0 | 69 | 134 | -0.068 |
| V3 | 7 | 0 | 60/7/3/0 | 72/2/0/0 | 67 | 134 | -0.068 |

| Variante, componente principal P99 | Celdas | Recorrido inferior | Ancho máximo proxy | Elongación | Compacidad | Grupos ramificación | Dist. placa media | Dist. costa media | Dist. eje media |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| V0 | 39 | 7 | 3 | 2.333 | 0.354 | 1 | 0.205 | 16.103 | 27.333 |
| V1 | 121 | 18 | 9 | 2.000 | 0.395 | 2 | 12.579 | 5.256 | 58.661 |
| V1T | 96 | 12 | 9 | 2.444 | 0.444 | 2 | 11.333 | 4.021 | 60.156 |
| V2 | 107 | 15 | 9 | 2.889 | 0.379 | 3 | 11.280 | 3.981 | 60.121 |
| V3 | 146 | 17 | 9 | 3.111 | 0.310 | 4 | 12.274 | 4.945 | 58.678 |

El componente principal es el de mayor área entre los detalles P99; no representa todos los componentes. El log incluye tamaños completos, hasta 12/30 componentes detallados por máscara, huecos, cuantiles de saltos/pendientes, bandas costeras y entradas/salidas de cada umbral respecto a V0 y al padre.

## Small / -132790047 — 200×133

Tierra 10640; mismo fixture oficial para las cinco variantes. BASE oficial `5D201EDFEF4E68610B4181C2782B13BB3C5E46D3503643F40950CB4E5E4088D4`, FINAL `D73FABF0A0753458C499B29B83EC79B8F4EDDBC072CDFAE5E7B972E38E3E0A54`, estado completo `A446F47C4A6ADFD7546322555F5AC3C8D9AD0E76787A001830838FE0A6D131EC`. Referencia P03-A.7.4 disponible: True. Todas las comprobaciones de igualdad/persistencia pasaron.

| Variante | Media | Mediana | P90 | P99 | Máximo | Pendiente media | Salto costa medio | Δ medio padre | Δ absoluto medio V0 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| V0 | 31.973 | 26.000 | 67.000 | 83.000 | 100 | 4.538 | 30.125 | 0.000 | 0.000 |
| V1 | 32.172 | 26.000 | 71.000 | 80.000 | 100 | 3.854 | 29.855 | 0.199 | 3.398 |
| V1T | 32.480 | 26.000 | 71.000 | 86.000 | 100 | 3.946 | 29.927 | 0.307 | 3.625 |
| V2 | 50.539 | 50.000 | 68.000 | 74.000 | 82 | 2.319 | 49.869 | 18.059 | 20.205 |
| V3 | 50.376 | 50.000 | 68.000 | 73.000 | 78 | 2.258 | 49.813 | -0.163 | 20.233 |

| Variante | ≥60 celdas (%) | ≥75 celdas (%) | ≥85 celdas (%) | ≥90 celdas (%) | ≥95 celdas (%) | =100 celdas (%) | Aristas tierra ≥20 |
|---|---:|---:|---:|---:|---:|---:|---:|
| V0 | 1659 (15.592) | 629 (5.912) | 87 (0.818) | 45 (0.423) | 20 (0.188) | 3 (0.028) | 208 |
| V1 | 1843 (17.321) | 701 (6.588) | 61 (0.573) | 28 (0.263) | 12 (0.113) | 2 (0.019) | 1 |
| V1T | 1843 (17.321) | 810 (7.613) | 163 (1.532) | 53 (0.498) | 24 (0.226) | 4 (0.038) | 1 |
| V2 | 3022 (28.402) | 106 (0.996) | 0 (0.000) | 0 (0.000) | 0 (0.000) | 0 (0.000) | 0 |
| V3 | 3022 (28.402) | 43 (0.404) | 0 (0.000) | 0 (0.000) | 0 (0.000) | 0 (0.000) | 0 |

| Variante | Componentes ≥60 | Componentes ≥85 | P90 relativo: nivel / componentes / compactas / anillos | P99 relativo: nivel / componentes / crestas / anillos | Máximos cuantizados | Máximos potencial | r altura–H04 |
|---|---:|---:|---|---|---:|---:|---:|
| V0 | 7 | 4 | 67/12/2/0 | 83/5/2/0 | 109 | 129 | 0.341 |
| V1 | 4 | 3 | 71/5/2/0 | 80/5/0/0 | 106 | 119 | 0.288 |
| V1T | 4 | 5 | 71/5/2/0 | 86/5/0/0 | 84 | 119 | 0.290 |
| V2 | 8 | 0 | 68/5/2/0 | 74/5/0/0 | 71 | 119 | 0.275 |
| V3 | 8 | 0 | 68/5/3/0 | 73/5/0/0 | 67 | 119 | 0.273 |

| Variante, componente principal P99 | Celdas | Recorrido inferior | Ancho máximo proxy | Elongación | Compacidad | Grupos ramificación | Dist. placa media | Dist. costa media | Dist. eje media |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| V0 | 57 | 21 | 3 | 7.333 | 0.202 | 2 | 0.246 | 33.561 | 1.246 |
| V1 | 94 | 14 | 7 | 2.571 | 0.435 | 1 | 9.160 | 20.181 | 9.872 |
| V1T | 90 | 13 | 7 | 1.857 | 0.509 | 1 | 8.967 | 19.967 | 9.633 |
| V2 | 101 | 13 | 9 | 2.111 | 0.407 | 4 | 8.901 | 19.713 | 9.604 |
| V3 | 86 | 12 | 7 | 1.857 | 0.542 | 1 | 8.884 | 19.965 | 9.570 |

El componente principal es el de mayor área entre los detalles P99; no representa todos los componentes. El log incluye tamaños completos, hasta 12/30 componentes detallados por máscara, huecos, cuantiles de saltos/pendientes, bandas costeras y entradas/salidas de cada umbral respecto a V0 y al padre.

## Small / 2026 — 200×133

Tierra 10650; mismo fixture oficial para las cinco variantes. BASE oficial `E62EB0A75091A91B1D52D54CE82EAA3D8A0310D96DE382F27E3D6005C9C49A9F`, FINAL `8910FD259F94783A095F3F7F759B8A65C00C04BCCCA3103646AC8B3938EE9726`, estado completo `060471EBADB09C48AC693E67CDACEE19FEE455457DE771E8849E7FB530D0D3C8`. Referencia P03-A.7.4 disponible: False. Todas las comprobaciones de igualdad/persistencia pasaron.

| Variante | Media | Mediana | P90 | P99 | Máximo | Pendiente media | Salto costa medio | Δ medio padre | Δ absoluto medio V0 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| V0 | 32.209 | 28.000 | 68.000 | 78.000 | 100 | 5.606 | 35.889 | 0.000 | 0.000 |
| V1 | 32.274 | 29.000 | 67.000 | 86.000 | 100 | 3.689 | 25.622 | 0.065 | 7.510 |
| V1T | 32.494 | 29.000 | 67.000 | 90.000 | 100 | 3.698 | 25.685 | 0.220 | 7.724 |
| V2 | 50.710 | 52.000 | 67.000 | 75.000 | 79 | 2.011 | 47.677 | 18.216 | 21.327 |
| V3 | 50.575 | 52.000 | 66.000 | 73.000 | 76 | 1.965 | 47.631 | -0.135 | 21.336 |

| Variante | ≥60 celdas (%) | ≥75 celdas (%) | ≥85 celdas (%) | ≥90 celdas (%) | ≥95 celdas (%) | =100 celdas (%) | Aristas tierra ≥20 |
|---|---:|---:|---:|---:|---:|---:|---:|
| V0 | 1582 (14.854) | 549 (5.155) | 33 (0.310) | 13 (0.122) | 4 (0.038) | 1 (0.009) | 584 |
| V1 | 1553 (14.582) | 671 (6.300) | 149 (1.399) | 65 (0.610) | 21 (0.197) | 2 (0.019) | 0 |
| V1T | 1553 (14.582) | 704 (6.610) | 297 (2.789) | 126 (1.183) | 30 (0.282) | 2 (0.019) | 0 |
| V2 | 2750 (25.822) | 133 (1.249) | 0 (0.000) | 0 (0.000) | 0 (0.000) | 0 (0.000) | 0 |
| V3 | 2750 (25.822) | 22 (0.207) | 0 (0.000) | 0 (0.000) | 0 (0.000) | 0 (0.000) | 0 |

| Variante | Componentes ≥60 | Componentes ≥85 | P90 relativo: nivel / componentes / compactas / anillos | P99 relativo: nivel / componentes / crestas / anillos | Máximos cuantizados | Máximos potencial | r altura–H04 |
|---|---:|---:|---|---|---:|---:|---:|
| V0 | 15 | 4 | 68/15/2/0 | 78/10/0/0 | 148 | 170 | 0.152 |
| V1 | 11 | 6 | 67/6/3/0 | 86/5/0/0 | 113 | 166 | -0.019 |
| V1T | 11 | 5 | 67/6/3/0 | 90/3/1/0 | 100 | 166 | -0.022 |
| V2 | 10 | 0 | 67/6/3/0 | 75/3/0/0 | 85 | 166 | 0.000 |
| V3 | 10 | 0 | 66/7/3/0 | 73/3/0/0 | 85 | 166 | 0.003 |

| Variante, componente principal P99 | Celdas | Recorrido inferior | Ancho máximo proxy | Elongación | Compacidad | Grupos ramificación | Dist. placa media | Dist. costa media | Dist. eje media |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| V0 | 41 | 10 | 5 | 2.600 | 0.348 | 1 | 11.366 | 11.878 | 18.585 |
| V1 | 56 | 11 | 5 | 2.600 | 0.446 | 1 | 11.179 | 11.518 | 18.982 |
| V1T | 114 | 19 | 5 | 5.000 | 0.190 | 2 | 9.640 | 8.009 | 23.184 |
| V2 | 118 | 19 | 7 | 3.714 | 0.197 | 3 | 9.593 | 8.000 | 23.220 |
| V3 | 133 | 19 | 7 | 3.571 | 0.209 | 5 | 9.414 | 7.752 | 23.534 |

El componente principal es el de mayor área entre los detalles P99; no representa todos los componentes. El log incluye tamaños completos, hasta 12/30 componentes detallados por máscara, huecos, cuantiles de saltos/pendientes, bandas costeras y entradas/salidas de cada umbral respecto a V0 y al padre.

## Medium / -1387338765 — 250×167

Tierra 16700; mismo fixture oficial para las cinco variantes. BASE oficial `03CA3ACE527FCE3BB75FF158FC1D75C1C3294211BBD48B375337A65EAD42A5DF`, FINAL `E9F5A46F78CA83A304B6FF5C1C62BCB336E44A09949FD409B57C7282B9CAA1E3`, estado completo `C04E13126BDAA577D5BB923604585AB35475926A6EAFA94C49EA5FCE25B52575`. Referencia P03-A.7.4 disponible: True. Todas las comprobaciones de igualdad/persistencia pasaron.

| Variante | Media | Mediana | P90 | P99 | Máximo | Pendiente media | Salto costa medio | Δ medio padre | Δ absoluto medio V0 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| V0 | 32.107 | 27.000 | 68.000 | 78.000 | 100 | 3.621 | 36.544 | 0.000 | 0.000 |
| V1 | 32.154 | 27.000 | 69.000 | 80.000 | 100 | 3.157 | 36.915 | 0.047 | 1.706 |
| V1T | 32.489 | 27.000 | 69.000 | 85.000 | 100 | 3.199 | 38.060 | 0.334 | 2.031 |
| V2 | 51.203 | 51.000 | 67.000 | 74.000 | 85 | 1.804 | 54.281 | 18.714 | 20.319 |
| V3 | 51.065 | 51.000 | 66.000 | 73.000 | 80 | 1.760 | 53.946 | -0.138 | 20.373 |

| Variante | ≥60 celdas (%) | ≥75 celdas (%) | ≥85 celdas (%) | ≥90 celdas (%) | ≥95 celdas (%) | =100 celdas (%) | Aristas tierra ≥20 |
|---|---:|---:|---:|---:|---:|---:|---:|
| V0 | 2649 (15.862) | 732 (4.383) | 54 (0.323) | 14 (0.084) | 7 (0.042) | 1 (0.006) | 249 |
| V1 | 2617 (15.671) | 886 (5.305) | 65 (0.389) | 32 (0.192) | 15 (0.090) | 2 (0.012) | 0 |
| V1T | 2617 (15.671) | 1160 (6.946) | 252 (1.509) | 52 (0.311) | 25 (0.150) | 3 (0.018) | 0 |
| V2 | 4282 (25.641) | 165 (0.988) | 4 (0.024) | 0 (0.000) | 0 (0.000) | 0 (0.000) | 0 |
| V3 | 4282 (25.641) | 55 (0.329) | 0 (0.000) | 0 (0.000) | 0 (0.000) | 0 (0.000) | 0 |

| Variante | Componentes ≥60 | Componentes ≥85 | P90 relativo: nivel / componentes / compactas / anillos | P99 relativo: nivel / componentes / crestas / anillos | Máximos cuantizados | Máximos potencial | r altura–H04 |
|---|---:|---:|---|---|---:|---:|---:|
| V0 | 10 | 5 | 68/6/1/0 | 78/9/1/0 | 133 | 168 | -0.044 |
| V1 | 7 | 3 | 69/7/1/0 | 80/6/0/0 | 110 | 147 | -0.127 |
| V1T | 7 | 10 | 69/6/1/0 | 85/10/0/0 | 93 | 147 | -0.126 |
| V2 | 5 | 1 | 67/7/0/0 | 74/8/0/0 | 72 | 147 | -0.073 |
| V3 | 5 | 0 | 66/6/1/0 | 73/7/1/0 | 69 | 147 | -0.071 |

| Variante, componente principal P99 | Celdas | Recorrido inferior | Ancho máximo proxy | Elongación | Compacidad | Grupos ramificación | Dist. placa media | Dist. costa media | Dist. eje media |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| V0 | 87 | 43 | 3 | 14.333 | 0.084 | 0 | 0.126 | 15.276 | 0.851 |
| V1 | 83 | 13 | 7 | 2.571 | 0.366 | 3 | 8.120 | 3.337 | 44.313 |
| V1T | 113 | 17 | 7 | 3.571 | 0.369 | 3 | 8.301 | 3.478 | 42.655 |
| V2 | 128 | 16 | 9 | 3.222 | 0.345 | 3 | 8.766 | 3.938 | 42.023 |
| V3 | 101 | 13 | 7 | 2.857 | 0.390 | 3 | 8.079 | 3.257 | 43.139 |

El componente principal es el de mayor área entre los detalles P99; no representa todos los componentes. El log incluye tamaños completos, hasta 12/30 componentes detallados por máscara, huecos, cuantiles de saltos/pendientes, bandas costeras y entradas/salidas de cada umbral respecto a V0 y al padre.

## Medium / -132790047 — 250×167

Tierra 16700; mismo fixture oficial para las cinco variantes. BASE oficial `74A08014CDEE823BDB9435012150617B24583FC64B6997159432740222AE58EB`, FINAL `3DC72EB7959BEA59ADA23D0982A674F8A4CD9BEA325C9CCF1E7A2759C0D588F1`, estado completo `DB74641DB267891F94B7EBC9EFDC3A59E6A450FD7D7C33E9CC2B6C434AA504B0`. Referencia P03-A.7.4 disponible: True. Todas las comprobaciones de igualdad/persistencia pasaron.

| Variante | Media | Mediana | P90 | P99 | Máximo | Pendiente media | Salto costa medio | Δ medio padre | Δ absoluto medio V0 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| V0 | 32.219 | 28.000 | 61.000 | 86.000 | 100 | 4.401 | 26.040 | 0.000 | 0.000 |
| V1 | 32.935 | 28.000 | 68.000 | 81.000 | 100 | 3.560 | 23.725 | 0.716 | 4.755 |
| V1T | 33.207 | 28.000 | 68.000 | 86.000 | 100 | 3.640 | 23.688 | 0.271 | 4.919 |
| V2 | 54.613 | 55.000 | 69.000 | 75.000 | 82 | 1.831 | 49.055 | 21.407 | 24.107 |
| V3 | 54.407 | 55.000 | 68.000 | 73.000 | 78 | 1.767 | 48.978 | -0.206 | 23.991 |

| Variante | ≥60 celdas (%) | ≥75 celdas (%) | ≥85 celdas (%) | ≥90 celdas (%) | ≥95 celdas (%) | =100 celdas (%) | Aristas tierra ≥20 |
|---|---:|---:|---:|---:|---:|---:|---:|
| V0 | 1831 (10.964) | 744 (4.455) | 209 (1.251) | 70 (0.419) | 10 (0.060) | 1 (0.006) | 420 |
| V1 | 2587 (15.491) | 1002 (6.000) | 105 (0.629) | 41 (0.246) | 20 (0.120) | 1 (0.006) | 6 |
| V1T | 2587 (15.491) | 1119 (6.701) | 250 (1.497) | 81 (0.485) | 37 (0.222) | 4 (0.024) | 7 |
| V2 | 6631 (39.707) | 172 (1.030) | 0 (0.000) | 0 (0.000) | 0 (0.000) | 0 (0.000) | 0 |
| V3 | 6631 (39.707) | 70 (0.419) | 0 (0.000) | 0 (0.000) | 0 (0.000) | 0 (0.000) | 0 |

| Variante | Componentes ≥60 | Componentes ≥85 | P90 relativo: nivel / componentes / compactas / anillos | P99 relativo: nivel / componentes / crestas / anillos | Máximos cuantizados | Máximos potencial | r altura–H04 |
|---|---:|---:|---|---|---:|---:|---:|
| V0 | 11 | 4 | 61/12/3/0 | 86/6/3/0 | 148 | 173 | 0.421 |
| V1 | 9 | 3 | 68/13/5/0 | 81/4/0/0 | 106 | 135 | 0.195 |
| V1T | 9 | 5 | 68/13/5/0 | 86/5/0/0 | 87 | 135 | 0.206 |
| V2 | 3 | 0 | 69/10/4/0 | 75/5/0/0 | 76 | 135 | 0.160 |
| V3 | 3 | 0 | 68/10/3/0 | 73/5/0/0 | 70 | 135 | 0.156 |

| Variante, componente principal P99 | Celdas | Recorrido inferior | Ancho máximo proxy | Elongación | Compacidad | Grupos ramificación | Dist. placa media | Dist. costa media | Dist. eje media |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| V0 | 113 | 56 | 3 | 19.667 | 0.063 | 5 | 0.027 | 36.611 | 15.876 |
| V1 | 128 | 17 | 7 | 3.571 | 0.418 | 1 | 16.156 | 23.609 | 46.891 |
| V1T | 139 | 16 | 11 | 1.909 | 0.436 | 2 | 16.079 | 22.086 | 47.086 |
| V2 | 129 | 14 | 11 | 1.909 | 0.520 | 0 | 16.047 | 21.984 | 47.054 |
| V3 | 134 | 15 | 11 | 1.818 | 0.517 | 2 | 16.075 | 22.097 | 47.075 |

El componente principal es el de mayor área entre los detalles P99; no representa todos los componentes. El log incluye tamaños completos, hasta 12/30 componentes detallados por máscara, huecos, cuantiles de saltos/pendientes, bandas costeras y entradas/salidas de cada umbral respecto a V0 y al padre.

## Medium / 2026 — 250×167

Tierra 16700; mismo fixture oficial para las cinco variantes. BASE oficial `22381BDA7029CA3EC1FDFE6BD6CF5C9BB371117E718286900AC7D394CC9FF98B`, FINAL `130D03507844600D4A5AE99627DDEB5681794B5780A26F51E46E312CF2B33B46`, estado completo `0ED98AB6DE9FCFB886EB3A1DD1F2BB4C372EB1D5FD6994E997744F944B358D2D`. Referencia P03-A.7.4 disponible: False. Todas las comprobaciones de igualdad/persistencia pasaron.

| Variante | Media | Mediana | P90 | P99 | Máximo | Pendiente media | Salto costa medio | Δ medio padre | Δ absoluto medio V0 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| V0 | 31.892 | 26.000 | 68.000 | 80.000 | 100 | 4.224 | 38.076 | 0.000 | 0.000 |
| V1 | 32.016 | 26.000 | 67.000 | 87.000 | 100 | 2.762 | 24.676 | 0.124 | 5.883 |
| V1T | 32.227 | 26.000 | 67.000 | 90.000 | 100 | 2.768 | 24.679 | 0.211 | 6.085 |
| V2 | 48.846 | 48.000 | 67.000 | 75.000 | 79 | 1.665 | 45.351 | 16.619 | 19.796 |
| V3 | 48.710 | 48.000 | 66.000 | 73.000 | 76 | 1.630 | 45.334 | -0.136 | 19.827 |

| Variante | ≥60 celdas (%) | ≥75 celdas (%) | ≥85 celdas (%) | ≥90 celdas (%) | ≥95 celdas (%) | =100 celdas (%) | Aristas tierra ≥20 |
|---|---:|---:|---:|---:|---:|---:|---:|
| V0 | 2412 (14.443) | 1075 (6.437) | 60 (0.359) | 24 (0.144) | 7 (0.042) | 2 (0.012) | 639 |
| V1 | 2528 (15.138) | 1033 (6.186) | 229 (1.371) | 99 (0.593) | 31 (0.186) | 4 (0.024) | 0 |
| V1T | 2528 (15.138) | 1079 (6.461) | 445 (2.665) | 193 (1.156) | 46 (0.275) | 2 (0.012) | 0 |
| V2 | 4312 (25.820) | 209 (1.251) | 0 (0.000) | 0 (0.000) | 0 (0.000) | 0 (0.000) | 0 |
| V3 | 4312 (25.820) | 37 (0.222) | 0 (0.000) | 0 (0.000) | 0 (0.000) | 0 (0.000) | 0 |

| Variante | Componentes ≥60 | Componentes ≥85 | P90 relativo: nivel / componentes / compactas / anillos | P99 relativo: nivel / componentes / crestas / anillos | Máximos cuantizados | Máximos potencial | r altura–H04 |
|---|---:|---:|---|---|---:|---:|---:|
| V0 | 19 | 4 | 68/13/2/0 | 80/18/3/0 | 144 | 208 | 0.278 |
| V1 | 9 | 6 | 67/6/3/0 | 87/6/0/0 | 112 | 166 | -0.086 |
| V1T | 9 | 5 | 67/6/3/0 | 90/3/1/0 | 97 | 166 | -0.089 |
| V2 | 5 | 0 | 67/6/4/0 | 75/3/1/0 | 76 | 166 | -0.060 |
| V3 | 5 | 0 | 66/8/3/0 | 73/3/1/0 | 76 | 166 | -0.057 |

| Variante, componente principal P99 | Celdas | Recorrido inferior | Ancho máximo proxy | Elongación | Compacidad | Grupos ramificación | Dist. placa media | Dist. costa media | Dist. eje media |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| V0 | 68 | 24 | 3 | 8.333 | 0.177 | 0 | 0.176 | 6.618 | 0.809 |
| V1 | 75 | 13 | 7 | 2.000 | 0.383 | 1 | 20.120 | 37.067 | 20.120 |
| V1T | 178 | 24 | 7 | 4.286 | 0.188 | 4 | 25.596 | 42.730 | 25.607 |
| V2 | 190 | 24 | 7 | 5.000 | 0.215 | 4 | 25.705 | 42.858 | 25.716 |
| V3 | 199 | 24 | 7 | 5.000 | 0.201 | 4 | 25.653 | 42.794 | 25.663 |

El componente principal es el de mayor área entre los detalles P99; no representa todos los componentes. El log incluye tamaños completos, hasta 12/30 componentes detallados por máscara, huecos, cuantiles de saltos/pendientes, bandas costeras y entradas/salidas de cada umbral respecto a V0 y al padre.

## Large / -1387338765 — 300×200

Tierra 24022; mismo fixture oficial para las cinco variantes. BASE oficial `1CE9C56E62F178E915C88864258ECAC6958926214A12D9B703E665015628BFE0`, FINAL `4B38456B741CE7A2AB8BECA06158FE017B4D3D6D5E96E3D45104049A340D1EAD`, estado completo `084B394540D549145539EEEAE92F3860ED1340EEBD95AFB462FFB0A5377183E1`. Referencia P03-A.7.4 disponible: True. Todas las comprobaciones de igualdad/persistencia pasaron.

| Variante | Media | Mediana | P90 | P99 | Máximo | Pendiente media | Salto costa medio | Δ medio padre | Δ absoluto medio V0 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| V0 | 31.395 | 25.000 | 66.000 | 85.000 | 100 | 3.704 | 29.917 | 0.000 | 0.000 |
| V1 | 32.135 | 25.000 | 70.000 | 85.000 | 100 | 2.445 | 25.453 | 0.740 | 5.829 |
| V1T | 32.394 | 25.000 | 69.000 | 90.000 | 100 | 2.478 | 25.501 | 0.259 | 6.086 |
| V2 | 48.322 | 47.000 | 69.000 | 75.000 | 77 | 1.593 | 44.318 | 15.928 | 19.326 |
| V3 | 48.155 | 47.000 | 68.000 | 73.000 | 75 | 1.555 | 44.236 | -0.168 | 19.250 |

| Variante | ≥60 celdas (%) | ≥75 celdas (%) | ≥85 celdas (%) | ≥90 celdas (%) | ≥95 celdas (%) | =100 celdas (%) | Aristas tierra ≥20 |
|---|---:|---:|---:|---:|---:|---:|---:|
| V0 | 3394 (14.129) | 1102 (4.587) | 244 (1.016) | 105 (0.437) | 23 (0.096) | 2 (0.008) | 611 |
| V1 | 4154 (17.292) | 1708 (7.110) | 258 (1.074) | 126 (0.525) | 44 (0.183) | 2 (0.008) | 0 |
| V1T | 4154 (17.292) | 1725 (7.181) | 724 (3.014) | 300 (1.249) | 96 (0.400) | 5 (0.021) | 0 |
| V2 | 6451 (26.855) | 257 (1.070) | 0 (0.000) | 0 (0.000) | 0 (0.000) | 0 (0.000) | 0 |
| V3 | 6451 (26.855) | 5 (0.021) | 0 (0.000) | 0 (0.000) | 0 (0.000) | 0 (0.000) | 0 |

| Variante | Componentes ≥60 | Componentes ≥85 | P90 relativo: nivel / componentes / compactas / anillos | P99 relativo: nivel / componentes / crestas / anillos | Máximos cuantizados | Máximos potencial | r altura–H04 |
|---|---:|---:|---|---|---:|---:|---:|
| V0 | 11 | 7 | 66/13/0/0 | 85/7/4/0 | 177 | 239 | 0.142 |
| V1 | 4 | 9 | 70/10/3/0 | 85/9/0/0 | 115 | 180 | -0.023 |
| V1T | 4 | 5 | 69/7/1/0 | 90/10/0/0 | 112 | 180 | -0.023 |
| V2 | 4 | 0 | 69/10/3/0 | 75/11/0/0 | 99 | 180 | -0.051 |
| V3 | 4 | 0 | 68/7/2/0 | 73/10/1/0 | 91 | 180 | -0.053 |

| Variante, componente principal P99 | Celdas | Recorrido inferior | Ancho máximo proxy | Elongación | Compacidad | Grupos ramificación | Dist. placa media | Dist. costa media | Dist. eje media |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| V0 | 99 | 44 | 3 | 15.000 | 0.091 | 0 | 0.071 | 2.576 | 1.182 |
| V1 | 96 | 14 | 7 | 2.286 | 0.490 | 0 | 2.010 | 14.365 | 7.302 |
| V1T | 102 | 14 | 7 | 2.429 | 0.520 | 0 | 2.029 | 14.706 | 7.176 |
| V2 | 97 | 13 | 7 | 2.143 | 0.495 | 1 | 2.021 | 14.825 | 7.258 |
| V3 | 106 | 15 | 7 | 2.286 | 0.468 | 1 | 2.075 | 14.585 | 7.160 |

El componente principal es el de mayor área entre los detalles P99; no representa todos los componentes. El log incluye tamaños completos, hasta 12/30 componentes detallados por máscara, huecos, cuantiles de saltos/pendientes, bandas costeras y entradas/salidas de cada umbral respecto a V0 y al padre.

## Large / -132790047 — 300×200

Tierra 24974; mismo fixture oficial para las cinco variantes. BASE oficial `2DF30795672AFCE53C9CAFF2A37B689660B74D34931E6E88215CE23BD8F8E9F9`, FINAL `B9425AA5A54EA19E5203E48135215264B782978A8A5A0F603BEE9CA50BEBA72A`, estado completo `F216907ADAE93C722FEFBD36B57014FD81AFB7F960315CDE505B055782054D33`. Referencia P03-A.7.4 disponible: True. Todas las comprobaciones de igualdad/persistencia pasaron.

| Variante | Media | Mediana | P90 | P99 | Máximo | Pendiente media | Salto costa medio | Δ medio padre | Δ absoluto medio V0 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| V0 | 32.011 | 27.000 | 69.000 | 80.000 | 100 | 3.609 | 30.717 | 0.000 | 0.000 |
| V1 | 32.215 | 28.000 | 69.000 | 82.000 | 100 | 2.875 | 31.965 | 0.204 | 3.661 |
| V1T | 32.595 | 28.000 | 69.000 | 90.000 | 100 | 2.932 | 32.299 | 0.380 | 4.013 |
| V2 | 49.950 | 51.000 | 65.000 | 72.000 | 75 | 1.511 | 50.025 | 17.355 | 20.332 |
| V3 | 49.874 | 51.000 | 65.000 | 70.000 | 73 | 1.490 | 49.957 | -0.076 | 20.389 |

| Variante | ≥60 celdas (%) | ≥75 celdas (%) | ≥85 celdas (%) | ≥90 celdas (%) | ≥95 celdas (%) | =100 celdas (%) | Aristas tierra ≥20 |
|---|---:|---:|---:|---:|---:|---:|---:|
| V0 | 3945 (15.796) | 1486 (5.950) | 180 (0.721) | 149 (0.597) | 54 (0.216) | 1 (0.004) | 416 |
| V1 | 3639 (14.571) | 1610 (6.447) | 141 (0.565) | 40 (0.160) | 10 (0.040) | 2 (0.008) | 0 |
| V1T | 3639 (14.571) | 1856 (7.432) | 608 (2.435) | 269 (1.077) | 75 (0.300) | 5 (0.020) | 0 |
| V2 | 5221 (20.906) | 5 (0.020) | 0 (0.000) | 0 (0.000) | 0 (0.000) | 0 (0.000) | 0 |
| V3 | 5221 (20.906) | 0 (0.000) | 0 (0.000) | 0 (0.000) | 0 (0.000) | 0 (0.000) | 0 |

| Variante | Componentes ≥60 | Componentes ≥85 | P90 relativo: nivel / componentes / compactas / anillos | P99 relativo: nivel / componentes / crestas / anillos | Máximos cuantizados | Máximos potencial | r altura–H04 |
|---|---:|---:|---|---|---:|---:|---:|
| V0 | 6 | 1 | 69/10/3/0 | 80/4/1/0 | 141 | 183 | 0.307 |
| V1 | 12 | 6 | 69/6/2/0 | 82/5/0/0 | 97 | 154 | 0.135 |
| V1T | 12 | 6 | 69/6/2/0 | 90/5/0/0 | 92 | 154 | 0.125 |
| V2 | 10 | 0 | 65/7/3/0 | 72/5/0/0 | 69 | 154 | 0.162 |
| V3 | 10 | 0 | 65/6/3/0 | 70/6/0/0 | 65 | 154 | 0.165 |

| Variante, componente principal P99 | Celdas | Recorrido inferior | Ancho máximo proxy | Elongación | Compacidad | Grupos ramificación | Dist. placa media | Dist. costa media | Dist. eje media |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| V0 | 241 | 79 | 3 | 27.000 | 0.070 | 0 | 0.315 | 21.087 | 10.075 |
| V1 | 163 | 24 | 7 | 3.429 | 0.176 | 3 | 19.687 | 20.601 | 19.466 |
| V1T | 208 | 23 | 11 | 2.091 | 0.337 | 2 | 18.399 | 20.221 | 18.125 |
| V2 | 208 | 23 | 11 | 2.091 | 0.337 | 2 | 18.399 | 20.221 | 18.125 |
| V3 | 287 | 28 | 13 | 2.385 | 0.332 | 2 | 18.624 | 19.805 | 18.700 |

El componente principal es el de mayor área entre los detalles P99; no representa todos los componentes. El log incluye tamaños completos, hasta 12/30 componentes detallados por máscara, huecos, cuantiles de saltos/pendientes, bandas costeras y entradas/salidas de cada umbral respecto a V0 y al padre.

## Large / 2026 — 300×200

Tierra 27543; mismo fixture oficial para las cinco variantes. BASE oficial `33646922AA766E29A561FAAB190C2BA297208C2B94A17F2D61B3D2CA608A106D`, FINAL `3AB5354F42E1DBD286EB09793B4C944EDB272227BBB9BF041D81D92EFB4C3041`, estado completo `A019508E511D6A2EFB5642052D0B2B198187BEF9D36EBE8812EF5635714921F2`. Referencia P03-A.7.4 disponible: False. Todas las comprobaciones de igualdad/persistencia pasaron.

| Variante | Media | Mediana | P90 | P99 | Máximo | Pendiente media | Salto costa medio | Δ medio padre | Δ absoluto medio V0 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| V0 | 32.111 | 27.000 | 66.000 | 80.000 | 100 | 3.539 | 40.911 | 0.000 | 0.000 |
| V1 | 32.398 | 27.000 | 67.000 | 86.000 | 100 | 3.110 | 42.222 | 0.287 | 2.083 |
| V1T | 32.482 | 27.000 | 67.000 | 86.000 | 100 | 3.047 | 42.349 | 0.084 | 2.183 |
| V2 | 42.861 | 42.000 | 56.000 | 65.000 | 78 | 1.478 | 46.478 | 10.379 | 15.119 |
| V3 | 42.853 | 42.000 | 56.000 | 64.000 | 75 | 1.473 | 46.459 | -0.008 | 15.127 |

| Variante | ≥60 celdas (%) | ≥75 celdas (%) | ≥85 celdas (%) | ≥90 celdas (%) | ≥95 celdas (%) | =100 celdas (%) | Aristas tierra ≥20 |
|---|---:|---:|---:|---:|---:|---:|---:|
| V0 | 4049 (14.701) | 1391 (5.050) | 102 (0.370) | 43 (0.156) | 19 (0.069) | 1 (0.004) | 148 |
| V1 | 4200 (15.249) | 1650 (5.991) | 366 (1.329) | 143 (0.519) | 27 (0.098) | 1 (0.004) | 0 |
| V1T | 4200 (15.249) | 1732 (6.288) | 417 (1.514) | 81 (0.294) | 30 (0.109) | 3 (0.011) | 0 |
| V2 | 1218 (4.422) | 17 (0.062) | 0 (0.000) | 0 (0.000) | 0 (0.000) | 0 (0.000) | 0 |
| V3 | 1218 (4.422) | 3 (0.011) | 0 (0.000) | 0 (0.000) | 0 (0.000) | 0 (0.000) | 0 |

| Variante | Componentes ≥60 | Componentes ≥85 | P90 relativo: nivel / componentes / compactas / anillos | P99 relativo: nivel / componentes / crestas / anillos | Máximos cuantizados | Máximos potencial | r altura–H04 |
|---|---:|---:|---|---|---:|---:|---:|
| V0 | 15 | 8 | 66/15/1/0 | 80/15/2/0 | 180 | 240 | 0.194 |
| V1 | 12 | 15 | 67/10/2/0 | 86/13/0/0 | 151 | 213 | 0.090 |
| V1T | 12 | 7 | 67/10/1/0 | 86/7/0/0 | 143 | 213 | 0.086 |
| V2 | 10 | 0 | 56/10/2/0 | 65/7/0/0 | 102 | 213 | 0.074 |
| V3 | 10 | 0 | 56/10/2/0 | 64/7/1/0 | 101 | 213 | 0.074 |

| Variante, componente principal P99 | Celdas | Recorrido inferior | Ancho máximo proxy | Elongación | Compacidad | Grupos ramificación | Dist. placa media | Dist. costa media | Dist. eje media |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| V0 | 89 | 15 | 5 | 4.000 | 0.315 | 2 | 24.449 | 13.517 | 51.573 |
| V1 | 98 | 13 | 7 | 2.714 | 0.320 | 2 | 24.276 | 13.327 | 51.653 |
| V1T | 118 | 16 | 7 | 2.286 | 0.455 | 0 | 16.017 | 5.644 | 121.839 |
| V2 | 117 | 16 | 7 | 2.286 | 0.432 | 0 | 15.991 | 5.624 | 121.812 |
| V3 | 132 | 19 | 9 | 2.111 | 0.398 | 1 | 22.417 | 11.303 | 53.023 |

El componente principal es el de mayor área entre los detalles P99; no representa todos los componentes. El log incluye tamaños completos, hasta 12/30 componentes detallados por máscara, huecos, cuantiles de saltos/pendientes, bandas costeras y entradas/salidas de cada umbral respecto a V0 y al padre.

## Enormous / -1387338765 — 400×267

Tierra 42722; mismo fixture oficial para las cinco variantes. BASE oficial `4CB2B3416C8C8D443D72EAC18439591E81EE02FBDC45F9697B02B9846A94C178`, FINAL `13E03F0FC066757ADC939751677316583AB86616F36614A828773D41E0752E93`, estado completo `5B7168F89FF906EF5381823D5315B3F6BEC0C16A71671D0BD86D10AF2E9EF033`. Referencia P03-A.7.4 disponible: True. Todas las comprobaciones de igualdad/persistencia pasaron.

| Variante | Media | Mediana | P90 | P99 | Máximo | Pendiente media | Salto costa medio | Δ medio padre | Δ absoluto medio V0 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| V0 | 31.197 | 26.000 | 63.000 | 79.000 | 100 | 2.283 | 40.590 | 0.000 | 0.000 |
| V1 | 31.411 | 26.000 | 64.000 | 80.000 | 100 | 1.887 | 35.974 | 0.214 | 2.456 |
| V1T | 31.826 | 26.000 | 64.000 | 88.000 | 100 | 1.888 | 36.847 | 0.415 | 2.793 |
| V2 | 43.527 | 42.000 | 60.000 | 74.000 | 85 | 1.187 | 46.066 | 11.701 | 14.337 |
| V3 | 43.459 | 42.000 | 60.000 | 72.000 | 80 | 1.174 | 45.946 | -0.069 | 14.392 |

| Variante | ≥60 celdas (%) | ≥75 celdas (%) | ≥85 celdas (%) | ≥90 celdas (%) | ≥95 celdas (%) | =100 celdas (%) | Aristas tierra ≥20 |
|---|---:|---:|---:|---:|---:|---:|---:|
| V0 | 5139 (12.029) | 2007 (4.698) | 31 (0.073) | 13 (0.030) | 6 (0.014) | 2 (0.005) | 374 |
| V1 | 5501 (12.876) | 1888 (4.419) | 189 (0.442) | 92 (0.215) | 43 (0.101) | 5 (0.012) | 0 |
| V1T | 5501 (12.876) | 2766 (6.474) | 990 (2.317) | 274 (0.641) | 102 (0.239) | 8 (0.019) | 0 |
| V2 | 4305 (10.077) | 379 (0.887) | 12 (0.028) | 0 (0.000) | 0 (0.000) | 0 (0.000) | 0 |
| V3 | 4305 (10.077) | 190 (0.445) | 0 (0.000) | 0 (0.000) | 0 (0.000) | 0 (0.000) | 0 |

| Variante | Componentes ≥60 | Componentes ≥85 | P90 relativo: nivel / componentes / compactas / anillos | P99 relativo: nivel / componentes / crestas / anillos | Máximos cuantizados | Máximos potencial | r altura–H04 |
|---|---:|---:|---|---|---:|---:|---:|
| V0 | 11 | 3 | 63/12/1/0 | 79/14/3/0 | 213 | 290 | 0.159 |
| V1 | 9 | 3 | 64/8/1/0 | 80/3/0/0 | 114 | 194 | 0.085 |
| V1T | 9 | 5 | 64/8/1/0 | 88/4/0/0 | 110 | 194 | 0.083 |
| V2 | 6 | 1 | 60/6/1/0 | 74/3/0/0 | 85 | 194 | 0.083 |
| V3 | 6 | 0 | 60/6/1/0 | 72/2/0/0 | 82 | 194 | 0.084 |

| Variante, componente principal P99 | Celdas | Recorrido inferior | Ancho máximo proxy | Elongación | Compacidad | Grupos ramificación | Dist. placa media | Dist. costa media | Dist. eje media |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| V0 | 220 | 91 | 3 | 31.333 | 0.041 | 4 | 0.064 | 22.227 | 15.427 |
| V1 | 458 | 39 | 15 | 3.533 | 0.278 | 5 | 11.996 | 11.194 | 20.072 |
| V1T | 398 | 26 | 17 | 2.765 | 0.450 | 2 | 9.113 | 8.585 | 17.668 |
| V2 | 427 | 30 | 17 | 3.235 | 0.378 | 2 | 9.276 | 8.820 | 17.468 |
| V3 | 580 | 38 | 19 | 3.316 | 0.284 | 4 | 11.838 | 11.374 | 19.071 |

El componente principal es el de mayor área entre los detalles P99; no representa todos los componentes. El log incluye tamaños completos, hasta 12/30 componentes detallados por máscara, huecos, cuantiles de saltos/pendientes, bandas costeras y entradas/salidas de cada umbral respecto a V0 y al padre.

## Enormous / -132790047 — 400×267

Tierra 42730; mismo fixture oficial para las cinco variantes. BASE oficial `3B4E5208EB6444EE47A5F87D091B43E11349B703FD18963D329BBAB32E92219F`, FINAL `1F797D94DEF4E9CB4A1AA945FCAD4156229E76486570F8BDC34E029CEB000B00`, estado completo `BEC0FD828B15236B41D422E25040D890DE7C5B1C35445A7245041F951A504691`. Referencia P03-A.7.4 disponible: True. Todas las comprobaciones de igualdad/persistencia pasaron.

| Variante | Media | Mediana | P90 | P99 | Máximo | Pendiente media | Salto costa medio | Δ medio padre | Δ absoluto medio V0 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| V0 | 32.207 | 26.000 | 71.000 | 77.000 | 100 | 3.094 | 35.214 | 0.000 | 0.000 |
| V1 | 32.566 | 27.000 | 71.000 | 81.000 | 100 | 2.105 | 29.165 | 0.359 | 3.983 |
| V1T | 32.844 | 27.000 | 71.000 | 87.000 | 100 | 2.121 | 29.182 | 0.278 | 4.238 |
| V2 | 51.261 | 51.000 | 67.000 | 72.000 | 76 | 1.180 | 49.056 | 18.417 | 21.105 |
| V3 | 51.148 | 51.000 | 67.000 | 71.000 | 74 | 1.163 | 48.983 | -0.113 | 21.153 |

| Variante | ≥60 celdas (%) | ≥75 celdas (%) | ≥85 celdas (%) | ≥90 celdas (%) | ≥95 celdas (%) | =100 celdas (%) | Aristas tierra ≥20 |
|---|---:|---:|---:|---:|---:|---:|---:|
| V0 | 7276 (17.028) | 2160 (5.055) | 62 (0.145) | 21 (0.049) | 10 (0.023) | 2 (0.005) | 1092 |
| V1 | 7001 (16.384) | 3161 (7.398) | 181 (0.424) | 71 (0.166) | 21 (0.049) | 3 (0.007) | 0 |
| V1T | 7001 (16.384) | 3414 (7.990) | 793 (1.856) | 254 (0.594) | 65 (0.152) | 2 (0.005) | 0 |
| V2 | 11235 (26.293) | 67 (0.157) | 0 (0.000) | 0 (0.000) | 0 (0.000) | 0 (0.000) | 0 |
| V3 | 11235 (26.293) | 0 (0.000) | 0 (0.000) | 0 (0.000) | 0 (0.000) | 0 (0.000) | 0 |

| Variante | Componentes ≥60 | Componentes ≥85 | P90 relativo: nivel / componentes / compactas / anillos | P99 relativo: nivel / componentes / crestas / anillos | Máximos cuantizados | Máximos potencial | r altura–H04 |
|---|---:|---:|---|---|---:|---:|---:|
| V0 | 15 | 7 | 71/13/3/1 | 77/12/5/0 | 250 | 281 | 0.253 |
| V1 | 7 | 5 | 71/6/3/0 | 81/16/1/0 | 127 | 198 | 0.062 |
| V1T | 7 | 10 | 71/6/3/0 | 87/8/0/0 | 106 | 198 | 0.069 |
| V2 | 7 | 0 | 67/5/3/0 | 72/6/0/0 | 83 | 198 | 0.044 |
| V3 | 7 | 0 | 67/6/2/0 | 71/7/0/0 | 83 | 198 | 0.042 |

| Variante, componente principal P99 | Celdas | Recorrido inferior | Ancho máximo proxy | Elongación | Compacidad | Grupos ramificación | Dist. placa media | Dist. costa media | Dist. eje media |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| V0 | 246 | 103 | 5 | 21.400 | 0.039 | 7 | 0.098 | 20.114 | 1.463 |
| V1 | 107 | 17 | 7 | 2.429 | 0.349 | 1 | 9.916 | 12.084 | 9.252 |
| V1T | 224 | 25 | 9 | 3.556 | 0.301 | 3 | 2.545 | 17.625 | 3.321 |
| V2 | 285 | 28 | 11 | 3.000 | 0.330 | 2 | 3.249 | 17.175 | 3.940 |
| V3 | 248 | 26 | 11 | 3.545 | 0.294 | 3 | 2.931 | 17.415 | 3.657 |

El componente principal es el de mayor área entre los detalles P99; no representa todos los componentes. El log incluye tamaños completos, hasta 12/30 componentes detallados por máscara, huecos, cuantiles de saltos/pendientes, bandas costeras y entradas/salidas de cada umbral respecto a V0 y al padre.

## Enormous / 2026 — 400×267

Tierra 42720; mismo fixture oficial para las cinco variantes. BASE oficial `E5D47324C8C1C32992B7F04691D81A8519AB603D5AEF45CE7486E06B656597B8`, FINAL `C463E92779F2B0D2EA8F2DE50AC740064404871645F8978FA94DEEF30AC59EB8`, estado completo `384A12055A860A831473C6A569DB566B99A3B2DAA78E586403236E71DC0FDF1B`. Referencia P03-A.7.4 disponible: False. Todas las comprobaciones de igualdad/persistencia pasaron.

| Variante | Media | Mediana | P90 | P99 | Máximo | Pendiente media | Salto costa medio | Δ medio padre | Δ absoluto medio V0 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| V0 | 32.341 | 28.000 | 68.000 | 80.000 | 100 | 2.631 | 28.349 | 0.000 | 0.000 |
| V1 | 32.424 | 28.000 | 68.000 | 85.000 | 100 | 2.147 | 29.074 | 0.083 | 2.244 |
| V1T | 32.713 | 28.000 | 68.000 | 89.000 | 100 | 2.155 | 29.857 | 0.289 | 2.480 |
| V2 | 48.510 | 49.000 | 63.000 | 72.000 | 77 | 1.151 | 46.883 | 15.797 | 18.410 |
| V3 | 48.448 | 49.000 | 63.000 | 71.000 | 75 | 1.140 | 46.737 | -0.061 | 18.470 |

| Variante | ≥60 celdas (%) | ≥75 celdas (%) | ≥85 celdas (%) | ≥90 celdas (%) | ≥95 celdas (%) | =100 celdas (%) | Aristas tierra ≥20 |
|---|---:|---:|---:|---:|---:|---:|---:|
| V0 | 6200 (14.513) | 2479 (5.803) | 260 (0.609) | 100 (0.234) | 17 (0.040) | 2 (0.005) | 604 |
| V1 | 6023 (14.099) | 2431 (5.691) | 490 (1.147) | 213 (0.499) | 86 (0.201) | 2 (0.005) | 0 |
| V1T | 6023 (14.099) | 2837 (6.641) | 1106 (2.589) | 408 (0.955) | 89 (0.208) | 3 (0.007) | 0 |
| V2 | 6386 (14.949) | 90 (0.211) | 0 (0.000) | 0 (0.000) | 0 (0.000) | 0 (0.000) | 0 |
| V3 | 6386 (14.949) | 2 (0.005) | 0 (0.000) | 0 (0.000) | 0 (0.000) | 0 (0.000) | 0 |

| Variante | Componentes ≥60 | Componentes ≥85 | P90 relativo: nivel / componentes / compactas / anillos | P99 relativo: nivel / componentes / crestas / anillos | Máximos cuantizados | Máximos potencial | r altura–H04 |
|---|---:|---:|---|---|---:|---:|---:|
| V0 | 6 | 4 | 68/6/2/0 | 80/7/1/0 | 142 | 230 | 0.245 |
| V1 | 6 | 3 | 68/5/2/0 | 85/3/2/0 | 112 | 190 | 0.044 |
| V1T | 6 | 4 | 68/5/2/0 | 89/4/1/0 | 104 | 190 | 0.043 |
| V2 | 12 | 0 | 63/5/3/0 | 72/4/1/0 | 81 | 190 | 0.032 |
| V3 | 12 | 0 | 63/5/3/0 | 71/3/1/0 | 78 | 190 | 0.032 |

| Variante, componente principal P99 | Celdas | Recorrido inferior | Ancho máximo proxy | Elongación | Compacidad | Grupos ramificación | Dist. placa media | Dist. costa media | Dist. eje media |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| V0 | 374 | 155 | 3 | 53.333 | 0.029 | 8 | 0.152 | 51.535 | 4.575 |
| V1 | 439 | 50 | 11 | 6.273 | 0.130 | 6 | 11.068 | 10.845 | 14.312 |
| V1T | 234 | 29 | 11 | 2.636 | 0.306 | 2 | 14.786 | 13.338 | 18.376 |
| V2 | 238 | 29 | 11 | 2.636 | 0.311 | 2 | 14.803 | 13.252 | 18.416 |
| V3 | 216 | 29 | 11 | 2.636 | 0.305 | 3 | 14.870 | 13.398 | 18.417 |

El componente principal es el de mayor área entre los detalles P99; no representa todos los componentes. El log incluye tamaños completos, hasta 12/30 componentes detallados por máscara, huecos, cuantiles de saltos/pendientes, bandas costeras y entradas/salidas de cada umbral respecto a V0 y al padre.

## Dos casos de referencia: lectura adicional

En Pequeño/−1387338765, V1 baja pendiente media de 5.167 a 3.556 y aristas ≥20 de 624 a 0, pero sube el salto costero de 41.720 a 45.435. V2/V3 lo llevan aproximadamente a 50.5/50.4. El cambio reduce discontinuidades interiores sin resolver necesariamente la conexión al nivel oceánico.

En Enorme/−132790047, V1 baja pendiente media de 3.094 a 2.105 y aristas ≥20 de 1092 a 0; el salto costero mejora de 35.214 a 29.165. V2/V3 lo empeoran a unos 49 niveles. La media continental pasa de 32.207 en V0 a unos 51.3 en V2: un coste de escala, aunque el máximo baje de 100 a 76.

### Small / -1387338765

| Variante / componente P99 | Celdas | Recorrido inferior | Ancho proxy | Elongación | Ramas | Dist. placa | Dist. costa | Dist. eje |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| V0/2 | 39 | 7 | 3 | 2.333 | 1 | 0.205 | 16.103 | 27.333 |
| V0/1 | 26 | 19 | 1 | 19.000 | 0 | 0.000 | 33.615 | 20.423 |
| V0/10 | 21 | 11 | 3 | 3.667 | 1 | 0.048 | 11.857 | 2.429 |
| V1/4 | 121 | 18 | 9 | 2.000 | 2 | 12.579 | 5.256 | 58.661 |
| V1/2 | 2 | 1 | 1 | 1.000 | 0 | 4.000 | 0.000 | 79.000 |
| V1/3 | 2 | 1 | 1 | 1.000 | 0 | 7.500 | 0.500 | 58.500 |
| V1T/3 | 96 | 12 | 9 | 2.444 | 2 | 11.333 | 4.021 | 60.156 |
| V1T/2 | 12 | 4 | 3 | 1.333 | 0 | 17.250 | 9.917 | 52.083 |
| V1T/1 | 4 | 1 | 1 | 1.000 | 0 | 4.500 | 0.500 | 78.500 |
| V2/3 | 107 | 15 | 9 | 2.889 | 3 | 11.280 | 3.981 | 60.121 |
| V2/2 | 17 | 5 | 3 | 1.667 | 0 | 17.471 | 10.118 | 51.882 |
| V2/1 | 5 | 2 | 1 | 2.000 | 0 | 4.600 | 0.400 | 78.400 |
| V3/2 | 146 | 17 | 9 | 3.111 | 4 | 12.274 | 4.945 | 58.678 |
| V3/1 | 6 | 3 | 1 | 3.000 | 0 | 4.500 | 0.333 | 78.500 |

| Variante / máximo | Col/fila | Altura | Meseta celdas | Dist. mínima H04 | Dist. mínima eje | Dist. costa representante |
|---|---|---:|---:|---:|---:|---:|
| V0/12472 | 72/62 | 100 | 1 | 1 | 26 | 15 |
| V0/13272 | 72/66 | 99 | 1 | 1 | 30 | 15 |
| V0/20416 | 16/102 | 99 | 1 | 3 | 2 | 13 |
| V1/18480 | 80/92 | 100 | 2 | 11 | 60 | 4 |
| V1/16879 | 79/84 | 89 | 3 | 16 | 52 | 10 |
| V1/2341 | 141/11 | 83 | 1 | 7 | 87 | 0 |
| V1T/18480 | 80/92 | 100 | 2 | 11 | 60 | 4 |
| V1T/5367 | 167/26 | 91 | 2 | 4 | 79 | 0 |
| V1T/16878 | 78/84 | 90 | 6 | 16 | 51 | 11 |
| V2/18480 | 80/92 | 85 | 3 | 11 | 60 | 4 |
| V2/5567 | 167/27 | 77 | 1 | 4 | 79 | 0 |
| V2/16880 | 80/84 | 76 | 1 | 17 | 52 | 10 |
| V3/18480 | 80/92 | 80 | 3 | 11 | 60 | 4 |
| V3/5367 | 167/26 | 74 | 2 | 4 | 79 | 0 |
| V3/16880 | 80/84 | 74 | 1 | 17 | 52 | 10 |

### Enormous / -132790047

| Variante / componente P99 | Celdas | Recorrido inferior | Ancho proxy | Elongación | Ramas | Dist. placa | Dist. costa | Dist. eje |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| V0/1 | 246 | 103 | 5 | 21.400 | 7 | 0.098 | 20.114 | 1.463 |
| V0/11 | 89 | 38 | 3 | 12.667 | 0 | 0.067 | 0.685 | 19.978 |
| V0/7 | 46 | 12 | 5 | 2.800 | 1 | 0.674 | 44.978 | 1.457 |
| V1/4 | 107 | 17 | 7 | 2.429 | 1 | 9.916 | 12.084 | 9.252 |
| V1/10 | 102 | 13 | 9 | 1.667 | 1 | 17.216 | 20.137 | 21.324 |
| V1/5 | 91 | 13 | 7 | 2.143 | 2 | 15.330 | 18.582 | 25.758 |
| V1T/6 | 224 | 25 | 9 | 3.556 | 3 | 2.545 | 17.625 | 3.321 |
| V1T/7 | 162 | 18 | 11 | 1.636 | 1 | 17.827 | 20.531 | 22.907 |
| V1T/1 | 65 | 15 | 5 | 3.000 | 1 | 8.738 | 10.831 | 8.000 |
| V2/4 | 285 | 28 | 11 | 3.000 | 2 | 3.249 | 17.175 | 3.940 |
| V2/5 | 187 | 19 | 11 | 2.636 | 0 | 17.770 | 20.449 | 22.856 |
| V2/2 | 88 | 15 | 5 | 3.200 | 2 | 12.727 | 16.977 | 14.864 |
| V3/5 | 248 | 26 | 11 | 3.545 | 3 | 2.931 | 17.415 | 3.657 |
| V3/6 | 171 | 19 | 11 | 3.091 | 1 | 17.842 | 20.532 | 22.936 |
| V3/1 | 66 | 15 | 5 | 3.000 | 1 | 8.727 | 10.818 | 7.985 |

| Variante / máximo | Col/fila | Altura | Meseta celdas | Dist. mínima H04 | Dist. mínima eje | Dist. costa representante |
|---|---|---:|---:|---:|---:|---:|
| V0/603 | 203/1 | 100 | 2 | 0 | 1 | 3 |
| V0/89385 | 185/223 | 95 | 4 | 0 | 17 | 1 |
| V0/91369 | 169/228 | 89 | 1 | 0 | 33 | 1 |
| V1/43039 | 239/107 | 100 | 3 | 17 | 25 | 20 |
| V1/42560 | 160/106 | 95 | 7 | 7 | 6 | 11 |
| V1/91520 | 320/228 | 91 | 4 | 20 | 22 | 23 |
| V1T/91520 | 320/228 | 100 | 2 | 20 | 24 | 23 |
| V1T/78719 | 319/196 | 97 | 3 | 0 | 0 | 20 |
| V1T/42560 | 160/106 | 93 | 7 | 7 | 6 | 11 |
| V2/91119 | 319/227 | 76 | 16 | 19 | 21 | 23 |
| V2/78318 | 318/195 | 75 | 20 | 0 | 0 | 21 |
| V2/42558 | 158/106 | 74 | 14 | 6 | 5 | 10 |
| V3/91119 | 319/227 | 74 | 14 | 19 | 22 | 23 |
| V3/78318 | 318/195 | 73 | 24 | 0 | 0 | 21 |
| V3/42159 | 159/105 | 72 | 25 | 6 | 5 | 11 |

## Rendimiento y memoria

Tres ejecuciones medidas tras un calentamiento por variante/configuración. Las muestras del núcleo incluyen validación, potencial, normalización y tratamiento de cumbres, con asignaciones GC del hilo. Excluyen captura de fuentes, estadísticas, hashes y serialización de logs. V0 reconstruye el pipeline de elevación sobre campos ya capturados, no copia solamente la referencia. No equivale al coste de generar un mundo completo.

| Tamaño | Variante | Mediana ms entre tres semillas | Mín.–máx. medianas ms | Mediana asignada MiB | Máximo asignado MiB |
|---|---|---:|---|---:|---:|
| Small | V0 | 13.244 | 9.041–15.595 | 0.864 | 0.870 |
| Small | V1 | 11.890 | 8.293–12.293 | 0.864 | 0.870 |
| Small | V1T | 7.805 | 7.777–10.962 | 0.458 | 0.464 |
| Small | V2 | 7.822 | 6.229–8.755 | 0.254 | 0.254 |
| Small | V3 | 6.184 | 6.143–9.648 | 0.254 | 0.254 |
| Medium | V0 | 14.020 | 13.885–15.567 | 1.355 | 1.355 |
| Medium | V1 | 13.283 | 13.186–14.604 | 1.355 | 1.355 |
| Medium | V1T | 12.442 | 11.782–13.578 | 0.718 | 0.718 |
| Medium | V2 | 9.759 | 8.864–10.179 | 0.398 | 0.398 |
| Medium | V3 | 10.548 | 9.847–10.944 | 0.398 | 0.398 |
| Large | V0 | 19.561 | 19.021–20.870 | 1.965 | 2.014 |
| Large | V1 | 18.828 | 18.071–19.923 | 1.965 | 2.014 |
| Large | V1T | 18.158 | 18.072–18.233 | 1.049 | 1.106 |
| Large | V2 | 13.559 | 12.500–19.264 | 0.572 | 0.572 |
| Large | V3 | 16.903 | 13.746–22.111 | 0.572 | 0.572 |
| Enormous | V0 | 29.646 | 27.104–31.352 | 3.464 | 3.464 |
| Enormous | V1 | 30.777 | 25.225–33.044 | 3.464 | 3.464 |
| Enormous | V1T | 26.281 | 21.456–28.965 | 1.834 | 1.834 |
| Enormous | V2 | 21.393 | 15.567–26.538 | 1.019 | 1.019 |
| Enormous | V3 | 21.734 | 16.793–23.594 | 1.019 | 1.019 |

En Enorme se procesan 106800 celdas por variante sin matrices permanentes. Las variantes percentiles ordenan L celdas terrestres, O(L log L), y el resto de sus pasos son lineales. V2/V3 evitan esa ordenación en el núcleo; el análisis de métricas sí ordena datos. Se mantiene un conjunto de resultados por configuración, liberable al terminarla. La asignación GC no es memoria retenida ni pico RSS; no se afirma haber medido esos picos.

CaptureSession mide sesión manual completa, copias, instrumentación, hashes de protección y clima/biomas, por lo que no se compara directamente con el núcleo. Diagnostics registra construcción de métricas excluyendo serialización; las enumeraciones de deltas terminan durante la serialización y no están incluidas en ese contador. El tiempo total del comando en SUMMARY incluye todo. Estos límites evitan presentar ese contador parcial como coste total de diagnóstico.

La ruta normal productiva no carga el módulo de Tests ni se ha editado: no tiene nuevas matrices, ramas experimentales o callbacks de variantes. Se comprueba reentrada normal con los mismos hashes después de experimentar; los tiempos contextuales siguientes son una muestra antes/después por configuración y no un benchmark estadístico ni una promesa de latencia idéntica.

| Mundo | Generate normal antes ms | Después ms | Asignado antes MiB | Después MiB | Captura/sesión ms | Captura/sesión MiB |
|---|---:|---:|---:|---:|---:|---:|
| Small/-1387338765 | 491.855 | 544.539 | 17.169 | 16.490 | 921.467 | 71.284 |
| Small/-132790047 | 580.475 | 485.908 | 16.656 | 16.657 | 722.900 | 57.549 |
| Small/2026 | 454.005 | 457.242 | 16.772 | 16.771 | 646.962 | 62.709 |
| Medium/-1387338765 | 746.583 | 815.364 | 25.881 | 25.882 | 1075.851 | 99.645 |
| Medium/-132790047 | 792.843 | 770.251 | 25.907 | 25.907 | 1144.853 | 76.923 |
| Medium/2026 | 837.270 | 780.788 | 26.140 | 26.140 | 1068.827 | 76.419 |
| Large/-1387338765 | 1110.430 | 1106.083 | 37.385 | 37.386 | 1528.026 | 111.659 |
| Large/-132790047 | 1046.450 | 1081.833 | 36.750 | 36.756 | 1387.553 | 137.504 |
| Large/2026 | 1103.339 | 1254.447 | 36.642 | 36.642 | 1441.049 | 125.918 |
| Enormous/-1387338765 | 2052.522 | 2014.274 | 64.664 | 64.664 | 2427.551 | 179.821 |
| Enormous/-132790047 | 1868.160 | 2052.507 | 65.230 | 65.230 | 2721.899 | 195.937 |
| Enormous/2026 | 1894.733 | 1918.104 | 64.208 | 64.208 | 2485.682 | 177.886 |

## Ventajas, desventajas y decisión pendiente

| Variante | Ventaja observada | Desventaja / límite | Recomendación |
|---|---|---|---|
| V0 | Referencia protegida, comparación histórica exacta | Bandas estrechas, saltos interiores y máximos estadísticos | Mantener como oficial hasta decisión explícita |
| V1 | Menores pendientes medias en 12/12, casi elimina saltos ≥20 y reduce candidatos alargados | Fuerza 100; cinco costas empeoran; cambiar un campo reubica bastante relieve aunque la media varíe poco | Mejor control para seguir estudiando retirada de presión; no adoptar |
| V1T | Aísla el papel del tratamiento superior, sin añadir geometría nueva | Percentiles aún fuerzan 100 y más tierra alta que V1 | Control factorial útil, no propuesta final |
| V2 | Escala independiente del máximo observado, núcleo lineal y menores discontinuidades interiores | Centro 50.5 eleva llanuras y costas; máximo menor no valida morfología | Revisar escala/centrado en una futura decisión antes de considerarla candidata |
| V3 | Curva monótona C1, conserva banda baja/media y reduce suavemente cumbres | Hereda el desplazamiento de V2; no crea una solución espacial a contornos; cero ≥85 en la muestra no es calidad | Aporte conceptual de tratamiento superior, no variante ganadora |

No se selecciona automáticamente una variante. La recomendación no es implantar V1 ni ajustar más constantes en esta entrega: continuar contrastando la independencia regional de presión, conservar el control de tierras bajas/costas y exigir validación visual cuando se autorice un mecanismo de exposición. P03-A.8.3 y posteriores requieren una nueva decisión.

## Resultados técnicos finales

| Validación | Estado | Código | Duración s | Log completo |
|---|---|---:|---:|---|
| Compilación aislada | PASS, 0 advertencias/errores | 0 | 7.350 | [build.log](build.log) |
| Controles +60 comparaciones | PASS | 0 | 73.875 | [base-variants.log](base-variants.log) |
| Equivalencia y sesiones, 28 Pangea +12 controles | PASS | 0 | 106.288 | [pangaea-staged.log](pangaea-staged.log) |
| Laboratorio, cuatro tamaños, sin ventana/GL | PASS | 0 | 19.833 | [pangaea-laboratory.log](pangaea-laboratory.log) |
| General | **FAIL: 17 suites PASS /2 FAIL históricos** | 1 | 685.328 | [all-suites.log](all-suites.log) |

Fallos separados: TectonicMotion (4 escenarios) y ContinentalGeology (12), ambos por baselines históricos de elevación. Los 32 registros de escenario y hashes esperados/obtenidos son idénticos a P03-A.7.4. Las 28 líneas STAGED_EQ y los 12 STAGED_OTHER_PASS también coinciden exactamente. No se cambia ninguna referencia ni se declara la general aprobada. El script conserva el código agregado 1.

La [infraestructura 393da16](../P03-A81_20261108_230339516Z_b9ad3385/REPORT.md) pasó build y controles. El comparador ef53b0b tuvo [un fallo CS0246 conservado](../P03-A8_20261108_230933671Z_4194a94a/REPORT.md); el único cambio posterior de código, 8cd7a06, fue el using faltante. Sobre 8cd7a06 se ejecutó esta validación canónica con fuente limpia. El commit documental final no requiere repetir pruebas porque solo añade texto/evidencia.

## Commits y archivos

### test(worldgen): introduce isolated Pangaea base calibration variants

SHA completo: `393da16d604d7e8e12b4f3d1bb45d10c2eca4bfb`.

- `GEvolve.Tests/PANGAEA_BASE_VARIANTS.md`
- `GEvolve.Tests/PangaeaBaseVariantControls.cs`
- `GEvolve.Tests/PangaeaBaseVariants.cs`
- `GEvolve.Tests/Program.cs`
- `GEvolve.Tests/ValidationReports/commands.ps1`

### test(worldgen): compare regional Pangaea base relief variants

SHA completo: `ef53b0b343e3ac158d5cc31569c8711b07db15af`.

- `GEvolve.Tests/PANGAEA_BASE_VARIANTS.md`
- `GEvolve.Tests/PangaeaBaseAuditTests.cs`
- `GEvolve.Tests/PangaeaBaseVariantControls.cs`
- `GEvolve.Tests/PangaeaBaseVariantMetrics.cs`
- `GEvolve.Tests/PangaeaBaseVariantTests.cs`
- `GEvolve.Tests/Program.cs`
- `GEvolve.Tests/ValidationReports/commands.ps1`

### fix(tests): import generated world contract in base comparisons

SHA completo: `8cd7a0604691e0f70fe59fc79e60c501716d5647`.

- `GEvolve.Tests/PangaeaBaseVariantTests.cs`

Resultados por commit: infraestructura 393da16, build/controles PASS; comparador ef53b0b, primera compilación FAIL por import faltante de GeneratedWorld y dependientes SKIPPED; corrección 8cd7a06, compilación y 60 comparaciones PASS, con regresión completa descrita en SUMMARY. Se conserva el [fallo de compilación](../P03-A8_20261108_230933671Z_4194a94a/REPORT.md), sin actualizar baselines. El commit documental posterior publica estos reportes, README, memoria y roadmaps; no cambia código experimental ni productivo.

## Repetición desde la raíz

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\GEvolve.Tests\ValidationReports\commands.ps1 -ValidationSet BaseRegionalVariants -Label P03-A8 -IsolatedArtifacts
```

Incluye build, comparación, equivalencia/sesiones, laboratorio y general. BaseRegionalComparison ejecuta solo build+comparación y BaseVariantControls solo build+controles. No se ejecuta Game. Los scripts crean un directorio UTC nuevo, preservan stdout/stderr y códigos individuales y no publican binarios. Las partidas y baselines históricos permanecen intactos. El swap ajeno `.BiomeTests.cs.swp` se conserva sin tocar.

Archivos del commit documental `docs(worldgen): publish P03-A8 regional relief calibration evidence` (SHA disponible al publicar, sin autoreferencia dentro de su propio contenido):

- `.agents/PROJECT_MEMORY.md`
- `GEvolve.Engine/PANGAEA_ROADMAP.md`
- `GEvolve.Engine/WORLD_GENERATION_ROADMAP.md`
- `GEvolve.Tests/PANGAEA_BASE_VARIANTS.md`
- `GEvolve.Tests/ValidationReports/P03-A8_20261108_230933671Z_4194a94a/all-suites.log`
- `GEvolve.Tests/ValidationReports/P03-A8_20261108_230933671Z_4194a94a/base-variants.log`
- `GEvolve.Tests/ValidationReports/P03-A8_20261108_230933671Z_4194a94a/build.log`
- `GEvolve.Tests/ValidationReports/P03-A8_20261108_230933671Z_4194a94a/commands.ps1`
- `GEvolve.Tests/ValidationReports/P03-A8_20261108_230933671Z_4194a94a/pangaea-laboratory.log`
- `GEvolve.Tests/ValidationReports/P03-A8_20261108_230933671Z_4194a94a/pangaea-staged.log`
- `GEvolve.Tests/ValidationReports/P03-A8_20261108_230933671Z_4194a94a/REPORT.md`
- `GEvolve.Tests/ValidationReports/P03-A8_20261108_230933671Z_4194a94a/SUMMARY.md`
- `GEvolve.Tests/ValidationReports/P03-A8_20261108_231013846Z_120f9b55/all-suites.log`
- `GEvolve.Tests/ValidationReports/P03-A8_20261108_231013846Z_120f9b55/base-variants.log`
- `GEvolve.Tests/ValidationReports/P03-A8_20261108_231013846Z_120f9b55/build.log`
- `GEvolve.Tests/ValidationReports/P03-A8_20261108_231013846Z_120f9b55/commands.ps1`
- `GEvolve.Tests/ValidationReports/P03-A8_20261108_231013846Z_120f9b55/pangaea-laboratory.log`
- `GEvolve.Tests/ValidationReports/P03-A8_20261108_231013846Z_120f9b55/pangaea-staged.log`
- `GEvolve.Tests/ValidationReports/P03-A8_20261108_231013846Z_120f9b55/REPORT.md`
- `GEvolve.Tests/ValidationReports/P03-A8_20261108_231013846Z_120f9b55/SUMMARY.md`
- `GEvolve.Tests/ValidationReports/P03-A81_20261108_230339516Z_b9ad3385/base-variant-controls.log`
- `GEvolve.Tests/ValidationReports/P03-A81_20261108_230339516Z_b9ad3385/build.log`
- `GEvolve.Tests/ValidationReports/P03-A81_20261108_230339516Z_b9ad3385/commands.ps1`
- `GEvolve.Tests/ValidationReports/P03-A81_20261108_230339516Z_b9ad3385/REPORT.md`
- `GEvolve.Tests/ValidationReports/P03-A81_20261108_230339516Z_b9ad3385/SUMMARY.md`
- `GEvolve.Tests/ValidationReports/README.md`

Revisión final: 21 archivos de reporte, sin coincidencias de rutas personales/credenciales en búsqueda estática ni enlaces relativos ausentes. Solo formatos texto/Markdown/PowerShell. Los tres directorios propios de artefactos aislados se eliminaron tras terminar todas las ejecuciones, comprobando rutas y ausencia de enlaces. Informes e historial conservados; swap ajeno intacto.
