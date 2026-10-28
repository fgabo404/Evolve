# Biomas, pendientes y representación del terreno

La clasificación `biomes-3` se ejecuta en Engine después de aceptar el terreno. Pangea conserva `landmass-4`, semillas, intentos, superficie, placas y elevaciones 0–100. No se suaviza el relieve ni se introduce aleatoriedad. La clasificación todavía no utiliza temperatura ni humedad: esta distribución es provisional. La temperatura se calcula después, como etapa independiente documentada en [TEMPERATURE.md](TEMPERATURE.md), sin cambiar estas reglas.

## Catálogo y latitud

`GEvolve.Definitions/Catalogs/World/biomes.json`, incorporado como recurso de Definitions, es la fuente de rangos inclusivos. `BiomeCatalog` lo valida una vez; modificarlo requiere recompilar. La latitud lógica es `90 - 180 * fila / (filas - 1)`; para una sola fila es 0. Repetir horizontalmente el mapa no cambia latitud ni ID.

Primero se filtra por superficie, elevación, latitud absoluta y hemisferio. Los polos solo admiten tierra. El océano siempre admite únicamente `marine`, incluso en latitudes polares, con preferencia por `marine_open_ocean`. No se asigna un ID incompatible para rellenar huecos.

## Pendiente

`TerrainSlope.Calculate` utiliza la topología hexagonal existente:

`pendiente(i) = máximo de |elevación(i) - elevación(j)| entre los vecinos terrestres válidos j`.

La unidad es **niveles de elevación por paso entre centros hexagonales**, no grados ni metros. El resultado es un byte 0–100. Se respeta la conexión horizontal y se excluyen vecinos inexistentes en los bordes norte y sur. Los océanos no intervienen: su altura cero no crea pendientes terrestres artificiales en las costas. Una tierra sin vecinos terrestres tiene pendiente 0; los océanos almacenan 0. Sin elevaciones, el arreglo permanece `null`.

`LandmassState.SlopeByCellId` almacena un byte por celda (50.000 bytes en Grande), alineado por ID, sin modificar las alturas originales. `BiomeClassifier.GetCell` devuelve altura, latitud, bioma, subbioma, pendiente y condición de cumbre. La caché del clasificador incluye superficie, altura y pendiente por fila: igual altura no implica igual paisaje. Game no recalcula pendientes.

## Prioridades provisionales

Los parámetros se centralizan en `BiomeTerrainRules`; las cumbres conservan `TerrainRules.ImpassableSummitMinimumElevation = 85`. Entre candidatos compatibles:

1. Polo del hemisferio correspondiente.
2. Montaña rocosa desde altura 65 con pendiente ≥10, o desde 85 independientemente de la pendiente; montaña alpina desde 75 con pendiente ≥4.
3. Tundra en su intervalo de latitud y elevación.
4. Llanuras con pendiente <4 y bosques con pendiente ≥4. Humedales y bosques pantanosos no reciben esta preferencia porque la pendiente no demuestra humedad.
5. Los demás candidatos ordinarios compatibles.
6. Montaña alpina, rocosa y acantilados, en ese orden, como último recurso si no hay candidatos ordinarios compatibles.

Dentro de cada prioridad se ordena por amplitud del intervalo de latitud y después por ID ordinal. Se sustituye el desempate anterior por área elevación × latitud: ampliar el rango de vegetación ya no la penaliza en tierras bajas. Sin pendiente no se activan preferencias dependientes de ella.

Una meseta a altura 70 u 80 puede ser pradera, una ladera moderada a 70 puede ser bosque y una pendiente ≥10 a 70 favorece montaña rocosa. Las montañas de fallback pueden existir fuera de esos umbrales prácticos si ningún bioma ordinario cubre el punto; no se fuerzan bosques o llanuras incompatibles. `mountain_alpine` conserva ID y color, sin crear un nuevo sistema de vegetación alpina.

Los cambios siguen el relieve local, sin cuotas porcentuales ni suavizado de alturas. Los límites de latitud y umbrales discretos todavía pueden producir transiciones visibles; no se simula clima para ocultarlas.

### Rangos modificados respecto a biomes-2

Solo se amplían ocho máximos de elevación para permitir vegetación elevada antes de la cumbre. Mínimos, latitudes, hemisferios y demás rangos no cambian:

| Subbioma | Máximo anterior | Máximo nuevo |
| --- | ---: | ---: |
| `forest_tropical` | 30 | 74 |
| `forest_subtropical` | 50 | 74 |
| `forest_temperate` | 60 | 74 |
| `forest_boreal` | 35 | 74 |
| `plains_savanna` | 40 | 84 |
| `plains_prairie` | 45 | 84 |
| `plains_steppe` | 50 | 84 |
| `tundra_tundra` | 60 | 84 |

Se conservan ajustes anteriores: océano abierto hasta latitud 90, hielo norte hasta elevación 100, mínimo rocoso 45 y mínimo alpino 50. Estos últimos son rangos de compatibilidad; las nuevas prioridades prácticas dependen también de la pendiente.

## Cumbres y colores

`LandmassState.IsImpassableSummit(id)` mantiene **tierra con elevación 85–100**. Elevaciones 0–84 son terreno normal. Océanos y celdas sin altura nunca son cumbres. El indicador se deriva del estado persistido, sin máscara duplicada y sin implementar movimiento.

La cumbre conserva bioma y altura originales, pero su relleno blanco tiene prioridad sobre cualquier bioma, incluso polar, y sobre la vista de alturas. Esta última usa altura/100 para 0–84 y blanco para cumbres. Contornos, selección y etiquetas siguen visibles.

`BiomePalette` es la única paleta de ejecución; los subbiomas usan el color de su bioma principal:

| Bioma | Color |
| --- | --- |
| Desierto | `#D8BC82` |
| Bosque | `#43845A` |
| Llanura | `#A7C47C` |
| Marino | `#347FA8` |
| Tundra | `#91A99A` |
| Montaña | `#918B80` |
| Polo Norte | `#DCEEF1` |
| Polo Sur | `#EDF4F5` |
| Cumbre infranqueable | `#FFFFFF` |

Game solo visualiza: mantiene cuadrícula, cámara, zoom, desplazamiento, selección y elevaciones. El panel incluye pendiente en `NIV/HEX` y conserva «CUMBRE INFRANQUEABLE». No se añaden texturas ni degradados.

## Estado y compatibilidad

`LandmassState.Biomes` conserva una tabla de IDs y un byte de índice por celda, con versión del clasificador y SHA-256 del catálogo. No duplica coordenadas, elevación ni latitud. La pendiente es opcional dentro del formato de guardado 2; se valida longitud, rango, presencia de alturas y cero en océanos. Los guardados anteriores sin pendientes o biomas siguen siendo legibles sin regeneración y mantienen su clasificación anterior. Las reglas nuevas se aplican al generar o reclasificar explícitamente.

Continentes, Continentes pequeños y Archipiélagos no reciben alturas inventadas: sus pendientes permanecen `null` y su tierra conserva `MissingElevation`. Sus océanos usan nivel 0 solo para consultar el catálogo, sin crear alturas. Un hueco real devuelve `NoCatalogMatch`, nunca un ID incompatible. El catálogo actual cubre el dominio terrestre 0–100 y −90..90.

## Comparación reproducible

`BiomeDistributionBaseline.json` captura biomes-2 en `7ebfe5b`, antes del cambio. Las pruebas comparan los mismos nueve mundos de Pangea. La tabla cuenta montaña subyacente, incluidas cumbres cuyo bioma sea montaña; los porcentajes usan la tierra de cada mundo:

| Tamaño | Semilla | Montaña antes | Montaña ahora | Antes % | Ahora % |
| --- | ---: | ---: | ---: | ---: | ---: |
| Pequeño | 2026 | 435 | 172 | 15,53 | 6,14 |
| Pequeño | 42 | 681 | 162 | 24,25 | 5,77 |
| Pequeño | −731 | 630 | 209 | 22,48 | 7,46 |
| Mediano | 2026 | 1087 | 343 | 18,11 | 5,72 |
| Mediano | 42 | 1483 | 403 | 24,60 | 6,69 |
| Mediano | −731 | 1383 | 415 | 23,03 | 6,91 |
| Grande | 2026 | 3245 | 1010 | 16,23 | 5,05 |
| Grande | 42 | 4674 | 1105 | 23,37 | 5,53 |
| Grande | −731 | 2992 | 1150 | 14,96 | 5,75 |

Agregado ponderado por celda sobre 86.448 tierras (216.000 celdas totales):

| Bioma | Antes % terrestre | Ahora % terrestre |
| --- | ---: | ---: |
| Desierto | 1,24 | 0,05 |
| Bosque | 25,84 | 28,95 |
| Llanura | 17,20 | 26,23 |
| Montaña | 19,21 | 5,75 |
| Tundra | 7,39 | 9,91 |
| Polo Norte | 23,79 | 23,79 |
| Polo Sur | 5,32 | 5,32 |

Océano: 129.552 celdas, 59,98 % del mapa total, sin cambios. Son resultados medidos, no objetivos porcentuales. La escasez de desierto es una limitación de priorizar vegetación compatible sin temperatura ni humedad; no demuestra un clima húmedo real. No se introducen cuotas para compensarlo.

## Validación

`SlopeTests` verifica mesetas, laderas, determinismo, vecinos horizontales, bordes polares, exclusión del océano costero, terreno sin alturas y los nueve colores exactos. `BiomeTests` verifica IDs contra el catálogo, polos terrestres, océanos marinos, límite 84/85/100, blanco puro, guardado/carga y compatibilidad anterior.

`TerrainBaseline.txt` mantiene hashes capturados de `95af296`: tres tamaños × cuatro generadores × tres semillas. Las 36 regresiones comprueban superficie, elevaciones, placas e intento determinista sin alterar los generadores.

`--graphics-smoke` comprueba píxeles del framebuffer en vistas normal y de alturas, blanco RGB 255 exacto en cumbres y el recorrido gráfico existente. Es diagnóstico, no un benchmark de FPS. Comandos desde Evolve:

```powershell
dotnet build GEvolve.sln --no-restore
dotnet run --project GEvolve.Tests -c Debug --no-restore
dotnet run --project GEvolve.Game -c Debug --no-restore -- --graphics-smoke
git diff --check
```
