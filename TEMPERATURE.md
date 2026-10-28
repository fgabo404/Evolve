# Temperatura media anual y vista infrarroja

## Etapa independiente

`TemperatureGenerator` (Engine) se ejecuta después de generar el terreno y clasificar los biomas. No modifica superficie, placas, elevaciones, pendientes, semillas ni asignaciones de biomas. `biomes.json` y las prioridades de `biomes-3` permanecen intactos. El modelo climático es `temperature-1`.

Todos los hexágonos de Pangea, terrestres y oceánicos, reciben temperatura. Los demás generadores todavía no tienen elevaciones: conservan `Climate = null`, sin inventar alturas ni cambiar sus algoritmos. En ellos, o en guardados antiguos, el botón indica «CLIMA PENDIENTE» y no activa infrarrojo. No se regenera automáticamente el clima al cargar una partida.

## Fórmula y parámetros

`TemperatureSettings`, en Definitions, centraliza la calibración y valida sus parámetros. Con latitud φ en radianes, elevación h entre 0 y 100, distancia al océano d y ruido n entre −1 y 1:

```text
p = sin(φ)²
Tcontinental = 27 + (−23 − 27) × p
Tmarítima = 24 + (−18 − 24) × p
w = 1                                      en océano
w = 0,65 × exp(−d / 6)                     en tierra
w = 0                                      si no hay océano alcanzable
Tbase = Tcontinental + w × (Tmarítima − Tcontinental)
        − h × 40 / 1000 × 6,5 + 1,5 × n
```

La curva de latitud es suave y simétrica, sin bandas discretas. Los extremos continentales a nivel del mar son 27 °C en el ecuador y −23 °C en los polos, antes del ruido y la moderación. La referencia marítima es 24 °C y −18 °C: el océano enfría las costas cálidas y templa las frías, en lugar de calentarlas siempre.

Cada nivel equivale a 40 m: el gradiente es −0,26 °C por nivel, o −26 °C para 4.000 m. La elevación original no cambia. La temperatura representa una media anual aproximada, no temperatura instantánea ni una simulación meteorológica validada.

### Distancia al océano

Una propagación BFS multiorigen comienza simultáneamente en todos los océanos y recorre los vecinos de `HexTopology`. Su coste es O(celdas + conexiones). Respeta el envolvimiento horizontal y los bordes norte y sur. El océano tiene distancia 0; una tierra adyacente tiene 1. En un mapa totalmente terrestre, el valor −1 representa ausencia de océano alcanzable.

La distancia se mide en pasos hexagonales, **no kilómetros**. La influencia decae exponencialmente, con escala configurable de seis pasos y sin cortes por distancia. A latitud y altura constantes, los saltos entre distancias terrestres consecutivas son inferiores a 0,5 °C con la calibración actual. La transición mar/tierra también incorpora sus referencias distintas. No se alteran las costas ni se calcula una búsqueda independiente para cada celda. El arreglo temporal de distancias se descarta después de generar el clima.

### Variación regional

Se reutiliza `PeriodicNoise.Sample`, sin modificar su implementación: ruido de valores suavizado, tres octavas y frecuencia inicial 2, con la semilla del mundo. La amplitud máxima es ±1,5 °C. La retícula es periódica en X y abierta en Y; no hay números aleatorios independientes por hexágono. Muestrear columna 0 o columna N produce exactamente el mismo ruido. Las celdas de los extremos del mapa son vecinas, no duplicados, por lo que sus temperaturas pueden diferir suavemente y también reflejar distintas alturas o superficie.

## Base y temperatura actual

`LandmassState.Climate` contiene un `ClimateState` opcional:

- `ModelVersion`: versión del modelo aplicado.
- `BaseTemperatureTenthsByCellId`: `short[]` con signo, alineado por ID, en décimas de °C. Se redondea al décimo más cercano, con empates alejándose de cero. En Grande ocupa 100.000 bytes de datos numéricos en memoria, excluyendo contenedores y serialización JSON.
- `GlobalOffsetTenths`: modificador global, inicialmente 0.
- `RegionalOffsetsTenthsByCellId`: diccionario disperso opcional de modificadores por ID, inicialmente `null`.

`BaseCelsius(id)` consulta la base. `CurrentCelsius(id)` suma base, modificador global y modificador regional y convierte a °C. No se almacena una segunda copia completa de temperaturas. No hay eventos, emisiones ni actualizaciones climáticas automáticas: los modificadores solo preparan el contrato para sistemas futuros.

La base se calcula una vez al generar el mundo y se guarda con él; cambiar de vista no la modifica. La consulta actual es O(1) esperado y no recalcula geografía. Un futuro sistema climático podrá reemplazar los modificadores sin cambiar la base y deberá invalidar los colores del renderizador cuando publique un cambio. No se ha implementado ese sistema dinámico.

La persistencia mantiene el formato 2 con el campo opcional `Climate`. Se validan versión no vacía, longitud del arreglo y IDs de los modificadores regionales. Los guardados antiguos sin ese campo siguen siendo legibles. Los valores negativos y los modificadores sobreviven al guardado/carga.

## Vistas y representación

`MapViewMode` unifica **Biomes**, **Elevation** e **Infrared**. El botón «INFRARROJO» activa el modo térmico; pasa a «VER BIOMAS» para volver. El control de alturas sigue disponible y permite pasar directamente entre modos. La cámara, selección, geometría y estado del mundo no cambian.

`ThermalPalette` centraliza una escala estilizada de temperatura actual, separada de `BiomePalette`. Interpola linealmente RGB entre anclas; fuera del rango conserva el color extremo sin limitar la temperatura almacenada:

| °C | Color |
| ---: | --- |
| −40 o menos | `#281A59` |
| −20 | `#3457A4` |
| 0 | `#54C9E8` |
| 10 | `#63BA82` |
| 20 | `#E8D66D` |
| 30 | `#E87836` |
| 40 o más | `#C72D32` |

Esto no simula radiación infrarroja física. Se aplica tanto a océanos como a tierra. `IsImpassableSummit(id)` permanece intacto: tierra desde elevación 85. En biomas y alturas las cumbres siguen blancas; en infrarrojo muestran su temperatura sin perder la condición de infranqueables.

El panel de selección conserva altura, latitud, pendiente, bioma, subbioma y cumbre, y añade `TEMPERATURA: 16.4 °C`, usando la temperatura actual. La fuente local incluye el símbolo de grado. El renderizador actualiza colores de instancias visibles al cambiar cámara o modo, sin consultar vecinos ni ejecutar el generador climático por fotograma.

## Resultados reproducibles

Mediciones en mundos recién generados, incluyendo tierra y océano; todas en °C. El promedio es aritmético por hexágono, **no ponderado por área esférica**: esta cuadrícula da el mismo peso a filas polares y ecuatoriales. Por ello no debe interpretarse como una temperatura media planetaria comparable directamente con la terrestre.

| Tamaño | Semilla | Mínimo | Máximo | Promedio |
| --- | ---: | ---: | ---: | ---: |
| Pequeño | 2026 | −43,2 | 26,4 | −1,20 |
| Pequeño | 42 | −45,8 | 24,8 | −1,08 |
| Pequeño | −731 | −43,0 | 24,5 | −0,65 |
| Mediano | 2026 | −45,7 | 25,7 | −0,95 |
| Mediano | 42 | −46,0 | 24,9 | −1,28 |
| Mediano | −731 | −40,2 | 24,8 | −0,74 |
| Grande | 2026 | −44,3 | 25,3 | −0,92 |
| Grande | 42 | −45,7 | 24,9 | −0,92 |
| Grande | −731 | −44,7 | 25,4 | −0,80 |

Las temperaturas inferiores a −40 siguen almacenadas aunque compartan el extremo violeta. El ecuador es generalmente más cálido, el relieve elevado enfría y la latitud sigue siendo el factor global dominante. Se ven gradientes latitudinales amplios, especialmente sobre océanos; no se aumenta artificialmente el ruido para ocultarlos.

## Validación

`TemperatureTests` cubre determinismo, cambio de semilla, latitud, descenso por altura, moderación cálida/fría, propagación oceánica, mapa sin océanos, continuidad horizontal, ausencia de alturas, cobertura de nueve mundos, persistencia y guardados anteriores, consulta con modificadores, anclas e interpolación de paleta, cumbres y consultas de color sin mutaciones.

Las 36 regresiones de `TerrainBaseline.txt` conservan sus resultados esperados. El clasificador y sus pruebas existentes no cambian; superficie, elevaciones, placas, pendientes y biomas se conservan.

`--graphics-smoke` prueba los botones y sus áreas de clic, las transiciones entre los tres modos, preservación de selección/cámara/clima y colores reales del framebuffer. Incluye Pangea Grande/2026, Pequeño/42 y Mediano/−731, además de otros generadores. Se revisaron capturas térmicas de esos tres mundos con celdas seleccionadas: océanos y tierra coloreados, zonas altas frías y panel de temperatura visible.

```powershell
dotnet build GEvolve.sln --no-restore
dotnet run --project GEvolve.Tests -c Debug --no-restore
dotnet run --project GEvolve.Game -c Debug --no-restore -- --graphics-smoke
git diff --check
```

No se implementan humedad, precipitaciones, viento, estaciones, cambio climático real, cambio de biomas ni distribución de recursos. La clasificación ecológica todavía no consume temperatura; la siguiente etapa podrá consultarla desde State.
