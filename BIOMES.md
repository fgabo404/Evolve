# Clasificación inicial de biomas

La clasificación `biomes-1` se ejecuta en Engine, después de aceptar el terreno generado. No cambia los algoritmos de masas terrestres, sus semillas, intentos, placas ni elevaciones. Pangea mantiene `landmass-4` y su escala 0–100. La temperatura, humedad, pendientes, fauna, flora y minerales no se generan.

## Catálogo y latitud

`GEvolve.Definitions/Catalogs/World/biomes.json` es la única fuente de rangos. El archivo se incorpora como recurso del ensamblado de Definitions; `BiomeCatalog` lo lee y valida una vez. Así Game y Tests funcionan desde cualquier directorio, sin depender de la ruta del repositorio. Una modificación del JSON requiere recompilar. No se usan los rangos del generador histórico ni los siete biomas del antiguo catálogo guía.

La latitud lógica se calcula como `90 - 180 * fila / (filas - 1)`. La fila superior es +90°, la inferior −90° y el punto medio es 0°. Los tamaños actuales tienen un número par de filas: el ecuador pasa entre las dos filas centrales. Para una única fila se usa 0°. La fila es la coordenada lógica, independiente del pequeño desplazamiento visual de las columnas impares; repetir horizontalmente el mapa conserva la latitud y el ID.

Para cubrir el dominio completo 0–100 se amplió únicamente el máximo de elevación de `polar_north_ice` de 80 a 100. Antes, una tierra al norte de 75° y por encima de 80 no coincidía con ningún subbioma. No se modificó la estructura del JSON ni la elevación generada para resolver ese hueco.

## Selección provisional determinista

Primero se filtra por superficie, elevación, latitud absoluta y hemisferio. Los límites del catálogo son inclusivos. En tierra se excluye `marine`; en océano se admiten únicamente `marine` o los biomas polares compatibles.

Entre candidatos compatibles se aplica este orden:

1. Hielo polar del hemisferio correspondiente.
2. Montaña alpina o rocosa.
3. Tundra.
4. Océano abierto, preferido a arrecifes, manglares y estuarios porque no hay profundidad ni análisis costero.
5. Los demás subbiomas, ordenados por el área de su intervalo elevación × latitud, de menor a mayor; a igualdad, por ID con comparación ordinal.
6. Acantilados como último recurso terrestre compatible, porque su intervalo es muy amplio y todavía no se calcula pendiente.

Este orden no inventa temperatura ni humedad. Dos candidatos indistinguibles por las variables disponibles se resuelven por ese orden estable, sin aleatoriedad. Las etiquetas como humedal, bosque o desierto son aproximaciones provisionales, no confirmación de condiciones climáticas. Acantilados como fallback tampoco afirma que exista una pendiente real.

Con el catálogo actual todos los pares de elevación terrestre 0–100 y latitud −90..90 están cubiertos; el océano de Pangea usa la elevación original 0. Si un catálogo futuro deja un hueco, se conserva un estado explícito `NoCatalogMatch` con IDs nulos y se muestra «sin coincidencia en catálogo». Nunca se ajusta la altura ni se elige un ID incompatible. Si falta elevación, se usa `MissingElevation`: Continentes, Continentes pequeños y Archipiélagos mantienen sus datos originales y colores de superficie, y la selección informa «elevación pendiente».

## Estado y renderizado

`LandmassState.Biomes` contiene una tabla de pares de IDs y un byte de índice por celda. La versión del clasificador y el SHA-256 del catálogo identifican la clasificación aplicada. No se duplican elevaciones, coordenadas ni latitudes. `BiomeClassifier.GetCell` consulta por ID la altura original, latitud y asignación; las placas permanecen en su arreglo existente.

El guardado añade un campo opcional dentro del formato 2. Los guardados de formatos 1 y 2 anteriores siguen siendo legibles; los de formato 2 sin biomas conservan `Biomes = null`. No se exige regenerar un mapa al cargarlo. Se corrigió la validación previa de alturas, que todavía rechazaba valores superiores a 3, para admitir 0–100.

`BiomePalette` centraliza los ocho colores pedidos. El renderizador existente envía RGB junto al centro de cada instancia, conserva las dos llamadas de relleno/contorno y solo actualiza las celdas visibles cuando cambia la cámara. La geometría, zoom, arrastre, selección y etiquetas de elevación siguen usando el código existente. El panel de selección añade elevación, latitud y ambos IDs.

## Pruebas

`TerrainBaseline.txt` registra hashes capturados del commit `95af296` antes de integrar biomas: tres tamaños, cuatro generadores y semillas 2026, 42 y −731. Se comprueban superficie, elevaciones, placas e intento determinista de los 36 mundos. También se prueban cobertura del catálogo, límites inclusivos, hemisferios, repetición determinista, JSON alternativo, guardado/carga y compatibilidad con mapas sin alturas.

`--graphics-smoke` compara píxeles leídos del framebuffer con la paleta de las celdas visibles de Pangea, además del recorrido gráfico existente. Esta lectura diagnóstica añade sincronización y no debe interpretarse como un benchmark de FPS.
