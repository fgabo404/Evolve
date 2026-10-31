# Continentes: relieve de sistemas tectónicos

`PlateGroupSurfaceGenerator` fija superficie, corteza y continentes para cada agrupación candidata. La selección conserva sus reglas y resultados aprobados; véase [CONTINENTAL_SELECTION.md](CONTINENTAL_SELECTION.md). La etapa de elevación clasifica las interacciones de las 16 placas con las reglas deterministas de `ContinentalReliefGenerator.ClassifyContacts`. `TectonicReliefSystemAnalyzer` agrupa primero contactos vecinos del mismo par de placas y proceso. En la elevación final, conecta además tramos de colisión continental de pares distintos que comparten una placa y una sutura próxima. Cada sistema recibe un ID estable, sus interacciones, el eje de hexágonos conectado, las placas participantes, intensidad media, número de tramos y anchura derivada de esa longitud. Se usa la vecindad real de `HexTopology`, incluida la unión este-oeste.

`ContinentalElevationGenerator` calcula campos regionales a partir de esos ejes, sin propagar impulsos independientes desde cada hexágono fronterizo:

- **Colisión continental:** cinturón elevado a ambos lados del eje y meseta más amplia. Variación suave a lo largo del cinturón añade flancos, ramas menores y bajos intermontanos.
- **Subducción oceánica–continental:** fosa en la placa inferior oceánica y levantamiento con máximo desplazado hacia el interior de la placa superior terrestre. Se conserva la identidad de la placa superior para ubicar un arco volcánico en una etapa posterior; ahora no se crean volcanes.
- **Subducción oceánica–oceánica:** fosa en la placa inferior y campo de arco en la placa superior. Solo eleva tierra que ya existía; nunca crea nuevas islas ni mueve la costa.
- **Divergencia continental:** depresión en el eje y flancos elevados. **Divergencia oceánica:** dorsal de menor profundidad en un campo batimétrico separado.
- **Transformante:** deformación local acotada, sin un término de gran cordillera.

La intensidad usa la velocidad relativa de cada sistema. La anchura usa la longitud conectada, con límites globales iguales para todas las semillas. Una variación periódica pequeña modula únicamente el relieve secundario; no puede formar cordilleras lejos de los sistemas activos. El interior estable conserva llanuras, colinas y mesetas bajas. No hay cuantiles ni ajuste forzado de porcentajes de montaña.

`ElevationByCellId` mantiene tierra 1–100 y océano 0. La profundidad oceánica relativa sigue separada. Las cumbres desde 85 conservan la regla global de terreno infranqueable. `ContinentalElevationState` versión 5 guarda sistemas, contactos, tipos de relieve, campos regionales, placas participantes, clasificación de cordilleras y estadísticas de su extensión terrestre y elevación efectiva. El algoritmo numérico de elevación de la etapa de selección sigue siendo el anterior. Los guardados de las versiones 1 a 4 y los guardados solo de superficie siguen siendo legibles.

## Calibración de cordilleras

En la calibración anterior, la superficie y su selección de grupos de placas no se modificaron. Una colisión o subducción puede comenzar sobre corteza continental sumergida: el campo orogénico avanza por esa misma corteza hasta la tierra ya existente. El océano conserva elevación terrestre cero. El analizador une tramos convergentes del mismo par de placas separados por una sola discontinuidad local; incluye el hexágono intermedio en el eje conectado, sin cambiar la clasificación original de sus contactos. La etapa actual sí puede seleccionar otra agrupación continental para garantizar una cordillera efectiva.

Los sistemas con suficiente intensidad y tierra alcanzada se ordenan por el área de levantamiento fuerte, velocidad y longitud. Hasta los tres sistemas dominantes reciben la etiqueta **principal** si cumplen el umbral geológico; los siguientes son **secundarios** o **menores**. Esta jerarquía solo describe el relieve que ya se calculó: no aumenta alturas ni inventa una cordillera por cuota. Una semilla sin contactos útiles puede seguir sin sistema principal.

Comparación de semillas que antes tenían poca montaña, con la misma superficie y el mismo número de hexágonos terrestres:

| Tamaño / semilla | Montaña antes | Montaña ahora | Altura máxima antes → ahora |
| --- | ---: | ---: | ---: |
| Pequeño / 42 | 0 | 456 | 23 → 100 |
| Mediano / 42 | 64 | 241 | 60 → 78 |
| Mediano / -731 | 263 | 304 | 63 → 63 |
| Grande / 42 | 246 | 547 | 59 → 100 |

En seis semillas adicionales por tamaño, 23 de 24 mundos tienen al menos un sistema principal; 4 de los 12 mundos grandes o enormes tienen varios. La proporción de montaña varía entre semillas y permanece por debajo del 35 % de tierra de cada mundo probado.

## Comprobación

### Cinturones regionales y colisión continental

Cada contacto `ContinentalCollision` exige corteza continental en ambos lados y convergencia calculada por el movimiento original de las placas. Los contactos forman primero segmentos por par de placas. Dos segmentos se unen si comparten al menos una placa, sus intensidades medias son comparables (la menor alcanza el 30 % de la mayor) y sus ejes están a una distancia hexagonal máxima de dos. Una discontinuidad de una celda recibe un puente **solo en el eje del sistema**; no cambia contactos, corteza, superficie ni placas. La unión transitiva permite que una sutura recorra tres o más placas sin hacer que toda una placa sea montaña. Los contactos de subducción, divergencia y transformante mantienen sistemas separados.

El cinturón continental aplica plegamiento a ambos lados de la sutura, una meseta más ancha, flancos y transición hacia tierras bajas. La anchura depende de la longitud conectada y tiene un límite global. La fuerza depende de la convergencia del sistema y varía moderadamente con la compresión local ya calculada (`0,75–1,25` veces la señal). La variación secundaria preexistente evita una cresta de altura uniforme. La subducción oceánica–continental conserva su fosa oceánica y levantamiento desplazado hacia el interior terrestre; no usa esta unión de suturas continentales. Los rifts y las dorsales usan sus campos propios. Ningún proceso crea tierra ni cambia el nivel del mar.

La evaluación de las ocho agrupaciones posibles sigue utilizando el análisis y la elevación anteriores. Una vez elegida la agrupación, se calcula el relieve regional sobre **esa misma superficie**. Por ello los intentos elegidos, la agrupación, la corteza, las costas y los IDs de placa permanecen idénticos. `Selection` guarda por separado los conteos de sistemas principales/secundarios de evaluación y los del relieve final. Esto agrega una pasada de relieve a la generación de Continentes.

En 12 combinaciones de semillas y tamaños, 212 segmentos de colisión anteriores se agruparon en 171 sistemas regionales; 24 cinturones abarcan tres o más placas. El mayor eje conectado creció 236 aristas respecto al máximo anterior. Las mayores longitudes observadas fueron 142, 187, 503 y 519 aristas en Pequeño, Mediano, Grande y Enorme. En 28 cinturones la altura a lo largo del eje varió al menos 15 unidades; hubo 46 631 hexágonos de meseta y 75 233 de llanura terrestre en conjunto. En los 48 mundos de selección, todos conservaron al menos una cordillera principal y 17 de 24 mundos grandes/enormes tuvieron varias en el relieve final. Son medidas de las semillas probadas, no cuotas del generador.

Las capturas `continental-belts-*.svg` comparan, para siete configuraciones, superficie, placas y movimiento, sistemas tectónicos (ejes continentales de varias placas en morado), cordilleras, deformación y altura. Las huellas aprobadas de superficie y placas se mantienen iguales en las 12 configuraciones de regresión. El modelo continúa siendo una instantánea estática de la tectónica: no simula una historia real de colisiones ni erosión.

Las 12 huellas históricas de `ApprovedContinentalSurfaceBaseline.txt` permanecen intactas y demuestran que los IDs de placas no cambiaron. `ApprovedContinentalSelectionBaseline.txt` fija las nuevas superficies, agrupaciones, cortezas e identidades de 12 configuraciones tras validar su calidad. Las pruebas verifican la partición de contactos en ejes conectados, determinismo, alturas, continuidad horizontal, persistencia y señales de colisiones, subducción, rifts y dorsales. Las 36 huellas de Pangea, Continentes pequeños y Archipiélagos siguen bajo sus pruebas históricas.

Para exportar comparaciones de **superficie**, **placas y movimiento**, **sistemas clasificados**, **cordilleras principales y secundarias**, **levantamiento/subsidencia** y **altura final** de la misma semilla:

```powershell
dotnet run --project GEvolve.Tests --no-build --no-restore -- --continental-elevation-captures
```

Este es un modelo estático de la cinemática actual. No integra millones de años de acreción, erosión o enfriamiento. Algunas semillas carecen de cordilleras altas si sus contactos fuertes ocurren principalmente en el océano. La presencia de sistemas oceánicos no implica que haya islas preexistentes cerca de todos ellos. No se activan volcanes, clima ni biomas.
