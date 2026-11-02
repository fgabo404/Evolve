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

## Capas 03 y 04 — Pendientes de redefinición

Se retiraron las reglas anteriores de alturas, rangos, movilidad y asignación de biomas. El generador tectónico mantiene tierra/océano y placas; no clasifica elevaciones ni biomas. Las nuevas reglas se definirán expresamente.

## Flujo lógico previsto

```text
Definitions: parámetros y lineamientos aprobados
      ↓
01 Grid (celdas, coordenadas y continuidad)
      ↓
02 Forma del mundo (tierra/agua)
      ↓
03 Elevaciones (pendiente de redefinición)
      ↓
04 Biomas (pendiente de redefinición)
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
