# Clasificación inicial de biomas

La clasificación `biomes-2` se ejecuta en Engine, después de aceptar el terreno generado. No cambia los algoritmos de masas terrestres, sus semillas, intentos, placas ni elevaciones. Pangea mantiene `landmass-4` y su escala 0–100. La temperatura, humedad, pendientes, fauna, flora y minerales no se generan.

## Catálogo y latitud

`GEvolve.Definitions/Catalogs/World/biomes.json` es la única fuente de rangos. El archivo se incorpora como recurso del ensamblado de Definitions; `BiomeCatalog` lo lee y valida una vez. Así Game y Tests funcionan desde cualquier directorio, sin depender de la ruta del repositorio. Una modificación del JSON requiere recompilar. No se usan los rangos del generador histórico ni los siete biomas del antiguo catálogo guía.

La latitud lógica se calcula como `90 - 180 * fila / (filas - 1)`. La fila superior es +90°, la inferior −90° y el punto medio es 0°. Los tamaños actuales tienen un número par de filas: el ecuador pasa entre las dos filas centrales. Para una única fila se usa 0°. La fila es la coordenada lógica, independiente del pequeño desplazamiento visual de las columnas impares; repetir horizontalmente el mapa conserva la latitud y el ID.

Para cubrir el dominio completo 0–100 se amplió únicamente el máximo de elevación de `polar_north_ice` de 80 a 100. Antes, una tierra al norte de 75° y por encima de 80 no coincidía con ningún subbioma. No se modificó la estructura del JSON ni la elevación generada para resolver ese hueco.

## Selección provisional determinista

Primero se filtra por superficie, elevación, latitud absoluta y hemisferio. Los límites del catálogo son inclusivos. En tierra se excluye `marine`; en océano se admite únicamente `marine`, incluso en los polos. `polar_north` y `polar_south` solo se asignan sobre tierra y conservan su restricción de hemisferio. Los océanos polares mantienen el azul `#287DB5`.

Entre candidatos compatibles se aplica este orden:

1. Hielo polar del hemisferio correspondiente.
2. Montaña alpina o rocosa.
3. Tundra.
4. Océano abierto, preferido a arrecifes, manglares y estuarios porque no hay profundidad ni análisis costero.
5. Los demás subbiomas, ordenados por el área de su intervalo elevación × latitud, de menor a mayor; a igualdad, por ID con comparación ordinal.
6. Acantilados como último recurso terrestre compatible, porque su intervalo es muy amplio y todavía no se calcula pendiente.

Este orden no inventa temperatura ni humedad. Dos candidatos indistinguibles por las variables disponibles se resuelven por ese orden estable, sin aleatoriedad. Las etiquetas como humedal, bosque o desierto son aproximaciones provisionales, no confirmación de condiciones climáticas. Acantilados como fallback tampoco afirma que exista una pendiente real.

### Ajustes de biomes-2

Se conservó el orden de prioridades y se modificaron únicamente tres rangos del catálogo:

| Subbioma | Parámetro | Antes | Ahora |
| --- | --- | ---: | ---: |
| `marine_open_ocean` | Latitud máxima absoluta | 75 | 90 |
| `mountain_rocky` | Elevación mínima | 30 | 45 |
| `mountain_alpine` | Elevación mínima | 40 | 50 |

Los nuevos mínimos dejan más margen a los biomas ordinarios compatibles con la elevación y latitud. No se ampliaron sus rangos. Acantilados conserva su función de último recurso compatible: puede quedar montaña a menor elevación donde ningún bioma ordinario cubra el punto. No se suaviza ni se modifica el terreno para forzar una transición.

Comparación sobre el mismo terreno, contando el bioma `mountain` subyacente (también en cumbres):

| Tamaño | Semilla | Antes | Ahora |
| --- | ---: | ---: | ---: |
| Pequeño | 2026 | 716 | 435 |
| Pequeño | 42 | 909 | 681 |
| Pequeño | −731 | 939 | 630 |
| Mediano | 2026 | 1799 | 1087 |
| Mediano | 42 | 2001 | 1483 |
| Mediano | −731 | 2136 | 1383 |
| Grande | 2026 | 5736 | 3245 |
| Grande | 42 | 7417 | 4674 |
| Grande | −731 | 5134 | 2992 |

### Cumbres infranqueables

`TerrainRules.ImpassableSummitMinimumElevation` establece el umbral 85. `LandmassState.IsImpassableSummit(id)` expone la condición: superficie terrestre y elevación 85–100. `BiomeClassifier.GetCell` también devuelve el indicador. El bioma y la altura original se conservan. El océano y las celdas sin altura nunca son cumbres.

La condición se obtiene directamente del estado persistido, sin almacenar una máscara redundante que pueda desincronizarse. Sigue disponible al cargar una partida y no requiere regeneración ni un nuevo formato de guardado. Todavía no existe un sistema de movimiento que la aplique.

La cumbre tiene relleno `#FFFFFF` con prioridad sobre cualquier bioma, incluso polar, y sobre el gris de la vista de alturas. Los contornos, la selección y las etiquetas siguen visibles; la altura se dibuja en texto oscuro sobre las cumbres blancas. El panel de selección muestra «CUMBRE INFRANQUEABLE». Los otros colores de bioma se conservan. La vista de alturas sigue usando altura/100 para el terreno de 0–84; las cumbres de 85–100 son blancas por esta regla especial.

Con el catálogo actual todos los pares de elevación terrestre 0–100 y latitud −90..90 están cubiertos; el océano de Pangea usa la elevación original 0. Si un catálogo futuro deja un hueco, se conserva un estado explícito `NoCatalogMatch` con IDs nulos y se muestra «sin coincidencia en catálogo». Nunca se ajusta la altura ni se elige un ID incompatible. Si falta elevación en tierra, se usa `MissingElevation`: Continentes, Continentes pequeños y Archipiélagos mantienen sus datos originales, y la selección informa «elevación pendiente». Sus océanos también se clasifican como marinos usando la referencia de nivel marino 0 para consultar el catálogo, sin crear un arreglo de alturas ni cambiar el valor desconocido devuelto por la consulta del terreno. Todos los océanos generados se muestran azules.

## Estado y renderizado

`LandmassState.Biomes` contiene una tabla de pares de IDs y un byte de índice por celda. La versión del clasificador y el SHA-256 del catálogo identifican la clasificación aplicada. No se duplican elevaciones, coordenadas ni latitudes. `BiomeClassifier.GetCell` consulta por ID la altura original, latitud y asignación; las placas permanecen en su arreglo existente.

El guardado añade un campo opcional dentro del formato 2. Los guardados de formatos 1 y 2 anteriores siguen siendo legibles; los de formato 2 sin biomas conservan `Biomes = null`. No se exige regenerar un mapa al cargarlo. Se corrigió la validación previa de alturas, que todavía rechazaba valores superiores a 3, para admitir 0–100.

`BiomePalette` centraliza los ocho colores pedidos. El renderizador existente envía RGB junto al centro de cada instancia, conserva las dos llamadas de relleno/contorno y solo actualiza las celdas visibles cuando cambia la cámara. La geometría, zoom, arrastre, selección y etiquetas de elevación siguen usando el código existente. El panel de selección añade elevación, latitud y ambos IDs.

## Pruebas

`TerrainBaseline.txt` registra hashes capturados del commit `95af296` antes de integrar biomas: tres tamaños, cuatro generadores y semillas 2026, 42 y −731. Se comprueban superficie, elevaciones, placas e intento determinista de los 36 mundos. También se prueban cobertura del catálogo, límites inclusivos, hemisferios, repetición determinista, JSON alternativo, guardado/carga y compatibilidad con mapas sin alturas.

`--graphics-smoke` compara píxeles leídos del framebuffer con la paleta de las celdas visibles de Pangea, además del recorrido gráfico existente. Esta lectura diagnóstica añade sincronización y no debe interpretarse como un benchmark de FPS.

Las pruebas de biomes-2 verifican océanos marinos en todas las latitudes, polos terrestres, reducción de montaña frente a los conteos capturados en `29bc6aa`, umbral exacto 84/85/100, exclusión del océano y recuperación de la condición de cumbre después del guardado. OpenGL comprueba canales RGB exactamente 255 en las cumbres visibles, tanto en color como en la vista de alturas. Las 36 regresiones originales de superficie, elevación y placas siguen vigentes. Los guardados con biomas anteriores conservan su versión registrada; las nuevas reglas de distribución se aplican al generar o clasificar el mundo con biomes-2.
