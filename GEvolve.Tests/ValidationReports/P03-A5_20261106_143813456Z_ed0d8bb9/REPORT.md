# P03-A.5 — Auditoría tectónica y distribución de cordilleras

**Estado:** auditoría ejecutada; P03-A continúa pendiente de aprobación. No se implementa P03-A.6.

**Ejecución canónica:** 2026-11-06, 14:38:13–14:38:30 UTC, rama `worldgeneration_pangea`, commit ejecutado `74db94f6a4bc4f848245bb0bcf0e882aa71d7b1d`. SDK 10.0.402, runtime .NET 10.0.13, Windows 10.0.26200, X64; PowerShell 5.1. El estado fuente estaba limpio. [Resumen de ejecución](SUMMARY.md), [build completo](build.log), [auditoría completa](tectonic-audit.log), [script exacto](commands.ps1).

`dotnet build`: **PASS**, salida 0, 5,543 s, sin errores ni advertencias. `dotnet run --project GEvolve.Tests -- --pangaea-tectonic-audit`: **PASS**, salida 0, 11,464 s, las cinco semillas requeridas. No se ejecutó el juego interactivo. La suite general no se repitió en esta fase; su [ejecución anterior](../P03-A4_20261106_140522345Z_f747ba65/SUMMARY.md) conserva 13 suites PASS y dos fallos por huellas históricas de elevación, sin actualizar esos baselines.

## 1. Método y límites

- Una llamada normal y otra con traza por semilla, **solo en la prueba**, verificaron hashes idénticos del estado completo persistible, cuadrícula, estadísticas y campos BASE/APORTE. Los hashes completos de estado, superficie, placas y H04 están en cada `AUDIT_SUMMARY` del log. Se comprobó también serialización exacta de sistemas y conectividad terrestre de los ejes. El juego sigue generando un solo mundo.
- La instrumentación observa las ramas existentes del generador: no cambia predicados, RNG, asignación de parejas, recorridos, elevaciones, anchuras ni levantamiento. Los registros son temporales de Engine, fuera de State y de los guardados. Los valores y fórmulas de P03-A.4, paleta, shader y catálogos no se modificaron.
- «Contacto» significa una arista canónica entre dos celdas de placas distintas. «Terrestre» exige tierra en ambos extremos. Se distinguen cualquier convergencia normal positiva y la clasificada como `Convergent`/`Mixed` por H04; ninguna certifica una colisión cortical continental histórica.
- La elegibilidad actual exige además convergencia ≥1, convergencia ≥0,65×cizalla y tierra a ambos lados. Después cada celda se asigna a su pareja elegible dominante. En Large, el mínimo de eje es 10 celdas; los dos filtros compactos comparan longitudes con 24/40 y elongación con 2/1,35. Solo se observan: no se suavizan.
- `AUDIT_CANDIDATE` registra **todos** los componentes candidatos y su primer descarte real. Un eje o ancho no evaluado se marca nulo; no se inventa. `SpanProxyCells` es una extensión aproximada en el grafo mediante dos barridos, no kilómetros ni diámetro exacto de cualquier grafo. La elongación usada por selección se distingue de una medición diagnóstica. Los ciclos del grafo de celdas no certifican anillos geológicos.
- Las etiquetas favorable/desfavorable son las aportadas por el usuario. Son cinco casos seleccionados, todos Large 300×200: no una muestra aleatoria ni evidencia de frecuencias universales. Los números siguientes están redondeados para lectura; el log contiene los valores originales.

## 2. Hechos medidos: disponibilidad tectónica

| Semilla | Visual | Tierra | Contactos totales | Terrestres | Convergencia positiva terrestre | Convergencia clasificada terrestre | Clasificados/1000 tierra | Placas implicadas | Convergencia media |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|
| -714216669 | Favorable | 24018 | 4276 | 1938 | 758 | 417 | 17,36 | 12 | 4,15 |
| -2024691550 | Favorable | 24000 | 3994 | 1052 | 493 | 349 | 14,54 | 6 | 8,25 |
| -174327693 | Desfavorable | 24881 | 4147 | 1181 | 621 | 354 | 14,23 | 7 | 4,70 |
| -1979952984 | Desfavorable | 24000 | 3645 | 896 | 217 | 168 | 7,00 | 6 | 3,59 |
| 2076374193 | Desfavorable | 28127 | 3742 | 1064 | 622 | 379 | 13,47 | 7 | 9,51 |

Las longitudes de contacto se expresan en aristas; «placas implicadas» cuenta las que participan en esos contactos convergentes terrestres, no el número de placas generadas, que sigue siendo 16.

**Respuesta 1 — ¿Las desfavorables tienen menos convergencia terrestre?** No de manera general. `-1979952984` sí tiene una densidad claramente menor. `-174327693` tiene 354 contactos clasificados frente a 349 de la favorable `-2024691550`, con densidades próximas. `2076374193` tiene más contactos y mayor intensidad media que esa favorable; su mayor superficie terrestre reduce la densidad normalizada a 13,47. La hipótesis de escasez global explica parte del segundo caso, no las tres desfavorables.

## 3. Hechos medidos: selección y conservación de sistemas

| Semilla | Elegibles | % de convergentes clasificados | Candidatos | Seleccionados = serializados | Rechazo total | Componente <10 | Eje <10 | Rechazos compactos tras longitud | Celdas de eje | Influencia/tierra |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| -714216669 | 318 | 76,26 % | 50 | 15 | 70,00 % | 31 | 4 | 0 | 268 | 15,20 % |
| -2024691550 | 326 | 93,41 % | 22 | 5 | 77,27 % | 12 | 5 | 0 | 220 | 13,40 % |
| -174327693 | 354 | 100,00 % | 29 | 7 | 75,86 % | 19 | 3 | 0 | 242 | 12,33 % |
| -1979952984 | 134 | 79,76 % | 18 | 7 | 61,11 % | 10 | 1 | 0 | 117 | 7,26 % |
| 2076374193 | 379 | 100,00 % | 24 | 7 | 70,83 % | 16 | 1 | 0 | 264 | 11,91 % |

Se aceptaron los **41 candidatos que superaron la longitud mínima**, sin rechazos compactos en esta muestra. De 143 componentes candidatos, 88 se descartaron por tamaño de componente y 14 por longitud de eje. No se presupone que un componente rechazado fuera una cordillera geológicamente válida; «suficientemente largo» y «válido» no son equivalentes.

Tras la asignación dominante de parejas, el porcentaje de aristas convergentes internas a candidatos fue 76,26/93,12/99,72/79,17/100,00 %. El porcentaje asociado a un componente seleccionado fue 56,12/79,66/85,88/67,86/92,08 %, respectivamente. No es cobertura de terreno: las celdas de influencia son otra métrica.

**Respuesta 2 — ¿Los filtros rechazan proporcionalmente más sistemas válidos en las desfavorables?** No lo demuestra esta muestra. El rechazo total se solapa con las favorables; la desfavorable `-1979952984` tiene el menor porcentaje de rechazo. Ningún filtro compacto eliminó sistemas que pasaran longitud. Las desfavorables `-174327693` y `2076374193` retienen más eje que la favorable `-2024691550`. No hay evidencia para rebajar filtros o forzar un número de sistemas.

### Contactos extensos sin sistema seleccionado

Se identificaron segmentos convergentes sin selección cuya extensión aproximada alcanzaba al menos las 10 celdas del mínimo actual:

| Semilla | Segmento / pareja | Aristas | Span aproximado | Elegibles | Resultado |
|---|---|---:|---:|---:|---|
| -714216669 | 16 / 1–10 | 12 | 11 | 0 | Convergencia máxima 0,576, inferior a 1 |
| -714216669 | 41 / 3–10 | 44 | 50 | 0 | Convergencia máxima 0,525, inferior a 1 |
| -714216669 | 43 / 3–10 | 14 | 16 | 0 | Convergencia máxima 0,525, inferior a 1 |
| -2024691550 | 4 / 1–11 | 26 | 18 | 8 | La parte elegible queda en candidatos 19 y 20: eje 9 y componente 2, respectivamente |

Estos casos pertenecen a semillas favorables. No se encontró un segmento **completamente no seleccionado** de esa extensión en las tres desfavorables. Esto no significa que cada ramal de un segmento aceptado forme parte del eje: el log conserva todos los componentes y decisiones para examinarlo.

## 4. Hechos medidos: BASE, crestas y representación

| Semilla | BASE media en eje | FINAL media en eje | Aporte medio en eje | Prominencia inmediata | Pendiente media de influencia | Eje ≥85 | Eje con color terrestre uniforme |
|---|---:|---:|---:|---:|---:|---:|---:|
| -714216669 | 50,94 | 60,59 | 9,65 | 3,77 | 9,25 | 18,66 % | 81,34 % |
| -2024691550 | 69,92 | 83,29 | 13,37 | 3,82 | 9,97 | 65,91 % | 34,09 % |
| -174327693 | 39,56 | 53,02 | 13,45 | 4,09 | 7,89 | 13,22 % | 86,78 % |
| -1979952984 | 43,00 | 52,03 | 9,03 | 2,12 | 5,36 | 21,37 % | 78,63 % |
| 2076374193 | 39,53 | 54,53 | 15,00 | 2,87 | 6,01 | 17,42 % | 82,58 % |

Unidades: niveles de altura; la pendiente usa el máximo salto a vecinos terrestres del contrato `TerrainSlope`, promediado en la influencia. La prominencia inmediata es altura del eje menos media de sus vecinos terrestres, ponderada por celdas de eje. Se informa también una referencia exterior al corredor en cada candidato: no es prominencia física en metros.

**Respuesta 3 — ¿Hay sistemas aceptados poco prominentes o poco visibles?** Sí, pero no todos los ejes bajos son poco prominentes:

- Desfavorable `-174327693`, sistema 6: **81 celdas**, BASE media 44,59, FINAL 57,09, prominencia inmediata 5,89 y exterior 41,81; ninguna celda de eje llega a 85. Es un sistema extenso con contraste local real.
- La misma semilla, sistema 7: **73 celdas**, BASE 11,16, FINAL 29,51, aporte 18,34 y prominencia exterior 18,88. Existe, pero su altura absoluta queda baja.
- Favorable `-2024691550`, sistema 2: **116 celdas**, BASE 71,14, FINAL 86,46 y prominencia exterior 41,40. Contraste exterior parecido al sistema 6 anterior, pero altura absoluta muy distinta.
- Desfavorable `-1979952984`, sistema 5: 13 celdas, FINAL 25,77, prominencia inmediata 0,50 y exterior 2,75. Aquí sí hay poca prominencia en ambas referencias. También existe un sistema débil en la favorable `-714216669` (sistema 9: FINAL 13,92 y prominencia inmediata 0,29).

**Representación comprobada, no una modificación de paleta:** las cinco generaciones tienen `Biomes=null` y exactamente un color terrestre no blanco en la vista `Biomes`/superficie. Se verificó que esa vista y la diagnóstica consumen la altura FINAL correcta. La [paleta actual](../../../GEvolve.Game/Graphics/BiomePalette.cs) devuelve un verde uniforme mientras no hay bioma clasificado, salvo cumbres desde 85; el [shader y sus flags](../../../GEvolve.Game/Graphics/HexGridRenderer.cs) no aplican escala de altura continua en esa vista. Por tanto, entre 78,63 % y 86,78 % de los ejes desfavorables recibe el mismo color que terreno ordinario en superficie. ALTURAS: FINAL y RELIEVE: CORDILLERAS sí permiten inspeccionar esos sistemas.

La limitación de superficie es real, pero no separa por sí sola ambas clases: la favorable `-714216669` también tiene 81,34 % del eje en ese color. Tampoco basta contar blanco: la desfavorable `2076374193` tiene 526 celdas blancas finales y la favorable `-714216669`, 135. No se conoce aquí qué vista/zoom se usó en cada imagen del usuario, así que no se atribuye un porcentaje de la evaluación visual a esta representación.

### Procedencia de elevaciones altas

| Semilla | BASE ≥75 | FINAL ≥75 | % de FINAL ≥75 ya ≥75 en BASE | BASE ≥75 fuera de influencia | % de tierra correspondiente | BASE ≥85 / FINAL ≥85 | % de blanco final heredado |
|---|---:|---:|---:|---:|---:|---:|---:|
| -714216669 | 1369 | 1507 | 90,84 % | 934 | 3,89 % | 38 / 135 | 28,15 % |
| -2024691550 | 1372 | 1672 | 82,06 % | 718 | 2,99 % | 188 / 629 | 29,89 % |
| -174327693 | 1522 | 1628 | 93,49 % | 1265 | 5,08 % | 139 / 191 | 72,77 % |
| -1979952984 | 822 | 877 | 93,73 % | 665 | 2,77 % | 139 / 175 | 79,43 % |
| 2076374193 | 1727 | 1813 | 95,26 % | 1353 | 4,81 % | 324 / 526 | 61,60 % |

**Respuesta 4 — ¿Qué parte procede de BASE?** En las desfavorables, 93,49–95,26 % de las celdas finales ≥75 ya superaba ese umbral en BASE; 61,60–79,43 % del blanco final también era blanco en BASE. Son atribuciones del **cruce de umbral**, no porcentajes de energía o altura física. Una celda heredada puede recibir levantamiento adicional; «fuera de influencia de cordilleras» tampoco significa «sin ninguna causa tectónica» en el relieve inicial.

## 5. Distribución regional

Se usaron nueve cajas diagnósticas: `region=floor(fila*3/200)*3+floor(columna*3/300)`. No afectan superficie, tectónica ni selección. Los extremos E/O siguen conectados aunque una caja corte esa costura. Contactos y sistemas que tocan dos cajas aparecen en ambas; no se deben sumar como conteos globales. Las superficies terrestres sí son disjuntas, y las densidades y fracciones se normalizan por su tierra real. Cajas sin tierra usan `n/a`.

- `-174327693`: la región 7 tiene 6600 celdas terrestres y 215 contactos convergentes incidentes, pero BASE media **17,17**. Hay cuatro sistemas con eje en esa caja. La zona 5, de BASE media 72,14, tiene solo dos contactos y ningún eje. Hay desajuste espacial entre la envolvente alta y la convergencia.
- `-1979952984`: región 4, 5837 celdas de tierra sin contactos convergentes ni ejes; región 1, 5696 celdas con dos contactos y ningún eje. La escasez local de sustrato tectónico adecuado es importante; no sería correcto llenar esas áreas mediante cuotas.
- `2076374193`: hay ejes en **siete de las nueve cajas**, más que en cada favorable (cinco). Su problema no puede describirse simplemente como selección concentrada en una sola región.
- La proporción de tierra en cajas sin ningún eje es 9,58/12,55 % en las favorables y 36,75/51,58/7,40 % en las desfavorables. Es un indicador grueso de distribución, no el porcentaje exacto de terreno sin montañas: una influencia puede entrar en una caja sin eje propio.

El apéndice incluye las nueve regiones por semilla; `AUDIT_REGION` conserva métricas adicionales de altura heredada, incremento y compresión H04 en el log completo.

## 6. Interpretación y recomendación

**Respuesta 5 — causa principal:** los datos respaldan una combinación de la hipótesis 1 **localmente** y la hipótesis 3. No respaldan que los filtros compactos sean el causante general de las desfavorables. Las tres tienen ejes de menor altura absoluta media; dos conservan tanto eje y convergencia como una favorable. La colocación de BASE respecto a esos ejes y la representación bajo 85 son relevantes. El caso `-1979952984` añade escasez y distribución espacial de convergencia. No hay una causa única demostrada para todas las semillas.

**Mecanismo numérico verificable:** el levantamiento actual es aditivo, con máximo solicitado 25, atenuado por intensidad, perfil longitudinal y distancia al eje; los sistemas cortos reciben además escala 0,65. La composición conserva `max(BASE,min(94,round(BASE+aporte)))`. Un eje sobre BASE 11 no puede convertirse en altura 75 con ese incremento, aunque tenga continuidad y convergencia: depende fuertemente de una envolvente BASE que se calcula por otra estrategia. Esto explica diferencias de altura sin pérdida de sistemas; no demuestra por sí solo qué altura debería tener cada cordillera.

**Respuesta 6 — P03-A.6 recomendado, sujeto a autorización:** mantener selección, ejes y anchuras, y ensayar **una única modificación de composición vertical dentro de los corredores existentes**: una envolvente de cresta derivada de convergencia H04 y continuidad del sistema, mezclada gradualmente con BASE, frente a la suma de un incremento acotado independiente de la altura de partida. No fijar un mínimo universal de 85, no añadir sistemas ni elevar regiones vacías. Comparar las mismas cinco semillas y las referencias anteriores por prominencia, pendientes y concentración, no por cantidad de blanco. Esta es una hipótesis de mejora a contrastar, no una fórmula ya aprobada.

Antes de juzgar ese ensayo, usar ALTURAS: FINAL y RELIEVE: CORDILLERAS al mismo zoom junto con SUPERFICIE. Si la insatisfacción se limita a esta última, el cambio mínimo correcto sería una representación continua del relieve final sub85 (altura/pendiente sobre el color de terreno), sin modificar elevaciones; debe tratarse aparte para no mezclar efectos de generación y representación. La auditoría no cambia paleta ni shader ni prescribe montañas altas por motivos de color.

## 7. Reproducción y alcance de la evidencia

Desde la raíz del repositorio:

```powershell
.\GEvolve.Tests\ValidationReports\commands.ps1 -ValidationSet TectonicAudit -Label P03-A5
```

El script crea un directorio UTC nuevo, captura completos stdout/stderr en secciones separadas, códigos y duración, normaliza rutas personales y conserva la copia exacta del script. El [resumen](SUMMARY.md) permite repetir el snapshot validado con sus argumentos. Los registros `AUDIT_CANDIDATE`, `AUDIT_SEGMENT`, `AUDIT_REGION`, `AUDIT_SURFACE` y `AUDIT_SUMMARY` son evidencia numérica; las interpretaciones y recomendaciones de este documento no modifican resultados.

No se añadieron semillas fuera de las cinco solicitadas. Los tiempos de generación con/sin traza oscilan entre 474 y 765 ms en esta ejecución; el control se ejecuta primero, por lo que calentamiento JIT/caché impide interpretar esa diferencia como una optimización o un benchmark de sobrecoste. No hay aprobación visual ni generalización a otros tamaños. Los controles de invariancia pasaron en los cinco casos y la revisión de código conserva intactas las decisiones de generación; no se actualizaron baselines ni contratos de persistencia.

## Apéndice: regiones, normalizadas por tierra

`Conv/1000` son contactos incidentes por 1000 celdas terrestres; `% elevadas` son celdas con incremento P03-A positivo sobre la tierra de la caja. Las dos medias usan solo celdas terrestres. Las columnas `Cand` y `Ejes` cuentan sistemas que tocan la caja, no cuotas ni nuevos criterios de generación.

| Semilla | Región | Tierra | Conv/1000 | Cand | Ejes | % elevadas | BASE media | FINAL media | BASE ≥75 fuera / tierra |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| -714216669 | 0 | 6700 | 9.40 | 8 | 3 | 11.07 | 33.45 | 33.96 | 2.79 % |
| -714216669 | 1 | 4596 | 18.28 | 13 | 5 | 16.17 | 41.09 | 41.85 | 5.59 % |
| -714216669 | 2 | 5820 | 9.79 | 12 | 4 | 8.04 | 22.80 | 22.95 | 0.81 % |
| -714216669 | 3 | 3795 | 32.15 | 11 | 5 | 22.06 | 32.11 | 33.17 | 2.71 % |
| -714216669 | 4 | 805 | 6.21 | 2 | 1 | 7.20 | 45.58 | 45.87 | 6.09 % |
| -714216669 | 5 | 2302 | 38.23 | 7 | 0 | 0.43 | 27.13 | 27.13 | 12.64 % |
| -714216669 | 6 | 0 | n/a | 0 | 0 | n/a | n/a | n/a | n/a % |
| -714216669 | 7 | 0 | n/a | 0 | 0 | n/a | n/a | n/a | n/a % |
| -714216669 | 8 | 0 | n/a | 0 | 0 | n/a | n/a | n/a | n/a % |
| -2024691550 | 0 | 2115 | 23.64 | 3 | 2 | 27.99 | 54.27 | 55.56 | 13.43 % |
| -2024691550 | 1 | 6193 | 21.64 | 1 | 1 | 17.12 | 36.15 | 37.37 | 0.00 % |
| -2024691550 | 2 | 650 | 0.00 | 0 | 0 | 0.00 | 16.65 | 16.65 | 0.00 % |
| -2024691550 | 3 | 25 | 0.00 | 0 | 0 | 0.00 | 2.88 | 2.88 | 0.00 % |
| -2024691550 | 4 | 5789 | 5.87 | 7 | 2 | 3.64 | 24.44 | 24.62 | 2.66 % |
| -2024691550 | 5 | 1933 | 13.97 | 2 | 1 | 11.33 | 44.03 | 44.69 | 5.48 % |
| -2024691550 | 6 | 1688 | 0.00 | 0 | 0 | 0.00 | 32.89 | 32.89 | 7.41 % |
| -2024691550 | 7 | 4958 | 19.77 | 10 | 2 | 11.21 | 23.01 | 23.65 | 0.58 % |
| -2024691550 | 8 | 649 | 13.87 | 3 | 0 | 3.85 | 28.73 | 28.87 | 3.08 % |
| -174327693 | 0 | 514 | 0.00 | 0 | 0 | 0.00 | 29.51 | 29.51 | 0.00 % |
| -174327693 | 1 | 2613 | 0.00 | 0 | 0 | 0.00 | 25.63 | 25.63 | 0.00 % |
| -174327693 | 2 | 0 | n/a | 0 | 0 | n/a | n/a | n/a | n/a % |
| -174327693 | 3 | 1875 | 7.47 | 6 | 0 | 0.27 | 51.93 | 51.93 | 14.13 % |
| -174327693 | 4 | 5796 | 11.73 | 13 | 4 | 12.37 | 48.17 | 48.70 | 9.54 % |
| -174327693 | 5 | 483 | 4.14 | 1 | 0 | 0.62 | 72.14 | 72.15 | 59.63 % |
| -174327693 | 6 | 3658 | 0.00 | 0 | 0 | 0.00 | 18.10 | 18.10 | 1.48 % |
| -174327693 | 7 | 6600 | 32.58 | 10 | 4 | 21.79 | 17.17 | 18.45 | 0.12 % |
| -174327693 | 8 | 3342 | 18.25 | 2 | 2 | 13.91 | 34.08 | 34.81 | 2.90 % |
| -1979952984 | 0 | 377 | 0.00 | 0 | 0 | 0.00 | 40.64 | 40.64 | 2.12 % |
| -1979952984 | 1 | 5696 | 0.35 | 2 | 0 | 0.00 | 39.67 | 39.67 | 8.66 % |
| -1979952984 | 2 | 3401 | 9.11 | 4 | 2 | 12.73 | 39.93 | 40.45 | 4.41 % |
| -1979952984 | 3 | 5788 | 19.00 | 6 | 4 | 12.65 | 27.56 | 28.02 | 0.00 % |
| -1979952984 | 4 | 5837 | 0.00 | 0 | 0 | 0.00 | 21.14 | 21.14 | 0.00 % |
| -1979952984 | 5 | 193 | 0.00 | 0 | 0 | 0.00 | 12.90 | 12.90 | 0.00 % |
| -1979952984 | 6 | 2433 | 10.28 | 8 | 2 | 8.63 | 30.68 | 31.10 | 0.58 % |
| -1979952984 | 7 | 275 | 0.00 | 0 | 0 | 0.00 | 33.64 | 33.64 | 0.00 % |
| -1979952984 | 8 | 0 | n/a | 0 | 0 | n/a | n/a | n/a | n/a % |
| 2076374193 | 0 | 3604 | 29.69 | 8 | 3 | 23.83 | 29.51 | 31.25 | 1.50 % |
| 2076374193 | 1 | 146 | 54.79 | 1 | 1 | 25.34 | 56.05 | 58.14 | 3.42 % |
| 2076374193 | 2 | 3727 | 9.39 | 2 | 1 | 6.57 | 37.59 | 38.02 | 4.27 % |
| 2076374193 | 3 | 6700 | 18.51 | 13 | 3 | 12.48 | 32.47 | 33.15 | 5.30 % |
| 2076374193 | 4 | 3092 | 21.02 | 1 | 1 | 15.52 | 38.92 | 39.90 | 12.42 % |
| 2076374193 | 5 | 1981 | 0.00 | 0 | 0 | 0.10 | 32.10 | 32.10 | 6.97 % |
| 2076374193 | 6 | 3964 | 5.55 | 1 | 1 | 3.25 | 15.23 | 15.36 | 0.00 % |
| 2076374193 | 7 | 4813 | 6.65 | 3 | 2 | 6.69 | 40.23 | 40.54 | 4.84 % |
| 2076374193 | 8 | 100 | 0.00 | 0 | 0 | 0.00 | 65.97 | 65.97 | 25.00 % |
