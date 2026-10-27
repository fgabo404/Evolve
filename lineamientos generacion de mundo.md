# Evolve — Lineamientos de generación del mundo

**Estado:** Capa 01 implementada; capas 02–04 pendientes. **Actualizado:** 2026-10-27.
**Alcance:** capas 01–04. Los valores explícitamente pendientes no son reglas aprobadas ni deben completarse por inferencia.
**Fuentes:** acuerdos de diseño del proyecto y `fgabo404/EvolveDoc`: `data/world/world.catalog.guide.json`, `lang/es/world.catalog.guide.md`, `lang/es/tecnologias-arquitectura.md`.

## Arquitectura y principio general

- `GEvolve.Definitions`: configuración, selección de algoritmos, parámetros y reglas declarativas.
- `GEvolve.Engine`: ejecuta las etapas, calcula geometría/terreno/elevaciones y asigna los atributos aprobados.
- `GEvolve.State`: conserva configuración efectiva, semilla, resultados generados y cambios posteriores; la estrategia definitiva de snapshots está pendiente.
- `GEvolve.Game`: única aplicación gráfica; tendrá dos modos de visualización (2D y 3D) sobre el mismo mundo de Engine/State. El antiguo Simulator web y el prototipo 3D fueron retirados.

Las capas son **lógicas y secuenciales**, no renderizadores independientes. La Capa 01 está implementada por encargo explícito; las capas posteriores permanecen sin implementar.

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
- La Capa 01 se genera solo al solicitarla con **Generar mundo**; al iniciar el Simulator no existe un grid activo. La vista general usa una imagen interna de la cuadrícula y el zoom cercano dibuja solo celdas visibles, para no procesar los 50 000 hexágonos en cada fotograma.
- Convenio de la implementación: columnas impares desplazadas hacia abajo (`odd-q`); ID estable `fila × columnas + columna`; axial `(q, r) = (columna, fila − ⌊columna/2⌋)`. Para los anchos pares oficiales, el cierre horizontal traslada `(q, r)` por `(±ancho, ∓ancho/2)`. Así se conserva la fila, la distancia entre centros y la vecindad geométrica sin duplicar celdas.

## Capa 02 — Forma de masas terrestres (categorías acordadas)

Cuatro opciones del perfil de forma terrestre:

1. **Continentes** (`continents`): varias masas terrestres principales separadas por océanos.
2. **Supercontinente / Pangea** (`pangaea`): una masa terrestre predominante, con posibilidad de tierras secundarias.
3. **Continentes pequeños** (`small_continents`): mayor fragmentación en varias masas continentales menores.
4. **Archipiélagos** (`archipelagos`): numerosas islas y conjuntos insulares.

**Resultado lógico de esta capa:** distribución tierra/agua para cada hexágono, compatible con continuidad este–oeste y con una semilla reproducible. Estos identificadores son etiquetas de diseño propuestas, no nuevos IDs operativos aprobados.

**Pendiente:** algoritmos concretos (ruidos u otros), porcentajes de océano, cantidad y tamaño de masas, anchura de estrechos, relación con polos, suavizado y parámetros de cada perfil. No heredar automáticamente los `WorldPresets` ni los algoritmos del prototipo `GEvolve.Engine`; son antecedentes técnicos, no aprobación de sus valores.

## Capa 03 — Elevaciones (cuatro niveles documentados)

Fuente canónica de diseño: `EvolveDoc/data/world/world.catalog.guide.json`, campo `elevationLevels`, y `lang/es/world.catalog.guide.md`.

| Nivel | Nombre ES | Tránsito terrestre ordinario | Otras reglas |
| --- | --- | --- | --- |
| **0** | Nivel del mar | Depende de tierra/agua | No confundir nivel 0 con océano obligatorio. |
| **1** | Nivel medio | Permitido | Sin restricción adicional documentada. |
| **2** | Nivel alto | Permitido | Restricciones de movimiento/construcción por concretar (`null` en fuente). |
| **3** | Cumbres infranqueables | No permitido | Asentamientos convencionales no permitidos. |

- Elevación y bioma son atributos independientes.
- **Visualización inicial 2D:** colocar el número de nivel (`0`, `1`, `2` o `3`) sobre cada hexágono. No dibujar relieve tridimensional en Simulator como requisito de esta capa.
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

`EvolveDoc` mantiene antecedentes, candidatos y guías de diseño. Este documento reúne los lineamientos **vigentes para la nueva generación**; las configuraciones ejecutables deberán incorporarse luego a `GEvolve.Definitions` mediante contratos aprobados. Los resultados por hexágono y datos de partida corresponden a `GEvolve.State`. Aún no hay contrato definitivo para `WorldState`, `HexState`, `EntityState` ni `WorldSnapshot`.

### Decisiones explícitamente pospuestas

- Parámetros cuantitativos de cada forma terrestre.
- Algoritmos de ruido, composición y erosión.
- Sistema de coordenadas/indexación exacto con wrap horizontal.
- Relación entre alturas físicas y niveles 0–3.
- Cálculo climático y reglas de asignación de biomas.
- Versionado del generador y persistencia completa del mapa.

**Regla de trabajo:** diseñar y aprobar cada capa antes de encargar su implementación. Los valores heredados del primer prototipo 3D y del generador anterior son referencias, no decisiones obligatorias.
