# P03-A.7.4 — Auditoría técnica del relieve BASE de Pangea

**Commit auditado:** `d1d4571d262a17ea4b1da0643c4bad0f254448c6`, `test(worldgen): audit Pangaea base elevation sources and morphology`. Rama `worldgeneration_pangea`, base anterior `84932b9396001f0c883ce2d4c6a4d4d3dcd29f45`. Fecha de ejecución UTC 2026-11-08. Comandos, SDK, códigos, tiempos y fuente limpia: [SUMMARY.md](SUMMARY.md). [Log numérico completo](base-audit.log).

**Solo auditoría e instrumentación opcional de lectura. No hay correcciones ni recalibración.** No cambian superficie/costas, placas/H04, ruido utilizado, fórmulas BASE, ShapeUpperRelief, RecoverRegionalShoulders, cordilleras/FINAL, clima/biomas, catálogos ni baselines. No hay controles nuevos ni cambios en Game. No se ejecutó el juego gráfico, evaluaron capturas ni crearon imágenes/mapas/CSV. Los dos casos visuales se estudian numéricamente; no se afirma haber identificado las estructuras de las capturas del usuario.

## Hallazgos principales y grado de certeza

- **Demostrado:** BASE no consume H04 ni sistemas montañosos seleccionados. Sus velocidades vienen de otro RNG y su proyección usa centros de placas, no normales de arista. Hay relación espacial por la geometría compartida, pero no equivalencia cinemática.
- **Demostrado:** los campos A–E, la normalización F y ShapeUpperRelief G existen antes de la etapa 04; H es su recuperación de hombros. Las estructuras altas ya están presentes cuando la selección de sistemas aún es nula.
- **Demostrado:** ShapeUpperRelief puede subir y bajar alturas >60 al reescalar prominencias por un máximo global. Mantiene al menos una cumbre 100, sin exigir convergencia H04. RecoverRegionalShoulders solo recupera alturas y conserva exactamente las alturas/máscaras ≥85 de G en los ocho mundos.
- **Demostrado:** la composición FINAL nunca crea alturas >94: `max(BASE, min(94, round(BASE+aporte)))`. Por tanto, en esta implementación todos los valores FINAL ≥95 y =100 ya proceden de BASE. Esto explica por qué los máximos pueden quedar fuera de los sistemas seleccionados; no prueba que todos sean ajenos a contactos H04.
- **Demostrado en la muestra:** hay crestas numéricamente estrechas, componentes compactas y cierres de zonas bajas que satisfacen varios criterios de posible anillo. No son etiquetas de validez geológica ni aprobación visual.
- **Probable:** velocidades constantes por placa/proyección por pareja, presión de frontera suavizada solo cuatro pasos y reescalado superior favorecen bandas largas. El ruido suave produce envolventes regionales y contornos cerrados; su interacción con la banda superior puede destacar depresiones rodeadas de altura. No se atribuye cada anillo a una fuente única.
- **No determinado:** correspondencia exacta con las capturas, origen diferencial de cada cierre de nivel bajo entre F/G/H, realismo geológico y estrategia futura de corrección. La correlación de una fuente con BASE no es un porcentaje causal de elevación.

## Arquitectura real y fórmulas

[Generador BASE](../../../GEvolve.Engine/World/LandmassGeneration/PangaeaFromPlatesGenerator.cs), [sesión](../../../GEvolve.Engine/World/PangaeaGenerationSession.cs), [H04](../../../GEvolve.Engine/World/Tectonics/TectonicMotionGenerator.cs), [cordilleras](../../../GEvolve.Engine/World/LandmassGeneration/PangaeaMountainRangeGenerator.cs), [ruido](../../../GEvolve.Engine/World/LandmassGeneration/Shared/PeriodicNoise.cs).

### Superficie y presión heredada

Se reciben los 16 IDs oficiales de placa. Centro X = media circular de columnas, centro Y = media de filas de cada placa. Se usa SplitMix64 local con stream `0x4C414E44 + intento`; por placa se toman dos valores `2*NextDouble()-1` como velocidades X/Y sin unidades físicas ni normalización de módulo. Esos sorteos preceden a la selección de regiones y forman parte del RNG histórico de superficie.

Para vecinos de placas a/b distintas, `dx = cx[b]-cx[a]-W*Round((cx[b]-cx[a])/W)` y `dy = cy[b]-cy[a]`. La tasa heredada es `((vx[a]-vx[b])*dx + (vy[a]-vy[b])*dy)/sqrt(dx²+dy²+1e-6)`. En cada celda, compression = máximo positivo observado, extension = máximo de la tasa negada, ambos arrancando en cero. En una unión de varias placas pueden coexistir ambos; A es `max(0, compression-extension)`, no una suma ni el campo de H04.

La superficie elige una raíz y hasta seis regiones conectadas, guiadas por dirección y RNG. Su score usa distancia firmada desde el borde de esas regiones (saturada a 20), ruidos de frecuencias 4/10/21 con pesos 5.3/2.5/.8 y `1.2*(compression-extension)`. Crece una sola masa por vecinos hasta la fracción objetivo y cierra agua interior desconectada de N/S. El servicio acepta el primer intento de 0..3 que cumple el perfil. Esta auditoría no toca esos cálculos, sorteos, decisiones ni orden.

### Fuentes A–H

| Campo | Cálculo efectivo | Diagnóstico reutilizado |
|---|---|---|
| A | `max(0, compression-extension)` antes de suavizar | `Sources.Fields.InitialPressure`, canales/centros/velocidades copiados |
| B | 4 pasadas: `(2*valor_celda + suma(vecinos))/(2+grado)` | `Fields.SmoothedPressure` |
| C | PeriodicNoise frecuencia 5, seed `semilla+intento*2011+1009` | `Fields.RegionalNoise` |
| D | PeriodicNoise frecuencia 15, seed `semilla+intento*2011+2053` | `Fields.LocalNoise` |
| E | `2.5*B + 1.3*C + .45*D` | `Fields.Potential` |
| F | Interpolación por anclas de percentiles del potencial terrestre | `Sources.PreviousBaseByCellId` |
| G | ShapeUpperRelief(F,D,B) | `Sources.PriorCalibrationBaseByCellId` |
| H | RecoverRegionalShoulders(F,G), BASE definitivo | snapshot.Base / diagnostics.BaseByCellId |

B se suaviza en todas las celdas, también océano; no se detiene en costas. La máscara terrestre se usa para normalizar y fijar océano a cero. No entra una distancia a costa en la fórmula de BASE: la costa influye indirectamente por la máscara aceptada y sus anclas. Los campos de presión no son presión física en pascales.

PeriodicNoise es ruido de valores con tres octavas, frecuencias f/2f/4f, pesos 1/.5/.25 divididos por 1.75, retícula periódica en X y abierta en Y. Hash entero determinista a valores −1..1; interpolación bilineal con `s(t)=t²*(3−2t)`. No hay una primitiva explícita que dibuje cordilleras, círculos o cráteres en BASE. Las frecuencias se definen respecto al tamaño del mundo; las cuatro pasadas de B son un ancho fijo en pasos hexagonales. No deben confundirse estas dos escalas.

F ordena solo E terrestre. Cuantiles `[0,.34,.78,.955,1]` eligen índices `Round(q*(N−1))` y niveles `[0,15,50,80,100]`. En cada tramo interpola linealmente, aplica Round (empates al par) y limita tierra a 1..100. Tramos de amplitud ≤1e−12 usan fracción cero. No es un ranking uniforme por celda: la fracción depende del potencial dentro de cada intervalo. El máximo de la muestra se lleva a 100 incluso si su origen es regional.

G normaliza D por su mínimo/máximo terrestre y B por su máximo terrestre. `support=.25+.75*max(local², pressure)`. Para F>60: `prominence=(F−60)*support`, `M=max(prominence)` y `G=Round(60+40*prominence/M)` limitado a 60..100. F≤60 permanece igual. Al dividir por M, una celda bien soportada puede subir respecto a F mientras otras bajan. El soporte conserva un suelo .25 aunque no haya cresta local; no consulta H04 ni sistemas seleccionados.

H solo actúa si F>G. `t=clamp((F−75)/10,0,1)`, `recovery=1−t²*(3−2t)` y `H=Round(G+recovery*(F−G))`. Recupera completamente hasta F=75, gradualmente hasta F=85 y nada por encima; nunca rebaja una cumbre G. Los contadores de cambios distinguen recuperación de nuevas celdas que cruzan un umbral.

### H04 y etapa 04/05

H04 usa stream separado `0x4D4F5431`, solo semilla (sin intento), ángulo 0..360° y velocidad 1..8 cm/año. Proyecta velocidad relativa b−a sobre la normal geométrica local: X=1.5*delta_columna y Y=√3*(delta_fila+.5*(paridad_b−paridad_a)), con costura corregida. Convergencia=max(0,−proyección), divergencia=max(0,proyección), cizalla=valor tangencial absoluto. Umbral activo .15, razón mixta .5; propagación de máximo gaussiano con radio .02*min(W,H), corte 3 radios.

La selección montañosa requiere contacto Convergent/Mixed, convergencia≥1, convergencia≥.65*cizalla y ambos lados terrestres. Agrupa por pareja, exige longitud mínima `max(7, filas/20)`, obtiene ejes por dos barridos, rechaza algunos contactos compactos y desplaza como máximo un paso puntos interiores hacia compresión. Propaga dentro de corredores existentes. El plan es independiente de BASE; en FINAL se compone usando BASE. La auditoría mide distancias tanto a contactos H04 como a ejes/huella seleccionados, que son conjuntos distintos.

Las distancias H04 del informe parten de contactos Convergent/Mixed con convergencia≥1, incluidos contactos oceánicos. La elegibilidad adicional de montaña se mide aparte. Las coincidencias de signo comparan aristas con |tasa heredada|>1e−9 y |convergencia−divergencia H04|>.15. Son comparaciones espaciales/cinemáticas; las unidades y normales de ambos modelos no son iguales.

## Instrumentación y criterios numéricos

`CreatePangaeaSession(config, captureBaseFields:true)` activa copias de campos existentes. La API original y los modos automáticos conservan el valor por defecto false. Fuera del modo opt-in no se reservan las nuevas matrices; `Sources.Fields` es null. La expresión de ruido se envuelve para copiar su resultado en la rama opt-in: argumentos, número de muestras para el mundo, aritmética y orden de evaluación se conservan. Los métodos ShapeUpperRelief y RecoverRegionalShoulders no cambian. Los snapshots copian también los nuevos respaldos; las trazas no forman parte del estado guardable.

- Se valida A a partir de los canales reales, B con las cuatro pasadas sobre vecinos y E contra los tres campos capturados, con igualdad exacta. Se comparan State/BASE/FINAL contra la [captura previa](../P03-A74-reference_20261108_221618802Z_0fd6f9ac/REPORT.md), modo sin traza y generación automática/manual completa con clima/biomas. Se verifica determinismo de fuentes, aislamiento de copias y guardado/carga sin campos de auditoría.
- Se miden componentes por seis vecinos reales, costura E/O incluida y N/S abierto. Diámetro = cota inferior por dos barridos BFS; no es el diámetro exacto en grafos cíclicos. Inradio = máxima distancia de erosión al exterior; ancho máximo proxy = 2*inradio+1, ancho medio proxy = media de 2*profundidad+1. No son anchuras físicas ni FWHM.
- Esqueleto por eliminación secuencial de puntos simples no terminales en seis pasadas direccionales, hasta converger o 128 ciclos. Extremos y agrupaciones conectadas de nodos grado≥3 son estimaciones de ramificación. Puede producir espolones y depende de discretización/orden; no identifica por sí solo sistemas geológicos. El log incluye convergencia y rango de ciclos del esqueleto.
- Elongación usa longitud grafológica/ancho máximo proxy; cresta candidata: ≥8 celdas, recorrido≥8 y elongación≥4. Componente compacta candidata: ≥10 celdas, compacidad `4π*área_hex/perímetro²`≥.35 y elongación≤3. Son heurísticas diagnósticas, no reglas del generador. Componentes mixtas pueden contener crestas sin superar ese filtro.
- Posible anillo: componente baja complementaria cerrada, sin tocar N/S, sin envolver el cilindro, rodeada por un único componente alto, interior≥3 celdas, ≥90% tierra y contraste entre borde mínimo y media interior≥5 niveles. Esto combina conectividad, topología, superficie y altura; no basta el rango de ciclos del grafo. Se descartan bandas cilíndricas y dos barreras separadas. Una falsa ausencia sigue siendo posible según umbral/contraste; no se reconstruye automáticamente una forma circular.
- Controles sintéticos: anillo cruzando la costura, disco compacto con ciclos pero sin anillo y cresta delgada de 11 celdas que cruza E/O, con recorrido 10 y dos extremos. No se crean imágenes. Se analizan H a todos los umbrales y F/G/FINAL a ≥85; no se midió morfología F/G en todos los demás niveles.
- Máximos: mesetas de igual altura ≥85 sin vecino terrestre más alto. La distancia de una meseta a contactos/ejes/huella usa su celda más próxima; las coordenadas y fuentes detalladas corresponden al representante de menor ID. −1 indica ausencia de fuentes de distancia, no lejanía arbitraria. Los percentiles de distancia omiten fuentes inexistentes.

Antes de F, E no tiene unidades de altura; aplicar ≥60..100 directamente a E sería engañoso. El log conserva distribución real A–E y cortes de E equivalentes a esos niveles mediante la inversa de las anclas, **sin redondear**. Solo F/G/H/FINAL se cuentan como alturas reales. Los cortes no redondeados pueden diferir de F por Round y empates.

## Resumen de los ocho mundos

| Tamaño / semilla | Tierra | F≥85 | G≥85 | H≥85 | FINAL≥85 | % FINAL≥85 ya en BASE | Máximos ≥85 | Máximos a >10 pasos H04 | Máximos ≤3 pasos de eje | Coincidencia signo % |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Small/-1387338765 | 10995 | 223 | 170 | 170 | 175 | 97.143 | 23 | 3 | 2 | 44.677 |
| Small/-132790047 | 10640 | 207 | 87 | 87 | 182 | 47.802 | 10 | 0 | 5 | 54.549 |
| Medium/-1387338765 | 16700 | 162 | 54 | 54 | 99 | 54.545 | 8 | 1 | 6 | 52.166 |
| Medium/-132790047 | 16700 | 463 | 209 | 209 | 416 | 50.240 | 27 | 3 | 10 | 57.165 |
| Large/-1387338765 | 24022 | 657 | 244 | 244 | 452 | 53.982 | 24 | 8 | 13 | 49.088 |
| Large/-132790047 | 24974 | 467 | 180 | 180 | 390 | 46.154 | 15 | 0 | 5 | 50.454 |
| Enormous/-1387338765 | 42722 | 809 | 31 | 31 | 247 | 12.551 | 3 | 1 | 1 | 42.427 |
| Enormous/-132790047 | 42730 | 720 | 62 | 62 | 742 | 8.356 | 9 | 0 | 2 | 62.783 |

La proporción heredada se refiere a pertenencia al conjunto de altura, no al porcentaje de incremento vertical explicado. Una celda ya alta también puede recibir aporte; se cuenta aparte. Para ≥95 y =100 no hay cruces nuevos desde BASE en ninguno de los ocho mundos.

## Small / -1387338765 — 200 × 133

Intento aceptado 0; 10995 celdas terrestres. Estado geológico SHA256 `E10739D08DC71995BB6CF72D7B119090376D2563B98C84085B98D78ECC879B5F`; BASE `0068BB72CB8AA3B526702C274A7262BC32DCFF4C0D6028DD70B50513BBE43428`; FINAL `0F1C615E0DCD5DA290992E92D32996D3F3FB3C9BCE63C0343676A8A93E9D73C8`. Igualdad exacta con referencia previa y modo sin traza comprobada.

| Fuente | Mínimo | Media | Desv. | Máximo | r con E | r con F | r con G | r con H | Media en H≥85 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| A_InitialPressure | 0.000 | 0.032 | 0.197 | 1.425 | 0.440 | 0.394 | 0.401 | 0.392 | 1.056 |
| B_FourPassPressure | 0.000 | 0.032 | 0.126 | 0.984 | 0.503 | 0.466 | 0.471 | 0.463 | 0.640 |
| C_RegionalNoise5 | -0.855 | -0.188 | 0.301 | 0.701 | 0.729 | 0.743 | 0.739 | 0.745 | -0.195 |
| D_LocalNoise15 | -0.801 | -0.061 | 0.285 | 0.790 | 0.343 | 0.319 | 0.321 | 0.319 | 0.047 |
| E_Potential | -1.265 | -0.193 | 0.487 | 1.946 | 1.000 | 0.981 | 0.982 | 0.980 | 1.367 |

Anclas E: -1.265, -0.427, 0.154, 0.759, 1.946. Correlación B–compresión H04 propagada 0.621; BASE–compresión H04 0.307. Celdas H≥85 sin presión B: 19. Celdas altas a >5/>10/>20 pasos de contacto H04: 65/23/0. No equivalen a porcentajes causales.

| Etapa | Media | P90 | P99 | ≥60 celdas (%) | ≥75 celdas (%) | ≥85 celdas (%) | ≥90 celdas (%) | ≥95 celdas (%) | =100 celdas (%) |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| F_Percentiles | 31.312 | 65.000 | 88.000 | 1540 (14.006) | 676 (6.148) | 223 (2.028) | 82 (0.746) | 32 (0.291) | 2 (0.018) |
| G_ShapeUpperRelief | 30.937 | 63.000 | 87.000 | 1540 (14.006) | 475 (4.320) | 170 (1.546) | 75 (0.682) | 23 (0.209) | 1 (0.009) |
| H_BASE | 31.193 | 65.000 | 87.000 | 1540 (14.006) | 676 (6.148) | 170 (1.546) | 75 (0.682) | 23 (0.209) | 1 (0.009) |
| FINAL | 31.309 | 65.000 | 87.000 | 1545 (14.052) | 678 (6.166) | 175 (1.592) | 79 (0.719) | 23 (0.209) | 1 (0.009) |

**F_to_G:** suben 55, bajan 1227; delta medio -0.374 niveles.

| Umbral | Permanecen altas | Cruzan desde abajo | Dejan de ser altas | Ya altas y suben | Ya altas y bajan |
|---|---:|---:|---:|---:|---:|
| 60 | 1540 | 0 | 0 | 55 | 1227 |
| 75 | 474 | 1 | 202 | 53 | 589 |
| 85 | 170 | 0 | 53 | 49 | 149 |
| 90 | 62 | 13 | 20 | 15 | 64 |
| 95 | 17 | 6 | 15 | 0 | 30 |
| 100 | 1 | 0 | 1 | 0 | 1 |

**G_to_H:** suben 990, bajan 0; delta medio 0.256 niveles.

| Umbral | Permanecen altas | Cruzan desde abajo | Dejan de ser altas | Ya altas y suben | Ya altas y bajan |
|---|---:|---:|---:|---:|---:|
| 60 | 1540 | 0 | 0 | 990 | 0 |
| 75 | 475 | 201 | 0 | 150 | 0 |
| 85 | 170 | 0 | 0 | 0 | 0 |
| 90 | 75 | 0 | 0 | 0 | 0 |
| 95 | 23 | 0 | 0 | 0 | 0 |
| 100 | 1 | 0 | 0 | 0 | 0 |

**H_to_FINAL:** suben 260, bajan 0; delta medio 0.115 niveles.

| Umbral | Permanecen altas | Cruzan desde abajo | Dejan de ser altas | Ya altas y suben | Ya altas y bajan |
|---|---:|---:|---:|---:|---:|
| 60 | 1540 | 5 | 0 | 40 | 0 |
| 75 | 676 | 2 | 0 | 20 | 0 |
| 85 | 170 | 5 | 0 | 10 | 0 |
| 90 | 75 | 4 | 0 | 5 | 0 |
| 95 | 23 | 0 | 0 | 0 | 0 |
| 100 | 1 | 0 | 0 | 0 | 0 |

| Umbral FINAL | Altas FINAL | Ya en BASE | Nuevas por aporte | Ya altas con incremento | BASE sobre eje | BASE ≤3 pasos eje | BASE en huella de sistema |
|---|---:|---:|---:|---:|---:|---:|---:|
| 60 | 1545 | 1540 | 5 | 40 | 7 | 50 | 52 |
| 75 | 678 | 676 | 2 | 20 | 2 | 28 | 30 |
| 85 | 175 | 170 | 5 | 10 | 2 | 17 | 18 |
| 90 | 79 | 75 | 4 | 5 | 1 | 12 | 12 |
| 95 | 23 | 23 | 0 | 0 | 0 | 5 | 5 |
| 100 | 1 | 1 | 0 | 0 | 0 | 0 | 0 |

| Morfología | Umbral | Celdas altas | Componentes | Mayor componente | Crestas candidatas | Compactas candidatas | Posibles anillos |
|---|---:|---:|---:|---:|---:|---:|---:|
| F | 85 | 223 | 10 | 64 | 3 | 2 | 0 |
| G | 85 | 170 | 11 | 45 | 2 | 2 | 0 |
| H | 60 | 1540 | 10 | 662 | 2 | 3 | 1 |
| H | 75 | 676 | 6 | 312 | 1 | 2 | 0 |
| H | 85 | 170 | 11 | 45 | 2 | 2 | 0 |
| H | 90 | 75 | 10 | 34 | 1 | 0 | 1 |
| H | 95 | 23 | 3 | 17 | 0 | 0 | 1 |
| H | 100 | 1 | 1 | 1 | 0 | 0 | 0 |
| FINAL | 85 | 175 | 11 | 45 | 2 | 2 | 0 |

## Small / -132790047 — 200 × 133

Intento aceptado 0; 10640 celdas terrestres. Estado geológico SHA256 `7D3DF1BA3F6AD1C6A83C0B65E76C47900BA95BAD9DC369708D9440EBAABEBAE9`; BASE `5D201EDFEF4E68610B4181C2782B13BB3C5E46D3503643F40950CB4E5E4088D4`; FINAL `D73FABF0A0753458C499B29B83EC79B8F4EDDBC072CDFAE5E7B972E38E3E0A54`. Igualdad exacta con referencia previa y modo sin traza comprobada.

| Fuente | Mínimo | Media | Desv. | Máximo | r con E | r con F | r con G | r con H | Media en H≥85 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| A_InitialPressure | 0.000 | 0.016 | 0.100 | 0.775 | 0.280 | 0.290 | 0.301 | 0.290 | 0.601 |
| B_FourPassPressure | 0.000 | 0.016 | 0.062 | 0.489 | 0.348 | 0.360 | 0.365 | 0.354 | 0.350 |
| C_RegionalNoise5 | -0.701 | 0.002 | 0.349 | 0.712 | 0.910 | 0.885 | 0.889 | 0.889 | 0.347 |
| D_LocalNoise15 | -0.741 | -0.003 | 0.284 | 0.787 | 0.097 | 0.071 | 0.072 | 0.068 | 0.027 |
| E_Potential | -1.074 | 0.042 | 0.480 | 1.709 | 1.000 | 0.973 | 0.979 | 0.974 | 1.337 |

Anclas E: -1.074, -0.210, 0.479, 0.811, 1.709. Correlación B–compresión H04 propagada 0.227; BASE–compresión H04 0.341. Celdas H≥85 sin presión B: 4. Celdas altas a >5/>10/>20 pasos de contacto H04: 10/0/0. No equivalen a porcentajes causales.

| Etapa | Media | P90 | P99 | ≥60 celdas (%) | ≥75 celdas (%) | ≥85 celdas (%) | ≥90 celdas (%) | ≥95 celdas (%) | =100 celdas (%) |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| F_Percentiles | 32.204 | 67.000 | 88.000 | 1659 (15.592) | 665 (6.250) | 207 (1.945) | 70 (0.658) | 30 (0.282) | 4 (0.038) |
| G_ShapeUpperRelief | 31.573 | 64.000 | 83.000 | 1659 (15.592) | 364 (3.421) | 87 (0.818) | 45 (0.423) | 20 (0.188) | 3 (0.028) |
| H_BASE | 31.973 | 67.000 | 83.000 | 1659 (15.592) | 629 (5.912) | 87 (0.818) | 45 (0.423) | 20 (0.188) | 3 (0.028) |
| FINAL | 33.087 | 70.000 | 87.000 | 1832 (17.218) | 782 (7.350) | 182 (1.711) | 78 (0.733) | 20 (0.188) | 3 (0.028) |

**F_to_G:** suben 42, bajan 1426; delta medio -0.632 niveles.

| Umbral | Permanecen altas | Cruzan desde abajo | Dejan de ser altas | Ya altas y suben | Ya altas y bajan |
|---|---:|---:|---:|---:|---:|
| 60 | 1659 | 0 | 0 | 42 | 1426 |
| 75 | 362 | 2 | 303 | 33 | 602 |
| 85 | 83 | 4 | 124 | 22 | 168 |
| 90 | 39 | 6 | 31 | 7 | 54 |
| 95 | 19 | 1 | 11 | 0 | 22 |
| 100 | 3 | 0 | 1 | 0 | 1 |

**G_to_H:** suben 1212, bajan 0; delta medio 0.400 niveles.

| Umbral | Permanecen altas | Cruzan desde abajo | Dejan de ser altas | Ya altas y suben | Ya altas y bajan |
|---|---:|---:|---:|---:|---:|
| 60 | 1659 | 0 | 0 | 1212 | 0 |
| 75 | 364 | 265 | 0 | 94 | 0 |
| 85 | 87 | 0 | 0 | 0 | 0 |
| 90 | 45 | 0 | 0 | 0 | 0 |
| 95 | 20 | 0 | 0 | 0 | 0 |
| 100 | 3 | 0 | 0 | 0 | 0 |

**H_to_FINAL:** suben 1332, bajan 0; delta medio 1.114 niveles.

| Umbral | Permanecen altas | Cruzan desde abajo | Dejan de ser altas | Ya altas y suben | Ya altas y bajan |
|---|---:|---:|---:|---:|---:|
| 60 | 1659 | 173 | 0 | 555 | 0 |
| 75 | 629 | 153 | 0 | 260 | 0 |
| 85 | 87 | 95 | 0 | 22 | 0 |
| 90 | 45 | 33 | 0 | 9 | 0 |
| 95 | 20 | 0 | 0 | 0 | 0 |
| 100 | 3 | 0 | 0 | 0 | 0 |

| Umbral FINAL | Altas FINAL | Ya en BASE | Nuevas por aporte | Ya altas con incremento | BASE sobre eje | BASE ≤3 pasos eje | BASE en huella de sistema |
|---|---:|---:|---:|---:|---:|---:|---:|
| 60 | 1832 | 1659 | 173 | 555 | 97 | 613 | 640 |
| 75 | 782 | 629 | 153 | 260 | 57 | 307 | 313 |
| 85 | 182 | 87 | 95 | 22 | 12 | 47 | 47 |
| 90 | 78 | 45 | 33 | 9 | 11 | 32 | 32 |
| 95 | 20 | 20 | 0 | 0 | 6 | 19 | 19 |
| 100 | 3 | 3 | 0 | 0 | 0 | 3 | 3 |

| Morfología | Umbral | Celdas altas | Componentes | Mayor componente | Crestas candidatas | Compactas candidatas | Posibles anillos |
|---|---:|---:|---:|---:|---:|---:|---:|
| F | 85 | 207 | 6 | 87 | 2 | 2 | 0 |
| G | 85 | 87 | 4 | 49 | 2 | 0 | 0 |
| H | 60 | 1659 | 7 | 1432 | 2 | 1 | 0 |
| H | 75 | 629 | 9 | 310 | 3 | 1 | 0 |
| H | 85 | 87 | 4 | 49 | 2 | 0 | 0 |
| H | 90 | 45 | 4 | 32 | 1 | 0 | 0 |
| H | 95 | 20 | 2 | 19 | 1 | 0 | 0 |
| H | 100 | 3 | 2 | 2 | 0 | 0 | 0 |
| FINAL | 85 | 182 | 9 | 66 | 4 | 0 | 0 |

## Medium / -1387338765 — 250 × 167

Intento aceptado 0; 16700 celdas terrestres. Estado geológico SHA256 `AF582F34DA90B4060EE19321C3A93D2824901F917A2FFA18CD0E8A23F9783324`; BASE `03CA3ACE527FCE3BB75FF158FC1D75C1C3294211BBD48B375337A65EAD42A5DF`; FINAL `E9F5A46F78CA83A304B6FF5C1C62BCB336E44A09949FD409B57C7282B9CAA1E3`. Igualdad exacta con referencia previa y modo sin traza comprobada.

| Fuente | Mínimo | Media | Desv. | Máximo | r con E | r con F | r con G | r con H | Media en H≥85 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| A_InitialPressure | 0.000 | 0.006 | 0.065 | 0.756 | 0.161 | 0.184 | 0.192 | 0.185 | 0.546 |
| B_FourPassPressure | 0.000 | 0.006 | 0.041 | 0.485 | 0.179 | 0.201 | 0.210 | 0.202 | 0.300 |
| C_RegionalNoise5 | -0.693 | 0.041 | 0.291 | 0.700 | 0.929 | 0.866 | 0.869 | 0.867 | 0.233 |
| D_LocalNoise15 | -0.853 | -0.063 | 0.259 | 0.938 | 0.430 | 0.457 | 0.464 | 0.458 | 0.210 |
| E_Potential | -1.114 | 0.040 | 0.420 | 1.386 | 1.000 | 0.956 | 0.963 | 0.958 | 1.147 |

Anclas E: -1.114, -0.136, 0.389, 0.731, 1.386. Correlación B–compresión H04 propagada 0.296; BASE–compresión H04 -0.044. Celdas H≥85 sin presión B: 15. Celdas altas a >5/>10/>20 pasos de contacto H04: 17/3/0. No equivalen a porcentajes causales.

| Etapa | Media | P90 | P99 | ≥60 celdas (%) | ≥75 celdas (%) | ≥85 celdas (%) | ≥90 celdas (%) | ≥95 celdas (%) | =100 celdas (%) |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| F_Percentiles | 32.475 | 68.000 | 84.000 | 2649 (15.862) | 1138 (6.814) | 162 (0.970) | 64 (0.383) | 15 (0.090) | 1 (0.006) |
| G_ShapeUpperRelief | 31.571 | 64.000 | 78.000 | 2649 (15.862) | 275 (1.647) | 54 (0.323) | 14 (0.084) | 7 (0.042) | 1 (0.006) |
| H_BASE | 32.107 | 68.000 | 78.000 | 2649 (15.862) | 732 (4.383) | 54 (0.323) | 14 (0.084) | 7 (0.042) | 1 (0.006) |
| FINAL | 33.627 | 69.000 | 80.000 | 2820 (16.886) | 793 (4.749) | 99 (0.593) | 62 (0.371) | 7 (0.042) | 1 (0.006) |

**F_to_G:** suben 0, bajan 2497; delta medio -0.903 niveles.

| Umbral | Permanecen altas | Cruzan desde abajo | Dejan de ser altas | Ya altas y suben | Ya altas y bajan |
|---|---:|---:|---:|---:|---:|
| 60 | 2649 | 0 | 0 | 0 | 2497 |
| 75 | 275 | 0 | 863 | 0 | 1135 |
| 85 | 54 | 0 | 108 | 0 | 159 |
| 90 | 14 | 0 | 50 | 0 | 63 |
| 95 | 7 | 0 | 8 | 0 | 14 |
| 100 | 1 | 0 | 0 | 0 | 0 |

**G_to_H:** suben 2239, bajan 0; delta medio 0.536 niveles.

| Umbral | Permanecen altas | Cruzan desde abajo | Dejan de ser altas | Ya altas y suben | Ya altas y bajan |
|---|---:|---:|---:|---:|---:|
| 60 | 2649 | 0 | 0 | 2239 | 0 |
| 75 | 275 | 457 | 0 | 77 | 0 |
| 85 | 54 | 0 | 0 | 0 | 0 |
| 90 | 14 | 0 | 0 | 0 | 0 |
| 95 | 7 | 0 | 0 | 0 | 0 |
| 100 | 1 | 0 | 0 | 0 | 0 |

**H_to_FINAL:** suben 1954, bajan 0; delta medio 1.520 niveles.

| Umbral | Permanecen altas | Cruzan desde abajo | Dejan de ser altas | Ya altas y suben | Ya altas y bajan |
|---|---:|---:|---:|---:|---:|
| 60 | 2649 | 171 | 0 | 143 | 0 |
| 75 | 732 | 61 | 0 | 100 | 0 |
| 85 | 54 | 45 | 0 | 28 | 0 |
| 90 | 14 | 48 | 0 | 6 | 0 |
| 95 | 7 | 0 | 0 | 0 | 0 |
| 100 | 1 | 0 | 0 | 0 | 0 |

| Umbral FINAL | Altas FINAL | Ya en BASE | Nuevas por aporte | Ya altas con incremento | BASE sobre eje | BASE ≤3 pasos eje | BASE en huella de sistema |
|---|---:|---:|---:|---:|---:|---:|---:|
| 60 | 2820 | 2649 | 171 | 143 | 37 | 144 | 157 |
| 75 | 793 | 732 | 61 | 100 | 36 | 108 | 109 |
| 85 | 99 | 54 | 45 | 28 | 18 | 36 | 36 |
| 90 | 62 | 14 | 48 | 6 | 9 | 14 | 14 |
| 95 | 7 | 7 | 0 | 0 | 4 | 7 | 7 |
| 100 | 1 | 1 | 0 | 0 | 1 | 1 | 1 |

| Morfología | Umbral | Celdas altas | Componentes | Mayor componente | Crestas candidatas | Compactas candidatas | Posibles anillos |
|---|---:|---:|---:|---:|---:|---:|---:|
| F | 85 | 162 | 8 | 77 | 1 | 1 | 0 |
| G | 85 | 54 | 5 | 26 | 1 | 1 | 0 |
| H | 60 | 2649 | 10 | 1396 | 3 | 2 | 0 |
| H | 75 | 732 | 22 | 133 | 3 | 3 | 0 |
| H | 85 | 54 | 5 | 26 | 1 | 1 | 0 |
| H | 90 | 14 | 2 | 12 | 0 | 0 | 0 |
| H | 95 | 7 | 1 | 7 | 0 | 0 | 0 |
| H | 100 | 1 | 1 | 1 | 0 | 0 | 0 |
| FINAL | 85 | 99 | 3 | 81 | 1 | 1 | 0 |

## Medium / -132790047 — 250 × 167

Intento aceptado 0; 16700 celdas terrestres. Estado geológico SHA256 `F1783F97DF71BE0EB5F94E4DE1E1AF201B5FA6C70616F243FAD261F0273D0055`; BASE `74A08014CDEE823BDB9435012150617B24583FC64B6997159432740222AE58EB`; FINAL `3DC72EB7959BEA59ADA23D0982A674F8A4CD9BEA325C9CCF1E7A2759C0D588F1`. Igualdad exacta con referencia previa y modo sin traza comprobada.

| Fuente | Mínimo | Media | Desv. | Máximo | r con E | r con F | r con G | r con H | Media en H≥85 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| A_InitialPressure | 0.000 | 0.029 | 0.157 | 1.279 | 0.441 | 0.393 | 0.388 | 0.383 | 0.986 |
| B_FourPassPressure | 0.000 | 0.029 | 0.098 | 0.734 | 0.553 | 0.516 | 0.494 | 0.491 | 0.546 |
| C_RegionalNoise5 | -0.704 | 0.106 | 0.309 | 0.712 | 0.835 | 0.815 | 0.825 | 0.823 | 0.431 |
| D_LocalNoise15 | -0.754 | 0.016 | 0.274 | 0.883 | 0.164 | 0.160 | 0.168 | 0.167 | 0.057 |
| E_Potential | -1.078 | 0.217 | 0.490 | 2.471 | 1.000 | 0.965 | 0.963 | 0.960 | 1.950 |

Anclas E: -1.078, -0.001, 0.567, 0.978, 2.471. Correlación B–compresión H04 propagada 0.631; BASE–compresión H04 0.421. Celdas H≥85 sin presión B: 0. Celdas altas a >5/>10/>20 pasos de contacto H04: 68/41/0. No equivalen a porcentajes causales.

| Etapa | Media | P90 | P99 | ≥60 celdas (%) | ≥75 celdas (%) | ≥85 celdas (%) | ≥90 celdas (%) | ≥95 celdas (%) | =100 celdas (%) |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| F_Percentiles | 32.522 | 61.000 | 92.000 | 1831 (10.964) | 884 (5.293) | 463 (2.772) | 254 (1.521) | 67 (0.401) | 2 (0.012) |
| G_ShapeUpperRelief | 32.017 | 60.000 | 86.000 | 1831 (10.964) | 554 (3.317) | 209 (1.251) | 70 (0.419) | 10 (0.060) | 1 (0.006) |
| H_BASE | 32.219 | 61.000 | 86.000 | 1831 (10.964) | 744 (4.455) | 209 (1.251) | 70 (0.419) | 10 (0.060) | 1 (0.006) |
| FINAL | 33.334 | 63.000 | 93.000 | 2054 (12.299) | 961 (5.754) | 416 (2.491) | 254 (1.521) | 10 (0.060) | 1 (0.006) |

**F_to_G:** suben 27, bajan 1601; delta medio -0.505 niveles.

| Umbral | Permanecen altas | Cruzan desde abajo | Dejan de ser altas | Ya altas y suben | Ya altas y bajan |
|---|---:|---:|---:|---:|---:|
| 60 | 1831 | 0 | 0 | 27 | 1601 |
| 75 | 554 | 0 | 330 | 27 | 848 |
| 85 | 206 | 3 | 257 | 18 | 442 |
| 90 | 70 | 0 | 184 | 2 | 251 |
| 95 | 10 | 0 | 57 | 2 | 64 |
| 100 | 1 | 0 | 1 | 0 | 1 |

**G_to_H:** suben 1109, bajan 0; delta medio 0.202 niveles.

| Umbral | Permanecen altas | Cruzan desde abajo | Dejan de ser altas | Ya altas y suben | Ya altas y bajan |
|---|---:|---:|---:|---:|---:|
| 60 | 1831 | 0 | 0 | 1109 | 0 |
| 75 | 554 | 190 | 0 | 69 | 0 |
| 85 | 209 | 0 | 0 | 0 | 0 |
| 90 | 70 | 0 | 0 | 0 | 0 |
| 95 | 10 | 0 | 0 | 0 | 0 |
| 100 | 1 | 0 | 0 | 0 | 0 |

**H_to_FINAL:** suben 1833, bajan 0; delta medio 1.115 niveles.

| Umbral | Permanecen altas | Cruzan desde abajo | Dejan de ser altas | Ya altas y suben | Ya altas y bajan |
|---|---:|---:|---:|---:|---:|
| 60 | 1831 | 223 | 0 | 739 | 0 |
| 75 | 744 | 217 | 0 | 344 | 0 |
| 85 | 209 | 207 | 0 | 87 | 0 |
| 90 | 70 | 184 | 0 | 15 | 0 |
| 95 | 10 | 0 | 0 | 0 | 0 |
| 100 | 1 | 0 | 0 | 0 | 0 |

| Umbral FINAL | Altas FINAL | Ya en BASE | Nuevas por aporte | Ya altas con incremento | BASE sobre eje | BASE ≤3 pasos eje | BASE en huella de sistema |
|---|---:|---:|---:|---:|---:|---:|---:|
| 60 | 2054 | 1831 | 223 | 739 | 129 | 660 | 772 |
| 75 | 961 | 744 | 217 | 344 | 97 | 341 | 359 |
| 85 | 416 | 209 | 207 | 87 | 30 | 92 | 96 |
| 90 | 254 | 70 | 184 | 15 | 5 | 22 | 23 |
| 95 | 10 | 10 | 0 | 0 | 0 | 4 | 4 |
| 100 | 1 | 1 | 0 | 0 | 0 | 1 | 1 |

| Morfología | Umbral | Celdas altas | Componentes | Mayor componente | Crestas candidatas | Compactas candidatas | Posibles anillos |
|---|---:|---:|---:|---:|---:|---:|---:|
| F | 85 | 463 | 3 | 445 | 2 | 0 | 0 |
| G | 85 | 209 | 4 | 127 | 3 | 0 | 0 |
| H | 60 | 1831 | 11 | 1516 | 2 | 3 | 0 |
| H | 75 | 744 | 18 | 558 | 3 | 1 | 0 |
| H | 85 | 209 | 4 | 127 | 3 | 0 | 0 |
| H | 90 | 70 | 6 | 28 | 2 | 0 | 0 |
| H | 95 | 10 | 3 | 5 | 0 | 0 | 0 |
| H | 100 | 1 | 1 | 1 | 0 | 0 | 0 |
| FINAL | 85 | 416 | 4 | 377 | 3 | 0 | 0 |

## Large / -1387338765 — 300 × 200

Intento aceptado 0; 24022 celdas terrestres. Estado geológico SHA256 `FC6AA5BF55D2213C4B7EAA689FF035E4B00169BE7BCDBEA59B0D3F437F825C9C`; BASE `1CE9C56E62F178E915C88864258ECAC6958926214A12D9B703E665015628BFE0`; FINAL `4B38456B741CE7A2AB8BECA06158FE017B4D3D6D5E96E3D45104049A340D1EAD`. Igualdad exacta con referencia previa y modo sin traza comprobada.

| Fuente | Mínimo | Media | Desv. | Máximo | r con E | r con F | r con G | r con H | Media en H≥85 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| A_InitialPressure | 0.000 | 0.042 | 0.222 | 1.920 | 0.446 | 0.364 | 0.357 | 0.346 | 1.590 |
| B_FourPassPressure | 0.000 | 0.040 | 0.135 | 1.203 | 0.544 | 0.476 | 0.457 | 0.447 | 0.868 |
| C_RegionalNoise5 | -0.855 | -0.039 | 0.373 | 0.703 | 0.810 | 0.825 | 0.838 | 0.838 | 0.091 |
| D_LocalNoise15 | -0.876 | -0.059 | 0.260 | 0.796 | 0.267 | 0.249 | 0.255 | 0.255 | -0.001 |
| E_Potential | -1.267 | 0.023 | 0.607 | 2.950 | 1.000 | 0.971 | 0.972 | 0.966 | 2.288 |

Anclas E: -1.267, -0.282, 0.515, 0.934, 2.950. Correlación B–compresión H04 propagada 0.385; BASE–compresión H04 0.142. Celdas H≥85 sin presión B: 0. Celdas altas a >5/>10/>20 pasos de contacto H04: 76/64/33. No equivalen a porcentajes causales.

| Etapa | Media | P90 | P99 | ≥60 celdas (%) | ≥75 celdas (%) | ≥85 celdas (%) | ≥90 celdas (%) | ≥95 celdas (%) | =100 celdas (%) |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| F_Percentiles | 31.710 | 66.000 | 90.000 | 3394 (14.129) | 1328 (5.528) | 657 (2.735) | 307 (1.278) | 90 (0.375) | 7 (0.029) |
| G_ShapeUpperRelief | 31.067 | 63.000 | 85.000 | 3394 (14.129) | 761 (3.168) | 244 (1.016) | 105 (0.437) | 23 (0.096) | 2 (0.008) |
| H_BASE | 31.395 | 66.000 | 85.000 | 3394 (14.129) | 1102 (4.587) | 244 (1.016) | 105 (0.437) | 23 (0.096) | 2 (0.008) |
| FINAL | 34.237 | 68.000 | 89.000 | 3951 (16.447) | 1486 (6.186) | 452 (1.882) | 197 (0.820) | 23 (0.096) | 2 (0.008) |

**F_to_G:** suben 26, bajan 3047; delta medio -0.642 niveles.

| Umbral | Permanecen altas | Cruzan desde abajo | Dejan de ser altas | Ya altas y suben | Ya altas y bajan |
|---|---:|---:|---:|---:|---:|
| 60 | 3394 | 0 | 0 | 26 | 3047 |
| 75 | 761 | 0 | 567 | 26 | 1290 |
| 85 | 244 | 0 | 413 | 26 | 619 |
| 90 | 100 | 5 | 207 | 14 | 289 |
| 95 | 18 | 5 | 72 | 4 | 86 |
| 100 | 0 | 2 | 7 | 0 | 7 |

**G_to_H:** suben 2330, bajan 0; delta medio 0.328 niveles.

| Umbral | Permanecen altas | Cruzan desde abajo | Dejan de ser altas | Ya altas y suben | Ya altas y bajan |
|---|---:|---:|---:|---:|---:|
| 60 | 3394 | 0 | 0 | 2330 | 0 |
| 75 | 761 | 341 | 0 | 98 | 0 |
| 85 | 244 | 0 | 0 | 0 | 0 |
| 90 | 105 | 0 | 0 | 0 | 0 |
| 95 | 23 | 0 | 0 | 0 | 0 |
| 100 | 2 | 0 | 0 | 0 | 0 |

**H_to_FINAL:** suben 5739, bajan 0; delta medio 2.842 niveles.

| Umbral | Permanecen altas | Cruzan desde abajo | Dejan de ser altas | Ya altas y suben | Ya altas y bajan |
|---|---:|---:|---:|---:|---:|
| 60 | 3394 | 557 | 0 | 1267 | 0 |
| 75 | 1102 | 384 | 0 | 580 | 0 |
| 85 | 244 | 208 | 0 | 110 | 0 |
| 90 | 105 | 92 | 0 | 45 | 0 |
| 95 | 23 | 0 | 0 | 0 | 0 |
| 100 | 2 | 0 | 0 | 0 | 0 |

| Umbral FINAL | Altas FINAL | Ya en BASE | Nuevas por aporte | Ya altas con incremento | BASE sobre eje | BASE ≤3 pasos eje | BASE en huella de sistema |
|---|---:|---:|---:|---:|---:|---:|---:|
| 60 | 3951 | 3394 | 557 | 1267 | 184 | 1077 | 1409 |
| 75 | 1486 | 1102 | 384 | 580 | 117 | 528 | 652 |
| 85 | 452 | 244 | 208 | 110 | 51 | 128 | 140 |
| 90 | 197 | 105 | 92 | 45 | 35 | 66 | 70 |
| 95 | 23 | 23 | 0 | 0 | 12 | 14 | 15 |
| 100 | 2 | 2 | 0 | 0 | 0 | 0 | 0 |

| Morfología | Umbral | Celdas altas | Componentes | Mayor componente | Crestas candidatas | Compactas candidatas | Posibles anillos |
|---|---:|---:|---:|---:|---:|---:|---:|
| F | 85 | 657 | 7 | 198 | 5 | 0 | 0 |
| G | 85 | 244 | 7 | 99 | 4 | 0 | 0 |
| H | 60 | 3394 | 11 | 1530 | 4 | 2 | 0 |
| H | 75 | 1102 | 30 | 285 | 4 | 4 | 0 |
| H | 85 | 244 | 7 | 99 | 4 | 0 | 0 |
| H | 90 | 105 | 10 | 33 | 3 | 0 | 0 |
| H | 95 | 23 | 7 | 5 | 0 | 0 | 0 |
| H | 100 | 2 | 1 | 2 | 0 | 0 | 0 |
| FINAL | 85 | 452 | 15 | 134 | 7 | 0 | 0 |

## Large / -132790047 — 300 × 200

Intento aceptado 0; 24974 celdas terrestres. Estado geológico SHA256 `EC512A51FE2F3951A32F2A3B212F00A04B6EE8E95A5AF9F8B5239279535B5D91`; BASE `2DF30795672AFCE53C9CAFF2A37B689660B74D34931E6E88215CE23BD8F8E9F9`; FINAL `B9425AA5A54EA19E5203E48135215264B782978A8A5A0F603BEE9CA50BEBA72A`. Igualdad exacta con referencia previa y modo sin traza comprobada.

| Fuente | Mínimo | Media | Desv. | Máximo | r con E | r con F | r con G | r con H | Media en H≥85 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| A_InitialPressure | 0.000 | 0.021 | 0.167 | 1.842 | 0.475 | 0.303 | 0.300 | 0.288 | 1.643 |
| B_FourPassPressure | 0.000 | 0.021 | 0.104 | 1.048 | 0.596 | 0.430 | 0.414 | 0.399 | 0.928 |
| C_RegionalNoise5 | -0.694 | -0.030 | 0.290 | 0.564 | 0.798 | 0.837 | 0.851 | 0.850 | 0.038 |
| D_LocalNoise15 | -0.761 | 0.042 | 0.288 | 0.885 | 0.286 | 0.294 | 0.297 | 0.295 | 0.179 |
| E_Potential | -1.016 | 0.032 | 0.493 | 2.958 | 1.000 | 0.944 | 0.947 | 0.938 | 2.450 |

Anclas E: -1.016, -0.161, 0.360, 0.733, 2.958. Correlación B–compresión H04 propagada 0.468; BASE–compresión H04 0.307. Celdas H≥85 sin presión B: 0. Celdas altas a >5/>10/>20 pasos de contacto H04: 20/0/0. No equivalen a porcentajes causales.

| Etapa | Media | P90 | P99 | ≥60 celdas (%) | ≥75 celdas (%) | ≥85 celdas (%) | ≥90 celdas (%) | ≥95 celdas (%) | =100 celdas (%) |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| F_Percentiles | 32.317 | 69.000 | 89.000 | 3945 (15.796) | 1695 (6.787) | 467 (1.870) | 221 (0.885) | 121 (0.485) | 2 (0.008) |
| G_ShapeUpperRelief | 31.471 | 65.000 | 79.000 | 3945 (15.796) | 598 (2.394) | 180 (0.721) | 149 (0.597) | 54 (0.216) | 1 (0.004) |
| H_BASE | 32.011 | 69.000 | 80.000 | 3945 (15.796) | 1486 (5.950) | 180 (0.721) | 149 (0.597) | 54 (0.216) | 1 (0.004) |
| FINAL | 32.686 | 70.000 | 89.000 | 4173 (16.709) | 1783 (7.139) | 390 (1.562) | 240 (0.961) | 54 (0.216) | 1 (0.004) |

**F_to_G:** suben 0, bajan 3724; delta medio -0.847 niveles.

| Umbral | Permanecen altas | Cruzan desde abajo | Dejan de ser altas | Ya altas y suben | Ya altas y bajan |
|---|---:|---:|---:|---:|---:|
| 60 | 3945 | 0 | 0 | 0 | 3724 |
| 75 | 598 | 0 | 1097 | 0 | 1667 |
| 85 | 180 | 0 | 287 | 0 | 443 |
| 90 | 149 | 0 | 72 | 0 | 197 |
| 95 | 54 | 0 | 67 | 0 | 97 |
| 100 | 1 | 0 | 1 | 0 | 1 |

**G_to_H:** suben 3245, bajan 0; delta medio 0.540 niveles.

| Umbral | Permanecen altas | Cruzan desde abajo | Dejan de ser altas | Ya altas y suben | Ya altas y bajan |
|---|---:|---:|---:|---:|---:|
| 60 | 3945 | 0 | 0 | 3245 | 0 |
| 75 | 598 | 888 | 0 | 158 | 0 |
| 85 | 180 | 0 | 0 | 0 | 0 |
| 90 | 149 | 0 | 0 | 0 | 0 |
| 95 | 54 | 0 | 0 | 0 | 0 |
| 100 | 1 | 0 | 0 | 0 | 0 |

**H_to_FINAL:** suben 2282, bajan 0; delta medio 0.675 niveles.

| Umbral | Permanecen altas | Cruzan desde abajo | Dejan de ser altas | Ya altas y suben | Ya altas y bajan |
|---|---:|---:|---:|---:|---:|
| 60 | 3945 | 228 | 0 | 866 | 0 |
| 75 | 1486 | 297 | 0 | 346 | 0 |
| 85 | 180 | 210 | 0 | 38 | 0 |
| 90 | 149 | 91 | 0 | 25 | 0 |
| 95 | 54 | 0 | 0 | 0 | 0 |
| 100 | 1 | 0 | 0 | 0 | 0 |

| Umbral FINAL | Altas FINAL | Ya en BASE | Nuevas por aporte | Ya altas con incremento | BASE sobre eje | BASE ≤3 pasos eje | BASE en huella de sistema |
|---|---:|---:|---:|---:|---:|---:|---:|
| 60 | 4173 | 3945 | 228 | 866 | 115 | 705 | 973 |
| 75 | 1783 | 1486 | 297 | 346 | 54 | 321 | 403 |
| 85 | 390 | 180 | 210 | 38 | 13 | 58 | 77 |
| 90 | 240 | 149 | 91 | 25 | 12 | 45 | 62 |
| 95 | 54 | 54 | 0 | 0 | 12 | 22 | 27 |
| 100 | 1 | 1 | 0 | 0 | 0 | 1 | 1 |

| Morfología | Umbral | Celdas altas | Componentes | Mayor componente | Crestas candidatas | Compactas candidatas | Posibles anillos |
|---|---:|---:|---:|---:|---:|---:|---:|
| F | 85 | 467 | 1 | 467 | 1 | 0 | 0 |
| G | 85 | 180 | 1 | 180 | 1 | 0 | 0 |
| H | 60 | 3945 | 6 | 1369 | 3 | 1 | 0 |
| H | 75 | 1486 | 32 | 514 | 5 | 3 | 0 |
| H | 85 | 180 | 1 | 180 | 1 | 0 | 0 |
| H | 90 | 149 | 1 | 149 | 1 | 0 | 0 |
| H | 95 | 54 | 8 | 22 | 2 | 0 | 0 |
| H | 100 | 1 | 1 | 1 | 0 | 0 | 0 |
| FINAL | 85 | 390 | 3 | 334 | 2 | 1 | 0 |

## Enormous / -1387338765 — 400 × 267

Intento aceptado 0; 42722 celdas terrestres. Estado geológico SHA256 `F91B85E3A0BE51BA2EF11D1749684FE0D6F0247EC37FBD722781D4BF24FF5044`; BASE `4CB2B3416C8C8D443D72EAC18439591E81EE02FBDC45F9697B02B9846A94C178`; FINAL `13E03F0FC066757ADC939751677316583AB86616F36614A828773D41E0752E93`. Igualdad exacta con referencia previa y modo sin traza comprobada.

| Fuente | Mínimo | Media | Desv. | Máximo | r con E | r con F | r con G | r con H | Media en H≥85 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| A_InitialPressure | 0.000 | 0.026 | 0.182 | 1.721 | 0.523 | 0.350 | 0.315 | 0.304 | 1.665 |
| B_FourPassPressure | 0.000 | 0.022 | 0.109 | 1.405 | 0.624 | 0.457 | 0.411 | 0.403 | 1.127 |
| C_RegionalNoise5 | -0.855 | -0.162 | 0.301 | 0.702 | 0.843 | 0.908 | 0.917 | 0.920 | 0.013 |
| D_LocalNoise15 | -0.806 | -0.078 | 0.260 | 0.691 | 0.339 | 0.340 | 0.354 | 0.350 | 0.138 |
| E_Potential | -1.267 | -0.191 | 0.539 | 3.599 | 1.000 | 0.963 | 0.949 | 0.947 | 2.896 |

Anclas E: -1.267, -0.456, 0.126, 0.816, 3.599. Correlación B–compresión H04 propagada 0.338; BASE–compresión H04 0.159. Celdas H≥85 sin presión B: 0. Celdas altas a >5/>10/>20 pasos de contacto H04: 5/5/5. No equivalen a porcentajes causales.

| Etapa | Media | P90 | P99 | ≥60 celdas (%) | ≥75 celdas (%) | ≥85 celdas (%) | ≥90 celdas (%) | ≥95 celdas (%) | =100 celdas (%) |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| F_Percentiles | 31.567 | 63.000 | 87.000 | 5139 (12.029) | 2610 (6.109) | 809 (1.894) | 141 (0.330) | 13 (0.030) | 2 (0.005) |
| G_ShapeUpperRelief | 30.856 | 62.000 | 78.000 | 5139 (12.029) | 1083 (2.535) | 31 (0.073) | 13 (0.030) | 6 (0.014) | 2 (0.005) |
| H_BASE | 31.197 | 63.000 | 79.000 | 5139 (12.029) | 2007 (4.698) | 31 (0.073) | 13 (0.030) | 6 (0.014) | 2 (0.005) |
| FINAL | 32.692 | 65.000 | 82.000 | 5453 (12.764) | 2545 (5.957) | 247 (0.578) | 72 (0.169) | 6 (0.014) | 2 (0.005) |

**F_to_G:** suben 0, bajan 4739; delta medio -0.711 niveles.

| Umbral | Permanecen altas | Cruzan desde abajo | Dejan de ser altas | Ya altas y suben | Ya altas y bajan |
|---|---:|---:|---:|---:|---:|
| 60 | 5139 | 0 | 0 | 0 | 4739 |
| 75 | 1083 | 0 | 1527 | 0 | 2592 |
| 85 | 31 | 0 | 778 | 0 | 807 |
| 90 | 13 | 0 | 128 | 0 | 139 |
| 95 | 6 | 0 | 7 | 0 | 11 |
| 100 | 2 | 0 | 0 | 0 | 0 |

**G_to_H:** suben 3710, bajan 0; delta medio 0.341 niveles.

| Umbral | Permanecen altas | Cruzan desde abajo | Dejan de ser altas | Ya altas y suben | Ya altas y bajan |
|---|---:|---:|---:|---:|---:|
| 60 | 5139 | 0 | 0 | 3710 | 0 |
| 75 | 1083 | 924 | 0 | 343 | 0 |
| 85 | 31 | 0 | 0 | 0 | 0 |
| 90 | 13 | 0 | 0 | 0 | 0 |
| 95 | 6 | 0 | 0 | 0 | 0 |
| 100 | 2 | 0 | 0 | 0 | 0 |

**H_to_FINAL:** suben 4976, bajan 0; delta medio 1.495 niveles.

| Umbral | Permanecen altas | Cruzan desde abajo | Dejan de ser altas | Ya altas y suben | Ya altas y bajan |
|---|---:|---:|---:|---:|---:|
| 60 | 5139 | 314 | 0 | 1424 | 0 |
| 75 | 2007 | 538 | 0 | 520 | 0 |
| 85 | 31 | 216 | 0 | 12 | 0 |
| 90 | 13 | 59 | 0 | 3 | 0 |
| 95 | 6 | 0 | 0 | 0 | 0 |
| 100 | 2 | 0 | 0 | 0 | 0 |

| Umbral FINAL | Altas FINAL | Ya en BASE | Nuevas por aporte | Ya altas con incremento | BASE sobre eje | BASE ≤3 pasos eje | BASE en huella de sistema |
|---|---:|---:|---:|---:|---:|---:|---:|
| 60 | 5453 | 5139 | 314 | 1424 | 177 | 934 | 1697 |
| 75 | 2545 | 2007 | 538 | 520 | 109 | 406 | 597 |
| 85 | 247 | 31 | 216 | 12 | 5 | 22 | 22 |
| 90 | 72 | 13 | 59 | 3 | 3 | 13 | 13 |
| 95 | 6 | 6 | 0 | 0 | 2 | 6 | 6 |
| 100 | 2 | 2 | 0 | 0 | 0 | 2 | 2 |

| Morfología | Umbral | Celdas altas | Componentes | Mayor componente | Crestas candidatas | Compactas candidatas | Posibles anillos |
|---|---:|---:|---:|---:|---:|---:|---:|
| F | 85 | 809 | 10 | 475 | 6 | 0 | 0 |
| G | 85 | 31 | 3 | 22 | 0 | 1 | 0 |
| H | 60 | 5139 | 11 | 2849 | 2 | 0 | 0 |
| H | 75 | 2007 | 40 | 1085 | 4 | 1 | 0 |
| H | 85 | 31 | 3 | 22 | 0 | 1 | 0 |
| H | 90 | 13 | 1 | 13 | 0 | 1 | 0 |
| H | 95 | 6 | 1 | 6 | 0 | 0 | 0 |
| H | 100 | 2 | 1 | 2 | 0 | 0 | 0 |
| FINAL | 85 | 247 | 11 | 69 | 4 | 0 | 0 |

## Enormous / -132790047 — 400 × 267

Intento aceptado 0; 42730 celdas terrestres. Estado geológico SHA256 `D43775D0EE80CAC777ED60E59A4887CA7DC41690E5F299D5F0E6A7B30409DF6F`; BASE `3B4E5208EB6444EE47A5F87D091B43E11349B703FD18963D329BBAB32E92219F`; FINAL `1F797D94DEF4E9CB4A1AA945FCAD4156229E76486570F8BDC34E029CEB000B00`. Igualdad exacta con referencia previa y modo sin traza comprobada.

| Fuente | Mínimo | Media | Desv. | Máximo | r con E | r con F | r con G | r con H | Media en H≥85 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| A_InitialPressure | 0.000 | 0.021 | 0.139 | 1.569 | 0.384 | 0.303 | 0.291 | 0.275 | 1.324 |
| B_FourPassPressure | 0.000 | 0.020 | 0.085 | 0.989 | 0.464 | 0.389 | 0.369 | 0.351 | 0.818 |
| C_RegionalNoise5 | -0.708 | 0.023 | 0.304 | 0.712 | 0.836 | 0.831 | 0.843 | 0.843 | -0.054 |
| D_LocalNoise15 | -0.742 | -0.007 | 0.280 | 0.884 | 0.209 | 0.177 | 0.189 | 0.181 | 0.147 |
| E_Potential | -1.086 | 0.076 | 0.455 | 2.687 | 1.000 | 0.952 | 0.956 | 0.945 | 2.041 |

Anclas E: -1.086, -0.120, 0.430, 0.738, 2.687. Correlación B–compresión H04 propagada 0.512; BASE–compresión H04 0.253. Celdas H≥85 sin presión B: 0. Celdas altas a >5/>10/>20 pasos de contacto H04: 0/0/0. No equivalen a porcentajes causales.

| Etapa | Media | P90 | P99 | ≥60 celdas (%) | ≥75 celdas (%) | ≥85 celdas (%) | ≥90 celdas (%) | ≥95 celdas (%) | =100 celdas (%) |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| F_Percentiles | 32.625 | 71.000 | 87.000 | 7276 (17.028) | 3170 (7.419) | 720 (1.685) | 244 (0.571) | 21 (0.049) | 4 (0.009) |
| G_ShapeUpperRelief | 31.509 | 65.000 | 77.000 | 7276 (17.028) | 666 (1.559) | 62 (0.145) | 21 (0.049) | 10 (0.023) | 2 (0.005) |
| H_BASE | 32.207 | 71.000 | 77.000 | 7276 (17.028) | 2160 (5.055) | 62 (0.145) | 21 (0.049) | 10 (0.023) | 2 (0.005) |
| FINAL | 33.805 | 72.000 | 88.000 | 7661 (17.929) | 3011 (7.047) | 742 (1.736) | 283 (0.662) | 10 (0.023) | 2 (0.005) |

**F_to_G:** suben 0, bajan 6909; delta medio -1.116 niveles.

| Umbral | Permanecen altas | Cruzan desde abajo | Dejan de ser altas | Ya altas y suben | Ya altas y bajan |
|---|---:|---:|---:|---:|---:|
| 60 | 7276 | 0 | 0 | 0 | 6909 |
| 75 | 666 | 0 | 2504 | 0 | 3157 |
| 85 | 62 | 0 | 658 | 0 | 714 |
| 90 | 21 | 0 | 223 | 0 | 238 |
| 95 | 10 | 0 | 11 | 0 | 15 |
| 100 | 2 | 0 | 2 | 0 | 2 |

**G_to_H:** suben 6075, bajan 0; delta medio 0.698 niveles.

| Umbral | Permanecen altas | Cruzan desde abajo | Dejan de ser altas | Ya altas y suben | Ya altas y bajan |
|---|---:|---:|---:|---:|---:|
| 60 | 7276 | 0 | 0 | 6075 | 0 |
| 75 | 666 | 1494 | 0 | 49 | 0 |
| 85 | 62 | 0 | 0 | 0 | 0 |
| 90 | 21 | 0 | 0 | 0 | 0 |
| 95 | 10 | 0 | 0 | 0 | 0 |
| 100 | 2 | 0 | 0 | 0 | 0 |

**H_to_FINAL:** suben 7555, bajan 0; delta medio 1.598 niveles.

| Umbral | Permanecen altas | Cruzan desde abajo | Dejan de ser altas | Ya altas y suben | Ya altas y bajan |
|---|---:|---:|---:|---:|---:|
| 60 | 7276 | 385 | 0 | 2318 | 0 |
| 75 | 2160 | 851 | 0 | 844 | 0 |
| 85 | 62 | 680 | 0 | 17 | 0 |
| 90 | 21 | 262 | 0 | 6 | 0 |
| 95 | 10 | 0 | 0 | 0 | 0 |
| 100 | 2 | 0 | 0 | 0 | 0 |

| Umbral FINAL | Altas FINAL | Ya en BASE | Nuevas por aporte | Ya altas con incremento | BASE sobre eje | BASE ≤3 pasos eje | BASE en huella de sistema |
|---|---:|---:|---:|---:|---:|---:|---:|
| 60 | 7661 | 7276 | 385 | 2318 | 331 | 1686 | 2582 |
| 75 | 3011 | 2160 | 851 | 844 | 179 | 700 | 917 |
| 85 | 742 | 62 | 680 | 17 | 4 | 19 | 27 |
| 90 | 283 | 21 | 262 | 6 | 3 | 13 | 15 |
| 95 | 10 | 10 | 0 | 0 | 0 | 5 | 6 |
| 100 | 2 | 2 | 0 | 0 | 0 | 2 | 2 |

| Morfología | Umbral | Celdas altas | Componentes | Mayor componente | Crestas candidatas | Compactas candidatas | Posibles anillos |
|---|---:|---:|---:|---:|---:|---:|---:|
| F | 85 | 720 | 7 | 299 | 4 | 0 | 0 |
| G | 85 | 62 | 7 | 23 | 0 | 1 | 0 |
| H | 60 | 7276 | 15 | 4348 | 4 | 4 | 1 |
| H | 75 | 2160 | 66 | 591 | 11 | 3 | 0 |
| H | 85 | 62 | 7 | 23 | 0 | 1 | 0 |
| H | 90 | 21 | 2 | 15 | 0 | 1 | 0 |
| H | 95 | 10 | 2 | 6 | 0 | 0 | 0 |
| H | 100 | 2 | 1 | 2 | 0 | 0 | 0 |
| FINAL | 85 | 742 | 14 | 269 | 7 | 0 | 0 |

## Detalle adicional de los dos casos de referencia

En BASE del laboratorio el color es gris lineal H/100: solo 100 es blanco puro. No existe allí un corte categórico en 85. SUPERFICIE usa blanco para tierra >90. Por tanto ≥85 es un umbral numérico de relieve alto elegido para analizar componentes, no una equivalencia universal con “mancha blanca”. No se modificó ninguna paleta.

### Small / -1387338765

BASE ≥85: 170 celdas en 11 componentes; FINAL ≥85: 175, de las que 170 ya estaban en BASE (97.143%). El máximo de nivel 100 está en celda 12472 (columna 72, fila 62), a un paso de contacto H04 y 26 del eje seleccionado más próximo. La componente delgada mayor tiene 45 celdas, recorrido inferior 30, inradio 0 y dos extremos: una banda estrecha numéricamente demostrada. No puede atribuirse a la etapa 04, aún no ejecutada al producir BASE.

G reduce el total ≥85 de 223 a 170; H no cambia ese conjunto. F→G sube 55 celdas y baja 1227 en toda la tierra; G→H recupera 990. Aparecen cierres candidatos a ≥60, ≥90 y ≥95; los dos últimos comparten una zona cercana pero tienen interiores distintos. A ≥85 no se detecta un interior cerrado con estos criterios: el nivel elegido cambia la conectividad. Los cierres ≥90/95 ya existen en G porque H conserva todas esas alturas, pero no se midió morfología F a esos dos umbrales para demostrar si G los creó o los heredó.

| Componente H≥85 | Celdas | Representante | Recorrido inferior | Ancho máx. proxy | Elongación | Extremos | Grupos de ramificación | Dist. placa media | Dist. costa media | Dist. eje media |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 1 | 45 | 11457 | 30 | 1 | 31.000 | 2 | 0 | 0.000 | 34.822 | 24.511 |
| 2 | 43 | 12071 | 8 | 3 | 3.000 | 1 | 1 | 0.186 | 16.093 | 27.419 |
| 10 | 24 | 20017 | 11 | 3 | 3.667 | 2 | 0 | 0.042 | 11.833 | 2.625 |
| 7 | 19 | 18279 | 5 | 3 | 1.667 | 2 | 0 | 10.579 | 3.579 | 61.263 |
| 4 | 14 | 16837 | 10 | 1 | 10.000 | 2 | 0 | 0.000 | 28.214 | 26.500 |
| 5 | 10 | 17045 | 6 | 1 | 6.000 | 2 | 0 | 0.100 | 34.200 | 30.200 |
| 11 | 10 | 20201 | 6 | 1 | 6.000 | 2 | 0 | 0.000 | 1.400 | 11.000 |
| 3 | 2 | 16045 | 1 | 1 | 1.000 | 2 | 0 | 0.000 | 37.000 | 37.500 |
| 6 | 1 | 17827 | 0 | 1 | 0.000 | 1 | 0 | 0.000 | 22.000 | 20.000 |
| 8 | 1 | 18825 | 0 | 1 | 0.000 | 1 | 0 | 0.000 | 20.000 | 14.000 |
| 9 | 1 | 19621 | 0 | 1 | 0.000 | 1 | 0 | 0.000 | 16.000 | 8.000 |

| Umbral H | Interior representativo | Celdas interiores | Altura interior media | Mín./máx. borde | Contraste | Cumple filtros de posible anillo |
|---|---:|---:|---:|---|---:|---|
| 60 | 20813 | 21 | 41.143 | 60/83 | 18.857 | True |
| 75 | 16077 | 1 | 74.000 | 75/76 | 1.000 | False |
| 75 | 17490 | 1 | 74.000 | 75/76 | 1.000 | False |
| 85 | 12671 | 3 | 82.667 | 87/96 | 4.333 | False |
| 90 | 12670 | 5 | 84.600 | 90/97 | 5.400 | True |
| 95 | 12669 | 10 | 88.000 | 95/99 | 7.000 | True |

| Máximo representativo | Col/fila | Meseta celdas | BASE/FINAL | Dist. mín. H04 | Dist. mín. eje | A | B | C | D | E |
|---|---|---:|---|---:|---:|---:|---:|---:|---:|---:|
| 12472 | 72/62 | 1 | 100/100 | 1 | 26 | 1.240 | 0.748 | 0.050 | 0.023 | 1.946 |
| 13272 | 72/66 | 1 | 99/99 | 1 | 30 | 1.240 | 0.719 | 0.105 | -0.024 | 1.924 |
| 20416 | 16/102 | 1 | 99/99 | 3 | 2 | 1.285 | 0.984 | -0.560 | -0.545 | 1.486 |
| 12868 | 68/64 | 1 | 98/98 | 0 | 26 | 1.240 | 0.714 | -0.029 | 0.298 | 1.882 |
| 20610 | 10/103 | 2 | 95/95 | 0 | 2 | 1.285 | 0.884 | -0.483 | -0.408 | 1.397 |
| 17443 | 43/87 | 2 | 94/94 | 1 | 29 | 1.285 | 0.849 | -0.501 | -0.098 | 1.427 |
| 11856 | 56/59 | 1 | 94/94 | 4 | 15 | 1.285 | 0.722 | -0.157 | 0.129 | 1.658 |
| 12055 | 55/60 | 1 | 94/94 | 6 | 16 | 1.285 | 0.730 | -0.218 | 0.195 | 1.629 |
| 17233 | 33/86 | 1 | 92/92 | 4 | 26 | 1.285 | 0.805 | -0.397 | -0.272 | 1.374 |
| 18480 | 80/92 | 2 | 91/91 | 11 | 60 | 0.000 | 0.000 | 0.694 | 0.682 | 1.209 |
| 17036 | 36/85 | 1 | 91/91 | 1 | 29 | 1.285 | 0.806 | -0.450 | -0.349 | 1.274 |
| 20400 | 0/102 | 1 | 91/91 | 7 | 12 | 1.285 | 0.797 | -0.507 | 0.079 | 1.370 |
| 13249 | 49/66 | 4 | 90/90 | 10 | 24 | 1.285 | 0.730 | -0.460 | 0.458 | 1.433 |
| 13050 | 50/65 | 1 | 90/90 | 13 | 22 | 1.285 | 0.722 | -0.444 | 0.353 | 1.386 |
| 14642 | 42/73 | 1 | 89/89 | 3 | 34 | 1.285 | 0.715 | -0.460 | 0.346 | 1.347 |
| 14045 | 45/70 | 2 | 88/88 | 7 | 29 | 1.285 | 0.716 | -0.493 | 0.370 | 1.314 |
| 15443 | 43/77 | 1 | 88/88 | 0 | 38 | 1.285 | 0.730 | -0.442 | -0.096 | 1.207 |
| 15844 | 44/79 | 1 | 87/87 | 0 | 39 | 1.285 | 0.730 | -0.431 | -0.253 | 1.151 |
| 17628 | 28/88 | 1 | 87/87 | 9 | 22 | 1.285 | 0.735 | -0.371 | -0.443 | 1.155 |
| 16045 | 45/80 | 2 | 85/85 | 0 | 37 | 1.285 | 0.722 | -0.425 | -0.342 | 1.099 |
| 17827 | 27/89 | 1 | 85/85 | 10 | 20 | 1.285 | 0.724 | -0.397 | -0.458 | 1.088 |
| 18825 | 25/94 | 1 | 85/85 | 14 | 14 | 1.285 | 0.730 | -0.471 | -0.285 | 1.084 |
| 19621 | 21/98 | 1 | 85/85 | 8 | 8 | 1.285 | 0.730 | -0.516 | -0.222 | 1.054 |

### Enormous / -132790047

BASE ≥85: 62 celdas en 7 componentes; FINAL ≥85: 742, de las que 62 ya estaban en BASE (8.356%) y 680 cruzaron por aporte. Las dos celdas BASE=100 se conservan; una meseta máxima tiene representante 603 (columna 203, fila 1), junto a contacto H04 y a un paso del eje. No todos los máximos están alejados de H04: los nueve máximos ≥85 de este caso están a ≤10 pasos de un contacto, pero solo dos mesetas quedan a ≤3 pasos de ejes seleccionados.

F→G reduce ≥85 de 720 a 62 y baja 6909 celdas sin subidas; H recupera 6075 por debajo de la máscara ≥85. Se detecta un posible cierre a ≥60 con 79 celdas interiores y contraste 13.519; otros tres cierres de ese nivel no pasan el contraste. No hay candidatos a ≥75/85/90/95/100 bajo los filtros usados. Esto no identifica por sí solo el anillo de una captura ni demuestra ausencia visual de formas anulares.

| Componente H≥85 | Celdas | Representante | Recorrido inferior | Ancho máx. proxy | Elongación | Extremos | Grupos de ramificación | Dist. placa media | Dist. costa media | Dist. eje media |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 1 | 23 | 202 | 8 | 3 | 2.667 | 3 | 1 | 0.130 | 3.087 | 2.043 |
| 5 | 23 | 88985 | 10 | 3 | 3.333 | 2 | 0 | 0.000 | 0.826 | 19.217 |
| 7 | 9 | 90971 | 6 | 1 | 6.000 | 2 | 0 | 0.000 | 0.444 | 34.111 |
| 6 | 3 | 90575 | 2 | 1 | 2.000 | 2 | 0 | 0.000 | 0.667 | 28.000 |
| 3 | 2 | 87797 | 1 | 1 | 1.000 | 2 | 0 | 0.000 | 0.500 | 5.500 |
| 2 | 1 | 12556 | 0 | 1 | 0.000 | 1 | 0 | 0.000 | 41.000 | 1.000 |
| 4 | 1 | 88193 | 0 | 1 | 0.000 | 1 | 0 | 0.000 | 0.000 | 9.000 |

| Umbral H | Interior representativo | Celdas interiores | Altura interior media | Mín./máx. borde | Contraste | Cumple filtros de posible anillo |
|---|---:|---:|---:|---|---:|---|
| 60 | 44560 | 24 | 55.417 | 60/69 | 4.583 | False |
| 60 | 57511 | 79 | 46.481 | 60/76 | 13.519 | True |
| 60 | 85527 | 6 | 59.000 | 60/62 | 1.000 | False |
| 60 | 95933 | 26 | 57.192 | 60/63 | 2.808 | False |
| 75 | 5783 | 1 | 74.000 | 75/81 | 1.000 | False |
| 75 | 66319 | 39 | 74.000 | 75/76 | 1.000 | False |
| 75 | 69113 | 21 | 74.000 | 75/76 | 1.000 | False |
| 75 | 72319 | 3 | 74.000 | 75/76 | 1.000 | False |
| 75 | 73917 | 40 | 72.275 | 75/76 | 2.725 | False |

| Máximo representativo | Col/fila | Meseta celdas | BASE/FINAL | Dist. mín. H04 | Dist. mín. eje | A | B | C | D | E |
|---|---|---:|---|---:|---:|---:|---:|---:|---:|---:|
| 603 | 203/1 | 2 | 100/100 | 0 | 1 | 1.569 | 0.981 | 0.112 | 0.148 | 2.663 |
| 89385 | 185/223 | 4 | 95/95 | 0 | 17 | 1.393 | 0.975 | -0.230 | 0.040 | 2.158 |
| 91369 | 169/228 | 1 | 89/89 | 0 | 33 | 1.393 | 0.776 | -0.014 | 0.333 | 2.071 |
| 91766 | 166/229 | 1 | 89/89 | 0 | 36 | 1.393 | 0.763 | 0.046 | 0.447 | 2.168 |
| 90575 | 175/226 | 2 | 86/86 | 0 | 27 | 1.393 | 0.783 | -0.126 | 0.007 | 1.796 |
| 90578 | 178/226 | 1 | 86/86 | 0 | 24 | 1.393 | 0.791 | -0.145 | -0.080 | 1.755 |
| 87797 | 197/219 | 2 | 85/89 | 0 | 5 | 1.393 | 0.774 | -0.281 | 0.328 | 1.717 |
| 12556 | 156/31 | 1 | 85/94 | 0 | 1 | 1.122 | 0.695 | 0.154 | -0.129 | 1.880 |
| 88193 | 193/220 | 1 | 85/85 | 0 | 9 | 1.393 | 0.776 | -0.273 | 0.239 | 1.692 |

## Rendimiento normal sin traza

La captura inicial y la posterior registran tres muestras por mundo con el mismo protocolo GenerateGeology y calentamiento Small. Su cambio de mediana fue −16.5%..+35.5%, por lo que no se asumió ausencia de regresión. Después de terminar la suite general se ejecutaron los binarios previos/actuales en orden old/new/new/old: seis muestras por mundo y versión. Las diferencias fueron −7.006%..+1.166%, sin una regresión sistemática observable. Los hashes State/BASE/FINAL coinciden en los cuatro procesos (todos código 0). [Tabla completa y límites de rendimiento](../P03-A74-performance_20261108_223547490Z/REPORT.md). No se detecta el incremento sistemático de las primeras mediciones; no se demuestra coste exactamente cero ni una mejora. Se verifica Fields=null sin nuevas matrices por celda en modo normal. La comparación está limitada por variación del equipo, calentamiento y tamaño de muestra.

| Mundo | Mediana previa ms | Mediana actual ms | Cambio % | Asignación mediana previa bytes | Cambio asignación bytes | State/BASE/FINAL idénticos |
|---|---:|---:|---:|---:|---:|---|
| Small/-1387338765 | 224.895 | 227.517 | 1.166 | 12860400 | 1272 | True |
| Small/-132790047 | 200.365 | 186.327 | -7.006 | 13045816 | 40 | True |
| Medium/-1387338765 | 307.045 | 291.443 | -5.081 | 20247520 | -2432 | True |
| Medium/-132790047 | 443.088 | 430.368 | -2.871 | 20265140 | -108 | True |
| Large/-1387338765 | 654.775 | 645.824 | -1.367 | 29196756 | -448 | True |
| Large/-132790047 | 628.496 | 608.181 | -3.232 | 28590096 | 1320 | True |
| Enormous/-1387338765 | 1309.298 | 1276.427 | -2.511 | 50600888 | -672 | True |
| Enormous/-132790047 | 1323.170 | 1290.725 | -2.452 | 51253724 | 1028 | True |

## Resultados de validación

| Ejecución | Estado | Código | Duración s | Log |
|---|---|---:|---:|---|
| Build aislado | PASS, 0 advertencias/errores | 0 | 6.422 | [build.log](build.log) |
| Auditoría A–H, ocho mundos | PASS | 0 | 57.754 | [base-audit.log](base-audit.log) |
| Equivalencia por etapas, 28 Pangea + 12 controles | PASS | 0 | 99.972 | [pangaea-staged.log](pangaea-staged.log) |
| Suite general | **FAIL: 17 PASS / 2 FAIL históricos** | 1 | 564.103 | [all-suites.log](all-suites.log) |

Los dos fallos se conservan por separado: **TectonicMotion**, 4 escenarios de elevación; **ContinentalGeology**, 12. Se compararon los 32 registros de escenario/hash de baseline con el [informe P09.1](../P09-1_20261108_032515779Z_96457619/REPORT.md): cero diferencias en esperados u obtenidos. No se actualizó ningún baseline ni se presenta la general como aprobada. Las 28 líneas STAGED_EQ y los 12 STAGED_OTHER_PASS también son idénticos al informe previo.

Los 461 componentes detallados convergieron en el adelgazamiento; ninguno de esos registros envuelve el cilindro ni supera compacidad 1. Se conserva la advertencia metodológica para otras semillas/umbrales. El texto de los métodos RecoverRegionalShoulders y ShapeUpperRelief es idéntico al del commit base. La [precomprobación](../P03-A74-precheck_20261108_222423500Z_713a1e12/REPORT.md) no se utiliza para certificar el SHA final; la referencia anterior a instrumentación y la ejecución canónica se conservan separadas.

## Recomendaciones, no implementadas

1. **Relieve regional a conservar:** mantener la variación continental de baja/media elevación y separar sus objetivos de los de cumbres tectónicas. No eliminar todo el ruido ni imponer cuotas de montañas/desiertos para ocultar formas; la recuperación de hombros restaura una parte apreciable de ese relieve.
2. **Relieve dependiente de convergencia:** evaluar en una entrega autorizada el uso de H04 para la elevación tectónica de BASE y su relación con los sistemas seleccionados. La presión heredada también interviene en la superficie: sustituir sus velocidades dentro de GenerateSurface alteraría costas. Cualquier ensayo de BASE debe mantener fija esa máscara y desacoplar el consumo posterior, sin cambiar hoy el flujo aprobado.
3. **Procesamiento potencialmente artificial:** estudiar el máximo forzado de F/G y la competencia con el techo 94 de FINAL. Analizar sensibilidad del soporte .25, cuadrado de detalle, máximo global de prominencia y escalas fijas/proporcionales. No son recomendaciones de valores nuevos; requieren contrafactuales separados y autorización.
4. **Investigación adicional:** morfología F/G a todos los umbrales de cada cierre y fuentes dentro/borde, prominencia topográfica en vez de máximos locales, sensibilidad al umbral/adelgazamiento y comparación controlada de rendimiento. Registrar estas limitaciones evita atribuir causalidad a una correlación o a un anillo detectado en una sola altura.
5. **Validación posterior:** el usuario revisa coordenadas/capas en Evolve; cualquier nueva estrategia debe volver a demostrar conservación de máscara/tectónica y evaluar consecuencias en FINAL/clima/biomas. Esta auditoría no aprueba P03-A ni autoriza modificaciones correctivas.

## Archivos de implementación

- `GEvolve.Engine/World/LandmassGeneration/PangaeaBaseFields.cs`
- `GEvolve.Engine/World/LandmassGeneration/PangaeaElevationDiagnostics.cs`
- `GEvolve.Engine/World/LandmassGeneration/PangaeaFromPlatesGenerator.cs`
- `GEvolve.Engine/World/LandmassGeneration/PangaeaSurfaceWork.cs`
- `GEvolve.Engine/World/PangaeaGenerationSession.cs`
- `GEvolve.Engine/World/WorldGenerationService.cs`
- `GEvolve.Tests/PangaeaBaseAuditTests.cs`
- `GEvolve.Tests/PangaeaBaseMorphology.cs`
- `GEvolve.Tests/Program.cs`
- `GEvolve.Tests/ValidationReports/P03-A74-reference_20261108_221618802Z_0fd6f9ac/REPORT.md`
- `GEvolve.Tests/ValidationReports/P03-A74-reference_20261108_221618802Z_0fd6f9ac/SUMMARY.md`
- `GEvolve.Tests/ValidationReports/P03-A74-reference_20261108_221618802Z_0fd6f9ac/base-reference.log`
- `GEvolve.Tests/ValidationReports/P03-A74-reference_20261108_221618802Z_0fd6f9ac/build.log`
- `GEvolve.Tests/ValidationReports/P03-A74-reference_20261108_221618802Z_0fd6f9ac/commands.ps1`
- `GEvolve.Tests/ValidationReports/commands.ps1`

Los informes posteriores añaden resultados de auditoría/regresión y documentación/roadmap/memoria. El swap ajeno `.BiomeTests.cs.swp` queda sin tocar ni versionar. No se actualiza ningún baseline.

## Repetición

Desde la raíz del repositorio:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\GEvolve.Tests\ValidationReports\commands.ps1 -ValidationSet BaseReliefAudit -Label P03-A74 -IsolatedArtifacts
```

Incluye compilación, los ocho mundos auditados, equivalencia por etapas y suite general. BaseAuditControls solo ejecuta build+auditoría; BaseAuditReference captura el flujo normal actual, sin sobrescribir la referencia previa. Logs completos y códigos quedan en un directorio UTC nuevo; no se lanzan ventanas. La corrección futura espera revisión y decisión del usuario.

Revisión de publicación: 24 archivos de reportes de esta auditoría, sin coincidencias de rutas personales/credenciales en la búsqueda estática ni enlaces Markdown relativos ausentes. Tras las cuatro ejecuciones intercaladas se eliminaron únicamente los tres directorios propios de artefactos aislados, verificando rutas y ausencia de enlaces. Los informes y el swap ajeno se conservan.
