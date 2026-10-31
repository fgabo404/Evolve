# Continentes: superficie derivada de grupos tectónicos

La ruta vigente ejecuta `TectonicPlateGenerator` y `TectonicMotionGenerator` sin cambios. `OrogenicOpportunityAnalyzer` mide la longitud y la intensidad de márgenes convergentes conectados, antes de seleccionar corteza o tierra. `PlateGroupSurfaceGenerator` elige determinísticamente entre dos y seis placas iniciales con área suficiente, separación regional y potencial orogénico. Añade placas **vecinas** a cada conjunto hasta obtener regiones tectónicas conectadas. La geometría compartida de los límites, el tamaño de los conjuntos y la convergencia real deciden las uniones; una placa no equivale a un continente.

Dentro de cada conjunto, la distancia al borde externo del grupo expresa estabilidad interior. Los campos originales de compresión y extensión ajustan levemente la afinidad de emergencia. El ruido periódico en X añade irregularidad costera de escala grande y pequeña. La corteza continental se asigna solo donde esa afinidad lo permite; un único nivel del mar, calculado para una cobertura global próxima al 35 %, determina tierra y océano. **No hay cuotas de tierra por grupo ni crecimiento libre desde núcleos.** La misma placa puede contener tierra y océano; un continente puede cruzar límites internos entre placas.

`WorldGenerationService` evalúa hasta ocho agrupaciones deterministas sobre **las mismas placas y movimientos**. Rechaza superficies sin una masa principal reconocible por grupo y, tras generar el relieve existente, las que carecen de una cordillera principal efectiva. En Grande y Enorme busca dos o más cuando es posible; acepta una si no aparece una alternativa mejor dentro del límite. Si ninguna agrupación produce una cordillera, informa un error explícito. El método, los costos y las 48 semillas de validación están en [CONTINENTAL_SELECTION.md](CONTINENTAL_SELECTION.md).

`LandmassState.ContinentalSurface` versión 2 guarda agrupación, corteza, identidad y diagnósticos de intentos; `SurfaceByCellId` guarda tierra y océano. La etapa de elevación se documenta en [CONTINENTAL_ELEVATION.md](CONTINENTAL_ELEVATION.md). Los guardados de superficie versión 1 y los anteriores sin `ContinentalSurface`, incluidos los que contienen geología histórica, siguen siendo legibles.

El juego muestra superficie, altura, placas, compresión, extensión y cizalla. Oculta infrarrojo, precipitación y geología cuando faltan sus datos.

## Diagnóstico visual

`dotnet run --project GEvolve.Tests --no-build --no-restore -- --continental-captures` exporta cuatro paneles comparables: placas originales, grupos seleccionados, corteza y superficie con el color de identidad continental. Las capturas SVG incluidas cubren varias semillas y tamaños. El eje horizontal del mapa se cierra; las masas que tocan los bordes izquierdo y derecho pueden formar una sola región.

Las pruebas verifican cuatro resoluciones y tres semillas, grupos conectados, masa principal reconocible por grupo, mezcla de tierra y océano dentro de placas, determinismo, persistencia y sensibilidad de la superficie a una geometría tectónica distinta con la misma semilla superficial. Las 36 huellas aprobadas de Pangea, Continentes pequeños y Archipiélagos siguen sin cambios.

La suite completa aún señala el fallo previo de `StagedLandmass` para `Small/Pangaea`: algunas celdas terrestres tienen elevación cero. Esta etapa no cambia Pangea ni modifica esa expectativa histórica.
