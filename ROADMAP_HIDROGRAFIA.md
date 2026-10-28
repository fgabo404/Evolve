# Evolve — Roadmap de hidrografía

**Versión:** 1.0 · **Última actualización:** 2026-10-28  
**Estado:** planificación aprobada; ninguna etapa de hidrografía implementada  
**Siguiente paso:** fase 01 — depresiones, drenaje y cuencas.

## Objetivo

Construir primero **lagos y cuerpos de agua continentales** y después una red de ríos conectada a ellos. Existirán dos representaciones fluviales:

- **Ríos navegables:** recorridos continuos de **hexágonos completos**, con caudal suficiente y pendiente compatible.
- **Ríos pequeños / riachuelos:** recorridos sobre **lados y vértices de hexágonos**, sin ocupar el hexágono entero (estilo Civilization VI).

**Invariante de diseño:** todo río generado termina en un cuerpo de agua válido (**lago o mar**). Un río puede atravesar lagos y continuar aguas abajo. Una cuenca interior sin agua superficial no debe recibir un río permanente inventado. Los futuros ríos estacionales o perdedores requerirían una regla explícita distinta.

## Restricciones

1. **No modificar `PangaeaGenerator`, la tectónica, las placas, `SurfaceByCellId` ni `ElevationByCellId`.** La hidrografía será una etapa independiente y superpuesta al terreno.
2. Respetar los seis vecinos de `HexTopology` y la continuidad horizontal este-oeste; nunca crear conexiones inválidas entre bordes norte/sur.
3. Mundo determinista: igual semilla, mismo mapa, mismas estructuras y mismos IDs.
4. Mantener océanos distintos de lagos (agua continental), y no convertir los lagos en mar en el arreglo original de superficie.
5. Mantener las cumbres infranqueables desde elevación 85; la navegabilidad y la movilidad terrestre son reglas separadas del flujo de agua.
6. Estado compacto y persistible, con archivos guardados anteriores compatibles. No duplicar datos derivables innecesariamente.
7. **La precipitación actual requiere calibración.** No crear lagos permanentes ni caudales definitivos únicamente a partir de sus valores actuales.
8. Implementar **una sola fase cada vez**. No comenzar la siguiente sin revisar los resultados de la anterior.

## Fases

### [ ] 01 — Depresiones, drenaje topográfico y cuencas
**Objetivo:** preparar las posibles ubicaciones de lagos y salidas de agua, sin dibujar ni llenar lagos.

- Adaptar **Priority-Flood** a la malla hexagonal. Usar elevación hidrológica auxiliar para resolver celdas planas y depresiones **sin modificar la elevación original**.
- Identificar depresiones, nivel de rebose, posibles salidas, captaciones y cuencas interiores. El drenaje hacia el mar se identifica con la costa; las cuencas cerradas se registran por separado para evaluarlas más adelante.
- Registrar dirección/receptor de flujo por celda y pertenencia a cuenca, con valores compactos.
- **Criterios de aceptación:** rutas sin ciclos; receptores siempre vecinos válidos; salidas coherentes; continuidad este-oeste; mismo resultado con igual semilla; Pangea/placas/elevaciones idénticas a las regresiones existentes.
- **Entrega:** estado hidrológico de prueba y estadísticas por semilla; **sin ríos ni lagos visibles**.

### [ ] 02 — Geometría y selección de cuencas lacustres
**Objetivo:** delimitar dónde *puede* alojarse un lago.

- Agrupar celdas de depresión por cuenca; calcular fondo, superficie máxima inundable, cota del umbral de salida y conexión con otras cuencas.
- Distinguir **lago con desagüe potencial**, **cuenca endorreica** y **depresión demasiado pequeña**.
- Evitar lagos falsos debidos únicamente a empates por elevación, resolución del grid o ruido aislado.
- **Criterios:** celdas contiguas, sin solapamiento entre lagos, niveles coherentes y ninguna alteración de `SurfaceByCellId`.
- **Entrega:** candidatos geométricos de lago, todavía sin afirmar permanencia o salinidad.

### [ ] 03 — Balance hídrico y lagos reales
**Dependencia:** revisar/calibrar precipitaciones antes de fijar el llenado permanente.

- Estimar entradas por cuenca, aportes subterráneos simplificados si se justifican, evaporación e infiltración.
- Determinar cota de agua y extensión inundada; permitir **lago abierto**, **lago cerrado**, **estacional** o **depresión seca**.
- Distinguir agua dulce/salada por un modelo de balance y cierre, no solo por la forma de la depresión.
- **Criterios:** agua conservada de manera aproximada, lagos no rebosan arbitrariamente, salidas coherentes y resultados reproducibles.
- **Entrega:** `LakeState` mínimo con ID estable, celdas inundadas, nivel, tipo y salida opcional.

### [ ] 04 — Vista hidrográfica de lagos
- Mostrar el agua continental superpuesta a los biomas, sin convertirla en océano.
- Crear modo **HIDROGRAFÍA** y selección de lago (nombre/ID, extensión, nivel, tipo, salida).
- **Criterios:** cambio de vista sin mutar el mundo, costa intacta, rendimiento razonable y persistencia de lagos.

### [ ] 05 — Red lógica de ríos y acumulación de caudal
- Acumular aportes aguas arriba usando el drenaje validado; integrar lluvia una vez calibrada.
- Generar nacimientos, afluentes y desembocaduras. **Todo recorrido fluvial debe acabar en lago o mar**; una cuenca seca no justifica un río permanente.
- Mantener la red lógica independiente de cómo se dibuje.
- **Criterios:** sin ciclos, sin interrupciones, sin flujo hacia arriba sin una justificación explícita, tributarios conectados y terminal acuático válido.

### [ ] 06 — Riachuelos sobre bordes de hexágonos
- Convertir tramos menores de la red lógica en segmentos sobre **lados/vértices** de los hexágonos.
- Normalizar la identidad de bordes compartidos para evitar líneas duplicadas.
- Resolver visualmente giros, confluencias, cruces de vértice y continuidad en la unión horizontal.
- **Entrega:** riachuelos visibles sin reemplazar el bioma de la celda.

### [ ] 07 — Ríos navegables por hexágonos completos
- Seleccionar tramos con caudal, anchura y pendiente compatibles; no usar un porcentaje aleatorio.
- Convertirlos en **caminos conectados de hexágonos de agua fluvial**; separar navegación de la regla de cumbres infranqueables para unidades terrestres.
- Una misma corriente puede ser riachuelo aguas arriba y río navegable aguas abajo.
- **Entrega:** grandes ríos visibles y consultables, sin cambiar el terreno original.

### [ ] 08 — Integración, pruebas y calibración fluvial
- Unificar ríos pequeños y navegables en una sola red de afluentes y desembocaduras.
- Revisar continuidad, cotas, cuencas internas, grandes lagos, nacimientos, caminos y salidas costeras.
- Medir por múltiples semillas: número de lagos, área lacustre, longitudes de cauces y porcentaje navegable. Sin cuotas artificiales.
- Validar compilación, guardado/carga, visualización y las **36 regresiones tectónicas**. Ajustar solo parámetros hidrológicos.

### [ ] 09 — Integración ecológica (posterior)
- Permitir que lagos y ríos afecten humedales, vegetación ribereña, oasis y agua disponible.
- Más adelante añadir efectos sobre asentamientos, transporte, comercio y estaciones.
- **No modificar aquí flora, fauna, minerales ni tecnologías** sin un acuerdo separado.

## Estructura provisional

```text
HydrographyState
├── drainageReceiverByCellId[]    # dirección del flujo / sumidero
├── basinIdByCellId[]              # cuenca por celda
├── lakes[]                        # agua continental
│   ├── id
│   ├── cells[]
│   ├── waterLevel
│   ├── type
│   └── outletCellId               # opcional
└── rivers[]                       # red lógica; representación independiente
    ├── id
    ├── destinationWaterBodyId
    ├── navigableCells[]           # rutas por hexágonos
    └── smallRiverEdges[]          # tramos en lados/vértices
```

*Esquema de diseño, no contrato de persistencia final. Revisaremos los nombres y tipos antes de codificar cada fase.*

## Cómo se actualizará

- Al iniciar una fase: marcar `[~]` en la descripción de estado (sin cambiar las casillas hasta completarla) y anotar el alcance.
- Al terminar y validar: marcar `[x]`, registrar semillas verificadas, pruebas, decisión de aprobación y commit de implementación.
- Una fase que falle la validación queda `[ ]`, documentando el problema; no se avanza automáticamente.
- Mantener este archivo como **fuente de referencia** para las siguientes conversaciones.

## Historial de decisiones

- **2026-10-28 — v1.0:** crear primero lagos, después riachuelos por bordes y ríos navegables por celdas. Todo río permanente termina en lago u océano. Pangea tectónica permanece intacta. Se inicia por la **fase 01**.
