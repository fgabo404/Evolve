# Crecimiento continental guiado por tectónica

Esta revisión reemplaza únicamente el ensamblaje cortical y la selección de superficie de **Continentes**. Conserva las 16 placas, sus velocidades, el cálculo de interacciones, el clasificador geológico, la subducción, los volcanes y los otros tres generadores de mundo. Clima, erosión y biomas siguen fuera de esta ruta.

## Procedimiento

1. `WorldGenerationService` obtiene las placas y `TectonicMotionState` originales.
2. `ContinentalCrustGenerator` elige de dos a seis cratones en interiores de placas, con preferencia por regiones estables y latitudes alejadas de los extremos del mapa. Los núcleos se separan mediante la distancia de la cuadrícula hexagonal, incluida su unión este–oeste. Una placa puede alojar corteza de ambos tipos; una masa emergida puede cruzar límites de placas.
3. Una cola de prioridad expande simultáneamente las regiones desde los cratones. Cada paso tiene costo positivo, modulado por campos espaciales periódicos y por la compresión/extensión calculada. Cruzar un contacto convergente cuesta menos; cruzar uno divergente cuesta más; la cizalla aporta una resistencia menor. Las mismas interacciones reales alimentan los accidentes y volcanes posteriores. No hay elipses, brazos de longitud fija ni velocidades privadas.
4. La menor distancia acumulada determina una afinidad de emersión continua. Variación regional en tres escalas deforma las costas: modifica penínsulas y golfos, mientras la distancia a los cratones mantiene la estructura principal. El 55 % superior de afinidad recibe corteza continental, incluyendo plataformas que pueden quedar sumergidas.
5. `ContinentsGenerator` prueba un **nivel del mar global**, nunca una cuota individual por continente. Verifica entre 2 y 6 masas principales según el perfil de la semilla, cada una conectada a un cratón distinto, el perfil geográfico existente y una cobertura cercana al 35 %. Si una disposición une núcleos o produce una masa grande sin núcleo, prueba hasta 32 disposiciones deterministas de los cratones y elige la válida más cercana al objetivo. No abre corredores oceánicos entre provincias ni elimina masas por el ID de provincia. Las islas secundarias pueden sobrevivir.
6. `ContinentalReliefGenerator` consume directamente corteza y superficie nuevas. Conserva la clasificación, las fuentes de relieve, las fosas, los arcos y la restricción de volcanes a la placa superior en subducción activa. La elevación terrestre y la profundidad oceánica siguen separadas.

La afinidad de emersión es un índice de juego, no una altitud en metros. Las edades de litosfera oceánica usadas para desempatar subducción siguen siendo las aproximaciones estáticas descritas en `CONTINENTAL_GEOLOGY.md`. El perfil del 35 % y la cantidad de continentes son objetivos de generación, no consecuencias predichas por una simulación temporal.

## Capturas comparables

Las cinco imágenes usan tamaño **Mediano** (250 × 167 hexágonos), la misma escala y colores: tierra beige, océano azul. Se generan directamente de `SurfaceByCellId`, sin biomas ni retoques. Los bordes izquierdo y derecho se unen en el mundo; una masa cortada por ambos lados puede ser el mismo continente.

| Semilla | Continentes | Superficie |
| --- | ---: | --- |
| 2026 | 2 | [Ver mapa](continental-captures/continents-medium-2026.png) |
| 42 | 3 | [Ver mapa](continental-captures/continents-medium-42.png) |
| -731 | 4 | [Ver mapa](continental-captures/continents-medium--731.png) |
| 9 | 5 | [Ver mapa](continental-captures/continents-medium-9.png) |
| 2 | 6 | [Ver mapa](continental-captures/continents-medium-2.png) |

Para regenerarlas: `dotnet run --project GEvolve.Tests -- --continental-captures` desde la raíz de Evolve. La salida se escribe en `continental-captures/`.

## Verificación

`ContinentalGeologyTests` comprueba cuatro tamaños × cinco semillas, el número de masas con un cratón conectado por componente, continuidad horizontal, dos clases de corteza, continentes que cruzan placas, profundidad/elevación, persistencia, determinismo y volcanes ligados a subducción. Una comparación controlada mantiene las placas y los cratones fijos mientras cambia los contactos de convergentes a divergentes: debe cambiar la afinidad y la distribución cortical. Se conservan las 36 huellas aprobadas de Pangea, Continentes pequeños y Archipiélagos, sin editar los archivos históricos de pruebas.

La selección del nivel del mar y las 32 disposiciones acotan la búsqueda; semillas fuera de la muestra todavía pueden revelar perfiles difíciles. Las capturas muestran una mejora clara frente a las figuras hexagonales anteriores, aunque no representan una reconstrucción geológica de la Tierra ni garantizan la silueta de un continente real.

### Resultados locales

- `dotnet build GEvolve.sln --no-restore`: cero errores y cero advertencias.
- Las 20 combinaciones de cuatro tamaños y cinco semillas cumplen 2–6 masas principales, con 35,06–35,27 % de tierra.
- La comparación controlada de movimiento cambia corteza, afinidad y tierra emergida manteniendo fijos los IDs de placa y los cratones.
- Las 36 huellas aprobadas de los otros tres tipos son idénticas, incluidas las 12 de Pangea.
- `dotnet run --project GEvolve.Game --no-build -- --graphics-smoke` verifica la representación OpenGL, incluidos Continentes y la vista de geología.
- La suite completa aprueba 12 grupos; `StagedLandmass` conserva su fallo histórico Pequeño/Pangea porque hay cuatro celdas terrestres aprobadas con elevación 0 y su expectativa exige más de 0. No se modificaron Pangea ni esa expectativa.
