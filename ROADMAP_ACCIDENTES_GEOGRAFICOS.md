# Evolve — Roadmap maestro de accidentes geográficos

**Versión:** 2.0 · **Fecha:** 2026-10-28  
**Repositorio:** `fgabo404/Evolve` · **Rama:** `master`  
**Estado:** diseño y organización; **0/16 etapas completadas**  
**Etapa siguiente:** **01 — Drenaje topográfico y cuencas**.  
**Alcance:** accidentes geográficos naturales que faltan en el mundo de Pangea: lagos, ríos, volcanes, costas y formaciones derivadas.

> **Una etapa por vez.** Cada etapa se implementará, medirá y validará antes de comenzar la siguiente. Este archivo es la fuente de seguimiento. Las marcas no prueban implementación hasta que se registre su validación y commit.

## 1. Objetivo y principios

Crear una capa geográfica **posterior** a la generación tectónica: la superficie y el relieve son entradas inmutables, y los accidentes son datos superpuestos consultables en los hexágonos.

### Reglas aprobadas

1. **No modificar `PangaeaGenerator`**, distribución/movimiento de placas, costas, `SurfaceByCellId`, `PlateByCellId`, `ElevationByCellId`, pendientes ni semillas del mundo. Conservar las 36 regresiones tectónicas.
2. Respetar los seis vecinos reales de `HexTopology` y la unión horizontal del mapa. Los extremos norte/sur **no** se envuelven entre sí.
3. Igual semilla + misma versión de datos/modelo = mismos accidentes, IDs y rutas. Evitar porcentajes arbitrarios de accidentes.
4. Los lagos ocupan **hexágonos completos** de agua continental (sin convertirlos en océanos). Pueden ser de agua dulce o salada según el balance hídrico futuro.
5. **Todo río permanente generado desemboca en un lago o en el océano.** Un río puede entrar a un lago y volver a salir; cada tramo de su red tiene receptor válido. Las cuencas secas no reciben ríos permanentes ficticios.
6. **Ríos pequeños/ riachuelos:** segmentos sobre **lados/vértices** de los hexágonos, al estilo Civilization VI. **Ríos navegables:** recorridos continuos de **hexágonos completos**. Ambas formas pertenecen a **la misma red** y una corriente puede cambiar de clase.
7. El caudal, la navegabilidad, los volcanes y los lagos derivan de factores geográficos verificables, no de colocación puramente aleatoria. La variación determinista solo rompe empates o añade diversidad compatible.
8. Cumbres terrestres con elevación **85–100** siguen siendo infranqueables. Esto no impide que el agua nazca en montañas.
9. La precipitación actual (`precipitation-2`) **necesita calibración**; no fijar la cantidad de agua de lagos y ríos a partir de un modelo todavía desbalanceado.
10. Mantener persistencia compatible con guardados anteriores y estructuras compactas por ID. No añadir cambios de biomas, flora, fauna, minerales ni mecánicas económicas sin una etapa explícita.

## 2. Secuencia de generación prevista

```text
Pangea: superficie + placas + elevaciones (INTOCABLE)
  ↓
Geografía base: latitud + pendiente + costas
  ↓
Temperatura + vientos + precipitación (calibración pendiente)
  ↓
Drenaje topográfico / depresiones / cuencas
  ↓
Balance hídrico → lagos y cuerpos de agua continentales
  ↓
Acumulación fluvial → riachuelos y ríos navegables
  ↓
Accidentes derivados: deltas, estuarios, cascadas, humedales...
  ↓
Accidentes tectónicos: volcanes y manifestaciones volcánicas
  ↓
Clasificación ecológica refinada + vistas del juego
```

El orden es lógico, no obliga a recalcular todo siempre. Puede ser necesario **calcular un drenaje preliminar** antes de llenar lagos y actualizar las conexiones una vez establecidos los cuerpos de agua. El diseño impedirá dependencias circulares.

## 3. Roadmap por etapas

### Bloque A — Lagos y cuencas (primero)

#### [ ] 01 — Drenaje topográfico y detección de depresiones
- Adaptar **Priority-Flood** a los seis vecinos de la malla; calcular cotas hidrológicas auxiliares, dirección/receptor y cuencas sin alterar elevaciones.
- Identificar cuencas que conectan al mar, depresiones cerradas y posibles umbrales de desbordamiento. Resolver mesetas y empates sin ciclos.
- **Entrega:** mapa interno de drenaje, cuencas y estadísticas, **sin lagos ni ríos dibujados**.
- **Aceptación:** receptores vecinos válidos, sin ciclos, unión este-oeste correcta, hashes del relieve originales intactos.

#### [ ] 02 — Candidatos geométricos de lagos
- Agrupar depresiones; medir fondo, cota de rebose, extensión potencial y salida opcional.
- Distinguir cuenca cerrada, cuenca abierta y microdepresión. Evitar lagos artificiales por resolución/empates.
- **Entrega:** candidatos de `Lake` con geometría, sin afirmar presencia de agua permanente.
- **Aceptación:** celdas conectadas, sin solapamiento de candidatos definitivos ni conversión accidental a océano.

#### [ ] 03 — Recalibrar precipitación y balance de agua
- Medir lluvia y humedad sobre nueve o más Pangeas (tamaños y semillas); revisar la sequedad continental observada.
- Ajustar transporte de vapor, evaporación continental, barlovento/sotavento y convergencia con curvas suaves **sin imponer cuotas de biomas**.
- Definir balance hídrico inicial por cuenca: entradas, evaporación, infiltración y desbordamiento.
- **Entrega:** modelo climático e hídrico medido y documentado, apto para dimensionar lagos y caudales.
- **Aceptación:** contraste de rangos y mapas secos/húmedos; reproducibilidad; no alterar terreno/temperatura base; tests compilados y ejecutados.

#### [ ] 04 — Generación de lagos reales
- Determinar qué depresiones se llenan, nivel y extensión de agua; permitir lagos abiertos, endorreicos, temporales y cuencas secas.
- Salinidad: no inferirla solo por baja lluvia; modelar balance/endorreísmo y dejar desconocido lo que no pueda justificarse.
- **Entrega:** estado lacustre compacto con IDs estables, `cells`, `waterLevel`, `type`, `outletCellId?`.
- **Aceptación:** inundación coherente, desagües válidos, balance razonable y océano original intacto.

#### [ ] 05 — Renderizado y selección de lagos
- Superponer agua continental sin cambiar tierra/océano; crear vista **HIDROGRAFÍA** reutilizable por los ríos.
- Mostrar al seleccionar: ID, tipo, extensión, nivel y desagüe.
- **Aceptación:** colores, hit-testing, persistencia y rendimiento; la vista no modifica el estado.

### Bloque B — Red fluvial única (después de los lagos)

#### [ ] 06 — Red de drenaje permanente, nacimientos y caudales
- Acumular aportes por cuenca según la lluvia calibrada y resolver tramos de paso por lagos.
- Establecer la topología lógica de fuentes, afluentes, confluencias y destinos.
- **Entrega:** red fluvial independiente de su representación visual.
- **Aceptación:** **todos los ríos permanentes terminan en un lago o mar**, rutas sin ciclos ni extremos sin resolver, salvo excepciones definidas y aprobadas.

#### [ ] 07 — Riachuelos sobre lados y vértices
- Convertir cursos menores en segmentos conectados por bordes/vértices hexagonales.
- Canonicalizar bordes compartidos; resolver confluencias, giros y continuidad en costuras del mundo.
- **Entrega:** riachuelos visibles sin ocupar celdas enteras.
- **Aceptación:** sin segmentos duplicados, conexiones rotas ni cambios del bioma bajo el cauce.

#### [ ] 08 — Grandes ríos navegables por hexágonos
- Seleccionar tramos por caudal, continuidad, anchura aproximada y baja pendiente; no hacer navegable toda la longitud de un río.
- Trazar caminos continuos de **hexágonos fluviales**, distintos de los océanos y de los lagos.
- **Entrega:** ríos navegables visibles y consultables; interfaz común con riachuelos.
- **Aceptación:** transición coherente riachuelo→río navegable, navegación sin rutas discontinuas y territorio base intacto.

#### [ ] 09 — Desembocaduras, estuarios y deltas
- Identificar la llegada de ríos al mar; distinguir estuario/delta solo cuando caudal, pendiente y geometría costera lo permitan.
- Los deltas no añaden automáticamente nuevas tierras ni modifican las costas originales.
- **Entrega:** desembocaduras y accidentes asociados al flujo.
- **Aceptación:** ningún río termina sobre tierra seca ni fuera del mapa; las salidas son consistentes.

### Bloque C — Accidentes volcánicos y tectónicos

#### [ ] 10 — Aptitud volcánica por placas y relieve
- Usar `PlateByCellId`, límites de placa y relieve para candidatos geológicos. Distinguir límite de placa de un volcán comprobado.
- **Limitación actual:** velocidades y convergencia/divergencia se calculan dentro de `PangaeaGenerator` pero **no se persisten**. No inventar subducción ni actividad volcánica a partir de solo IDs.
- Si hacen falta metadatos tectónicos, diseñar una **salida derivada externa** que no cambie ni una línea del generador, o pedir aprobación explícita antes de ampliar su API.
- **Entrega:** mapa de aptitud/candidatos volcánicos, sin cráteres ni conos simulados todavía.
- **Aceptación:** candidatos plausibles, no todas las fronteras volcánicas, tectónica intacta.

#### [ ] 11 — Volcanes y campos volcánicos
- Colocar volcanes discretos según aptitud tectónica y excepciones fundamentadas (puntos calientes).
- Estado activo/durmiente/extinto solo si un modelo explícito lo respalda; si no, estado **indeterminado**.
- Guardar `id`, `cellId`, tipo y estado; representar la estructura sin cambiar la altura tectónica.
- **Entrega:** volcanes visibles, seleccionables y persistibles.
- **Aceptación:** datos reproducibles, sin distribuciones aleatorias absurdas ni cambios de elevación.

### Bloque D — Accidentes derivados del relieve, agua y costas

#### [ ] 12 — Cascadas, rápidos, gargantas y cañones
- Cascadas y rápidos donde exista un cauce y un descenso apropiado entre hexágonos.
- Gargantas, barrancos y cañones como **rasgos identificados** en relieve/pendiente; la excavación por erosión real es futura y necesitaría una autorización de modificación de terreno.
- **Entrega:** rasgos secundarios asociados a ríos y montañas.
- **Aceptación:** no hay cascadas sin curso de agua ni cañones que alteren elevaciones.

#### [ ] 13 — Costas: cabos, bahías, playas y acantilados
- Detectar contornos, curvaturas, pendientes y vecinos marinos para **etiquetar** formas costeras existentes.
- No crear nuevas penínsulas, islas, archipiélagos ni cambiar la línea litoral actual.
- **Entrega:** accidentes costeros consultables; posible representación específica.
- **Aceptación:** costa, océano y placas originales idénticos.

#### [ ] 14 — Humedales, manantiales y manifestaciones geotérmicas
- Humedales vinculados a lagos, desbordamientos, nivel hídrico y drenaje deficiente.
- Manantiales/acuíferos solo cuando exista una aproximación explícita de aguas subterráneas; sin inventarlos por ruido.
- Aguas termales y géiseres solo con proximidad/aptitud geotérmica y disponibilidad de agua justificadas.
- **Entrega:** accidentes de agua/geotermia derivados, nunca sustitutos arbitrarios del bioma.
- **Aceptación:** dependencia comprobable de agua, relieve o volcanismo.

### Bloque E — Integración y validación general

#### [ ] 15 — Integración ecológica y capas de visualización
- Reflejar agua superficial en humedales, márgenes fluviales y oasis; conservar las restricciones del catálogo.
- Integrar la capa **ACCIDENTES GEOGRÁFICOS** con filtros de agua, volcanismo y relieve (aprovechar HIDROGRAFÍA).
- Preparar consultas para futuras ciudades, navegación, recursos, transporte y eventos naturales **sin implementar esas mecánicas**.
- **Aceptación:** superposiciones independientes, consultas por ID, sin sobrescribir biomas ni alterar la tectónica.

#### [ ] 16 — Auditoría de determinismo, rendimiento y regresiones
- Revisar todas las fases con distintas semillas/tamaños y pruebas de guardado/carga y compatibilidad.
- Medir número/superficie de lagos, longitudes y clases de ríos, descargas al mar, volcanes y accidentes por región.
- Verificar las **36 regresiones tectónicas** y comparar hashes de superficie, elevación y placas.
- **Aceptación:** sin ríos desconectados, sin lagos inválidos, sin volcanes mal fundamentados, buena respuesta del renderizador y ninguna alteración de Pangea.

## 4. Modelo de datos provisional

```text
GeographyState
├── Hydrography
│   ├── drainageReceiverByCellId[]   # fase 01
│   ├── basinIdByCellId[]             # fase 01
│   ├── lakes[]                       # fases 02–05
│   └── rivers[]                      # fases 06–09
│       ├── id
│       ├── destinationWaterBodyId
│       ├── smallRiverEdges[]         # bordes/vértices
│       └── navigableCells[]          # hexágonos completos
├── Volcanoes[]                       # fases 10–11
│   ├── id
│   ├── cellId
│   ├── type
│   └── status
└── Features[]                        # fases 09, 12–14
    ├── id
    ├── type
    └── cellIds[]
```

Es **un esquema conceptual, no un JSON ni contrato de persistencia definitivo**. Acordaremos los campos mínimos de cada entidad al implementar su fase. Reutilizar referencias por ID para no duplicar cauces y accidentes derivados.

## 5. Criterios transversales de revisión

Para cada etapa se documentarán: propósito, entradas, método, límites, semillas probadas, mediciones, pruebas automatizadas, regresiones tectónicas, captura visual cuando aplique y enlace del commit.

- `[ ]` pendiente · `[x]` implementada, probada y aprobada.
- Si está en curso, conservar `[ ]` e indicar **EN CURSO** en el texto hasta aprobar.
- No marcar completado solo por subir código; primero comprobar compilación, pruebas y resultados.
- Si un resultado contradice el objetivo, calibrar **esa capa**, no modificar Pangea para corregir porcentajes.
- Cada cambio debe actualizar la versión/historial y registrar el commit con enlace.

## 6. Decisiones e historial

- **2026-10-28 — v1.0:** roadmap inicial de hidrografía; lagos antes que ríos. Riachuelos por bordes y ríos navegables por hexágonos. Todos los ríos permanentes desembocan en lago o mar.
- **2026-10-28 — v2.0:** convertirlo en roadmap maestro de **accidentes geográficos**: se incorporan volcanes, accidentes costeros, cascadas, cañones, humedales y geotermia, con sus dependencias. La precipitación se calibra antes del llenado de lagos y del caudal final. No se ha comenzado ninguna fase de implementación.
