# Evolve — Roadmap maestro de accidentes geográficos

**Versión:** 2.2 · **Fecha:** 2026-10-28  
**Repositorio:** `fgabo404/Evolve` · **Rama:** `master`  
**Estado:** fase 01 implementada en código, **pendiente de compilación, pruebas y revisión visual**; **0/18 etapas aprobadas**  
**Siguiente trabajo:** **validar la fase 01 — Mapa de límites de placas tectónicas**.  
**Prioridad:** **accidentes tectónicos → lagos → ríos → accidentes derivados → integración**.

> Implementaremos **una etapa a la vez**, con revisión y aprobación antes de avanzar. Este archivo es el documento único de seguimiento del proyecto; `ROADMAP_HIDROGRAFIA.md` únicamente enlaza aquí.

## 1. Objetivo

Agregar los accidentes naturales que faltan en el mundo de Pangea como **capas geográficas posteriores** a su generación: volcanes y campos volcánicos, fallas/rifts etiquetados, lagos, ríos, deltas, cascadas, cañones, accidentes costeros, humedales y manifestaciones geotérmicas.

### Reglas innegociables

1. **No cambiar `PangaeaGenerator`, ni `SurfaceByCellId`, `ElevationByCellId`, `PlateByCellId`, las costas, la topología o la semilla.** Conservar las regresiones tectónicas existentes.
2. Las estructuras nuevas son **datos superpuestos**; no modifican la elevación, aunque una futura fase de erosión o geomorfología podría proponerse por separado.
3. Usar únicamente los **seis vecinos reales** de `HexTopology`, con continuidad este-oeste y sin unir norte/sur.
4. Igual configuración y semilla deben producir los mismos datos, IDs y geometrías. Evitar porcentajes forzados o ubicaciones aleatorias sin criterio geográfico.
5. Persistir versiones y estructuras compactas, con compatibilidad para guardados antiguos.
6. Los lagos serán **agua continental distinta del océano**, ocupando hexágonos completos; su nivel, salinidad y permanencia requieren balance hídrico.
7. **Todos los ríos permanentes terminan en un lago o en el mar**; pueden atravesar un lago y continuar aguas abajo. No inventar ríos permanentes hacia depresiones secas.
8. **Riachuelos**: trazos por lados y vértices, como en Civilization VI. **Ríos navegables**: caminos continuos de hexágonos completos. Una sola red lógica puede cambiar de representación a lo largo del cauce.
9. Las cumbres terrestres desde elevación 85 siguen infranqueables para unidades terrestres.
10. **La precipitación actual necesita calibración** antes de decidir el llenado definitivo de los lagos o los caudales.
11. No modificar aún flora, fauna, minerales, biomas o tecnologías sin una etapa explícita aprobada.
12. No marcar una fase como completada sin compilar, ejecutar pruebas, revisar sus resultados y registrar el commit.

## 2. Punto de partida: qué datos tectónicos existen

La implementación actual de `PangaeaGenerator` devuelve tres arreglos: **superficie, elevación y ID de placa**. Dentro de la generación también calcula centros, velocidades, compresión y extensión, **pero estos últimos datos no quedan persistidos**.

Por tanto:

- **Sí** podemos detectar límites entre placas, proximidad a fronteras, relieves montañosos y continuidad de posibles alineaciones.
- **No** podemos etiquetar confiablemente una frontera como zona de subducción, divergencia o transformación **solo** con `PlateByCellId`. No toda frontera es volcánica; no toda montaña es un volcán.
- Para una clasificación tectónica científicamente más sólida necesitaremos un **modelo derivado externo y verificable** o una extensión explícitamente aprobada de los datos tectónicos de salida. **No se modificará el generador por defecto**.
- En la primera versión, las ubicaciones volcánicas serán **candidatos plausibles** según datos disponibles y parámetros explícitos, no afirmaciones de subducción comprobada.

## 3. Roadmap secuencial

### Bloque A — Accidentes tectónicos (PRIMERO)

#### [ ] 01 — Detectar límites de placas · EN REVISIÓN
**Entrada:** `PlateByCellId`, `ElevationByCellId`, `HexTopology`.

- Identificar bordes entre hexágonos de placas distintas; agruparlos en tramos y medir distancia de cada hexágono a una frontera.
- Separar la geometría de la frontera de cualquier interpretación geológica de su actividad.
- Generar una **vista de diagnóstico de placas/fronteras** o exportación de estadísticas, sin volcanes todavía.
- **Aceptación:** fronteras coinciden con cambios de placa; costura este-oeste correcta; sin modificar arrays; determinismo y pruebas con distintas semillas.
- **Entrega preparada:** mapa derivado de bordes únicos, distancia mínima a un límite y vista **PLACAS TECTÓNICAS** con colores y trazos hexagonales. Incluye pruebas automatizadas para límites, continuidad este-oeste y determinismo. **Pendiente:** ejecutar `dotnet build`, `dotnet run --project GEvolve.Tests`, revisión gráfica y aceptación del usuario. No hay volcanes colocados en esta fase.

#### [ ] 02 — Aptitud volcánica y clasificación de candidatos
- Calcular una puntuación reproducible a partir de distancia a límites, relieve, topología y exclusiones explícitas.
- Distinguir candidatos de **arco/limítrofe** y **puntos calientes interiores** únicamente como hipótesis; no llamar subducción o rift real a una frontera no caracterizada.
- Estudiar si se necesitan datos de convergencia/divergencia; documentar limitaciones antes de decidir ampliaciones.
- **Aceptación:** ubicaciones justificables, sin volcanes indiscriminados a lo largo de toda frontera; distribución inspeccionable por semilla.
- **Entrega:** mapa de aptitud y candidatos, aún sin entidades volcánicas finales.

#### [ ] 03 — Generar volcanes y campos volcánicos
- Elegir candidatos locales de alta aptitud con distancia mínima/agrupamiento coherente; IDs estables por celda.
- Registrar `id`, `cellId`, `type` y `status`; usar `status = unknown` hasta contar con un modelo de actividad.
- Distinguir volcán aislado, alineación/campo volcánico y puntos calientes solo cuando los datos lo respalden.
- **Aceptación:** sin alterar elevaciones ni biomas; densidades razonables al comparar semillas; guardado/carga y regresiones tectónicas.
- **Entrega:** `VolcanoState` o entidades volcánicas persistibles.

#### [ ] 04 — Otros rasgos tectónicos derivados
- Detectar y etiquetar posibles **fallas, alineaciones, zonas fracturadas y valles de rift** como rasgos geológicos derivados.
- No simular movimiento de fallas ni abrir rifts modificando la geografía. Evitar diagnosticar tipo de falla sin cinemática.
- Identificar calderas o cráteres **solo como propiedades de volcanes representados**, no como depresiones inventadas en elevación.
- **Aceptación:** trazados coherentes con límites/relieve; nomenclatura honesta sobre incertidumbre.
- **Entrega:** accidentes tectónicos secundarios consultables.

#### [ ] 05 — Renderizado y selección tectónica
- Superponer símbolos de volcanes y líneas de límites/fallas con interruptores independientes.
- Al seleccionar: tipo, estado, celda, placa y fundamento de la ubicación; compatibilidad con vistas de biomas, temperatura y precipitaciones.
- **Aceptación:** símbolos no modifican el mundo, selección estable, rendimiento, persistencia y prueba visual.
- **Entrega:** primera capa de **ACCIDENTES GEOGRÁFICOS** visible en el juego.

### Bloque B — Lagos y cuencas (SEGUNDO)

#### [ ] 06 — Drenaje topográfico y detección de depresiones
- Adaptar **Priority-Flood** a los seis vecinos hexagonales, manteniendo una elevación hidrológica auxiliar.
- Determinar receptores de flujo, cuencas, umbrales de rebose y posibles salidas al océano sin ciclos.
- **Entrega:** drenaje interno y estadísticas, sin lagos visibles.
- **Aceptación:** direcciones vecinas válidas, continuidad este-oeste, determinismo y terreno intacto.

#### [ ] 07 — Candidatos geométricos de lagos
- Identificar fondos, áreas inundables, cotas de rebose, cuencas abiertas y endorreicas.
- Evitar microdepresiones falsas y solapamiento de áreas seleccionadas.
- **Entrega:** candidatos lacustres sin afirmar que tengan agua.
- **Aceptación:** geometría conexa, posibles salidas válidas y océano sin cambios.

#### [ ] 08 — Calibración de precipitación y balance hídrico
- Revisar la sequedad continental de `precipitation-2`, evaporación, transporte de vapor, sombras de lluvia y estabilidad del modelo.
- Medir distribuciones en nueve o más mapas de Pangea; sin cuotas arbitrarias de bosques o desiertos.
- Definir aportes, infiltración, evaporación y desbordamiento por cuenca.
- **Entrega:** modelo calibrado con estadísticas y pruebas ejecutadas.
- **Aceptación:** resultados reproducibles, mapas contrastados y terreno/temperatura base intactos.

#### [ ] 09 — Generar lagos reales
- Elegir depresiones con agua suficiente; calcular nivel, celdas inundadas, tamaño y salida opcional.
- Permitir lagos abiertos, endorreicos, estacionales y cuencas secas; salinidad solo cuando el balance lo justifique.
- **Entrega:** `LakeState` persistible, con IDs estables y separación explícita respecto del mar.
- **Aceptación:** niveles coherentes, lagos conectados, salidas válidas y sin cambiar `SurfaceByCellId`.

#### [ ] 10 — Visualización hidrográfica de lagos
- Representar lagos sobre hexágonos enteros y crear vista **HIDROGRAFÍA**.
- Mostrar extensión, nivel, tipo y desagüe al seleccionar.
- **Aceptación:** guardado/carga, selección, rendimiento y colores sin cambios en el terreno base.

### Bloque C — Red fluvial (TERCERO)

#### [ ] 11 — Red lógica de ríos y acumulación de caudal
- Acumular escorrentía aguas arriba, fuentes, afluentes y caudales sobre las cuencas ya validadas.
- Conectar rutas a un lago o al mar; no generar ríos permanentes sin destino acuático.
- **Entrega:** red fluvial única sin dependencia del dibujo.
- **Aceptación:** sin ciclos ni extremos inconexos, confluencias válidas y destino de agua confirmado.

#### [ ] 12 — Riachuelos sobre lados/vértices de hexágonos
- Convertir cursos menores a geometría continua de lados/vértices; canonicalizar los bordes compartidos.
- **Entrega:** riachuelos visibles sin ocupar ni modificar el hexágono subyacente.
- **Aceptación:** sin duplicaciones, saltos gráficos ni cruces espurios.

#### [ ] 13 — Ríos navegables por hexágonos completos
- Seleccionar tramos navegables por caudal, anchura aproximada, continuidad y pendiente; no por porcentaje aleatorio.
- Trazar cadenas de hexágonos con transición coherente de riachuelos a grandes ríos.
- **Entrega:** cursos navegables distinguibles de océanos y lagos.
- **Aceptación:** continuidad, destinos válidos, navegabilidad razonable y superficie original intacta.

#### [ ] 14 — Desembocaduras, estuarios y deltas
- Identificar contactos río-mar/lago y diferenciar deltas y estuarios por geometría costera, caudal y pendiente.
- No añadir tierra al océano ni modificar costas para representar un delta.
- **Entrega:** desembocaduras y rasgos fluviales derivados.
- **Aceptación:** todos los recorridos terminan en cuerpos de agua válidos y sin segmentos perdidos.

### Bloque D — Otros accidentes del agua, relieve y costas

#### [ ] 15 — Cascadas, rápidos, gargantas y cañones
- Cascadas/rápidos solo con cauce existente y descenso suficiente.
- Etiquetar gargantas, barrancos y cañones según relieve y pendientes, sin excavar ni deformar el terreno.
- **Aceptación:** rasgos vinculados a datos reales del mundo y no duplicados.

#### [ ] 16 — Accidentes costeros, humedales y geotermia
- Detectar **cabos, bahías, playas y acantilados** a partir de la línea de costa actual.
- Detectar **humedales** cerca de agua superficial o mal drenaje; **manantiales** solo si existe modelo de agua subterránea.
- Detectar **aguas termales/géiseres** solo cuando exista aptitud geotérmica y agua suficiente.
- **Aceptación:** costa intacta; no inventar agua subterránea ni actividad geotérmica sin fundamento.

### Bloque E — Integración y auditoría

#### [ ] 17 — Integración ecológica y consultas
- Exponer relaciones entre accidentes y hexágonos; preparar efectos futuros en biomas, recursos, navegación y asentamientos.
- Integrar filtros de vistas (tectónica, agua y relieve) sin sobrescribir biomas ni modificar los catálogos salvo aprobación posterior.
- **Aceptación:** consulta compacta por ID y capas superpuestas independientes.

#### [ ] 18 — Validación integral y rendimiento
- Compilar, ejecutar tests del motor, interfaz y guardado/carga; revisar varias semillas y tamaños.
- Verificar **36 regresiones tectónicas** de superficie, altura y placas (sin modificación).
- Medir volcanes por frontera, cuencas y lagos, caudal y continuidad de ríos, coste de memoria y renderizado.
- **Aceptación:** ningún río sin desembocadura, accidentes coherentes, determinismo y rendimiento aceptable.

## 4. Estructura provisional (no definitiva)

```text
GeographyState
├── Tectonics
│   ├── boundaryEdges[]                # fase 01 (derivable)
│   ├── volcanicSuitabilityByCellId[]  # fase 02 (derivable)
│   ├── volcanoes[]                    # fase 03
│   │   ├── id
│   │   ├── cellId
│   │   ├── type
│   │   └── status
│   └── tectonicFeatures[]             # fase 04
├── Hydrography
│   ├── drainageReceiverByCellId[]     # fase 06 (derivable)
│   ├── basinIdByCellId[]              # fase 06 (derivable)
│   ├── lakes[]                        # fases 07–10
│   └── rivers[]                       # fases 11–14
│       ├── id
│       ├── destinationWaterBodyId
│       ├── smallRiverEdges[]          # lados/vértices
│       └── navigableCells[]           # hexágonos completos
└── Features[]                         # fases 14–16
    ├── id
    ├── type
    └── cellIds[]
```

No es aún un contrato de persistencia: se definirán los campos mínimos fase a fase y no se guardarán copias de datos calculables sin una razón de rendimiento.

## 5. Actualización del roadmap

- **Pendiente:** `[ ]`. **Completada, probada y aprobada:** `[x]`.
- Durante el trabajo, mantener `[ ]` e indicar **EN CURSO** hasta validar.
- Para cada fase registrar: semilla/tamaño, algoritmos, pruebas y resultados, captura si aplica, límites conocidos y **SHA/enlace del commit**.
- Si una etapa no supera la validación, se corrige antes de avanzar. No tocar Pangea para arreglar problemas de capas posteriores.
- El siguiente trabajo será **solo la fase 01**, no una implementación simultánea de volcanes y todas las demás capas.

## 6. Historial de decisiones

- **2026-10-28 · v1.0:** plan de hidrografía: lagos antes que ríos, dos clases de río y desembocadura obligatoria en lago u océano.
- **2026-10-28 · v2.0:** roadmap maestro con volcanes, costas, agua y accidentes derivados.
- **2026-10-28 · v2.1:** **reordenar la implementación para comenzar por los accidentes relacionados con las placas tectónicas** (límites, aptitud volcánica, volcanes y vistas); después lagos, ríos y demás accidentes. Ninguna fase implementada todavía. Se mantiene intacto el generador tectónico de Pangea.

- **2026-10-28 · v2.2:** fase 01 codificada y publicada para revisión: `PlateBoundaryMap` analiza límites y distancia; vista de placas con colores, aristas y selección. Se agregaron pruebas. **La fase sigue [ ] hasta compilar, probar y aprobar.** No se generaron volcanes y no se modificó Pangea.
