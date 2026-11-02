# Continentes: morfología orogénica regional

La versión `continental-elevation-6` construye el relieve terrestre de Continentes a partir de los contactos y sistemas tectónicos existentes. No modifica las placas, su movimiento, los esfuerzos, los grupos propuestos por cada intento ni el generador de superficie. La selección usa ahora la cresta terrestre efectiva; por ello puede elegir otro intento. Los candidatos históricos se reproducen y comparan sin actualizar sus huellas.

## Causa de las cordilleras pequeñas

El modelo anterior medía la longitud del límite tectónico, aunque estuviera sumergido. Su perfil gaussiano alcanzaba el máximo junto al límite y decaía antes de llegar a tierra. Una subducción extensa podía dejar solo pequeños grupos elevados en la costa. Además, 24 celdas elevadas, incluso distribuidas en varios fragmentos, bastaban para validar un sistema principal. La selección evaluaba la versión anterior del relieve y después recalculaba la versión visible.

Se mantienen el análisis de oportunidades y los sistemas regionales de `9fec672`. El cambio se realiza en su expresión terrestre y en su medición, no aumentando globalmente la altura.

## Del contacto tectónico a la cresta

`TectonicReliefSystemAnalyzer` conserva los contactos clasificados y las uniones de suturas continentales compatibles. `OrogenicMorphology` calcula distancias mediante vecinos reales de `HexTopology`: cierre este-oeste, sin unión norte-sur.

1. El campo parte de los hexágonos del sistema tectónico. En colisión solo atraviesa corteza continental de sus placas participantes. En subducción oceánica–continental solo atraviesa corteza continental de la placa superior.
2. La deformación puede atravesar corteza continental sumergida. La distancia a tierra se mide dentro de esa corteza; en subducción también se restringe a la placa superior. No se proyecta una cresta a través de una cuenca de corteza oceánica.
3. La cresta continental sigue la sutura emergida o su llegada a tierra. En subducción se desplaza dos unidades de resolución hacia el interior respecto de esa llegada. Un segmento demasiado alejado de tierra no produce una cordillera terrestre.
4. La anchura parte de la extensión del sistema, acotada entre `3 × escala` y `9 × escala`, con `escala = filas / 133`. El alcance de propagación es `2,8 × anchura`; un origen sumergido solo alcanza una cresta si la tierra está a menos de `1,5 × anchura`.
5. Una pequeña deformación multiescala curva el contorno de distancia. Solo se cierra una discontinuidad de una celda terrestre dentro del mismo dominio. No se unen islas, sistemas incompatibles ni fronteras opuestas norte-sur.

Se guarda el origen tectónico de cada celda de cresta. El eje del sistema puede contener segmentos submarinos; la cresta efectiva solo contiene tierra elevada. Son dos geometrías distintas y se muestran por separado.

## Crestas, cordones, mesetas y valles

Desde la cresta se propaga un campo continuo por tierra elegible. El perfil transversal contiene una cresta central, cordones secundarios alrededor de `0,72 × anchura`, una depresión relativa entre ambos y estribaciones más amplias. La anchura y el desplazamiento secundario varían suavemente. Los cordones se curvan y se conectan al seguir un eje tectónico que cambia de orientación.

La intensidad combina la convergencia media del sistema (75 %) y la del contacto de origen (25 %), con una respuesta acotada respecto de la velocidad de referencia existente. Esto evita que una pequeña caída local de intensidad corte toda la cadena. El ruido periódico en horizontal modifica altura, anchura y pasos; no coloca cordilleras fuera de la deformación tectónica.

- **Colisión continental:** deformación a ambos lados de la sutura, cresta dominante, cordones secundarios y un campo de meseta más ancho. La meseta también contiene variación interna y desciende hacia los terrenos estables.
- **Subducción oceánica–continental:** cresta en la placa superior, desplazada hacia tierra; conserva la fosa en el lado oceánico. No usa el campo amplio de meseta continental.
- **Subducción oceánica–oceánica:** conserva el cálculo anterior de fosa y aptitud para elevar islas existentes. No crea superficie ni islas.
- **Divergencia y transformantes:** conservan los campos anteriores de rift, dorsal y falla local. No se convierten en sistemas de colisión.

Cuando coinciden campos convergentes, domina el levantamiento más fuerte; no se suman varias cordilleras completas sobre la misma columna de corteza. Se mantienen los coeficientes de altura de cada proceso, el relieve base y la subsidencia existente. La tierra conserva elevación 1–100, el océano 0 y las cumbres infranqueables comienzan en 85. La batimetría se verifica idéntica para el mismo candidato.

## Medición y aceptación

`OrogenicRangeMetrics` mide el relieve final, no la cantidad de cumbres blancas:

- **Cresta continua:** un recorrido real por vecinos de cresta terrestre con elevación ≥44. Dos búsquedas en anchura obtienen un recorrido largo; se guarda la ruta, no se suman longitudes de fragmentos desconectados. Es una medida conservadora de extensión, no una solución al problema de la ruta simple más larga.
- **Área conectada:** componente de terreno elevado asociado que contiene esa ruta.
- **Anchura efectiva:** `1 + 4 × distancia media a las crestas` dentro del componente. Es una estimación de franja equivalente que evita inflar la anchura dividiendo toda un área ramificada por una sola rama.
- **Prominencia:** altura media de la ruta respecto de los flancos terrestres externos del sistema. Sin flancos medibles no se atribuye prominencia.

Para ser principal, el sistema debe ser una colisión continental o una subducción oceánica–continental, alcanzar la longitud mínima, tener área conectada suficiente, anchura efectiva de al menos `2 × escala` y prominencia media de 12 unidades. La longitud mínima es el 10 % del número de filas, redondeado hacia arriba: 14, 17, 20 y 27 aristas en Pequeño, Mediano, Grande y Enorme. El área mínima es `longitud mínima × 2 × escala`. Son umbrales de extensión regional a resolución comparable; no representan cotas en metros ni una cuota de celdas de elevación 85.

Se conservan la clasificación principal/secundaria/menor y el máximo de tres sistemas dominantes. Una cadena estrecha o corta puede conservar relieve como secundaria sin justificar por sí sola la aceptación del mundo. Los mismos datos que se validan son los que se entregan al juego.

## Selección, persistencia y límites

Se mantienen ocho alternativas deterministas como máximo. No se regeneran placas ni se cambia la semilla. La selección conserva sus preferencias por varios sistemas en Grande y Enorme; si ninguna alternativa tiene una cordillera principal válida, falla de manera explícita. Véase [CONTINENTAL_SELECTION.md](CONTINENTAL_SELECTION.md).

La versión 6 persiste crestas, orígenes, recorrido continuo, área, anchura y prominencia. Los guardados de las versiones 1–5 y los de solo superficie siguen siendo legibles, sin inventar crestas ausentes ni regenerar el mundo al cargar. Se conserva el cálculo previo como referencia de pruebas; el juego usa la versión nueva.

El modelo sigue siendo estático. No simula millones de años de colisión, estratigrafía, erosión ni una historia paleogeográfica. Himalaya, Andes y Alpes son referencias conceptuales de morfología y proceso, no reproducciones físicas cuantitativas. No se avanzan volcanes, clima ni biomas.

## Reproducción y diagnósticos

```powershell
dotnet build GEvolve.sln --no-restore
dotnet run --project GEvolve.Tests --no-build --no-restore -- --orogenic-morphology
dotnet run --project GEvolve.Tests --no-build --no-restore -- --all-suites
dotnet run --project GEvolve.Game --no-build --no-restore -- --graphics-smoke
git diff --check
```

`--orogenic-morphology` comprueba 52 configuraciones: cuatro tamaños y trece semillas, incluida `-1358931540`. Reproduce los candidatos originales guardados antes de modificar el código, verifica sus hashes de superficie, grupos, corteza, identidad, placas y movimiento, comprueba determinismo, continuidad, procedencia tectónica, batimetría y persistencia. Incluye casos sintéticos para rechazar cumbres aisladas y comprobar ambos extremos del mapa.

`orogenic-comparison.csv` contiene los resultados antes/después, el intento seleccionado, cambios de superficie y tiempos. Los archivos `orogenic-before-*.svg` y `orogenic-after-*.svg` comparan cinco configuraciones que cubren los cuatro tamaños y la semilla señalada. Cada SVG muestra superficie con cordilleras, placas y direcciones, sistemas, crestas, levantamiento y elevación. Su CSV asociado incluye todos los sistemas. La versión anterior no guardaba una cresta terrestre separada; ese panel se identifica como no disponible.

Los archivos históricos `ApprovedContinentalSurfaceBaseline.txt`, `ApprovedContinentalSelectionBaseline.txt` y `ApprovedNonContinentalBaseline.txt` no se modifican. Cuando cambia el intento final, las pruebas informan la diferencia y reproducen exactamente el candidato histórico para comprobar el algoritmo de superficie. Las pruebas de ubicación usan además las crestas con procedencia tectónica validada; la comprobación de terrenos bajos incluye colinas de 18–29 junto a las llanuras. Se conservan los límites de cobertura y cumbres de las pruebas anteriores.
