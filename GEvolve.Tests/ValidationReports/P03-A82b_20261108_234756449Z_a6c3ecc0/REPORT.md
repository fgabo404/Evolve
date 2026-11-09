# P03-A.8.2b — Escalas regionales, presión factorial y tierras bajas

**Estado: calibración experimental completada; selección y aprobación visual pendientes.** No se modifica ni sustituye BASE oficial. No se inicia P03-A.8.3, no se añade UI ni se ejecuta Evolve gráficamente. No hay capturas, PNG/SVG/CSV ni partidas experimentales.

**Commit de implementación validado:** `47c7c7da9eab52ecadbf99a7aa87603186a7e4a2`, `test(worldgen): calibrate regional base elevation scales and coastal transitions`. Rama `worldgeneration_pangea`; base publicada `53601ff94227af5d14bb3644367a35a22edac20a`. [SUMMARY.md](SUMMARY.md) identifica SDK, UTC, fuente limpia, comandos, códigos y tiempos; [lowland-variants.log](lowland-variants.log) conserva todos los datos numéricos.

Referencias inmutables: [P03-A.8.1–8.2](../P03-A8_20261108_231013846Z_120f9b55/REPORT.md) y [P03-A.7.4](../P03-A74_20261108_222619723Z_ca4e68da/REPORT.md). Las cinco variantes anteriores se vuelven a calcular, no se reescriben sus archivos. Se exige igualdad exacta de los 60 pares de hashes de altura/potencial y de sus objetos de métricas históricos.

## Recomendación

**Priorizar V4O para una futura evaluación visual, con V4U como comparación de banda superior.** V4O elimina el centrado demasiado alto de V2 mediante un desplazamiento fijo y mantiene pendiente suave sin obligar a máximos 100. No es una selección definitiva: el promedio continental todavía varía por semilla y algunas costas empeoran frente a V0, aunque no existe una subida uniforme del continente como en V2.

V4S reduce el suelo recortado, pero comprime más el relieve y aumenta las corridas cuantizadas a igual distancia costera. V4C baja el salto al océano, pero aumenta pendientes interiores y proxies de uniformidad costera en parte de la muestra. **No recomendar V4C como solución costera final ni como candidata visual prioritaria.** El criterio anti-terrazas no queda aprobado por una mejor media costera; se conservan esos resultados como experimento negativo/inconcluso.

La evaluación visual futura necesitará una nueva decisión para exponer variantes en Evolve. Esta entrega no implementa ese mecanismo ni adopta valores experimentales en FINAL, clima o biomas.

## Arquitectura y protección

El módulo sigue exclusivamente en Tests y consume campos C/D/B y máscara oficiales congelados. El API original Generate(Input,Kind) mantiene la semántica de V0/V1/V1T/V2/V3; un helper de Tests parametriza solo las dos vías de presión para el factorial. El comparador comparte su arnés previo, manteniendo sus referencias y modos anteriores. Los métodos de Shape/Recover se reutilizan por los delegates ya existentes, sin copiar ni modificar Engine.

Los casos obligatorios son tres semillas ×cuatro tamaños (12), y se reserva 42 como control independiente en cuatro tamaños (4). Se fijaron todas las constantes antes de medir; no se reajustó ninguna tras ver datos principales o de control. Once variantes (cinco históricas y seis nuevas) ×16 configuraciones =176 comparaciones, 132 principales +44 de control. No hay búsqueda automática ni optimización por una métrica.

Se verifica: V0 F/G/H y potencial exactos; 60 registros previos de V0–V3 con métricas idénticas; controles P03-A.7.4; igualdad automática/manual de geología/clima/biomas; entradas congeladas sin mutación; generación normal antes/después idéntica; guardado/carga oficial completo e histórico sin campos ambientales. El código productivo de Engine/Game/State/Definitions y los baselines no cambian.

## Definición de variantes nuevas

P=1.3C+.45D para las variantes regionales. Q, S y R siguen siendo percentiles, ShapeUpperRelief y RecoverRegionalShoulders originales. Se conservan V0/V1/V1T/V2/V3 y sus [fórmulas anteriores](../../PANGAEA_BASE_VARIANTS.md).

| Nueva | Referencia | Fórmula / componente cambiado | Hipótesis | Ventaja esperada | Riesgo |
|---|---|---|---|---|---|
| VP | V0 | Q(2.5B+P), soporte S con B=0; R idéntico | Retirar solo presión del soporte | Distinguir soporte de potencial | Bandas de potencial y máximo estadístico permanecen |
| VS | V1 | Q(P), soporte S con B original; R idéntico | Añadir solo presión en soporte | Medir soporte con el potencial regional fijo | Puede remarcar contactos heredados |
| V4O | V2 | h=clamp(32.5+(99/3.5)P,1,100) | Solo desplazamiento vertical | Referencia regional baja/intermedia fija | Recorte inferior a 1 puede formar mesetas |
| V4S | V4O | h=clamp(32.5+25P,1,100) | Solo escala vertical | Menor amplitud entre semillas | Puede borrar tierras altas útiles o aumentar cuantización |
| V4U | V4O | Identidad h≤50; h=50+e/(1+e/30), e=h_original−50 | Solo compresión superior | Preservar llanuras, moderar cumbres sin máximo observado | No elimina familias de contornos cerrados |
| V4C | V4U | Taper contextual costero descrito abajo | Solo costa, padre fijo | Bajar costas bajas/suaves, proteger altas/abruptas | Terrazas, nuevos gradientes y clasificación imperfecta de costas |

V4S y V4U son dos ramas desde V4O, no un cambio combinado. Round al par y contrato tierra 1..100 se aplican al final; océano sigue cero. La compresión y costa usan alturas continuas antes del redondeo. VP mantiene exactamente potencial/anclas/F de V0; VS los de V1. Los extremos (presión sí/sí y no/no) reproducen V0/V1.

### Constantes e interpretación

- 32.5=(15+50)/2: centro del intervalo bajo/intermedio vigente. No se toma de la media de las semillas. V2 centraba 50.5; V4O desplaza exactamente 18 niveles, con el mismo contrato de límite inferior.
- Escala original 99/3.5: rango terrestre 100−1 dividido por amplitud teórica total 2*(1.3+.45). Escala 25=(80−15)/(2*1.3): ancho regional vigente 65 asociado a amplitud primaria regional ±1.3. No cambia el peso local .45 ni se usan percentiles de cada mundo.
- Umbral superior 50 y span 30=80−50: ancla intermedia y ancho regional superior ya existentes. La curva es continua y C1 en 50, con derivada superior 1/(1+e/30)²>0. No corta horizontalmente ni reescala por el máximo observado.
- R=min(columnas,filas)/(2*15): media longitud de onda primaria de D. En Small/Medium/Large/Enormous es 4.433/5.567/6.667/8.900 pasos desde la primera celda costera. Se usa la frecuencia existente, sin muestrear ruido nuevo.
- Protección por altura 49=50−1: un nivel de margen de cuantización preserva exactamente todo padre redondeado ≥50. Protección por relieve 35=50−15: intervalo bajo/intermedio. Retención mínima .5: límite experimental explícito para conservar al menos la mitad del exceso sobre altura terrestre 1. Es una restricción de seguridad geométrica de la hipótesis, no una ley física.

### Costa V4C

d=distancia hexagonal a la primera celda terrestre costera (d=0), por lo que la distancia real al océano es d+1. r=max|h−h_vecino| entre vecinos terrestres reales de V4U; excluir océano evita que el salto que se quiere estudiar active por sí mismo toda protección. s(x)=t²(3−2t), t=clamp(x,0,1).

w=1−s(d/R); a=1−s((h−15)/(49−15)); b=1−s(r/35); h_c=1+(h−1)*(1−.5*w*a*b). Si d≥R, h≥49 o r≥35, se devuelve h sin cambios. Fuera del soporte compacto y para alturas redondeadas ≥50 se comprueba igualdad exacta. No se fija una banda a altura constante; el taper y sus protecciones son suaves, pero distancia/altura rasterizadas pueden crear patrones cuantizados. La función no eleva el continente.

Altura/relieve son proxies y no una clasificación geomorfológica real: no se demuestra conservación de toda costa montañosa moderada o de todos los acantilados legítimos. Se comprueban por separado protecciones sintéticas de costa alta, abrupta y exterior, variedad de niveles y retención mínima. El resultado real se contrasta con nuevos indicadores de corridas y dominancia por profundidad; no se da por aprobada la condición de evitar líneas artificiales.

## Método de medición

Se mantienen media/mediana/P90/P99/máximo, umbrales ≥60/75/85/90/95/=100, componentes, crestas, anchuras/elongación, anillos conservadores, máximos, pendientes/saltos, distancia a placas/costa/ejes y correlación H04 de P03-A.8. Los cortes P90/P99 relativos son solo diagnósticos, no cuotas; el redondeo cambia sus áreas. Se analizan 1387 máscaras distintas en las 176 comparaciones.

Se añaden bandas [1,15], [16,50], [51,60], [61,75], [76,84], [85,94], [95,100], nivel 1 y franjas de distancia real al océano: primera terrestre=1, agregado 1–3, 4–8, interior≥9. **Primera=1 se incluye deliberadamente también en 1–3**; esas filas no se suman como partición. Se publican además profundidades exactas 1..8 y saltos terrestres por franja. Las bandas anteriores de distancia a costa se conservan sin cambiar su convenio.

El proxy de corridas litorales agrupa celdas conectadas con igual altura cuantizada y misma distancia entera al océano ≤8; exige ≥6 celdas y recorrido inferior ≥5 pasos. Se publica número, tamaño, recorrido y ejemplos antes/después, además de nuevas aristas iguales a lo largo de una misma profundidad. No es identificación visual de una terraza: la cuantización y el campo original ya generan muchas corridas. Un aumento es una señal de riesgo que se interpreta junto con pendiente, dominancia por profundidad y cambios costeros.

La comparación factorial mantiene reglas de normalización/Shape/Recover; el contraste del potencial incluye el reajuste de las anclas Q que ese potencial induce. No confundirlo con congelar anclas numéricas. Correlación o proximidad a H04 no prueba causalidad; BASE regional no se puntúa por parecer tectónica activa.

## Factorial de presión — resultados principales

| Mundo | Pendiente V0 /VP /VS /V1 | Aristas ≥20 V0 /VP /VS /V1 | Crestas P99 V0 /VP /VS /V1 |
|---|---|---|---|
| Small/-1387338765 | 5.167 /5.075 /3.556 /3.556 | 624 /574 /0 /0 | 2 /2 /0 /0 |
| Small/-132790047 | 4.538 /4.456 /3.867 /3.854 | 208 /182 /1 /1 | 2 /0 /1 /0 |
| Small/2026 | 5.606 /5.830 /3.690 /3.689 | 584 /624 /0 /0 | 0 /0 /0 /0 |
| Medium/-1387338765 | 3.621 /3.616 /3.157 /3.157 | 249 /237 /0 /0 | 1 /0 /0 /0 |
| Medium/-132790047 | 4.401 /4.302 /3.630 /3.560 | 420 /427 /6 /6 | 3 /1 /1 /0 |
| Medium/2026 | 4.224 /4.338 /2.762 /2.762 | 639 /667 /0 /0 | 3 /0 /0 /0 |
| Large/-1387338765 | 3.704 /3.600 /2.447 /2.445 | 611 /594 /0 /0 | 4 /4 /0 /0 |
| Large/-132790047 | 3.609 /3.572 /2.875 /2.875 | 416 /457 /0 /0 | 1 /2 /0 /0 |
| Large/2026 | 3.539 /3.554 /3.117 /3.110 | 148 /137 /0 /0 | 2 /1 /0 /0 |
| Enormous/-1387338765 | 2.283 /2.341 /1.887 /1.887 | 374 /426 /0 /0 | 3 /3 /0 /0 |
| Enormous/-132790047 | 3.094 /3.126 /2.119 /2.105 | 1092 /1154 /0 /0 | 5 /6 /2 /1 |
| Enormous/2026 | 2.631 /2.653 /2.148 /2.147 | 604 /661 /0 /0 | 1 /2 /2 /2 |

Suma de aristas ≥20 en los doce mundos: V0=5969, VP=6140, VS=7, V1=7. Efecto de incluir presión en potencial: +5962 con soporte activo (V0−VS), +6133 sin soporte (VP−V1). Efecto de incluir presión en soporte: −171 con presión en potencial (V0−VP), 0 sin ella (VS−V1). La presión del potencial domina este indicador; retirar solo soporte no lo corrige. No implica que el soporte no afecte celdas o morfología: el reescalado global puede subir unas alturas y bajar otras.

Las cuatro variantes del factorial mantienen máximo 100 en todos los casos: el forzado de máximo no requiere presión, pues procede de Q y S. Sus cantidades ≥95/=100, ubicaciones de máximos y componentes sí varían; se incluyen en las tablas y en los deltas del log. No se atribuye el máximo estadístico exclusivamente a B.

## Rangos de los doce mundos principales

| Variante | Media mínima–máxima | Máximo mínimo–máximo | Salto costero medio mín.–máx. | Aristas ≥20, suma | Celdas =1, suma | Corridas litoral, suma |
|---|---|---|---|---:|---:|---:|
| V0 | 31.193–32.341 | 100–100 | 26.040–41.720 | 5969 | 598 | 875 |
| V1 | 31.347–32.935 | 100–100 | 23.725–45.435 | 7 | 611 | 942 |
| V1T | 31.746–33.207 | 100–100 | 23.688–46.178 | 8 | 611 | 916 |
| V2 | 42.810–54.613 | 75–85 | 44.318–54.281 | 0 | 0 | 959 |
| V3 | 42.748–54.407 | 73–80 | 44.236–53.946 | 0 | 0 | 976 |
| VP | 31.124–32.582 | 100–100 | 26.045–41.845 | 6140 | 598 | 839 |
| VS | 31.347–33.011 | 100–100 | 23.725–45.435 | 7 | 611 | 942 |
| V4O | 24.820–36.613 | 57–67 | 26.318–36.281 | 0 | 473 | 965 |
| V4S | 25.701–36.139 | 54–63 | 27.028–35.845 | 0 | 90 | 1230 |
| V4U | 24.787–36.565 | 56–61 | 26.301–36.088 | 0 | 473 | 979 |
| V4C | 23.960–36.015 | 56–61 | 20.158–30.871 | 0 | 478 | 1092 |

V4O no eleva uniformemente el continente: medias 24.8–36.6 frente a 31.2–32.3 oficiales; todavía hay semillas por encima y por debajo de V0, como corresponde a una escala fija sobre distintas máscaras. La referencia consistente h(P) no garantiza una media mundial idéntica. V4S reduce recorte a nivel 1 (473→90 celdas en la suma de los doce mundos), pero aumenta corridas costeras cuantizadas. No se aprueba por una sola métrica.

## Small/-1387338765 — 200×133

Tierra 10995. Estado oficial `881BFF8D682CA53D75331BA9E15C22651C68AD25DD24C86104B8FDDA6674BD1B`, BASE `0068BB72CB8AA3B526702C274A7262BC32DCFF4C0D6028DD70B50513BBE43428`, FINAL `0F1C615E0DCD5DA290992E92D32996D3F3FB3C9BCE63C0343676A8A93E9D73C8`; idénticos antes/después, automático/manual y guardado/carga. Mismo fixture y máscara para todas las variantes.

| Variante | Media | Mediana | P90 | P99 | Máx. | Pendiente media/máx. | Costa media/máx. | Δ medio padre | Δ absoluto V0 |
|---|---:|---:|---:|---:|---:|---|---|---:|---:|
| V0 | 31.193 | 25.000 | 65 | 87 | 100 | 5.167/45 | 41.720/90 | 0.000 | 0.000 |
| V1 | 31.347 | 26.000 | 67 | 80 | 100 | 3.556/17 | 45.435/86 | 0.153 | 6.451 |
| V1T | 31.746 | 26.000 | 67 | 89 | 100 | 3.561/17 | 46.178/92 | 0.400 | 6.775 |
| V2 | 42.810 | 42.000 | 60 | 74 | 85 | 2.127/8 | 50.488/78 | 11.063 | 16.179 |
| V3 | 42.748 | 42.000 | 60 | 72 | 80 | 2.103/8 | 50.389/75 | -0.062 | 16.241 |
| VP | 31.124 | 25.000 | 65 | 86 | 100 | 5.075/45 | 41.845/86 | -0.069 | 0.301 |
| VS | 31.347 | 26.000 | 67 | 80 | 100 | 3.556/17 | 45.435/86 | 0.000 | 6.451 |
| V4O | 24.820 | 24.000 | 42 | 56 | 67 | 2.120/8 | 32.488/60 | -17.989 | 9.148 |
| V4S | 25.701 | 25.000 | 41 | 53 | 63 | 1.894/7 | 32.505/56 | 0.881 | 10.110 |
| V4U | 24.787 | 24.000 | 42 | 55 | 61 | 2.102/8 | 32.447/57 | -0.033 | 9.181 |
| V4C | 24.050 | 23.000 | 41 | 55 | 61 | 2.272/8 | 26.650/57 | -0.738 | 9.625 |

| Nueva variante | ≥60 /≥75 /≥85 /≥90 /≥95 /=100 celdas | ≥20 tierra | Crestas /compactas /anillos P99 relativo | Recorrido /ancho /elongación del componente P99 principal | r altura–H04 |
|---|---|---:|---|---|---:|
| VP | 1540 /576 /137 /58 /19 /2 | 574 | 2/1/0 (nivel 86) | 22/3/7.333 | 0.279 |
| VS | 1543 /498 /49 /23 /12 /2 | 0 | 0/1/0 (nivel 80) | 18/9/2.000 | -0.060 |
| V4O | 43 /0 /0 /0 /0 /0 | 0 | 0/2/0 (nivel 56) | 15/9/2.889 | -0.068 |
| V4S | 20 /0 /0 /0 /0 /0 | 0 | 0/0/0 (nivel 53) | 17/9/3.222 | -0.069 |
| V4U | 15 /0 /0 /0 /0 /0 | 0 | 0/2/0 (nivel 55) | 16/9/3.000 | -0.068 |
| V4C | 15 /0 /0 /0 /0 /0 | 0 | 0/2/0 (nivel 55) | 16/9/3.000 | -0.065 |

| Nueva variante | 1–15 | 16–50 | 51–60 | 61–75 | 76–84 | 85–94 | 95–100 |
|---|---:|---:|---:|---:|---:|---:|---:|
| VP | 3827 (34.807%) | 4797 (43.629%) | 918 (8.349%) | 932 (8.477%) | 384 (3.492%) | 118 (1.073%) | 19 (0.173%) |
| VS | 3815 (34.698%) | 4804 (43.693%) | 887 (8.067%) | 1090 (9.914%) | 350 (3.183%) | 37 (0.337%) | 12 (0.109%) |
| V4O | 2709 (24.638%) | 7936 (72.178%) | 313 (2.847%) | 37 (0.337%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) |
| V4S | 1984 (18.045%) | 8780 (79.854%) | 216 (1.965%) | 15 (0.136%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) |
| V4U | 2709 (24.638%) | 7936 (72.178%) | 347 (3.156%) | 3 (0.027%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) |
| V4C | 2985 (27.149%) | 7660 (69.668%) | 347 (3.156%) | 3 (0.027%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) |

Costa: cada celda de tabla muestra **media /mediana /P90**; distribuciones completas con P99, máximo, dispersión, conteo y niveles dominantes están en el log. Primera celda y 1–3 se solapan como se indicó.

| Curva | Primera, océano d=1 | Océano d=1–3 | d=4–8 | Interior d≥9 | Salto medio tierra litoral | Corridas antes→después | Nuevas aristas iguales a misma profundidad |
|---|---|---|---|---|---:|---|---:|
| V2 | 50.547/50.000/65.000 | 50.146/50.000/65.000 | 47.619/47.000/63.000 | 39.821/39.000/54.000 | 1.145 | 2→7 | 523 |
| V4O | 32.547/32.000/47.000 | 32.146/32.000/47.000 | 29.619/29.000/45.000 | 21.837/21.000/36.000 | 1.145 | 7→7 | 0 |
| V4S | 32.538/32.000/45.000 | 32.195/32.000/45.000 | 29.954/29.000/44.000 | 23.057/22.000/36.000 | 1.013 | 7→14 | 404 |
| V4U | 32.500/32.000/47.000 | 32.081/32.000/47.000 | 29.529/29.000/45.000 | 21.829/21.000/36.000 | 1.121 | 7→7 | 29 |
| V4C | 26.710/25.000/46.000 | 27.291/26.000/46.000 | 29.193/28.000/45.000 | 21.829/21.000/36.000 | 1.388 | 7→9 | 165 |

## Small/-132790047 — 200×133

Tierra 10640. Estado oficial `A446F47C4A6ADFD7546322555F5AC3C8D9AD0E76787A001830838FE0A6D131EC`, BASE `5D201EDFEF4E68610B4181C2782B13BB3C5E46D3503643F40950CB4E5E4088D4`, FINAL `D73FABF0A0753458C499B29B83EC79B8F4EDDBC072CDFAE5E7B972E38E3E0A54`; idénticos antes/después, automático/manual y guardado/carga. Mismo fixture y máscara para todas las variantes.

| Variante | Media | Mediana | P90 | P99 | Máx. | Pendiente media/máx. | Costa media/máx. | Δ medio padre | Δ absoluto V0 |
|---|---:|---:|---:|---:|---:|---|---|---:|---:|
| V0 | 31.973 | 26.000 | 67 | 83 | 100 | 4.538/35 | 30.125/93 | 0.000 | 0.000 |
| V1 | 32.172 | 26.000 | 71 | 80 | 100 | 3.854/21 | 29.855/76 | 0.199 | 3.398 |
| V1T | 32.480 | 26.000 | 71 | 86 | 100 | 3.946/21 | 29.927/82 | 0.307 | 3.625 |
| V2 | 50.539 | 50.000 | 68 | 74 | 82 | 2.319/7 | 49.869/72 | 18.059 | 20.205 |
| V3 | 50.376 | 50.000 | 68 | 73 | 78 | 2.258/7 | 49.813/71 | -0.163 | 20.233 |
| VP | 31.979 | 26.000 | 67 | 84 | 100 | 4.456/34 | 29.957/80 | 0.006 | 0.223 |
| VS | 32.185 | 26.000 | 71 | 82 | 100 | 3.867/21 | 29.855/76 | 0.012 | 3.386 |
| V4O | 32.539 | 32.000 | 50 | 56 | 64 | 2.319/7 | 31.869/54 | -18.000 | 9.137 |
| V4S | 32.528 | 32.000 | 48 | 54 | 60 | 2.057/6 | 31.935/52 | -0.011 | 10.183 |
| V4U | 32.491 | 32.000 | 50 | 55 | 60 | 2.288/7 | 31.863/54 | -0.048 | 9.185 |
| V4C | 31.917 | 31.000 | 50 | 55 | 60 | 2.387/8 | 26.401/54 | -0.574 | 8.764 |

| Nueva variante | ≥60 /≥75 /≥85 /≥90 /≥95 /=100 celdas | ≥20 tierra | Crestas /compactas /anillos P99 relativo | Recorrido /ancho /elongación del componente P99 principal | r altura–H04 |
|---|---|---:|---|---|---:|
| VP | 1659 /618 /87 /37 /10 /1 | 182 | 0/2/0 (nivel 84) | 14/5/2.800 | 0.339 |
| VS | 1843 /701 /61 /28 /12 /2 | 1 | 1/2/0 (nivel 82) | 13/7/2.286 | 0.288 |
| V4O | 37 /0 /0 /0 /0 /0 | 0 | 0/4/0 (nivel 56) | 13/9/2.111 | 0.275 |
| V4S | 10 /0 /0 /0 /0 /0 | 0 | 0/2/0 (nivel 54) | 12/7/1.857 | 0.275 |
| V4U | 2 /0 /0 /0 /0 /0 | 0 | 0/4/0 (nivel 55) | 13/9/2.222 | 0.276 |
| V4C | 2 /0 /0 /0 /0 /0 | 0 | 0/4/0 (nivel 55) | 13/9/2.222 | 0.279 |

| Nueva variante | 1–15 | 16–50 | 51–60 | 61–75 | 76–84 | 85–94 | 95–100 |
|---|---:|---:|---:|---:|---:|---:|---:|
| VP | 3694 (34.718%) | 4632 (43.534%) | 728 (6.842%) | 1041 (9.784%) | 458 (4.305%) | 77 (0.724%) | 10 (0.094%) |
| VS | 3702 (34.793%) | 4617 (43.393%) | 546 (5.132%) | 1204 (11.316%) | 510 (4.793%) | 49 (0.461%) | 12 (0.113%) |
| V4O | 1058 (9.944%) | 8544 (80.301%) | 1010 (9.492%) | 28 (0.263%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) |
| V4S | 601 (5.648%) | 9547 (89.727%) | 492 (4.624%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) |
| V4U | 1058 (9.944%) | 8545 (80.310%) | 1037 (9.746%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) |
| V4C | 1273 (11.964%) | 8330 (78.289%) | 1037 (9.746%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) |

Costa: cada celda de tabla muestra **media /mediana /P90**; distribuciones completas con P99, máximo, dispersión, conteo y niveles dominantes están en el log. Primera celda y 1–3 se solapan como se indicó.

| Curva | Primera, océano d=1 | Océano d=1–3 | d=4–8 | Interior d≥9 | Salto medio tierra litoral | Corridas antes→después | Nuevas aristas iguales a misma profundidad |
|---|---|---|---|---|---:|---|---:|
| V2 | 49.780/51.000/64.000 | 48.950/49.000/64.000 | 46.270/45.000/63.000 | 51.777/51.000/69.000 | 1.132 | 5→8 | 413 |
| V4O | 31.780/33.000/46.000 | 30.950/31.000/46.000 | 28.270/27.000/45.000 | 33.777/33.000/51.000 | 1.132 | 8→8 | 0 |
| V4S | 31.864/33.000/44.000 | 31.135/31.000/44.000 | 28.747/28.000/43.000 | 33.623/33.000/49.000 | 0.995 | 8→10 | 308 |
| V4U | 31.775/33.000/46.000 | 30.948/31.000/46.000 | 28.264/27.000/45.000 | 33.712/33.000/51.000 | 1.128 | 8→8 | 10 |
| V4C | 26.296/26.000/46.000 | 26.262/25.000/45.000 | 27.919/26.000/45.000 | 33.712/33.000/51.000 | 1.305 | 8→4 | 165 |

## Small/2026 — 200×133

Tierra 10650. Estado oficial `060471EBADB09C48AC693E67CDACEE19FEE455457DE771E8849E7FB530D0D3C8`, BASE `E62EB0A75091A91B1D52D54CE82EAA3D8A0310D96DE382F27E3D6005C9C49A9F`, FINAL `8910FD259F94783A095F3F7F759B8A65C00C04BCCCA3103646AC8B3938EE9726`; idénticos antes/después, automático/manual y guardado/carga. Mismo fixture y máscara para todas las variantes.

| Variante | Media | Mediana | P90 | P99 | Máx. | Pendiente media/máx. | Costa media/máx. | Δ medio padre | Δ absoluto V0 |
|---|---:|---:|---:|---:|---:|---|---|---:|---:|
| V0 | 32.209 | 28.000 | 68 | 78 | 100 | 5.606/51 | 35.889/98 | 0.000 | 0.000 |
| V1 | 32.274 | 29.000 | 67 | 86 | 100 | 3.689/19 | 25.622/84 | 0.065 | 7.510 |
| V1T | 32.494 | 29.000 | 67 | 90 | 100 | 3.698/19 | 25.685/87 | 0.220 | 7.724 |
| V2 | 50.710 | 52.000 | 67 | 75 | 79 | 2.011/7 | 47.677/74 | 18.216 | 21.327 |
| V3 | 50.575 | 52.000 | 66 | 73 | 76 | 1.965/6 | 47.631/72 | -0.135 | 21.336 |
| VP | 32.582 | 28.000 | 69 | 87 | 100 | 5.830/51 | 36.555/100 | 0.374 | 0.408 |
| VS | 32.275 | 29.000 | 67 | 86 | 100 | 3.690/19 | 25.628/84 | 0.001 | 7.509 |
| V4O | 32.710 | 34.000 | 49 | 57 | 61 | 2.011/7 | 29.677/56 | -18.000 | 10.893 |
| V4S | 32.682 | 33.000 | 47 | 54 | 57 | 1.794/6 | 29.985/53 | -0.028 | 11.603 |
| V4U | 32.664 | 34.000 | 49 | 56 | 58 | 1.987/7 | 29.667/55 | -0.045 | 10.938 |
| V4C | 31.590 | 32.000 | 49 | 56 | 58 | 2.265/7 | 23.292/55 | -1.074 | 10.675 |

| Nueva variante | ≥60 /≥75 /≥85 /≥90 /≥95 /=100 celdas | ≥20 tierra | Crestas /compactas /anillos P99 relativo | Recorrido /ancho /elongación del componente P99 principal | r altura–H04 |
|---|---|---:|---|---|---:|
| VP | 1582 /750 /179 /52 /9 /1 | 624 | 0/2/0 (nivel 87) | 9/3/3.667 | 0.151 |
| VS | 1553 /671 /149 /65 /21 /2 | 0 | 0/4/0 (nivel 86) | 11/5/2.600 | -0.019 |
| V4O | 17 /0 /0 /0 /0 /0 | 0 | 0/0/0 (nivel 57) | 19/7/3.714 | 0.000 |
| V4S | 0 /0 /0 /0 /0 /0 | 0 | 0/1/0 (nivel 54) | 19/7/3.571 | 0.000 |
| V4U | 0 /0 /0 /0 /0 /0 | 0 | 1/0/0 (nivel 56) | 19/5/4.400 | 0.001 |
| V4C | 0 /0 /0 /0 /0 /0 | 0 | 1/0/0 (nivel 56) | 19/5/4.400 | -0.056 |

| Nueva variante | 1–15 | 16–50 | 51–60 | 61–75 | 76–84 | 85–94 | 95–100 |
|---|---:|---:|---:|---:|---:|---:|---:|
| VP | 3697 (34.714%) | 4658 (43.737%) | 779 (7.315%) | 833 (7.822%) | 504 (4.732%) | 170 (1.596%) | 9 (0.085%) |
| VS | 3695 (34.695%) | 4656 (43.718%) | 822 (7.718%) | 860 (8.075%) | 468 (4.394%) | 128 (1.202%) | 21 (0.197%) |
| V4O | 1026 (9.634%) | 8843 (83.033%) | 779 (7.315%) | 2 (0.019%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) |
| V4S | 704 (6.610%) | 9452 (88.751%) | 494 (4.638%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) |
| V4U | 1026 (9.634%) | 8846 (83.061%) | 778 (7.305%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) |
| V4C | 1365 (12.817%) | 8507 (79.878%) | 778 (7.305%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) |

Costa: cada celda de tabla muestra **media /mediana /P90**; distribuciones completas con P99, máximo, dispersión, conteo y niveles dominantes están en el log. Primera celda y 1–3 se solapan como se indicó.

| Curva | Primera, océano d=1 | Océano d=1–3 | d=4–8 | Interior d≥9 | Salto medio tierra litoral | Corridas antes→después | Nuevas aristas iguales a misma profundidad |
|---|---|---|---|---|---:|---|---:|
| V2 | 47.902/49.000/60.000 | 48.268/50.000/61.000 | 50.102/51.000/64.000 | 51.930/53.000/69.000 | 1.046 | 35→22 | 732 |
| V4O | 29.902/31.000/42.000 | 30.268/32.000/43.000 | 32.102/33.000/46.000 | 33.930/35.000/51.000 | 1.046 | 22→22 | 0 |
| V4S | 30.181/31.000/41.000 | 30.529/32.000/42.000 | 32.150/33.000/44.000 | 33.756/34.000/49.000 | 0.922 | 22→38 | 693 |
| V4U | 29.891/31.000/42.000 | 30.252/32.000/43.000 | 32.059/33.000/46.000 | 33.872/35.000/51.000 | 1.038 | 22→24 | 28 |
| V4C | 23.596/23.000/40.000 | 25.211/25.000/42.000 | 31.719/33.000/46.000 | 33.872/35.000/51.000 | 1.354 | 24→29 | 294 |

## Small/42 — 200×133 — control independiente

Tierra 11486. Estado oficial `E519E15E904D781EAC5D8007E4AB917DB7DB479EC610A01AE01036D6041522A7`, BASE `C199218E01D950F2DB860C17CBC6FBBF9903960D0F6670C4F435D18BA0BF3A47`, FINAL `E32129A40B53CA9DC75B8D0C78D67E22E2E0B7ED58ECC698F198CBD3AB209F33`; idénticos antes/después, automático/manual y guardado/carga. Mismo fixture y máscara para todas las variantes.

| Variante | Media | Mediana | P90 | P99 | Máx. | Pendiente media/máx. | Costa media/máx. | Δ medio padre | Δ absoluto V0 |
|---|---:|---:|---:|---:|---:|---|---|---:|---:|
| V0 | 32.757 | 27.000 | 68 | 89 | 100 | 5.235/59 | 38.280/97 | 0.000 | 0.000 |
| V1 | 33.039 | 28.000 | 69 | 86 | 100 | 4.798/23 | 39.686/88 | 0.282 | 4.606 |
| V1T | 33.225 | 28.000 | 69 | 90 | 100 | 4.834/19 | 40.137/100 | 0.186 | 4.812 |
| V2 | 47.874 | 48.000 | 61 | 67 | 70 | 2.202/6 | 51.151/70 | 14.649 | 18.817 |
| V3 | 47.855 | 48.000 | 61 | 67 | 69 | 2.192/6 | 51.119/69 | -0.020 | 18.837 |
| VP | 32.508 | 27.000 | 68 | 79 | 100 | 5.061/59 | 38.135/87 | -0.249 | 0.258 |
| VS | 33.039 | 28.000 | 69 | 86 | 100 | 4.799/23 | 39.686/88 | 0.000 | 4.605 |
| V4O | 29.876 | 30.000 | 43 | 49 | 52 | 2.201/6 | 33.151/52 | -17.999 | 10.569 |
| V4S | 30.180 | 30.000 | 42 | 47 | 50 | 1.966/6 | 33.061/50 | 0.304 | 11.481 |
| V4U | 29.876 | 30.000 | 43 | 49 | 52 | 2.201/6 | 33.151/52 | 0.000 | 10.569 |
| V4C | 28.969 | 29.000 | 43 | 49 | 52 | 2.406/8 | 27.163/52 | -0.907 | 10.541 |

| Nueva variante | ≥60 /≥75 /≥85 /≥90 /≥95 /=100 celdas | ≥20 tierra | Crestas /compactas /anillos P99 relativo | Recorrido /ancho /elongación del componente P99 principal | r altura–H04 |
|---|---|---:|---|---|---:|
| VP | 1732 /531 /35 /17 /9 /1 | 260 | 1/1/0 (nivel 79) | 14/5/2.800 | 0.004 |
| VS | 1697 /799 /148 /43 /7 /1 | 3 | 1/2/0 (nivel 86) | 10/3/4.000 | -0.116 |
| V4O | 0 /0 /0 /0 /0 /0 | 0 | 0/5/0 (nivel 49) | 9/5/2.200 | -0.121 |
| V4S | 0 /0 /0 /0 /0 /0 | 0 | 1/5/0 (nivel 47) | 9/5/2.000 | -0.120 |
| V4U | 0 /0 /0 /0 /0 /0 | 0 | 0/5/0 (nivel 49) | 9/5/2.200 | -0.121 |
| V4C | 0 /0 /0 /0 /0 /0 | 0 | 0/5/0 (nivel 49) | 9/5/2.200 | -0.119 |

| Nueva variante | 1–15 | 16–50 | 51–60 | 61–75 | 76–84 | 85–94 | 95–100 |
|---|---:|---:|---:|---:|---:|---:|---:|
| VP | 3974 (34.599%) | 5045 (43.923%) | 795 (6.921%) | 1263 (10.996%) | 374 (3.256%) | 26 (0.226%) | 9 (0.078%) |
| VS | 3967 (34.538%) | 5034 (43.827%) | 846 (7.365%) | 906 (7.888%) | 585 (5.093%) | 141 (1.228%) | 7 (0.061%) |
| V4O | 675 (5.877%) | 10777 (93.827%) | 34 (0.296%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) |
| V4S | 457 (3.979%) | 11029 (96.021%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) |
| V4U | 675 (5.877%) | 10777 (93.827%) | 34 (0.296%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) |
| V4C | 945 (8.227%) | 10507 (91.477%) | 34 (0.296%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) |

Costa: cada celda de tabla muestra **media /mediana /P90**; distribuciones completas con P99, máximo, dispersión, conteo y niveles dominantes están en el log. Primera celda y 1–3 se solapan como se indicó.

| Curva | Primera, océano d=1 | Océano d=1–3 | d=4–8 | Interior d≥9 | Salto medio tierra litoral | Corridas antes→después | Nuevas aristas iguales a misma profundidad |
|---|---|---|---|---|---:|---|---:|
| V2 | 51.042/52.000/62.000 | 50.685/51.000/62.000 | 48.352/49.000/59.000 | 46.904/46.000/61.000 | 1.211 | 7→20 | 737 |
| V4O | 33.042/34.000/44.000 | 32.685/33.000/44.000 | 30.357/31.000/41.000 | 28.904/28.000/43.000 | 1.208 | 20→20 | 7 |
| V4S | 32.967/34.000/43.000 | 32.658/33.000/43.000 | 30.606/31.000/40.000 | 29.323/29.000/42.000 | 1.064 | 20→33 | 556 |
| V4U | 33.042/34.000/44.000 | 32.684/33.000/44.000 | 30.357/31.000/41.000 | 28.904/28.000/43.000 | 1.208 | 20→20 | 0 |
| V4C | 27.036/28.000/43.000 | 27.685/28.000/43.000 | 29.984/30.000/41.000 | 28.904/28.000/43.000 | 1.465 | 20→19 | 226 |

## Medium/-1387338765 — 250×167

Tierra 16700. Estado oficial `C04E13126BDAA577D5BB923604585AB35475926A6EAFA94C49EA5FCE25B52575`, BASE `03CA3ACE527FCE3BB75FF158FC1D75C1C3294211BBD48B375337A65EAD42A5DF`, FINAL `E9F5A46F78CA83A304B6FF5C1C62BCB336E44A09949FD409B57C7282B9CAA1E3`; idénticos antes/después, automático/manual y guardado/carga. Mismo fixture y máscara para todas las variantes.

| Variante | Media | Mediana | P90 | P99 | Máx. | Pendiente media/máx. | Costa media/máx. | Δ medio padre | Δ absoluto V0 |
|---|---:|---:|---:|---:|---:|---|---|---:|---:|
| V0 | 32.107 | 27.000 | 68 | 78 | 100 | 3.621/46 | 36.544/95 | 0.000 | 0.000 |
| V1 | 32.154 | 27.000 | 69 | 80 | 100 | 3.157/14 | 36.915/98 | 0.047 | 1.706 |
| V1T | 32.489 | 27.000 | 69 | 85 | 100 | 3.199/14 | 38.060/99 | 0.334 | 2.031 |
| V2 | 51.203 | 51.000 | 67 | 74 | 85 | 1.804/6 | 54.281/84 | 18.714 | 20.319 |
| V3 | 51.065 | 51.000 | 66 | 73 | 80 | 1.760/6 | 53.946/79 | -0.138 | 20.373 |
| VP | 32.244 | 27.000 | 69 | 81 | 100 | 3.616/46 | 37.052/98 | 0.137 | 0.216 |
| VS | 32.154 | 27.000 | 69 | 80 | 100 | 3.157/14 | 36.915/98 | 0.000 | 1.706 |
| V4O | 33.203 | 33.000 | 49 | 56 | 67 | 1.804/6 | 36.281/66 | -18.000 | 9.840 |
| V4S | 33.114 | 33.000 | 47 | 54 | 63 | 1.615/5 | 35.845/62 | -0.088 | 10.802 |
| V4U | 33.156 | 33.000 | 49 | 55 | 61 | 1.780/6 | 36.088/61 | -0.046 | 9.886 |
| V4C | 32.648 | 33.000 | 49 | 55 | 61 | 1.887/6 | 30.871/61 | -0.508 | 9.522 |

| Nueva variante | ≥60 /≥75 /≥85 /≥90 /≥95 /=100 celdas | ≥20 tierra | Crestas /compactas /anillos P99 relativo | Recorrido /ancho /elongación del componente P99 principal | r altura–H04 |
|---|---|---:|---|---|---:|
| VP | 2649 /1085 /82 /36 /15 /2 | 237 | 0/4/0 (nivel 81) | 13/7/2.571 | -0.050 |
| VS | 2617 /886 /65 /32 /15 /2 | 0 | 0/4/0 (nivel 80) | 13/7/2.571 | -0.127 |
| V4O | 52 /0 /0 /0 /0 /0 | 0 | 0/4/0 (nivel 56) | 16/9/3.222 | -0.073 |
| V4S | 24 /0 /0 /0 /0 /0 | 0 | 0/4/0 (nivel 54) | 16/7/3.429 | -0.073 |
| V4U | 24 /0 /0 /0 /0 /0 | 0 | 1/3/0 (nivel 55) | 19/9/3.222 | -0.073 |
| V4C | 24 /0 /0 /0 /0 /0 | 0 | 1/3/0 (nivel 55) | 19/9/3.222 | -0.086 |

| Nueva variante | 1–15 | 16–50 | 51–60 | 61–75 | 76–84 | 85–94 | 95–100 |
|---|---:|---:|---:|---:|---:|---:|---:|
| VP | 5784 (34.635%) | 7303 (43.731%) | 1063 (6.365%) | 1696 (10.156%) | 772 (4.623%) | 67 (0.401%) | 15 (0.090%) |
| VS | 5787 (34.653%) | 7310 (43.772%) | 1082 (6.479%) | 1838 (11.006%) | 618 (3.701%) | 50 (0.299%) | 15 (0.090%) |
| V4O | 1154 (6.910%) | 14304 (85.653%) | 1198 (7.174%) | 44 (0.263%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) |
| V4S | 728 (4.359%) | 15280 (91.497%) | 673 (4.030%) | 19 (0.114%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) |
| V4U | 1154 (6.910%) | 14307 (85.671%) | 1231 (7.371%) | 8 (0.048%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) |
| V4C | 1360 (8.144%) | 14101 (84.437%) | 1231 (7.371%) | 8 (0.048%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) |

Costa: cada celda de tabla muestra **media /mediana /P90**; distribuciones completas con P99, máximo, dispersión, conteo y niveles dominantes están en el log. Primera celda y 1–3 se solapan como se indicó.

| Curva | Primera, océano d=1 | Océano d=1–3 | d=4–8 | Interior d≥9 | Salto medio tierra litoral | Corridas antes→después | Nuevas aristas iguales a misma profundidad |
|---|---|---|---|---|---:|---|---:|
| V2 | 54.301/53.000/72.000 | 54.385/54.000/72.000 | 54.242/54.000/72.000 | 50.280/51.000/66.000 | 0.935 | 13→23 | 592 |
| V4O | 36.301/35.000/54.000 | 36.385/36.000/54.000 | 36.242/36.000/54.000 | 32.280/33.000/48.000 | 0.935 | 23→23 | 0 |
| V4S | 35.860/35.000/52.000 | 35.935/35.000/52.000 | 35.795/35.000/51.000 | 32.299/33.000/46.000 | 0.825 | 23→30 | 514 |
| V4U | 36.108/35.000/54.000 | 36.191/36.000/54.000 | 36.107/36.000/53.000 | 32.267/33.000/48.000 | 0.895 | 23→24 | 126 |
| V4C | 30.923/29.000/54.000 | 31.693/30.000/54.000 | 35.407/35.000/53.000 | 32.267/33.000/48.000 | 1.140 | 24→25 | 192 |

## Medium/-132790047 — 250×167

Tierra 16700. Estado oficial `DB74641DB267891F94B7EBC9EFDC3A59E6A450FD7D7C33E9CC2B6C434AA504B0`, BASE `74A08014CDEE823BDB9435012150617B24583FC64B6997159432740222AE58EB`, FINAL `3DC72EB7959BEA59ADA23D0982A674F8A4CD9BEA325C9CCF1E7A2759C0D588F1`; idénticos antes/después, automático/manual y guardado/carga. Mismo fixture y máscara para todas las variantes.

| Variante | Media | Mediana | P90 | P99 | Máx. | Pendiente media/máx. | Costa media/máx. | Δ medio padre | Δ absoluto V0 |
|---|---:|---:|---:|---:|---:|---|---|---:|---:|
| V0 | 32.219 | 28.000 | 61 | 86 | 100 | 4.401/51 | 26.040/82 | 0.000 | 0.000 |
| V1 | 32.935 | 28.000 | 68 | 81 | 100 | 3.560/22 | 23.725/89 | 0.716 | 4.755 |
| V1T | 33.207 | 28.000 | 68 | 86 | 100 | 3.640/24 | 23.688/81 | 0.271 | 4.919 |
| V2 | 54.613 | 55.000 | 69 | 75 | 82 | 1.831/5 | 49.055/71 | 21.407 | 24.107 |
| V3 | 54.407 | 55.000 | 68 | 73 | 78 | 1.767/5 | 48.978/70 | -0.206 | 23.991 |
| VP | 32.236 | 28.000 | 61 | 85 | 100 | 4.302/53 | 26.045/83 | 0.017 | 0.226 |
| VS | 33.011 | 28.000 | 68 | 84 | 100 | 3.630/22 | 23.725/89 | 0.076 | 4.696 |
| V4O | 36.613 | 37.000 | 51 | 57 | 64 | 1.831/5 | 31.055/53 | -18.000 | 10.794 |
| V4S | 36.139 | 37.000 | 49 | 54 | 60 | 1.627/5 | 31.232/51 | -0.475 | 11.406 |
| V4U | 36.565 | 37.000 | 51 | 55 | 60 | 1.808/5 | 31.054/53 | -0.048 | 10.842 |
| V4C | 36.015 | 37.000 | 51 | 55 | 60 | 1.938/8 | 25.142/53 | -0.550 | 10.418 |

| Nueva variante | ≥60 /≥75 /≥85 /≥90 /≥95 /=100 celdas | ≥20 tierra | Crestas /compactas /anillos P99 relativo | Recorrido /ancho /elongación del componente P99 principal | r altura–H04 |
|---|---|---:|---|---|---:|
| VP | 1831 /780 /182 /81 /26 /1 | 427 | 1/1/0 (nivel 85) | 43/5/8.600 | 0.409 |
| VS | 2587 /1063 /163 /52 /20 /1 | 6 | 1/3/0 (nivel 84) | 16/7/2.857 | 0.204 |
| V4O | 59 /0 /0 /0 /0 /0 | 0 | 0/3/0 (nivel 57) | 14/11/1.909 | 0.160 |
| V4S | 13 /0 /0 /0 /0 /0 | 0 | 0/4/0 (nivel 54) | 15/11/1.818 | 0.160 |
| V4U | 3 /0 /0 /0 /0 /0 | 0 | 0/5/0 (nivel 55) | 18/11/2.364 | 0.159 |
| V4C | 3 /0 /0 /0 /0 /0 | 0 | 0/5/0 (nivel 55) | 18/11/2.364 | 0.150 |

| Nueva variante | 1–15 | 16–50 | 51–60 | 61–75 | 76–84 | 85–94 | 95–100 |
|---|---:|---:|---:|---:|---:|---:|---:|
| VP | 5766 (34.527%) | 7374 (44.156%) | 1841 (11.024%) | 1037 (6.210%) | 500 (2.994%) | 156 (0.934%) | 26 (0.156%) |
| VS | 5797 (34.713%) | 7274 (43.557%) | 1147 (6.868%) | 1559 (9.335%) | 760 (4.551%) | 143 (0.856%) | 20 (0.120%) |
| V4O | 525 (3.144%) | 14194 (84.994%) | 1938 (11.605%) | 43 (0.257%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) |
| V4S | 266 (1.593%) | 15591 (93.359%) | 843 (5.048%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) |
| V4U | 525 (3.144%) | 14195 (85.000%) | 1980 (11.856%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) |
| V4C | 871 (5.216%) | 13849 (82.928%) | 1980 (11.856%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) |

Costa: cada celda de tabla muestra **media /mediana /P90**; distribuciones completas con P99, máximo, dispersión, conteo y niveles dominantes están en el log. Primera celda y 1–3 se solapan como se indicó.

| Curva | Primera, océano d=1 | Océano d=1–3 | d=4–8 | Interior d≥9 | Salto medio tierra litoral | Corridas antes→después | Nuevas aristas iguales a misma profundidad |
|---|---|---|---|---|---:|---|---:|
| V2 | 49.099/48.000/65.000 | 49.018/48.000/65.000 | 49.203/48.000/62.000 | 55.954/57.000/70.000 | 1.055 | 45→9 | 423 |
| V4O | 31.099/30.000/47.000 | 31.018/30.000/47.000 | 31.203/30.000/44.000 | 37.954/39.000/52.000 | 1.055 | 9→9 | 0 |
| V4S | 31.259/30.000/45.000 | 31.178/30.000/45.000 | 31.375/31.000/43.000 | 37.323/39.000/49.000 | 0.927 | 9→23 | 407 |
| V4U | 31.097/30.000/47.000 | 31.014/30.000/47.000 | 31.203/30.000/44.000 | 37.894/39.000/52.000 | 1.055 | 9→9 | 6 |
| V4C | 25.123/21.000/46.000 | 25.544/23.000/47.000 | 30.297/29.000/44.000 | 37.894/39.000/52.000 | 1.434 | 9→14 | 235 |

## Medium/2026 — 250×167

Tierra 16700. Estado oficial `0ED98AB6DE9FCFB886EB3A1DD1F2BB4C372EB1D5FD6994E997744F944B358D2D`, BASE `22381BDA7029CA3EC1FDFE6BD6CF5C9BB371117E718286900AC7D394CC9FF98B`, FINAL `130D03507844600D4A5AE99627DDEB5681794B5780A26F51E46E312CF2B33B46`; idénticos antes/después, automático/manual y guardado/carga. Mismo fixture y máscara para todas las variantes.

| Variante | Media | Mediana | P90 | P99 | Máx. | Pendiente media/máx. | Costa media/máx. | Δ medio padre | Δ absoluto V0 |
|---|---:|---:|---:|---:|---:|---|---|---:|---:|
| V0 | 31.892 | 26.000 | 68 | 80 | 100 | 4.224/43 | 38.076/97 | 0.000 | 0.000 |
| V1 | 32.016 | 26.000 | 67 | 87 | 100 | 2.762/15 | 24.676/76 | 0.124 | 5.883 |
| V1T | 32.227 | 26.000 | 67 | 90 | 100 | 2.768/15 | 24.679/78 | 0.211 | 6.085 |
| V2 | 48.846 | 48.000 | 67 | 75 | 79 | 1.665/5 | 45.351/70 | 16.619 | 19.796 |
| V3 | 48.710 | 48.000 | 66 | 73 | 76 | 1.630/5 | 45.334/69 | -0.136 | 19.827 |
| VP | 32.115 | 26.000 | 68 | 87 | 100 | 4.338/48 | 38.086/90 | 0.223 | 0.296 |
| VS | 32.016 | 26.000 | 67 | 87 | 100 | 2.762/15 | 24.676/76 | 0.000 | 5.883 |
| V4O | 30.846 | 30.000 | 49 | 57 | 61 | 1.665/5 | 27.351/52 | -18.000 | 9.333 |
| V4S | 31.039 | 30.000 | 47 | 54 | 57 | 1.502/5 | 27.965/50 | 0.193 | 10.415 |
| V4U | 30.803 | 30.000 | 49 | 56 | 58 | 1.648/5 | 27.351/52 | -0.043 | 9.377 |
| V4C | 29.833 | 29.000 | 49 | 56 | 58 | 1.843/8 | 20.663/52 | -0.970 | 9.319 |

| Nueva variante | ≥60 /≥75 /≥85 /≥90 /≥95 /=100 celdas | ≥20 tierra | Crestas /compactas /anillos P99 relativo | Recorrido /ancho /elongación del componente P99 principal | r altura–H04 |
|---|---|---:|---|---|---:|
| VP | 2412 /1139 /272 /68 /13 /2 | 667 | 0/3/0 (nivel 87) | 10/5/2.400 | 0.274 |
| VS | 2528 /1033 /229 /99 /31 /4 | 0 | 0/4/0 (nivel 87) | 13/7/2.000 | -0.086 |
| V4O | 24 /0 /0 /0 /0 /0 | 0 | 1/1/0 (nivel 57) | 24/7/5.000 | -0.060 |
| V4S | 0 /0 /0 /0 /0 /0 | 0 | 1/1/0 (nivel 54) | 24/7/4.714 | -0.060 |
| V4U | 0 /0 /0 /0 /0 /0 | 0 | 1/0/0 (nivel 56) | 24/7/4.714 | -0.059 |
| V4C | 0 /0 /0 /0 /0 /0 | 0 | 1/0/0 (nivel 56) | 24/7/4.714 | -0.133 |

| Nueva variante | 1–15 | 16–50 | 51–60 | 61–75 | 76–84 | 85–94 | 95–100 |
|---|---:|---:|---:|---:|---:|---:|---:|
| VP | 5784 (34.635%) | 7304 (43.737%) | 1322 (7.916%) | 1248 (7.473%) | 770 (4.611%) | 259 (1.551%) | 13 (0.078%) |
| VS | 5775 (34.581%) | 7316 (43.808%) | 1221 (7.311%) | 1438 (8.611%) | 721 (4.317%) | 198 (1.186%) | 31 (0.186%) |
| V4O | 2396 (14.347%) | 13081 (78.329%) | 1221 (7.311%) | 2 (0.012%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) |
| V4S | 1710 (10.240%) | 14208 (85.078%) | 782 (4.683%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) |
| V4U | 2396 (14.347%) | 13083 (78.341%) | 1221 (7.311%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) |
| V4C | 3062 (18.335%) | 12417 (74.353%) | 1221 (7.311%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) |

Costa: cada celda de tabla muestra **media /mediana /P90**; distribuciones completas con P99, máximo, dispersión, conteo y niveles dominantes están en el log. Primera celda y 1–3 se solapan como se indicó.

| Curva | Primera, océano d=1 | Océano d=1–3 | d=4–8 | Interior d≥9 | Salto medio tierra litoral | Corridas antes→después | Nuevas aristas iguales a misma profundidad |
|---|---|---|---|---|---:|---|---:|
| V2 | 45.314/45.000/59.000 | 45.463/45.000/60.000 | 45.387/45.000/60.000 | 50.512/50.000/69.000 | 0.911 | 41→40 | 746 |
| V4O | 27.314/27.000/41.000 | 27.463/27.000/42.000 | 27.387/27.000/42.000 | 32.512/32.000/51.000 | 0.911 | 40→40 | 0 |
| V4S | 27.935/28.000/40.000 | 28.044/27.000/41.000 | 27.989/28.000/41.000 | 32.510/32.000/49.000 | 0.806 | 40→70 | 745 |
| V4U | 27.314/27.000/41.000 | 27.463/27.000/42.000 | 27.383/27.000/42.000 | 32.449/32.000/51.000 | 0.910 | 40→40 | 3 |
| V4C | 20.650/18.000/39.000 | 21.594/19.000/40.000 | 26.443/26.000/42.000 | 32.449/32.000/51.000 | 1.239 | 40→55 | 488 |

## Medium/42 — 250×167 — control independiente

Tierra 16700. Estado oficial `CB64B4BE980DE92795DE5E41AC728F05EBBC4F2A1863FC7C618A3D78B70B6AB4`, BASE `FCF3646F8E90771073B18E742AF861BC8F3755880D98CCFA593E5B6874090BFF`, FINAL `2412919E85DE47AB4F0115DB5ED42C836B2E0FD56E8F6B270A5AE3798DA62961`; idénticos antes/después, automático/manual y guardado/carga. Mismo fixture y máscara para todas las variantes.

| Variante | Media | Mediana | P90 | P99 | Máx. | Pendiente media/máx. | Costa media/máx. | Δ medio padre | Δ absoluto V0 |
|---|---:|---:|---:|---:|---:|---|---|---:|---:|
| V0 | 32.461 | 28.000 | 65 | 86 | 100 | 4.245/43 | 31.421/90 | 0.000 | 0.000 |
| V1 | 32.979 | 28.000 | 67 | 84 | 100 | 3.826/15 | 33.469/85 | 0.519 | 4.237 |
| V1T | 33.267 | 28.000 | 67 | 91 | 100 | 3.854/15 | 33.794/91 | 0.287 | 4.522 |
| V2 | 49.079 | 49.000 | 61 | 69 | 73 | 1.645/5 | 48.836/69 | 15.813 | 19.559 |
| V3 | 49.053 | 49.000 | 61 | 68 | 71 | 1.635/5 | 48.808/68 | -0.026 | 19.583 |
| VP | 32.380 | 28.000 | 65 | 81 | 100 | 4.151/43 | 31.343/77 | -0.081 | 0.183 |
| VS | 32.980 | 28.000 | 67 | 84 | 100 | 3.826/15 | 33.469/85 | 0.000 | 4.237 |
| V4O | 31.079 | 31.000 | 43 | 51 | 55 | 1.645/5 | 30.836/51 | -18.000 | 10.850 |
| V4S | 31.247 | 31.000 | 42 | 49 | 52 | 1.481/4 | 31.042/49 | 0.167 | 11.688 |
| V4U | 31.077 | 31.000 | 43 | 51 | 54 | 1.644/5 | 30.836/51 | -0.002 | 10.852 |
| V4C | 30.163 | 30.000 | 43 | 51 | 54 | 1.832/6 | 24.543/51 | -0.914 | 10.549 |

| Nueva variante | ≥60 /≥75 /≥85 /≥90 /≥95 /=100 celdas | ≥20 tierra | Crestas /compactas /anillos P99 relativo | Recorrido /ancho /elongación del componente P99 principal | r altura–H04 |
|---|---|---:|---|---|---:|
| VP | 2256 /855 /85 /34 /11 /1 | 311 | 1/2/0 (nivel 81) | 25/3/8.333 | 0.459 |
| VS | 2441 /1035 /167 /72 /17 /2 | 0 | 0/2/0 (nivel 84) | 19/9/2.556 | 0.209 |
| V4O | 0 /0 /0 /0 /0 /0 | 0 | 0/0/0 (nivel 51) | 24/9/3.000 | 0.214 |
| V4S | 0 /0 /0 /0 /0 /0 | 0 | 0/2/0 (nivel 49) | 20/9/2.333 | 0.214 |
| V4U | 0 /0 /0 /0 /0 /0 | 0 | 0/0/0 (nivel 51) | 25/9/3.444 | 0.214 |
| V4C | 0 /0 /0 /0 /0 /0 | 0 | 0/0/0 (nivel 51) | 25/9/3.444 | 0.213 |

| Nueva variante | 1–15 | 16–50 | 51–60 | 61–75 | 76–84 | 85–94 | 95–100 |
|---|---:|---:|---:|---:|---:|---:|---:|
| VP | 5781 (34.617%) | 7326 (43.868%) | 1465 (8.772%) | 1364 (8.168%) | 679 (4.066%) | 74 (0.443%) | 11 (0.066%) |
| VS | 5775 (34.581%) | 7322 (43.844%) | 1274 (7.629%) | 1431 (8.569%) | 731 (4.377%) | 150 (0.898%) | 17 (0.102%) |
| V4O | 579 (3.467%) | 15922 (95.341%) | 199 (1.192%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) |
| V4S | 233 (1.395%) | 16411 (98.269%) | 56 (0.335%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) |
| V4U | 579 (3.467%) | 15925 (95.359%) | 196 (1.174%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) |
| V4C | 1066 (6.383%) | 15438 (92.443%) | 196 (1.174%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) |

Costa: cada celda de tabla muestra **media /mediana /P90**; distribuciones completas con P99, máximo, dispersión, conteo y niveles dominantes están en el log. Primera celda y 1–3 se solapan como se indicó.

| Curva | Primera, océano d=1 | Océano d=1–3 | d=4–8 | Interior d≥9 | Salto medio tierra litoral | Corridas antes→después | Nuevas aristas iguales a misma profundidad |
|---|---|---|---|---|---:|---|---:|
| V2 | 48.705/50.000/61.000 | 48.350/49.000/60.000 | 47.200/47.000/59.000 | 49.723/50.000/61.000 | 0.828 | 40→48 | 950 |
| V4O | 30.705/32.000/43.000 | 30.350/31.000/42.000 | 29.200/29.000/41.000 | 31.723/32.000/43.000 | 0.828 | 48→48 | 0 |
| V4S | 30.923/32.000/42.000 | 30.609/31.000/41.000 | 29.590/30.000/40.000 | 31.813/32.000/42.000 | 0.735 | 48→59 | 686 |
| V4U | 30.705/32.000/43.000 | 30.350/31.000/42.000 | 29.200/29.000/41.000 | 31.720/32.000/43.000 | 0.828 | 48→48 | 0 |
| V4C | 24.392/24.000/41.000 | 24.715/24.000/40.000 | 28.222/28.000/41.000 | 31.720/32.000/43.000 | 1.166 | 48→37 | 402 |

## Large/-1387338765 — 300×200

Tierra 24022. Estado oficial `084B394540D549145539EEEAE92F3860ED1340EEBD95AFB462FFB0A5377183E1`, BASE `1CE9C56E62F178E915C88864258ECAC6958926214A12D9B703E665015628BFE0`, FINAL `4B38456B741CE7A2AB8BECA06158FE017B4D3D6D5E96E3D45104049A340D1EAD`; idénticos antes/después, automático/manual y guardado/carga. Mismo fixture y máscara para todas las variantes.

| Variante | Media | Mediana | P90 | P99 | Máx. | Pendiente media/máx. | Costa media/máx. | Δ medio padre | Δ absoluto V0 |
|---|---:|---:|---:|---:|---:|---|---|---:|---:|
| V0 | 31.395 | 25.000 | 66 | 85 | 100 | 3.704/59 | 29.917/86 | 0.000 | 0.000 |
| V1 | 32.135 | 25.000 | 70 | 85 | 100 | 2.445/14 | 25.453/83 | 0.740 | 5.829 |
| V1T | 32.394 | 25.000 | 69 | 90 | 100 | 2.478/13 | 25.501/87 | 0.259 | 6.086 |
| V2 | 48.322 | 47.000 | 69 | 75 | 77 | 1.593/5 | 44.318/74 | 15.928 | 19.326 |
| V3 | 48.155 | 47.000 | 68 | 73 | 75 | 1.555/5 | 44.236/72 | -0.168 | 19.250 |
| VP | 31.340 | 25.000 | 66 | 82 | 100 | 3.600/57 | 29.823/85 | -0.055 | 0.200 |
| VS | 32.137 | 25.000 | 70 | 85 | 100 | 2.447/14 | 25.453/83 | 0.002 | 5.827 |
| V4O | 30.333 | 29.000 | 51 | 57 | 59 | 1.587/5 | 26.318/56 | -17.989 | 7.753 |
| V4S | 30.573 | 29.000 | 48 | 54 | 56 | 1.427/5 | 27.028/53 | 0.240 | 8.875 |
| V4U | 30.282 | 29.000 | 51 | 55 | 57 | 1.569/5 | 26.301/55 | -0.051 | 7.804 |
| V4C | 29.149 | 28.000 | 51 | 55 | 57 | 1.748/7 | 20.158/55 | -1.133 | 7.769 |

| Nueva variante | ≥60 /≥75 /≥85 /≥90 /≥95 /=100 celdas | ≥20 tierra | Crestas /compactas /anillos P99 relativo | Recorrido /ancho /elongación del componente P99 principal | r altura–H04 |
|---|---|---:|---|---|---:|
| VP | 3394 /1063 /154 /55 /15 /2 | 594 | 4/0/0 (nivel 82) | 19/5/3.800 | 0.141 |
| VS | 4154 /1710 /258 /126 /44 /2 | 0 | 0/6/0 (nivel 85) | 14/7/2.286 | -0.023 |
| V4O | 0 /0 /0 /0 /0 /0 | 0 | 0/6/0 (nivel 57) | 13/7/2.143 | -0.051 |
| V4S | 0 /0 /0 /0 /0 /0 | 0 | 1/5/0 (nivel 54) | 14/7/3.429 | -0.051 |
| V4U | 0 /0 /0 /0 /0 /0 | 0 | 1/2/0 (nivel 55) | 30/11/3.273 | -0.052 |
| V4C | 0 /0 /0 /0 /0 /0 | 0 | 1/2/0 (nivel 55) | 30/11/3.273 | -0.116 |

| Nueva variante | 1–15 | 16–50 | 51–60 | 61–75 | 76–84 | 85–94 | 95–100 |
|---|---:|---:|---:|---:|---:|---:|---:|
| VP | 8364 (34.818%) | 10453 (43.514%) | 1976 (8.226%) | 2318 (9.649%) | 757 (3.151%) | 139 (0.579%) | 15 (0.062%) |
| VS | 8340 (34.718%) | 10454 (43.518%) | 1192 (4.962%) | 2501 (10.411%) | 1277 (5.316%) | 214 (0.891%) | 44 (0.183%) |
| V4O | 4382 (18.242%) | 17212 (71.651%) | 2428 (10.107%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) |
| V4S | 3333 (13.875%) | 19253 (80.147%) | 1436 (5.978%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) |
| V4U | 4382 (18.242%) | 17219 (71.680%) | 2421 (10.078%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) |
| V4C | 5902 (24.569%) | 15699 (65.353%) | 2421 (10.078%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) |

Costa: cada celda de tabla muestra **media /mediana /P90**; distribuciones completas con P99, máximo, dispersión, conteo y niveles dominantes están en el log. Primera celda y 1–3 se solapan como se indicó.

| Curva | Primera, océano d=1 | Océano d=1–3 | d=4–8 | Interior d≥9 | Salto medio tierra litoral | Corridas antes→después | Nuevas aristas iguales a misma profundidad |
|---|---|---|---|---|---:|---|---:|
| V2 | 44.359/41.000/66.000 | 44.437/41.000/67.000 | 45.506/42.000/68.000 | 50.331/51.000/69.000 | 0.807 | 146→113 | 1417 |
| V4O | 26.359/23.000/48.000 | 26.438/23.000/49.000 | 27.526/24.000/50.000 | 32.341/33.000/51.000 | 0.802 | 113→119 | 35 |
| V4S | 27.062/25.000/46.000 | 27.138/24.000/47.000 | 28.090/25.000/48.000 | 32.347/33.000/49.000 | 0.714 | 119→130 | 1327 |
| V4U | 26.341/23.000/48.000 | 26.421/23.000/49.000 | 27.492/24.000/50.000 | 32.276/33.000/51.000 | 0.794 | 119→121 | 92 |
| V4C | 20.196/14.000/48.000 | 20.894/15.000/49.000 | 26.165/23.000/50.000 | 32.276/33.000/51.000 | 1.067 | 121→147 | 1040 |

## Large/-132790047 — 300×200

Tierra 24974. Estado oficial `F216907ADAE93C722FEFBD36B57014FD81AFB7F960315CDE505B055782054D33`, BASE `2DF30795672AFCE53C9CAFF2A37B689660B74D34931E6E88215CE23BD8F8E9F9`, FINAL `B9425AA5A54EA19E5203E48135215264B782978A8A5A0F603BEE9CA50BEBA72A`; idénticos antes/después, automático/manual y guardado/carga. Mismo fixture y máscara para todas las variantes.

| Variante | Media | Mediana | P90 | P99 | Máx. | Pendiente media/máx. | Costa media/máx. | Δ medio padre | Δ absoluto V0 |
|---|---:|---:|---:|---:|---:|---|---|---:|---:|
| V0 | 32.011 | 27.000 | 69 | 80 | 100 | 3.609/43 | 30.717/77 | 0.000 | 0.000 |
| V1 | 32.215 | 28.000 | 69 | 82 | 100 | 2.875/13 | 31.965/77 | 0.204 | 3.661 |
| V1T | 32.595 | 28.000 | 69 | 90 | 100 | 2.932/13 | 32.299/89 | 0.380 | 4.013 |
| V2 | 49.950 | 51.000 | 65 | 72 | 75 | 1.511/5 | 50.025/71 | 17.355 | 20.332 |
| V3 | 49.874 | 51.000 | 65 | 70 | 73 | 1.490/5 | 49.957/70 | -0.076 | 20.389 |
| VP | 32.039 | 27.000 | 70 | 81 | 100 | 3.572/43 | 30.735/79 | 0.028 | 0.191 |
| VS | 32.215 | 28.000 | 69 | 82 | 100 | 2.875/13 | 31.965/77 | 0.000 | 3.661 |
| V4O | 31.950 | 33.000 | 47 | 54 | 57 | 1.511/5 | 32.025/53 | -18.000 | 10.345 |
| V4S | 32.019 | 33.000 | 45 | 51 | 54 | 1.362/4 | 32.090/51 | 0.069 | 11.324 |
| V4U | 31.940 | 33.000 | 47 | 53 | 56 | 1.506/5 | 32.025/53 | -0.010 | 10.355 |
| V4C | 31.228 | 32.000 | 47 | 53 | 56 | 1.644/7 | 26.137/53 | -0.712 | 9.910 |

| Nueva variante | ≥60 /≥75 /≥85 /≥90 /≥95 /=100 celdas | ≥20 tierra | Crestas /compactas /anillos P99 relativo | Recorrido /ancho /elongación del componente P99 principal | r altura–H04 |
|---|---|---:|---|---|---:|
| VP | 3945 /1538 /134 /48 /8 /1 | 457 | 2/4/0 (nivel 81) | 38/5/8.000 | 0.304 |
| VS | 3639 /1610 /141 /40 /10 /2 | 0 | 0/2/0 (nivel 82) | 24/7/3.429 | 0.135 |
| V4O | 0 /0 /0 /0 /0 /0 | 0 | 0/1/0 (nivel 54) | 23/11/2.091 | 0.162 |
| V4S | 0 /0 /0 /0 /0 /0 | 0 | 1/2/0 (nivel 51) | 28/13/2.154 | 0.162 |
| V4U | 0 /0 /0 /0 /0 /0 | 0 | 1/2/0 (nivel 53) | 28/13/2.462 | 0.162 |
| V4C | 0 /0 /0 /0 /0 /0 | 0 | 1/2/0 (nivel 53) | 28/13/2.462 | 0.164 |

| Nueva variante | 1–15 | 16–50 | 51–60 | 61–75 | 76–84 | 85–94 | 95–100 |
|---|---:|---:|---:|---:|---:|---:|---:|
| VP | 8614 (34.492%) | 10966 (43.910%) | 1582 (6.335%) | 2478 (9.922%) | 1200 (4.805%) | 126 (0.505%) | 8 (0.032%) |
| VS | 8594 (34.412%) | 11002 (44.054%) | 1891 (7.572%) | 2230 (8.929%) | 1116 (4.469%) | 131 (0.525%) | 10 (0.040%) |
| V4O | 2447 (9.798%) | 21389 (85.645%) | 1138 (4.557%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) |
| V4S | 1535 (6.146%) | 23047 (92.284%) | 392 (1.570%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) |
| V4U | 2447 (9.798%) | 21394 (85.665%) | 1133 (4.537%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) |
| V4C | 3106 (12.437%) | 20735 (83.026%) | 1133 (4.537%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) |

Costa: cada celda de tabla muestra **media /mediana /P90**; distribuciones completas con P99, máximo, dispersión, conteo y niveles dominantes están en el log. Primera celda y 1–3 se solapan como se indicó.

| Curva | Primera, océano d=1 | Océano d=1–3 | d=4–8 | Interior d≥9 | Salto medio tierra litoral | Corridas antes→después | Nuevas aristas iguales a misma profundidad |
|---|---|---|---|---|---:|---|---:|
| V2 | 50.085/50.000/65.000 | 50.506/51.000/66.000 | 51.678/52.000/66.000 | 49.511/50.000/65.000 | 0.848 | 32→62 | 1333 |
| V4O | 32.085/32.000/47.000 | 32.506/33.000/48.000 | 33.678/34.000/48.000 | 31.511/32.000/47.000 | 0.848 | 62→62 | 0 |
| V4S | 32.145/32.000/46.000 | 32.511/33.000/46.000 | 33.551/34.000/46.000 | 31.630/32.000/45.000 | 0.753 | 62→75 | 856 |
| V4U | 32.085/32.000/47.000 | 32.504/33.000/48.000 | 33.673/34.000/48.000 | 31.499/32.000/47.000 | 0.847 | 62→61 | 20 |
| V4C | 26.209/24.000/47.000 | 27.247/26.000/47.000 | 32.421/33.000/48.000 | 31.499/32.000/47.000 | 1.165 | 61→69 | 551 |

## Large/2026 — 300×200

Tierra 27543. Estado oficial `A019508E511D6A2EFB5642052D0B2B198187BEF9D36EBE8812EF5635714921F2`, BASE `33646922AA766E29A561FAAB190C2BA297208C2B94A17F2D61B3D2CA608A106D`, FINAL `3AB5354F42E1DBD286EB09793B4C944EDB272227BBB9BF041D81D92EFB4C3041`; idénticos antes/después, automático/manual y guardado/carga. Mismo fixture y máscara para todas las variantes.

| Variante | Media | Mediana | P90 | P99 | Máx. | Pendiente media/máx. | Costa media/máx. | Δ medio padre | Δ absoluto V0 |
|---|---:|---:|---:|---:|---:|---|---|---:|---:|
| V0 | 32.111 | 27.000 | 66 | 80 | 100 | 3.539/28 | 40.911/97 | 0.000 | 0.000 |
| V1 | 32.398 | 27.000 | 67 | 86 | 100 | 3.110/17 | 42.222/94 | 0.287 | 2.083 |
| V1T | 32.482 | 27.000 | 67 | 86 | 100 | 3.047/15 | 42.349/97 | 0.084 | 2.183 |
| V2 | 42.861 | 42.000 | 56 | 65 | 78 | 1.478/5 | 46.478/75 | 10.379 | 15.119 |
| V3 | 42.853 | 42.000 | 56 | 64 | 75 | 1.473/5 | 46.459/73 | -0.008 | 15.127 |
| VP | 32.208 | 27.000 | 66 | 83 | 100 | 3.554/28 | 41.226/92 | 0.097 | 0.118 |
| VS | 32.404 | 27.000 | 67 | 86 | 100 | 3.117/17 | 42.238/94 | 0.006 | 2.077 |
| V4O | 24.862 | 24.000 | 38 | 47 | 60 | 1.477/5 | 28.478/57 | -17.999 | 10.622 |
| V4S | 25.746 | 25.000 | 37 | 45 | 57 | 1.326/4 | 28.943/54 | 0.884 | 11.415 |
| V4U | 24.860 | 24.000 | 38 | 47 | 57 | 1.476/5 | 28.473/56 | -0.002 | 10.624 |
| V4C | 23.960 | 24.000 | 37 | 46 | 57 | 1.588/6 | 22.284/56 | -0.900 | 10.984 |

| Nueva variante | ≥60 /≥75 /≥85 /≥90 /≥95 /=100 celdas | ≥20 tierra | Crestas /compactas /anillos P99 relativo | Recorrido /ancho /elongación del componente P99 principal | r altura–H04 |
|---|---|---:|---|---|---:|
| VP | 4049 /1510 /233 /64 /15 /2 | 137 | 1/5/0 (nivel 83) | 15/7/2.857 | 0.192 |
| VS | 4200 /1662 /374 /143 /27 /1 | 0 | 0/5/0 (nivel 86) | 13/7/2.714 | 0.093 |
| V4O | 1 /0 /0 /0 /0 /0 | 0 | 0/2/0 (nivel 47) | 16/7/2.286 | 0.074 |
| V4S | 0 /0 /0 /0 /0 /0 | 0 | 1/2/0 (nivel 45) | 17/7/2.429 | 0.074 |
| V4U | 0 /0 /0 /0 /0 /0 | 0 | 0/2/0 (nivel 47) | 16/7/2.286 | 0.074 |
| V4C | 0 /0 /0 /0 /0 /0 | 0 | 1/3/0 (nivel 46) | 35/9/4.333 | 0.083 |

| Nueva variante | 1–15 | 16–50 | 51–60 | 61–75 | 76–84 | 85–94 | 95–100 |
|---|---:|---:|---:|---:|---:|---:|---:|
| VP | 9560 (34.709%) | 12051 (43.753%) | 2061 (7.483%) | 2520 (9.149%) | 1118 (4.059%) | 218 (0.791%) | 15 (0.054%) |
| VS | 9542 (34.644%) | 12053 (43.761%) | 1956 (7.102%) | 2485 (9.022%) | 1133 (4.114%) | 347 (1.260%) | 27 (0.098%) |
| V4O | 4946 (17.957%) | 22522 (81.770%) | 75 (0.272%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) |
| V4S | 3379 (12.268%) | 24116 (87.558%) | 48 (0.174%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) |
| V4U | 4946 (17.957%) | 22522 (81.770%) | 75 (0.272%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) |
| V4C | 5908 (21.450%) | 21560 (78.278%) | 75 (0.272%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) |

Costa: cada celda de tabla muestra **media /mediana /P90**; distribuciones completas con P99, máximo, dispersión, conteo y niveles dominantes están en el log. Primera celda y 1–3 se solapan como se indicó.

| Curva | Primera, océano d=1 | Océano d=1–3 | d=4–8 | Interior d≥9 | Salto medio tierra litoral | Corridas antes→después | Nuevas aristas iguales a misma profundidad |
|---|---|---|---|---|---:|---|---:|
| V2 | 46.450/47.000/61.000 | 46.103/46.000/61.000 | 43.968/44.000/57.000 | 42.102/42.000/54.000 | 0.773 | 75→110 | 1545 |
| V4O | 28.450/29.000/43.000 | 28.103/28.000/43.000 | 25.968/26.000/39.000 | 24.103/24.000/36.000 | 0.773 | 110→110 | 0 |
| V4S | 28.924/29.000/42.000 | 28.607/29.000/41.000 | 26.729/27.000/39.000 | 25.075/25.000/35.000 | 0.680 | 110→135 | 1222 |
| V4U | 28.444/29.000/43.000 | 28.097/28.000/43.000 | 25.961/26.000/39.000 | 24.103/24.000/36.000 | 0.772 | 110→110 | 6 |
| V4C | 22.252/20.000/41.000 | 22.393/20.000/41.000 | 24.418/24.000/39.000 | 24.103/24.000/36.000 | 0.994 | 110→114 | 923 |

## Large/42 — 300×200 — control independiente

Tierra 24000. Estado oficial `1832108F2AD56BFB4FE0D78D4574F7717FED5AD8EB49D734C51889D415202B83`, BASE `1D64730F60F1DFB2A9F477E4E41866D5960F6DB16F428BD916D2DEC1C08B91F3`, FINAL `66B97AB0F0933593797332D74B383FB7E641935E6631785F097BA286420E569D`; idénticos antes/después, automático/manual y guardado/carga. Mismo fixture y máscara para todas las variantes.

| Variante | Media | Mediana | P90 | P99 | Máx. | Pendiente media/máx. | Costa media/máx. | Δ medio padre | Δ absoluto V0 |
|---|---:|---:|---:|---:|---:|---|---|---:|---:|
| V0 | 32.072 | 29.000 | 63 | 82 | 100 | 4.079/50 | 41.782/100 | 0.000 | 0.000 |
| V1 | 32.613 | 28.000 | 67 | 83 | 100 | 3.028/17 | 46.985/98 | 0.541 | 5.410 |
| V1T | 32.851 | 28.000 | 67 | 87 | 100 | 3.056/15 | 48.350/99 | 0.238 | 5.641 |
| V2 | 48.165 | 49.000 | 61 | 67 | 73 | 1.447/5 | 54.185/72 | 15.314 | 19.038 |
| V3 | 48.146 | 49.000 | 61 | 66 | 71 | 1.440/5 | 54.082/71 | -0.019 | 19.051 |
| VP | 32.064 | 29.000 | 63 | 81 | 100 | 4.019/50 | 41.771/82 | -0.008 | 0.197 |
| VS | 32.618 | 28.000 | 67 | 83 | 100 | 3.031/17 | 47.005/98 | 0.005 | 5.404 |
| V4O | 30.165 | 31.000 | 43 | 49 | 55 | 1.447/5 | 36.185/54 | -18.000 | 10.101 |
| V4S | 30.437 | 31.000 | 42 | 47 | 52 | 1.301/5 | 35.804/52 | 0.272 | 11.004 |
| V4U | 30.164 | 31.000 | 43 | 49 | 54 | 1.446/5 | 36.168/54 | -0.001 | 10.103 |
| V4C | 29.816 | 30.000 | 43 | 49 | 54 | 1.509/6 | 31.438/54 | -0.348 | 10.038 |

| Nueva variante | ≥60 /≥75 /≥85 /≥90 /≥95 /=100 celdas | ≥20 tierra | Crestas /compactas /anillos P99 relativo | Recorrido /ancho /elongación del componente P99 principal | r altura–H04 |
|---|---|---:|---|---|---:|
| VP | 2935 /1063 /122 /47 /17 /3 | 647 | 2/3/0 (nivel 81) | 17/7/2.429 | 0.192 |
| VS | 3562 /1449 /186 /75 /16 /2 | 0 | 0/3/0 (nivel 83) | 25/9/2.889 | 0.010 |
| V4O | 0 /0 /0 /0 /0 /0 | 0 | 0/2/0 (nivel 49) | 28/9/3.333 | 0.010 |
| V4S | 0 /0 /0 /0 /0 /0 | 0 | 0/2/0 (nivel 47) | 28/9/3.333 | 0.009 |
| V4U | 0 /0 /0 /0 /0 /0 | 0 | 0/2/0 (nivel 49) | 28/9/3.333 | 0.010 |
| V4C | 0 /0 /0 /0 /0 /0 | 0 | 0/2/0 (nivel 49) | 28/9/3.333 | 0.024 |

| Nueva variante | 1–15 | 16–50 | 51–60 | 61–75 | 76–84 | 85–94 | 95–100 |
|---|---:|---:|---:|---:|---:|---:|---:|
| VP | 8310 (34.625%) | 10568 (44.033%) | 2339 (9.746%) | 1854 (7.725%) | 807 (3.363%) | 105 (0.438%) | 17 (0.071%) |
| VS | 8309 (34.621%) | 10504 (43.767%) | 1808 (7.533%) | 2091 (8.713%) | 1102 (4.592%) | 170 (0.708%) | 16 (0.067%) |
| V4O | 1781 (7.421%) | 22077 (91.988%) | 142 (0.592%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) |
| V4S | 1152 (4.800%) | 22775 (94.896%) | 73 (0.304%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) |
| V4U | 1781 (7.421%) | 22077 (91.988%) | 142 (0.592%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) |
| V4C | 2064 (8.600%) | 21794 (90.808%) | 142 (0.592%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) |

Costa: cada celda de tabla muestra **media /mediana /P90**; distribuciones completas con P99, máximo, dispersión, conteo y niveles dominantes están en el log. Primera celda y 1–3 se solapan como se indicó.

| Curva | Primera, océano d=1 | Océano d=1–3 | d=4–8 | Interior d≥9 | Salto medio tierra litoral | Corridas antes→después | Nuevas aristas iguales a misma profundidad |
|---|---|---|---|---|---:|---|---:|
| V2 | 54.106/55.000/66.000 | 53.955/54.000/66.000 | 53.098/54.000/65.000 | 47.255/48.000/60.000 | 0.754 | 22→57 | 834 |
| V4O | 36.106/37.000/48.000 | 35.955/36.000/48.000 | 35.098/36.000/47.000 | 29.255/30.000/42.000 | 0.754 | 57→57 | 0 |
| V4S | 35.720/36.000/47.000 | 35.569/36.000/46.000 | 34.791/36.000/46.000 | 29.633/30.000/41.000 | 0.665 | 57→75 | 540 |
| V4U | 36.091/37.000/48.000 | 35.942/36.000/48.000 | 35.093/36.000/47.000 | 29.255/30.000/42.000 | 0.750 | 57→61 | 29 |
| V4C | 31.328/31.000/48.000 | 31.546/32.000/48.000 | 33.994/35.000/47.000 | 29.255/30.000/42.000 | 0.995 | 61→49 | 265 |

## Enormous/-1387338765 — 400×267

Tierra 42722. Estado oficial `5B7168F89FF906EF5381823D5315B3F6BEC0C16A71671D0BD86D10AF2E9EF033`, BASE `4CB2B3416C8C8D443D72EAC18439591E81EE02FBDC45F9697B02B9846A94C178`, FINAL `13E03F0FC066757ADC939751677316583AB86616F36614A828773D41E0752E93`; idénticos antes/después, automático/manual y guardado/carga. Mismo fixture y máscara para todas las variantes.

| Variante | Media | Mediana | P90 | P99 | Máx. | Pendiente media/máx. | Costa media/máx. | Δ medio padre | Δ absoluto V0 |
|---|---:|---:|---:|---:|---:|---|---|---:|---:|
| V0 | 31.197 | 26.000 | 63 | 79 | 100 | 2.283/42 | 40.590/84 | 0.000 | 0.000 |
| V1 | 31.411 | 26.000 | 64 | 80 | 100 | 1.887/7 | 35.974/88 | 0.214 | 2.456 |
| V1T | 31.826 | 26.000 | 64 | 88 | 100 | 1.888/7 | 36.847/93 | 0.415 | 2.793 |
| V2 | 43.527 | 42.000 | 60 | 74 | 85 | 1.187/4 | 46.066/79 | 11.701 | 14.337 |
| V3 | 43.459 | 42.000 | 60 | 72 | 80 | 1.174/4 | 45.946/76 | -0.069 | 14.392 |
| VP | 31.447 | 26.000 | 63 | 86 | 100 | 2.341/42 | 41.769/98 | 0.249 | 0.282 |
| VS | 31.413 | 26.000 | 64 | 80 | 100 | 1.887/7 | 36.023/88 | 0.002 | 2.455 |
| V4O | 25.538 | 24.000 | 42 | 56 | 67 | 1.183/4 | 28.066/61 | -17.989 | 7.899 |
| V4S | 26.336 | 25.000 | 41 | 53 | 63 | 1.079/3 | 28.578/58 | 0.797 | 8.933 |
| V4U | 25.504 | 24.000 | 42 | 55 | 61 | 1.174/4 | 28.031/58 | -0.034 | 7.933 |
| V4C | 24.560 | 23.000 | 41 | 55 | 61 | 1.288/5 | 22.194/58 | -0.944 | 8.432 |

| Nueva variante | ≥60 /≥75 /≥85 /≥90 /≥95 /=100 celdas | ≥20 tierra | Crestas /compactas /anillos P99 relativo | Recorrido /ancho /elongación del componente P99 principal | r altura–H04 |
|---|---|---:|---|---|---:|
| VP | 5139 /2336 /598 /235 /62 /1 | 426 | 3/5/0 (nivel 86) | 27/11/2.727 | 0.167 |
| VS | 5501 /1908 /189 /92 /43 /5 | 0 | 0/1/0 (nivel 80) | 39/15/3.533 | 0.085 |
| V4O | 177 /0 /0 /0 /0 /0 | 0 | 0/2/0 (nivel 56) | 30/17/3.235 | 0.083 |
| V4S | 79 /0 /0 /0 /0 /0 | 0 | 0/1/0 (nivel 53) | 37/19/3.316 | 0.083 |
| V4U | 61 /0 /0 /0 /0 /0 | 0 | 0/3/0 (nivel 55) | 30/19/2.947 | 0.083 |
| V4C | 61 /0 /0 /0 /0 /0 | 0 | 0/3/0 (nivel 55) | 30/19/2.947 | 0.026 |

| Nueva variante | 1–15 | 16–50 | 51–60 | 61–75 | 76–84 | 85–94 | 95–100 |
|---|---:|---:|---:|---:|---:|---:|---:|
| VP | 14872 (34.811%) | 18758 (43.907%) | 4204 (9.840%) | 2822 (6.605%) | 1468 (3.436%) | 536 (1.255%) | 62 (0.145%) |
| VS | 14881 (34.832%) | 18652 (43.659%) | 3963 (9.276%) | 3766 (8.815%) | 1271 (2.975%) | 146 (0.342%) | 43 (0.101%) |
| V4O | 9582 (22.429%) | 31602 (73.971%) | 1394 (3.263%) | 144 (0.337%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) |
| V4S | 6912 (16.179%) | 34810 (81.480%) | 946 (2.214%) | 54 (0.126%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) |
| V4U | 9582 (22.429%) | 31603 (73.974%) | 1521 (3.560%) | 16 (0.037%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) |
| V4C | 11459 (26.822%) | 29726 (69.580%) | 1521 (3.560%) | 16 (0.037%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) |

Costa: cada celda de tabla muestra **media /mediana /P90**; distribuciones completas con P99, máximo, dispersión, conteo y niveles dominantes están en el log. Primera celda y 1–3 se solapan como se indicó.

| Curva | Primera, océano d=1 | Océano d=1–3 | d=4–8 | Interior d≥9 | Salto medio tierra litoral | Corridas antes→después | Nuevas aristas iguales a misma profundidad |
|---|---|---|---|---|---:|---|---:|
| V2 | 45.973/44.000/66.000 | 45.392/43.000/66.000 | 44.460/42.000/64.000 | 43.092/42.000/58.000 | 0.568 | 174→212 | 2189 |
| V4O | 27.973/26.000/48.000 | 27.392/25.000/48.000 | 26.460/24.000/46.000 | 25.107/24.000/40.000 | 0.568 | 212→212 | 0 |
| V4S | 28.494/26.000/46.000 | 27.981/26.000/46.000 | 27.154/25.000/45.000 | 25.953/25.000/39.000 | 0.500 | 212→282 | 1840 |
| V4U | 27.938/26.000/48.000 | 27.346/25.000/48.000 | 26.395/24.000/46.000 | 25.081/24.000/40.000 | 0.558 | 212→217 | 105 |
| V4C | 22.098/16.000/48.000 | 21.745/16.000/48.000 | 23.859/20.000/46.000 | 25.074/24.000/40.000 | 0.840 | 217→260 | 1511 |

## Enormous/-132790047 — 400×267

Tierra 42730. Estado oficial `BEC0FD828B15236B41D422E25040D890DE7C5B1C35445A7245041F951A504691`, BASE `3B4E5208EB6444EE47A5F87D091B43E11349B703FD18963D329BBAB32E92219F`, FINAL `1F797D94DEF4E9CB4A1AA945FCAD4156229E76486570F8BDC34E029CEB000B00`; idénticos antes/después, automático/manual y guardado/carga. Mismo fixture y máscara para todas las variantes.

| Variante | Media | Mediana | P90 | P99 | Máx. | Pendiente media/máx. | Costa media/máx. | Δ medio padre | Δ absoluto V0 |
|---|---:|---:|---:|---:|---:|---|---|---:|---:|
| V0 | 32.207 | 26.000 | 71 | 77 | 100 | 3.094/57 | 35.214/89 | 0.000 | 0.000 |
| V1 | 32.566 | 27.000 | 71 | 81 | 100 | 2.105/12 | 29.165/84 | 0.359 | 3.983 |
| V1T | 32.844 | 27.000 | 71 | 87 | 100 | 2.121/10 | 29.182/85 | 0.278 | 4.238 |
| V2 | 51.261 | 51.000 | 67 | 72 | 76 | 1.180/4 | 49.056/71 | 18.417 | 21.105 |
| V3 | 51.148 | 51.000 | 67 | 71 | 74 | 1.163/4 | 48.983/70 | -0.113 | 21.153 |
| VP | 32.391 | 26.000 | 71 | 81 | 100 | 3.126/58 | 35.388/97 | 0.184 | 0.218 |
| VS | 32.579 | 27.000 | 71 | 82 | 100 | 2.119/12 | 29.165/84 | 0.014 | 3.996 |
| V4O | 33.261 | 33.000 | 49 | 54 | 58 | 1.180/4 | 31.056/53 | -18.000 | 10.731 |
| V4S | 33.172 | 33.000 | 47 | 52 | 55 | 1.071/3 | 31.223/51 | -0.089 | 11.609 |
| V4U | 33.243 | 33.000 | 49 | 54 | 57 | 1.176/4 | 31.049/53 | -0.018 | 10.749 |
| V4C | 32.627 | 33.000 | 49 | 54 | 57 | 1.258/5 | 25.310/53 | -0.616 | 10.455 |

| Nueva variante | ≥60 /≥75 /≥85 /≥90 /≥95 /=100 celdas | ≥20 tierra | Crestas /compactas /anillos P99 relativo | Recorrido /ancho /elongación del componente P99 principal | r altura–H04 |
|---|---|---:|---|---|---:|
| VP | 7276 /3107 /194 /65 /13 /1 | 1154 | 6/2/0 (nivel 81) | 48/5/10.400 | 0.259 |
| VS | 7001 /3162 /222 /84 /21 /3 | 0 | 2/7/0 (nivel 82) | 16/7/2.286 | 0.064 |
| V4O | 0 /0 /0 /0 /0 /0 | 0 | 0/3/0 (nivel 54) | 28/11/3.000 | 0.044 |
| V4S | 0 /0 /0 /0 /0 /0 | 0 | 0/2/0 (nivel 52) | 25/9/3.444 | 0.043 |
| V4U | 0 /0 /0 /0 /0 /0 | 0 | 0/3/0 (nivel 54) | 25/9/3.444 | 0.043 |
| V4C | 0 /0 /0 /0 /0 /0 | 0 | 0/3/0 (nivel 54) | 25/9/3.444 | -0.014 |

| Nueva variante | 1–15 | 16–50 | 51–60 | 61–75 | 76–84 | 85–94 | 95–100 |
|---|---:|---:|---:|---:|---:|---:|---:|
| VP | 14785 (34.601%) | 18675 (43.705%) | 2253 (5.273%) | 4410 (10.321%) | 2413 (5.647%) | 181 (0.424%) | 13 (0.030%) |
| VS | 14763 (34.549%) | 18718 (43.805%) | 2436 (5.701%) | 4072 (9.530%) | 2519 (5.895%) | 201 (0.470%) | 21 (0.049%) |
| V4O | 2867 (6.710%) | 36768 (86.047%) | 3095 (7.243%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) |
| V4S | 1737 (4.065%) | 40003 (93.618%) | 990 (2.317%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) |
| V4U | 2867 (6.710%) | 36775 (86.064%) | 3088 (7.227%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) |
| V4C | 3903 (9.134%) | 35739 (83.639%) | 3088 (7.227%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) |

Costa: cada celda de tabla muestra **media /mediana /P90**; distribuciones completas con P99, máximo, dispersión, conteo y niveles dominantes están en el log. Primera celda y 1–3 se solapan como se indicó.

| Curva | Primera, océano d=1 | Océano d=1–3 | d=4–8 | Interior d≥9 | Salto medio tierra litoral | Corridas antes→después | Nuevas aristas iguales a misma profundidad |
|---|---|---|---|---|---:|---|---:|
| V2 | 49.214/48.000/66.000 | 49.586/49.000/66.000 | 50.652/51.000/67.000 | 51.485/51.000/67.000 | 0.618 | 144→174 | 1558 |
| V4O | 31.214/30.000/48.000 | 31.586/31.000/48.000 | 32.652/33.000/49.000 | 33.485/33.000/49.000 | 0.618 | 174→174 | 0 |
| V4S | 31.365/30.000/46.000 | 31.689/31.000/46.000 | 32.637/33.000/47.000 | 33.370/33.000/47.000 | 0.544 | 174→202 | 1173 |
| V4U | 31.208/30.000/48.000 | 31.581/31.000/48.000 | 32.643/33.000/49.000 | 33.465/33.000/49.000 | 0.615 | 174→173 | 42 |
| V4C | 25.492/22.000/48.000 | 26.264/23.000/48.000 | 30.445/30.000/49.000 | 33.461/33.000/49.000 | 0.876 | 173→155 | 823 |

## Enormous/2026 — 400×267

Tierra 42720. Estado oficial `384A12055A860A831473C6A569DB566B99A3B2DAA78E586403236E71DC0FDF1B`, BASE `E5D47324C8C1C32992B7F04691D81A8519AB603D5AEF45CE7486E06B656597B8`, FINAL `C463E92779F2B0D2EA8F2DE50AC740064404871645F8978FA94DEEF30AC59EB8`; idénticos antes/después, automático/manual y guardado/carga. Mismo fixture y máscara para todas las variantes.

| Variante | Media | Mediana | P90 | P99 | Máx. | Pendiente media/máx. | Costa media/máx. | Δ medio padre | Δ absoluto V0 |
|---|---:|---:|---:|---:|---:|---|---|---:|---:|
| V0 | 32.341 | 28.000 | 68 | 80 | 100 | 2.631/53 | 28.349/80 | 0.000 | 0.000 |
| V1 | 32.424 | 28.000 | 68 | 85 | 100 | 2.147/10 | 29.074/97 | 0.083 | 2.244 |
| V1T | 32.713 | 28.000 | 68 | 89 | 100 | 2.155/10 | 29.857/100 | 0.289 | 2.480 |
| V2 | 48.510 | 49.000 | 63 | 72 | 77 | 1.151/4 | 46.883/77 | 15.797 | 18.410 |
| V3 | 48.448 | 49.000 | 63 | 71 | 75 | 1.140/4 | 46.737/75 | -0.061 | 18.470 |
| VP | 32.560 | 28.000 | 68 | 86 | 100 | 2.653/53 | 28.807/88 | 0.220 | 0.302 |
| VS | 32.425 | 28.000 | 68 | 85 | 100 | 2.148/10 | 29.077/97 | 0.001 | 2.243 |
| V4O | 30.510 | 31.000 | 45 | 54 | 59 | 1.151/4 | 28.883/59 | -18.000 | 9.972 |
| V4S | 30.741 | 31.000 | 44 | 51 | 56 | 1.044/3 | 29.291/56 | 0.232 | 10.971 |
| V4U | 30.497 | 31.000 | 45 | 53 | 57 | 1.148/4 | 28.830/57 | -0.013 | 9.985 |
| V4C | 29.730 | 30.000 | 45 | 53 | 57 | 1.241/5 | 22.338/57 | -0.767 | 9.472 |

| Nueva variante | ≥60 /≥75 /≥85 /≥90 /≥95 /=100 celdas | ≥20 tierra | Crestas /compactas /anillos P99 relativo | Recorrido /ancho /elongación del componente P99 principal | r altura–H04 |
|---|---|---:|---|---|---:|
| VP | 6200 /2813 /626 /174 /33 /2 | 661 | 2/0/0 (nivel 86) | 25/9/3.778 | 0.243 |
| VS | 6023 /2431 /490 /213 /86 /2 | 0 | 2/0/0 (nivel 85) | 50/11/6.273 | 0.044 |
| V4O | 0 /0 /0 /0 /0 /0 | 0 | 1/1/0 (nivel 54) | 29/11/2.636 | 0.032 |
| V4S | 0 /0 /0 /0 /0 /0 | 0 | 1/0/0 (nivel 51) | 73/13/6.308 | 0.032 |
| V4U | 0 /0 /0 /0 /0 /0 | 0 | 1/0/0 (nivel 53) | 75/13/6.538 | 0.032 |
| V4C | 0 /0 /0 /0 /0 /0 | 0 | 1/0/0 (nivel 53) | 75/13/6.538 | 0.022 |

| Nueva variante | 1–15 | 16–50 | 51–60 | 61–75 | 76–84 | 85–94 | 95–100 |
|---|---:|---:|---:|---:|---:|---:|---:|
| VP | 14780 (34.597%) | 18800 (44.007%) | 3187 (7.460%) | 3366 (7.879%) | 1961 (4.590%) | 593 (1.388%) | 33 (0.077%) |
| VS | 14766 (34.565%) | 18770 (43.937%) | 3368 (7.884%) | 3803 (8.902%) | 1523 (3.565%) | 404 (0.946%) | 86 (0.201%) |
| V4O | 3798 (8.890%) | 37453 (87.671%) | 1469 (3.439%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) |
| V4S | 2700 (6.320%) | 39325 (92.053%) | 695 (1.627%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) |
| V4U | 3798 (8.890%) | 37457 (87.680%) | 1465 (3.429%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) |
| V4C | 5211 (12.198%) | 36044 (84.373%) | 1465 (3.429%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) |

Costa: cada celda de tabla muestra **media /mediana /P90**; distribuciones completas con P99, máximo, dispersión, conteo y niveles dominantes están en el log. Primera celda y 1–3 se solapan como se indicó.

| Curva | Primera, océano d=1 | Océano d=1–3 | d=4–8 | Interior d≥9 | Salto medio tierra litoral | Corridas antes→después | Nuevas aristas iguales a misma profundidad |
|---|---|---|---|---|---:|---|---:|
| V2 | 46.687/44.000/67.000 | 46.582/44.000/67.000 | 46.814/44.000/68.000 | 48.925/50.000/62.000 | 0.601 | 204→179 | 1357 |
| V4O | 28.687/26.000/49.000 | 28.582/26.000/49.000 | 28.814/26.000/50.000 | 30.925/32.000/44.000 | 0.601 | 179→179 | 0 |
| V4S | 29.116/27.000/47.000 | 29.028/27.000/47.000 | 29.245/27.000/48.000 | 31.109/32.000/43.000 | 0.531 | 179→221 | 1348 |
| V4U | 28.637/26.000/49.000 | 28.537/26.000/49.000 | 28.791/26.000/50.000 | 30.917/32.000/44.000 | 0.595 | 179→185 | 117 |
| V4C | 22.083/17.000/49.000 | 22.345/17.000/49.000 | 26.097/23.000/50.000 | 30.912/32.000/44.000 | 0.902 | 185→211 | 941 |

## Enormous/42 — 400×267 — control independiente

Tierra 42721. Estado oficial `F2A815999C8CB4CDAB7717E8229B39A1C876A5894EE1D0F7DFF3B808371300EC`, BASE `15DCDB42D3F26CB11CBAB55D248EA6F873785019D482438EFDF6608DA91BCF73`, FINAL `ABEBB5F37B53988BB8533465D85CBA4F2F0AE1BA5641C890E83451E879023D31`; idénticos antes/después, automático/manual y guardado/carga. Mismo fixture y máscara para todas las variantes.

| Variante | Media | Mediana | P90 | P99 | Máx. | Pendiente media/máx. | Costa media/máx. | Δ medio padre | Δ absoluto V0 |
|---|---:|---:|---:|---:|---:|---|---|---:|---:|
| V0 | 32.440 | 30.000 | 64 | 79 | 100 | 3.209/51 | 44.139/90 | 0.000 | 0.000 |
| V1 | 33.062 | 31.000 | 68 | 83 | 100 | 2.247/11 | 29.783/80 | 0.622 | 5.192 |
| V1T | 33.371 | 31.000 | 68 | 90 | 100 | 2.284/11 | 29.989/86 | 0.309 | 5.494 |
| V2 | 49.096 | 51.000 | 62 | 69 | 73 | 1.144/4 | 48.017/68 | 15.725 | 19.341 |
| V3 | 49.062 | 51.000 | 62 | 68 | 71 | 1.136/4 | 47.997/67 | -0.034 | 19.358 |
| VP | 32.788 | 30.000 | 64 | 87 | 100 | 3.378/51 | 45.069/96 | 0.348 | 0.375 |
| VS | 33.064 | 31.000 | 68 | 83 | 100 | 2.250/11 | 29.798/80 | 0.003 | 5.190 |
| V4O | 31.096 | 33.000 | 44 | 51 | 55 | 1.144/4 | 30.017/50 | -18.000 | 9.920 |
| V4S | 31.260 | 33.000 | 43 | 49 | 52 | 1.044/4 | 30.334/48 | 0.164 | 10.855 |
| V4U | 31.094 | 33.000 | 44 | 51 | 54 | 1.144/4 | 30.017/50 | -0.002 | 9.922 |
| V4C | 30.441 | 32.000 | 44 | 51 | 54 | 1.227/5 | 23.623/50 | -0.653 | 9.908 |

| Nueva variante | ≥60 /≥75 /≥85 /≥90 /≥95 /=100 celdas | ≥20 tierra | Crestas /compactas /anillos P99 relativo | Recorrido /ancho /elongación del componente P99 principal | r altura–H04 |
|---|---|---:|---|---|---:|
| VP | 5520 /2387 /746 /261 /61 /1 | 1296 | 5/4/0 (nivel 87) | 33/3/11.667 | 0.205 |
| VS | 6272 /2871 /327 /116 /29 /3 | 0 | 0/4/0 (nivel 83) | 31/15/2.400 | 0.001 |
| V4O | 0 /0 /0 /0 /0 /0 | 0 | 0/1/0 (nivel 51) | 39/17/2.647 | 0.005 |
| V4S | 0 /0 /0 /0 /0 /0 | 0 | 0/3/0 (nivel 49) | 32/15/2.733 | 0.005 |
| V4U | 0 /0 /0 /0 /0 /0 | 0 | 0/1/0 (nivel 51) | 39/17/2.647 | 0.005 |
| V4C | 0 /0 /0 /0 /0 /0 | 0 | 0/1/0 (nivel 51) | 39/17/2.647 | -0.058 |

| Nueva variante | 1–15 | 16–50 | 51–60 | 61–75 | 76–84 | 85–94 | 95–100 |
|---|---:|---:|---:|---:|---:|---:|---:|
| VP | 14714 (34.442%) | 18877 (44.187%) | 3918 (9.171%) | 2991 (7.001%) | 1475 (3.453%) | 685 (1.603%) | 61 (0.143%) |
| VS | 14704 (34.419%) | 18788 (43.978%) | 3253 (7.615%) | 3476 (8.137%) | 2173 (5.086%) | 298 (0.698%) | 29 (0.068%) |
| V4O | 4154 (9.724%) | 38043 (89.050%) | 524 (1.227%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) |
| V4S | 2830 (6.624%) | 39743 (93.029%) | 148 (0.346%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) |
| V4U | 4154 (9.724%) | 38045 (89.055%) | 522 (1.222%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) |
| V4C | 5041 (11.800%) | 37158 (86.978%) | 522 (1.222%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) | 0 (0.000%) |

Costa: cada celda de tabla muestra **media /mediana /P90**; distribuciones completas con P99, máximo, dispersión, conteo y niveles dominantes están en el log. Primera celda y 1–3 se solapan como se indicó.

| Curva | Primera, océano d=1 | Océano d=1–3 | d=4–8 | Interior d≥9 | Salto medio tierra litoral | Corridas antes→después | Nuevas aristas iguales a misma profundidad |
|---|---|---|---|---|---:|---|---:|
| V2 | 48.045/49.000/61.000 | 48.060/49.000/61.000 | 48.221/49.000/62.000 | 49.280/51.000/62.000 | 0.582 | 113→117 | 1649 |
| V4O | 30.045/31.000/43.000 | 30.060/31.000/43.000 | 30.221/31.000/44.000 | 31.280/33.000/44.000 | 0.582 | 117→117 | 0 |
| V4S | 30.360/31.000/42.000 | 30.350/31.000/42.000 | 30.482/31.000/43.000 | 31.422/33.000/43.000 | 0.513 | 117→154 | 1137 |
| V4U | 30.045/31.000/43.000 | 30.060/31.000/43.000 | 30.221/31.000/44.000 | 31.278/33.000/44.000 | 0.582 | 117→117 | 0 |
| V4C | 23.675/23.000/41.000 | 24.056/23.000/42.000 | 27.637/28.000/44.000 | 31.273/33.000/44.000 | 0.890 | 117→118 | 826 |

## Evaluación de la transición costera

En el conjunto principal, V4C reduce el salto medio al océano frente a V4U en 12/12. Sin embargo, las corridas litorales pasan de 979 a 1092 en la suma de mundos y aumentan en 10/12 configuraciones. Aparecen nuevas aristas cuantizadas iguales a misma profundidad. La pendiente terrestre media también aumenta por la nueva transición. No se convierten esas corridas en una afirmación de artefactos visuales, pero son evidencia suficiente para no promover la costa por su media mejorada.

Las protecciones evitan cambios fuera de R y en todas las alturas redondeadas ≥50, y la prueba escalar conserva costas abruptas según el proxy r≥35. Esto demuestra límites algebraicos y de raster; no asegura conservación física de acantilados legítimos más bajos. V4C no fija una franja completa a un nivel, pero no puede darse por satisfecha la condición anti-líneas sin revisión posterior.

## Los dos casos prioritarios

| Caso /variante | Media | Máximo | Media primera costera | Media franja 1–3 | Salto al océano | Aristas tierra ≥20 | Corridas litorales |
|---|---:|---:|---:|---:|---:|---:|---:|
| Small/-1387338765/V0 | 31.193 | 100 | 41.784 | 40.788 | 41.720 | 624 | 7 |
| Small/-1387338765/V1 | 31.347 | 100 | 45.524 | 44.663 | 45.435 | 0 | 3 |
| Small/-1387338765/VP | 31.124 | 100 | 41.917 | 40.936 | 41.845 | 574 | 5 |
| Small/-1387338765/VS | 31.347 | 100 | 45.524 | 44.663 | 45.435 | 0 | 3 |
| Small/-1387338765/V4O | 24.820 | 67 | 32.547 | 32.146 | 32.488 | 0 | 7 |
| Small/-1387338765/V4S | 25.701 | 63 | 32.538 | 32.195 | 32.505 | 0 | 14 |
| Small/-1387338765/V4U | 24.787 | 61 | 32.500 | 32.081 | 32.447 | 0 | 7 |
| Small/-1387338765/V4C | 24.050 | 61 | 26.710 | 27.291 | 26.650 | 0 | 9 |
| Enormous/-132790047/V0 | 32.207 | 100 | 35.763 | 36.257 | 35.214 | 1092 | 125 |
| Enormous/-132790047/V1 | 32.566 | 100 | 29.442 | 30.201 | 29.165 | 0 | 149 |
| Enormous/-132790047/VP | 32.391 | 100 | 35.940 | 36.475 | 35.388 | 1154 | 125 |
| Enormous/-132790047/VS | 32.579 | 100 | 29.442 | 30.201 | 29.165 | 0 | 148 |
| Enormous/-132790047/V4O | 33.261 | 58 | 31.214 | 31.586 | 31.056 | 0 | 174 |
| Enormous/-132790047/V4S | 33.172 | 55 | 31.365 | 31.689 | 31.223 | 0 | 202 |
| Enormous/-132790047/V4U | 33.243 | 57 | 31.208 | 31.581 | 31.049 | 0 | 173 |
| Enormous/-132790047/V4C | 32.627 | 57 | 25.492 | 26.264 | 25.310 | 0 | 155 |

V4O merece la primera comparación visual por resolver el desplazamiento general sin una nueva dependencia de distancia a costa. V4U permite comprobar si su compresión adicional aporta algo o quita demasiado relieve alto. V4S conserva más celdas fuera del suelo 1, con coste de cuantización; V4C es un contraste de riesgo, no una solución ya aceptada. Los datos detallados de máximos, componentes, huecos y corridas incluyen IDs/coordenadas en el log; no se ha inspeccionado una imagen.

## Control independiente 42

Los valores de 42 no se usaron para modificar parámetros. V4O produce medias 29.88–31.10 y máximos 52–55 según tamaño, en vez de un máximo obligatorio 100. V4C mejora el salto medio al océano también en los cuatro controles, pero su proxy de corridas no responde igual que en las semillas principales. Esto confirma variación entre configuraciones y evita tratar una conclusión del conjunto principal como garantía universal.

## Rendimiento y memoria

Cada núcleo se calienta y se mide tres veces; se conservan tiempos/asignaciones por variante y mundo. La captura/sesión, construcción de métricas base y métricas costeras se registran separadas. No se incluyen hashes/serialización en tiempo de núcleo. Como antes, la construcción base excluye enumeraciones diferidas de deltas durante serialización; no es el coste total de toda la prueba. El comando completo lo mide SUMMARY. Asignación GC no significa memoria retenida ni pico RSS.

| Tamaño | Variante nueva | Mediana de medianas ms, 3 semillas principales | Máximo ms entre semillas | Mediana asignada MiB | Máxima asignación MiB |
|---|---|---:|---:|---:|---:|
| Small | VP | 5.535 | 8.198 | 0.864 | 0.870 |
| Small | VS | 5.185 | 7.155 | 0.864 | 0.870 |
| Small | V4O | 3.978 | 6.353 | 0.457 | 0.457 |
| Small | V4S | 3.776 | 5.923 | 0.457 | 0.457 |
| Small | V4U | 3.800 | 5.872 | 0.457 | 0.457 |
| Small | V4C | 5.634 | 7.155 | 0.457 | 0.457 |
| Medium | VP | 8.874 | 10.587 | 1.355 | 1.355 |
| Medium | VS | 7.445 | 8.137 | 1.355 | 1.355 |
| Medium | V4O | 5.965 | 6.071 | 0.717 | 0.717 |
| Medium | V4S | 6.021 | 6.036 | 0.717 | 0.717 |
| Medium | V4U | 5.727 | 5.739 | 0.717 | 0.717 |
| Medium | V4C | 8.141 | 8.265 | 0.717 | 0.717 |
| Large | VP | 13.372 | 13.419 | 1.965 | 2.014 |
| Large | VS | 11.920 | 11.924 | 1.965 | 2.014 |
| Large | V4O | 9.106 | 9.251 | 1.030 | 1.030 |
| Large | V4S | 9.173 | 9.270 | 1.030 | 1.030 |
| Large | V4U | 9.714 | 9.891 | 1.030 | 1.030 |
| Large | V4C | 12.988 | 13.243 | 1.030 | 1.030 |
| Enormous | VP | 21.488 | 21.492 | 3.464 | 3.464 |
| Enormous | VS | 19.852 | 20.408 | 3.464 | 3.472 |
| Enormous | V4O | 15.417 | 15.469 | 1.834 | 1.834 |
| Enormous | V4S | 15.288 | 16.994 | 1.834 | 1.834 |
| Enormous | V4U | 15.982 | 16.884 | 1.834 | 1.834 |
| Enormous | V4C | 22.216 | 22.312 | 1.834 | 1.834 |

Las nuevas curvas son lineales en número de celdas; V4C añade un recorrido de vecinos (grado≤6) y scratch continuo de tamaño N, sin caché permanente. Las métricas litorales recorren regiones una vez y hacen barridos de grupos candidatos; el análisis morfológico es diagnóstico explícito y más caro que el núcleo. Los resultados de cada configuración se liberan al terminarla. No existe coste de integración en la ruta Game: ningún código productivo ni dependencia de Tests se añade al juego.

## Hallazgos y límites

- **Demostrado:** las cinco referencias retienen sus 60 pares de hashes y métricas; el efecto de presión en el potencial domina los saltos ≥20; el soporte aislado no explica el máximo forzado 100. Los nuevos mapas no alcanzan 100 por una normalización del máximo observado. V4C no eleva ninguna celda, preserva alturas protegidas/exterior, pero aumenta indicadores de corridas en buena parte de la muestra.
- **Probable:** separar relieve regional de presión heredada y usar una referencia vertical fija moderada es una dirección más útil que mantener Q con un máximo 100. Un taper dependiente de distancia/quantización puede remarcar litorales aun siendo suave analíticamente.
- **No determinado:** realismo visual/físico, rango óptimo de alturas, pertinencia de los proxies de costa abrupta, desempeño con todas las semillas, ausencia universal de franjas/anillos y comportamiento de un FINAL/clima experimental. No se han calculado ni adoptado estos últimos. Cero candidatos en algunos umbrales no equivale a una garantía morfológica.

La recomendación es evaluación visual futura de V4O frente a V4U, con V0/V1 como controles; no seleccionar automáticamente V4O ni seguir reajustando aquí. Para continuar a P03-A.8.3 o añadir una exposición visual de variantes hace falta una nueva decisión del usuario.

## Archivos del commit de implementación

- `GEvolve.Tests/PANGAEA_LOWLAND_VARIANTS.md`
- `GEvolve.Tests/PangaeaBaseVariantTests.cs`
- `GEvolve.Tests/PangaeaBaseVariants.cs`
- `GEvolve.Tests/PangaeaLowlandMetrics.cs`
- `GEvolve.Tests/PangaeaRegionalLowlandVariants.cs`
- `GEvolve.Tests/Program.cs`
- `GEvolve.Tests/ValidationReports/commands.ps1`

Los informes y la actualización de roadmap/memoria se publican en el commit documental posterior. No se cambian los logs antiguos, catálogos o baselines. El swap ajeno `.BiomeTests.cs.swp` queda sin tocar ni versionar.

## Repetición

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\GEvolve.Tests\ValidationReports\commands.ps1 -ValidationSet BaseLowlandVariants -Label P03-A82b -IsolatedArtifacts
```

BaseLowlandControls ejecuta build+controles; BaseLowlandComparison build+176 comparaciones; BaseLowlandVariants añade equivalencia/sesiones, laboratorio y general. Todos guardan streams/códigos completos en una carpeta UTC nueva. No lanzan Game. La precomprobación conserva fuente sin commit explícitamente señalada; la ejecución canónica certifica 47c7c7d con fuente limpia. No se declara aprobada la apariencia.

## Validación técnica final

Ejecución canónica UTC 2026-11-08 23:47:56–23:57:15, SDK 10.0.402, commit 47c7c7da9eab52ecadbf99a7aa87603186a7e4a2 con fuente versionada limpia. Los tiempos y códigos reales están en [SUMMARY.md](SUMMARY.md).

| Comando / alcance | Estado | Código | Segundos |
|---|---|---:|---:|
| Build aislado, 0 advertencias/errores | PASS | 0 | 3.784 |
| Variantes: 176 casos, 16 mundos, 1387 máscaras y controles | PASS | 0 | 78.146 |
| Equivalencia por etapas/sesiones: 28 Pangea +12 controles | PASS | 0 | 64.815 |
| Laboratorio: cuatro tamaños | PASS | 0 | 12.987 |
| General: 17 suites PASS /2 FAIL históricos | FAIL | 1 | 395.992 |

Los dos fallos son TectonicMotion (4 escenarios) y ContinentalGeology (12 escenarios), exclusivamente referencias históricas de elevación ya discrepantes. Se compararon los 32 registros «BASELINE MISMATCH»/«elevation» con P03-A.8: 32 frente a 32, cero diferencias. Los 40 registros STAGED_EQ/STAGED_OTHER_PASS también coinciden exactamente. La suite general conserva su fallo y código 1; no se actualizó ningún baseline.

[Controles preliminares](../P03-A82b-controls_20261108_234259359Z_9c7fb946/REPORT.md) y [comparación preliminar](../P03-A82b-precheck_20261108_234443072Z_bc0be7c0/REPORT.md) se preservan con su identificación de fuente aún sin commit. No sustituyen esta validación canónica.

Documentación adicional modificada: PANGAEA_LOWLAND_VARIANTS.md, ValidationReports/README.md, WORLD_GENERATION_ROADMAP.md, PANGAEA_ROADMAP.md y .agents/PROJECT_MEMORY.md. Se añaden únicamente los tres directorios textuales de evidencia de esta entrega. No se cambian los reportes históricos anteriores.