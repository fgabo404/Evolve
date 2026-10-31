# Continentes: relieve de sistemas tectónicos

`PlateGroupSurfaceGenerator` sigue fijando la superficie, corteza y continentes aprobados. La etapa de elevación clasifica las interacciones de las 16 placas con las reglas deterministas de `ContinentalReliefGenerator.ClassifyContacts`. `TectonicReliefSystemAnalyzer` agrupa contactos vecinos **del mismo par de placas y del mismo proceso**; una frontera puede cambiar de proceso a lo largo de su recorrido. Cada sistema recibe un ID estable, sus interacciones, el eje de hexágonos conectado, intensidad media, número de tramos y anchura derivada de esa longitud. Se usa la vecindad real de `HexTopology`, incluida la unión este-oeste.

`ContinentalElevationGenerator` calcula campos regionales a partir de esos ejes, sin propagar impulsos independientes desde cada hexágono fronterizo:

- **Colisión continental:** cinturón elevado a ambos lados del eje y meseta más amplia. Variación suave a lo largo del cinturón añade flancos, ramas menores y bajos intermontanos.
- **Subducción oceánica–continental:** fosa en la placa inferior oceánica y levantamiento con máximo desplazado hacia el interior de la placa superior terrestre. Se conserva la identidad de la placa superior para ubicar un arco volcánico en una etapa posterior; ahora no se crean volcanes.
- **Subducción oceánica–oceánica:** fosa en la placa inferior y campo de arco en la placa superior. Solo eleva tierra que ya existía; nunca crea nuevas islas ni mueve la costa.
- **Divergencia continental:** depresión en el eje y flancos elevados. **Divergencia oceánica:** dorsal de menor profundidad en un campo batimétrico separado.
- **Transformante:** deformación local acotada, sin un término de gran cordillera.

La intensidad usa la velocidad relativa de cada sistema. La anchura usa la longitud conectada, con límites globales iguales para todas las semillas. Una variación periódica pequeña modula únicamente el relieve secundario; no puede formar cordilleras lejos de los sistemas activos. El interior estable conserva llanuras, colinas y mesetas bajas. No hay cuantiles ni ajuste forzado de porcentajes de montaña.

`ElevationByCellId` mantiene tierra 1–100 y océano 0. La profundidad oceánica relativa sigue separada. Las cumbres desde 85 conservan la regla global de terreno infranqueable. `ContinentalElevationState` versión 2 guarda sistemas, contactos, tipos de relieve y campos regionales de levantamiento y subsidencia. Los guardados de la versión 1 y los guardados solo de superficie siguen siendo legibles.

## Comprobación

Las 12 huellas de `ApprovedContinentalSurfaceBaseline.txt` se registraron antes de la primera integración de elevaciones. Comparan superficie, placas, agrupación, corteza e identidad continental para cuatro tamaños y tres semillas. Las pruebas nuevas verifican la partición de todos los contactos activos en ejes conectados, determinismo, alturas, continuidad horizontal, persistencia y señales de colisiones, subducción, rifts y dorsales. Las 36 huellas de Pangea, Continentes pequeños y Archipiélagos siguen bajo sus pruebas históricas.

Para exportar comparaciones de **placas y movimiento**, **sistemas clasificados**, **levantamiento/subsidencia** y **altura final** de la misma semilla:

```powershell
dotnet run --project GEvolve.Tests --no-build --no-restore -- --continental-elevation-captures
```

Este es un modelo estático de la cinemática actual. No integra millones de años de acreción, erosión o enfriamiento. Algunas semillas carecen de cordilleras altas si sus contactos fuertes ocurren principalmente en el océano. La presencia de sistemas oceánicos no implica que haya islas preexistentes cerca de todos ellos. No se activan volcanes, clima ni biomas.
