# Evolve — Lineamientos de generación del mundo

**Estado:** Capas 01 y 02 implementadas en v0.001; capas 03–04 pendientes. **Actualizado:** 2026-10-27.
**Alcance:** capas 01–04. Los valores explícitamente pendientes no son reglas aprobadas ni deben completarse por inferencia.
**Fuentes:** acuerdos de diseño del proyecto y `fgabo404/EvolveDoc`: `data/world/world.catalog.guide.json`, `lang/es/world.catalog.guide.md`, `lang/es/tecnologias-arquitectura.md`.

## Arquitectura y principio general

- `GEvolve.Definitions`: configuración, selección de algoritmos, parámetros y reglas declarativas.
- `GEvolve.Engine`: ejecuta las etapas, calcula geometría/terreno/elevaciones y asigna los atributos aprobados.
- `GEvolve.State`: conserva configuración efectiva, semilla, resultados generados y cambios posteriores; la estrategia definitiva de snapshots está pendiente.
- `GEvolve.Game`: única aplicación gráfica; tendrá dos modos de visualización (2D y 3D) sobre el mismo mundo de Engine/State. El antiguo Simulator web y el prototipo 3D fueron retirados.

Las capas son **lógicas y secuenciales**, no renderizadores independientes. Las capas 01 y 02 están implementadas; las posteriores permanecen sin implementar.

## Capa 01 — Grid de hexágonos (acordada)

| Perfil | Columnas × filas | Hexágonos |
| --- | ---: | ---: |
| Pequeño | 100 × 70 | 7 000 |
| Mediano | 150 × 100 | 15 000 |
| Grande | 250 × 200 | 50 000 |

- Hexágonos regulares, orientación **flat-top** (lado horizontal arriba).
- Extensión lógica rectangular; **no** forzar una proporción exacta 3:2: se conservan los tres tamaños aprobados.
- Continuidad **este–oeste**: los bordes laterales se conectan para emular la circunferencia planetaria, sin duplicar celdas.
- Límites **norte–sur**: no conectados.
- La continuidad se aplica tanto a vecindad/distancia como a la navegación visual; las coordenadas deben ser consistentes.
- El grid no define agua, masas terrestres, elevación, clima ni biomas.
- La Capa 01 se genera solo al solicitarla con **Generar mundo** desde `GEvolve.Game`; al iniciar no existe un grid activo. La vista estratégica 2D usa OpenGL con un hexágono base instanciado en los centros visibles, sin enviar una operación independiente por celda a la GPU. La cámara y la selección se apoyan en la geometría de Engine.
- Convenio de la implementación: columnas impares desplazadas hacia abajo (`odd-q`); ID estable `fila × columnas + columna`; axial `(q, r) = (columna, fila − ⌊columna/2⌋)`. Para los anchos pares oficiales, el cierre horizontal traslada `(q, r)` por `(±ancho, ∓ancho/2)`. Así se conserva la fila, la distancia entre centros y la vecindad geométrica sin duplicar celdas.

## Capa 02 — Forma de masas terrestres (v0.001)

Cuatro opciones del perfil de forma terrestre:

1. **Continentes** (`continents`): varias masas terrestres principales separadas por océanos.
2. **Supercontinente / Pangea** (`pangaea`): una masa terrestre predominante, con posibilidad de tierras secundarias.
3. **Continentes pequeños** (`small_continents`): mayor fragmentación en varias masas continentales menores.
4. **Archipiélagos** (`archipelagos`): numerosas islas y conjuntos insulares.

**Resultado lógico:** un byte por ID de hexágono (`0` océano, `1` tierra), sin cambiar la topología ni generar elevaciones o biomas. Los identificadores anteriores describen los tipos; el enum ejecutable está en `GEvolve.Definitions`. El mapa, la semilla, el perfil efectivo, la versión del generador y el intento elegido quedan en `GEvolve.State` y se recuperan sin regenerar.

Cada tipo utiliza una estrategia espacial propia en `GEvolve.Engine.World.LandmassGeneration`:

| Tipo | Estrategia | Tierra objetivo experimental |
| --- | --- | ---: |
| Continentes | Núcleos separados, crecimiento de varios frentes y costas irregulares | 35 % |
| Pangea | Región principal única que crece por frentes conectados | 40 % |
| Continentes pequeños | Muchos núcleos de alcance limitado con corredores oceánicos | 30 % |
| Archipiélagos | Cadenas curvas y agrupaciones de islas de tamaño variable | 20 % |

Los valores de v0.001 son provisionales: rangos de referencia 30–40 %, 35–45 %, 25–35 % y 15–25 %, respectivamente. `LandmassProfiles` centraliza los parámetros efectivos por tamaño y algoritmo (núcleos o cadenas, separación, radios, ruido e irregularidad). No proceden de los `WorldPresets` del prototipo anterior. `LandmassGenerationService` comprueba cobertura y morfología con `LandmassAnalysis` y limita los reintentos deterministas a cuatro. El análisis usa componentes hexagonales reales, tamaños, masa dominante, distribución e islas; un continente que cruza el borde este–oeste sigue siendo uno solo.

El ruido coherente multiescala se interpola con periodicidad horizontal exacta. La separación de núcleos y la expansión emplean vecinos y distancias con cierre este–oeste; norte y sur no se conectan. La misma combinación de tamaño, tipo, semilla entera, parámetros efectivos y versión `landmass-1` produce el mismo resultado. Un cambio futuro que altere ese resultado debe cambiar la versión del generador.

**Aún por decidir:** relación de esta forma con polos, anchuras de estrechos como regla de juego, erosión y parámetros definitivos. Estas ausencias no invalidan los perfiles experimentales actuales.

## Capa 03 — Elevaciones (cuatro niveles documentados)

Fuente canónica de diseño: `EvolveDoc/data/world/world.catalog.guide.json`, campo `elevationLevels`, y `lang/es/world.catalog.guide.md`.

| Nivel | Nombre ES | Tránsito terrestre ordinario | Otras reglas |
| --- | --- | --- | --- |
| **0** | Nivel del mar | Depende de tierra/agua | No confundir nivel 0 con océano obligatorio. |
| **1** | Nivel medio | Permitido | Sin restricción adicional documentada. |
| **2** | Nivel alto | Permitido | Restricciones de movimiento/construcción por concretar (`null` en fuente). |
| **3** | Cumbres infranqueables | No permitido | Asentamientos convencionales no permitidos. |

- Elevación y bioma son atributos independientes.
- **Visualización inicial 2D:** colocar el número de nivel (`0`, `1`, `2` o `3`) sobre cada hexágono. No dibujar relieve tridimensional en la vista estratégica 2D como requisito de esta capa.
- La capa puede requerir un cálculo interno de elevación más fino antes de clasificar los niveles, **pero no está decidido** si existirá una altura continua, sus unidades o umbrales. No equiparar estos cuatro niveles con la escala 1–100 del prototipo previo.
- Pendiente: modelo de generación de alturas, continuidad entre vecinos, cordilleras, relación con costas, umbrales de clasificación y codificación de profundidad marina.

## Capa 04 — Asignación de biomas (siete categorías documentadas)

Fuente canónica de diseño: `EvolveDoc/data/world/world.catalog.guide.json`, campo `biomes`.

| ID | Nombre ES |
| --- | --- |
| `tropical_forest` | Bosque tropical |
| `temperate_forest` | Bosque templado |
| `tundra` | Tundra |
| `savanna` | Sabana |
| `desert` | Desierto |
| `marine` | Marino |
| `alpine` | Alta montaña y pisos alpinos |

- Los biomas son **categorías generales de interfaz**, no siete fórmulas ecológicas completas.
- La asignación debe considerar, cuando se definan sus modelos, superficie tierra/agua, elevación, condiciones climáticas geográficas estáticas (temperatura y humedad) y otros condicionantes del terreno pertinentes.
- **No inferir** el bioma exclusivamente de la elevación.
- No incorporar variantes regionales como biomas jugables distintos; las variantes presentes en la documentación preservan procedencia histórica.
- No activar todavía generación de flora/fauna/recursos a partir del catálogo guía.
- Estaciones, tiempo atmosférico dinámico y desastres climáticos dinámicos quedan fuera del alcance inicial, conforme a `worldRules` de la fuente.
- Pendiente: cálculo de temperatura/humedad, umbrales ecológicos, latitud/gradientes, interacción con costa y alturas, y resolución de asignaciones en bordes.

## Flujo lógico previsto

```text
Definitions: parámetros y lineamientos aprobados
      ↓
01 Grid (celdas, coordenadas y continuidad)
      ↓
02 Forma del mundo (tierra/agua)
      ↓
03 Elevaciones (niveles, y solo los valores adicionales que se aprueben)
      ↓
04 Biomas (categoría por hexágono)
      ↓
Engine entrega resultados a State
      ↓
Game (vistas 2D y 3D) leen los mismos resultados
```

### Separación entre definición y estado

`EvolveDoc` mantiene antecedentes, candidatos y guías de diseño. Este documento reúne los lineamientos **vigentes para la nueva generación**; los perfiles experimentales ejecutables de la Capa 02 están en `GEvolve.Definitions`. Los resultados por hexágono y datos de partida corresponden a `GEvolve.State`. El formato de guardado 2 incluye Capa 02 y `GameSaveStore` aún lee el formato 1. Aún no hay contrato definitivo para `WorldState`, `HexState`, `EntityState` ni `WorldSnapshot`.

### Decisiones explícitamente pospuestas

- Parámetros definitivos de cada forma terrestre; los de v0.001 son experimentales.
- Algoritmos de composición y erosión posteriores a la Capa 02.
- Relación entre alturas físicas y niveles 0–3.
- Cálculo climático y reglas de asignación de biomas.
- Estrategia definitiva de snapshots y persistencia de capas futuras.

**Regla de trabajo:** diseñar y aprobar cada capa antes de encargar su implementación. Los valores heredados del primer prototipo 3D y del generador anterior son referencias, no decisiones obligatorias.
