# Evolve — Roadmap único de definiciones y entidades

> **Repositorio:** `fgabo404/Evolve`  
> **Rama de trabajo:** `definitions` (creada desde `develop`)  
> **Origen Git:** `772d4e34b596fe7db524b047aced16fd55ba76d2` — `docs(worldgen): record synchronized development branch and pending cleanup`  
> **Fecha de inicio:** 2026-11-08  
> **Estado:** D00 aprobado; D01 en curso con 8 biomas y 21 subbiomas; los 5 de Bosque tienen `spawn` declarativo, los otros 16 pendientes; fauna, flora y minerales vacíos  
> **Ubicación:** `GEvolve.Definitions/ROADMAP.md`

## 0. Reinicio controlado de los catálogos (2026-11-08)

**Decisión aprobada:** reconstruir desde cero los catálogos de **biomas, fauna, flora y minerales**, en ese orden de diseño, sin borrar los anteriores.

- Los JSON activos en `Biomas/biomes.json` y `Resources/{fauna,flora,minerals}.json` se sustituyen por contenedores mínimos con listas vacías: `biomes`, `animals`, `plants`, `minerals`. No se han inventado entidades ni reglas.
- Las cuatro versiones anteriores se conservan **byte por byte** bajo `Biomas/old/` y `Resources/old/`; son referencias **históricas, no catálogos activos**.
- Las carpetas principales `Biomas/`, `Resources/`, `Technologies/`, `Mechanics/`, `WorldGeneration/` y `wiki/` permanecen intactas.
- El generador antiguo sigue compilando `Biomas/old/biomes.json` como `Evolve.biomes.json` hasta que exista un contrato tipado nuevo; no interpreta el JSON activo vacío todavía.
- `DefinitionCatalog.Load` excluye los directorios `old/`, para que la lectura genérica de JSON no mezcle definiciones archivadas y nuevas.
- `resources.json`, tecnologías y contratos C# existentes quedan sin cambios de contenido. Sus referencias a entidades históricas deben considerarse **pendientes de reconciliación** mientras se reconstruyen los catálogos.
- La instantánea offline de la wiki se actualiza para mostrar los **catálogos nuevos vacíos**, manteniendo intactos los datos de recursos y tecnologías. La wiki y sus pruebas históricas requieren revisión de relaciones y expectativas durante D01–D08.
- En cada nueva entidad se decidirán los campos mínimos. Para biomas se ha acordado conceptualmente `Bioma → Subbioma → Spawn (condiciones ambientales)`; la gramática exacta de `spawn` queda pendiente de diseño. Los modelos tipados vendrán después.
- No se borran IDs antiguos del historial: al reintroducir la misma entidad se conservará su identidad o se documentará una migración explícita. El reinicio del diseño no equivale a autorizar reutilización arbitraria de IDs.

**Nuevo estado de D01:** se conservan 8 biomas y 21 subbiomas activos; Montaña solo tiene Montaña rocosa y Cumbres. Los 5 subbiomas de **Bosque** cuentan ya con `spawn` declarativo (`required` y `preferred`); los otros 16 siguen sin `spawn`. El catálogo histórico permanece intacto en `old/`.

## 1. Objetivo y alcance

Pulir las **definiciones lógicas y científicas** del juego sin comenzar desde cero, preservando los identificadores, catálogos, vínculos y conocimiento existentes. La meta es pasar de listas de entidades a un sistema de definiciones coherente, extensible y listo para futuras mecánicas.

El proyecto se trabajará en **dos pasadas**:

1. **Pasada global:** definir y perfeccionar las cuatro familias, en este orden: **biomas/subbiomas → fauna → flora → minerales**.
2. **Pasada individual:** desarrollar **una entidad por vez** hasta completar su ficha ecológica, recursos, condiciones, relaciones y, cuando corresponda, reproducción, adaptación, selección o domesticación. Se comienza por **fauna**, luego flora y minerales, salvo cambio explícito de prioridad del director.

Este roadmap **no sustituye** el roadmap independiente de `GEvolve.Engine/World/WorldGeneration/ROADMAP.md`. El motor de generación de mundos, la simulación dinámica y el laboratorio de Pangea mantienen sus responsabilidades.

## 2. Inventario inicial confirmado en `develop`

| Catálogo | Referencia | Estado de diseño |
|---|---|---|
| Biomas | 8 biomas y 22 subbiomas; `Biomas/biomes.json` | Clasificación y reglas ambientales por depurar |
| Fauna | 92 animales; `Resources/fauna.json` | Identidades, hábitats y recursos iniciales |
| Flora | 91 plantas; `Resources/flora.json` | Identidades, hábitats y campos de cultivo |
| Minerales | 45 entradas; `Resources/minerals.json` | Identidades, hábitats geológicos propuestos y recursos |
| Recursos transversales | 121 recursos, 582 vínculos de origen y 43 procesos candidatos; `Resources/resources.json` | Referencias que deben conservarse y validarse |

Los números describen el **archivo histórico anterior al reinicio**, no el contenido activo ni metas/cuotas de contenido. Las definiciones de recursos y procesos marcadas como propuestas **no se consideran reglas ejecutables**.

## 3. Principios obligatorios

- **Realidad como guía, jugabilidad como adaptación:** toda simplificación debe ser explícita y justificable.
- **Diseño antes que código:** aprobar contratos conceptuales primero; solicitar implementación al desarrollador en un paso separado.
- **IDs estables:** no reasignar IDs, reordenarlos como si fueran secuenciales, ni romper vínculos con `resources.json`, tecnologías, wiki o partidas.
- **Sin duplicación:** una entidad canónica puede tener múltiples fuentes, usos y apariciones, pero no se duplica para cada bioma.
- **Un solo responsable por dato:** Definitions declara lo posible; Engine calcula/aplica; State almacena lo que existe o cambia durante la partida; Game lo presenta.
- **Distinguir potencial de realidad:** pertenecer a un hábitat no implica aparición garantizada; una vinculación a recurso no implica cantidad, inventario ni receta ejecutable.
- **Sin parámetros inventados:** los valores sin decisión permanecen pendientes; `null` no equivale a cero o ilimitado.
- **No cambiar el generador viejo ni el nuevo desde esta rama** por un ajuste de catálogo sin contrato y validación independiente.
- **Una entidad por ciclo:** revisión, propuesta, aprobación, actualización y auditoría antes de pasar a la siguiente.

## 4. Primera pasada — Fundamentos de los cuatro catálogos

### D00 — Contratos, inventario y trazabilidad
- [x] Crear la rama `definitions` desde `develop`.
- [x] Crear este roadmap específico, sin reemplazar el roadmap de generación mundial.
- [x] Registrar el inventario inicial.
- [x] Levantar dependencias entre `biomes.json`, `fauna.json`, `flora.json`, `minerals.json`, `resources.json` y tecnologías. Ver [D00_CONTRATOS.md](D00_CONTRATOS.md#2-mapa-de-dependencias).
- [x] Documentar vocabulario canónico: entidad, especie, grupo taxonómico, subbioma, población, depósito, recurso, proceso, condición y efecto. Ver [D00_CONTRATOS.md](D00_CONTRATOS.md#3-vocabulario-canónico).
- [x] Establecer validaciones mínimas y criterio de cierre de una entidad. Ver [D00_CONTRATOS.md](D00_CONTRATOS.md#4-validaciones-mínimas-y-criterio-de-cierre).
- [x] **Aprobación del director:** contratos D00 aprobados explícitamente el 2026-11-08.

**Salida:** mapa de dependencias, vocabulario y validaciones documentados y **aprobados**; las reglas ejecutables siguen pendientes de implementación.

### D01 — Biomas y subbiomas (primero)
- [x] Recuperar los 8 biomas históricos sin cambiar sus IDs; mantener `id` y `name`.
- [x] Restaurar los subbiomas históricos sin `spawn`, salvo **Montaña**: solo `mountain_rocky` y `mountain_summits` (nuevo). Total: 21 subbiomas.
- [x] Revisar y acordar mantener el catálogo histórico, salvo Montaña con dos subbiomas.
- [x] Iniciar las definiciones ambientales en `forest_tropical` (nombre `Selva`; conservar ID).
- [x] Añadir `spawn.required` y `spawn.preferred` a `forest_subtropical`, `forest_temperate`, `forest_boreal` y `forest_wooded_swamp`, sin modificar otros biomas.
- [ ] Validar con el director los umbrales del conjunto Bosque y la resolución de solapamientos antes de tipar reglas en Engine.
- [ ] Revisar y confirmar los parámetros iniciales de spawn de Selva antes de establecer reglas tipadas del motor.
- [ ] Acordar jerarquía **bioma → subbioma**, separando clima/vegetación de rasgos superpuestos como relieve, costa, cuerpos de agua y humedales.
- [ ] Revisar qué categorías conservar, renombrar, dividir o agregar, sin romper IDs existentes.
- [ ] Definir condiciones necesarias y preferentes: temperatura, precipitación, humedad útil, estacionalidad, latitud, altitud, pendiente, suelo, hidrología y proximidad marina cuando aplique.
- [ ] Distinguir unidades físicas (°C, mm/año, metros) de escalas internas normalizadas; no fijar valores arbitrarios.
- [ ] Describir límites, transiciones, zonas ecotonales y restricciones geográficas especiales.
- [ ] Revisar el caso marino (aguas costeras, estuarios, arrecifes, mar abierto) y la separación de hielo continental y hielo marino.
- [ ] Revalidar qué flora, fauna y minerales son **candidatos** a cada subbioma; evitar que el bioma por sí solo determine depósitos geológicos.
- [ ] Definir escenarios de prueba conceptuales y criterios visuales de plausibilidad terrestre.
- [ ] Aprobar el contrato del catálogo antes de pedir cambios de clasificación al motor.

### D01 — Catálogo histórico recuperado con excepción Montaña

Se mantiene la estructura acordada `Bioma → Subbiomas`. Todos los elementos conservan `id` y `name` (es/en); **los 5 subbiomas de `forest` incorporan `spawn`**, con requisitos y preferencias ambientales. Los otros 16 subbiomas no tienen `spawn`; aún no se han agregado relaciones con fauna, flora o minerales.

| Bioma | Subbiomas |
|---|---|
| Desierto (`desert`) | Arenoso (dunas) (`desert_sandy`); Rocoso (cañones y mesetas) (`desert_rocky`); Salar (`desert_salt_flat`) |
| Bosque (`forest`) | Selva (`forest_tropical`); Subtropical (`forest_subtropical`); Templado (`forest_temperate`); Boreal (taiga) (`forest_boreal`); Pantano arbolado (Everglades) (`forest_wooded_swamp`) |
| Llanuras (`plains`) | Sabana (`plains_savanna`); Pradera (`plains_prairie`); Estepa (`plains_steppe`); Humedal abierto (Pantanal) (`plains_open_wetland`) |
| Marino (`marine`) | Arrecife (`marine_reef`); Estuario o litoral (`marine_estuary_littoral`); Manglar (`marine_mangrove`); Océano abierto (alta mar) (`marine_open_ocean`) |
| Tundra (`tundra`) | Tundra (`tundra_tundra`) |
| Montaña (`mountain`) | Montaña rocosa (`mountain_rocky`); Cumbres (`mountain_summits`) |
| Polo Norte (`polar_north`) | Hielo polar (`polar_north_ice`) |
| Polo Sur (`polar_south`) | Hielo polar (`polar_south_ice`) |

**Único cambio deliberado respecto al histórico:** Montaña deja de tener `mountain_alpine` y `mountain_cliffs` y conserva `mountain_rocky`, que se presenta como Montaña rocosa. Se incorpora `mountain_summits` (Cumbres). Los IDs eliminados del catálogo activo permanecen en `old/`; no se reasignan. Los ecosistemas forestales permanecen en Bosque.

Los 8 biomas históricos / 22 subbiomas siguen archivados íntegramente en `Biomas/old/biomes.json`. La nueva lista activa tiene 8 biomas / 21 subbiomas. La clasificación está en **revisión de diseño**; el motor antiguo continúa usando el archivo histórico integrado hasta una migración aprobada.

**Pendiente:** revisar y aprobar los parámetros de los cinco bosques, resolver solapamientos, diseñar un contrato C# tipado general y reconciliar vínculos históricos.

### D01 — Selva (`forest_tropical`): primer spawn declarativo

Se conserva el ID histórico, y se cambia el nombre visible a **Selva** (`Rainforest`). El `spawn` se configura de forma declarativa, **sin cambios en Engine ni modelos C#**.

| Condición | Tipo | Rango o límite | Interpretación |
|---|---|---|---|
| Superficie | Obligatorio | `terrain: land` | No aparece como bioma de agua abierta |
| Temperatura media anual | Obligatorio | 20–30 °C | Promedio anual, no temperatura del día |
| Precipitación anual | Obligatorio | ≥ 1500 mm/año | No se define máximo de precipitación |
| Estación seca | Obligatorio | ≤ 3 meses con lluvia < 60 mm/mes | Requiere datos mensuales; el umbral 60 mm es una convención de diseño compatible con criterios climáticos |
| Altitud | Preferente | 0–1500 m s. n. m. | Fuera de esa banda no hay veto automático |
| Precipitación óptima | Preferente | 2000–4000 mm/año | Más compatible dentro de la banda |
| Humedad relativa media anual | Preferente | 75–90 % | Factor adicional, no condición excluyente |
| Latitud absoluta | Preferente | 0–23,5° | Simetría norte/sur; la latitud sola no define el ecosistema |

**Semántica propuesta de este primer JSON:** `required` contiene las condiciones de elegibilidad, `preferred` describe la banda favorable; un máximo omitido en la lluvia anual significa que **esta regla no establece un máximo**. Una preferencia no debe convertirse en condición de descarte. Si el motor no calcula alguna métrica requerida, no debe inventarla: esa integración permanece pendiente de diseño y pruebas.

**Fuentes de referencia:** [FAO, Global Ecological Zoning](https://www.fao.org/4/ad652e/ad652e07.htm) (precipitación ≥1500 mm, distribución anual y ≤3 meses secos); [UK Met Office, Tropical Rainforest Climate](https://weather.metoffice.gov.uk/climate/climate-explained/climate-zones) (temperaturas ~20–30 °C y lluvia mensual); [USGS/USNVC](https://data.usgs.gov/usnvc-explorer/unitDetails/1298971) (elevaciones tropicales de tierras bajas hasta ~1000–1700 m según región); [National Geographic](https://education.nationalgeographic.org/resource/rain-forest/) (humedad y distribución tropical). Los límites son **valores iniciales de jugabilidad**, no una ley ecológica universal.

**Estado:** primera propuesta aplicada, ajuste/confirmación del director y futura deserialización tipada pendientes.


### D01 — Spawn aplicado a los cinco subbiomas de Bosque (2026-11-09)

Se conserva íntegra la primera propuesta de Selva. A los cuatro subbiomas restantes se añadió el mismo esquema `spawn.required` / `spawn.preferred` sin crear especies, vegetación ni mineralizaciones. Los campos se interpretan como **reglas declarativas iniciales**, no como límites científicos universales.

| Subbioma | `required` | `preferred` |
|---|---|---|
| Subtropical (`forest_subtropical`) | Terreno terrestre; ≥8 meses con temperatura media mensual >10 °C; ≥600 mm/año | Temperatura media anual 14–22 °C; lluvia 900–2200 mm/año; altitud 0–1800 m; latitud absoluta 20–40° |
| Templado (`forest_temperate`) | Terreno terrestre; 4–8 meses con temperatura media mensual >10 °C; ≥450 mm/año | Temperatura media anual 4–15 °C; lluvia 650–1800 mm/año; altitud 0–2000 m; latitud absoluta 30–60° |
| Boreal (`forest_boreal`) | Terreno terrestre; 1–3 meses con temperatura media mensual >10 °C; ≥200 mm/año | Temperatura media anual −7 a 5 °C; lluvia 300–900 mm/año; altitud 0–1200 m; latitud absoluta 50–75° |
| Pantano arbolado (`forest_wooded_swamp`) | Terreno terrestre; suelo saturado durante la temporada de crecimiento; humedal de agua dulce | Lluvia 700–2500 mm/año; temperatura media anual 10–28 °C; altitud 0–600 m; pendiente 0–5° |

**Convenciones del JSON:** `monthsMeanTemperatureAbove10C` es la cantidad de meses con temperatura media mensual estrictamente mayor que 10 °C (de 0 a 12). `soilSaturatedDuringGrowingSeason` exige saturación del suelo durante la temporada de crecimiento, no inundación todos los días. `wetlandWaterType: freshwater` restringe la regla actual a humedal de agua dulce y deja los manglares en su subbioma marino. `absoluteLatitudeDegrees` evita duplicar hemisferio N/S.

**Procedencia:** [FAO — Global Ecological Zones](https://www.fao.org/4/Y4171E/Y4171E52.htm) describe subtrópicos (≥8 meses cálidos), templados (4–8 meses) y boreales (<4 meses), y distingue formaciones forestales según humedad; [EPA — Classification and Types of Wetlands](https://www.epa.gov/wetlands/classification-and-types-wetlands) describe suelos saturados e inundaciones estacionales en pantanos arbolados. **Los valores numéricos de precipitación, altitud, latitud y temperatura anual son parámetros orientativos elegidos para Evolve**, no umbrales oficiales de la FAO o EPA.

**Advertencias de integración:** existen solapamientos (p. ej. el mes cálido número 8 puede cumplir subtropical y templado; un pantano arbolado puede coincidir climáticamente con otros bosques). Las reglas de prioridad/desempate, la definición física de humedad edáfica y los datos mensuales que necesita Engine siguen **pendientes**. No se han creado modelos C# ni se ha activado la clasificación. Los otros 16 subbiomas permanecen sin `spawn`.

**Estado:** datos JSON aplicados, pendientes de validación ecológica, contrato tipado y pruebas.


**Salida:** matriz de subbiomas con condiciones ambientales y relaciones candidatas revisadas.

### D02 — Fauna: modelo común
- [ ] Definir qué representa cada entrada actual: especie, grupo genérico o población.
- [ ] Acordar atributos comunes: identificación, distribución nativa, hábitat, dieta, comportamiento, reproducción, mortalidad y movilidad.
- [ ] Definir existencia potencial frente a población realmente generada.
- [ ] Diseñar relaciones depredador–presa, competencia, migración y estacionalidad, según especie.
- [ ] Normalizar domesticación, crianza, selección y recursos obtenibles, sin asumir que aplican a todos los animales.
- [ ] Conservar vínculos a subbiomas y recursos existentes.

**Salida:** plantilla de ficha de fauna aprobada y catálogo global coherente.

### D03 — Flora: modelo común
- [ ] Distinguir especie silvestre, población, cultivo y variedad domesticada.
- [ ] Acordar atributos comunes: clima, suelo, agua, estacionalidad, crecimiento, reproducción, dispersión y supervivencia.
- [ ] Modelar semillas, germinación, ciclo vital, cosecha y aprovechamiento según especie.
- [ ] Separar cambios biológicos heredables de tecnologías que habilitan acciones agrícolas.
- [ ] Conservar vínculos a subbiomas, recursos y tecnologías; no asumir producción automática.

**Salida:** plantilla de ficha de flora aprobada y catálogo global coherente.

### D04 — Minerales y materiales geológicos: modelo común
- [ ] Distinguir mineral, roca, sedimento, mena y depósito explotable.
- [ ] Definir contexto geológico: formación, roca huésped, ambiente tectónico, alteración, erosión, transporte y concentración.
- [ ] Separar **probabilidad de depósito geológico** de condiciones de acceso superficial, bioma y extracción.
- [ ] Identificar profundidad, abundancia relativa, calidad, métodos de extracción y procesamiento como campos pendientes de balance cuando no existan datos.
- [ ] Conservar vínculos al catálogo de recursos y sus productos transformados.

**Salida:** plantilla de ficha geológica aprobada y catálogo global coherente.

## 5. Segunda pasada — Una entidad a la vez

### D05 — Fauna: fichas completas
- [ ] Seleccionar la primera entidad animal y abrir su ficha usando su ID existente.
- [ ] Revisar nombre, taxonomía, distribución, biomas/subbiomas, dieta, depredadores, reproducción, movimiento y ciclos de vida.
- [ ] Documentar todas sus fuentes de recursos, métodos de obtención, restricciones, consecuencias y cantidades **solo cuando estén fundamentadas y aprobadas**.
- [ ] Definir interacciones con sociedades: caza, manejo, domesticación o aprovechamiento cuando correspondan.
- [ ] Registrar posibilidades de adaptación, selección y cambios heredables **sin simular especiación obligatoria**.
- [ ] Validar sus referencias cruzadas; cerrar la ficha y continuar con la siguiente.
- [ ] Reintroducir una por una las entidades aprobadas; las 92 entradas archivadas son referencia, no cuota ni fichas activas.

### D06 — Flora: fichas completas
- [ ] Comenzar por la entidad `plant_southwest_asian_wild_wheat` (**Trigo silvestre**), ya presente en `flora.json` y vinculada a `plains_steppe`.
- [ ] Desglosar hábitat, condiciones del suelo y clima, fases de crecimiento, semillas, reproducción y competencia.
- [ ] Identificar recursos obtenibles en distintas fases y procesos posteriores: cosecha, secado, almacenamiento, molienda y otros **solo si corresponden**.
- [ ] Diseñar el recorrido silvestre → cultivo → selección de rasgos → posibles linajes domesticados, con cambios heredables, condiciones y consecuencias; no convertirlo en una transformación instantánea por tecnología.
- [ ] Separar la capacidad de cultivar, que puede desbloquear una tecnología, del cambio biológico de la planta.
- [ ] Validar IDs, recursos, hábitats, tecnologías y relaciones; cerrar la ficha.
- [ ] Continuar una por una con las entidades de flora que se aprueben, consultando el archivo histórico.

### D07 — Minerales: fichas completas
- [ ] Seleccionar el primer material geológico existente con su ID canónico.
- [ ] Desglosar origen, tipos de depósito, entorno geológico, localización posible y disponibilidad.
- [ ] Documentar extracción, preparación, productos, subproductos y vínculos a recursos sin habilitar recetas todavía pendientes.
- [ ] Validar vínculos y cerrar la ficha.
- [ ] Continuar individualmente con los materiales geológicos aprobados, consultando el archivo histórico.

### D08 — Validación global e integración futura
- [ ] Comprobar IDs únicos, referencias válidas y vínculos bidireccionales cuando apliquen.
- [ ] Verificar compatibilidad entre hábitats, condiciones ambientales, especies y procesos de recursos.
- [ ] Detectar entidades huérfanas, contradicciones, cantidades pendientes y duplicaciones semánticas.
- [ ] Clasificar cada regla: **documentada**, **aprobada para diseño**, **lista para implementación**, **implementada** o **pendiente**; nunca confundir estados.
- [ ] Preparar entregas específicas para Arquitecto/Dev/Auditor según la etapa de implementación.
- [ ] No cambiar contratos de partidas existentes sin un plan de migración.

## 6. Plantilla obligatoria: ficha de cada entidad

Cada entidad se revisará con esta estructura lógica (no implica que todos los campos deban convertirse inmediatamente en columnas JSON):

1. **Identidad:** ID canónico, nombre, tipo exacto, sinónimos y nivel taxonómico o geológico.
2. **Contexto real:** qué es, de dónde procede, evidencia y simplificaciones para el juego.
3. **Ambiente:** subbiomas candidatos; límites climáticos, geológicos e hídricos; distribución regional.
4. **Ciclo o formación:** desarrollo y reproducción en seres vivos; génesis y transformación física en materiales.
5. **Relaciones:** interacciones con otras entidades, el entorno, tecnologías y acciones.
6. **Recursos:** salidas posibles, partes, métodos de obtención, requisitos, subproductos, cantidades pendientes y destino.
7. **Cambios a lo largo del tiempo:** genética, selección o domesticación cuando sean seres vivos; procesos geológicos cuando sean minerales.
8. **Efectos sobre el mundo:** disponibilidad, agotamiento, recuperación, población o regeneración según tipo.
9. **Estado de implementación:** definido versus realmente ejecutado; datos faltantes y riesgos.
10. **Criterios de aceptación:** referencias válidas, coherencia ecológica, casos de prueba y aprobación explícita.

**Regla de granularidad:** completar una ficha conceptualmente antes de desarrollar la siguiente; no expandir a docenas de especies nuevas por iniciativa automática.

## 7. Qué significa «evolución» en este roadmap

- **Fauna:** evolución biológica poblacional a largo plazo y selección bajo domesticación solo cuando se diseñe una mecánica apropiada; no se fuerza a todas las especies a transformarse.
- **Flora:** reproducción, herencia, variación, selección y domesticación progresiva; las tecnologías pueden habilitar técnicas, no alterar genes por sí solas.
- **Minerales:** formación, meteorización, concentración, transporte y transformación geológica; no evolución biológica.
- **Recursos:** extracción y procesamiento distinguidos de la entidad de origen; una receta no equivale a una ley natural de aparición.

## 8. Ciclo de trabajo y control

| Paso | Acción | Responsable |
|---|---|---|
| 1 | Elegir catálogo o entidad y revisar lo que ya existe | Diseñador lógico |
| 2 | Elaborar propuesta científica y jugable, con alternativas si hacen falta | Diseñador lógico |
| 3 | Aprobar conceptos, límites y decisiones | Director del proyecto |
| 4 | Preparar cambios mínimos de datos/contratos sin romper IDs | Desarrollador, si se solicita |
| 5 | Revisar integridad, compatibilidad, pruebas y regresiones | Auditor |
| 6 | Anotar resultado, commit descriptivo y siguiente entidad en este roadmap | Responsable de la tarea |

**Estados permitidos por ficha:** `pendiente`, `en_análisis`, `propuesta`, `aprobada`, `implementada`, `auditada`. No declarar «implementada» solo porque exista un JSON.

### Registro de decisiones y avances

| Fecha | Tema | Decisión | Estado | Evidencia |
|---|---|---|---|---|
| 2026-11-08 | Flujo de trabajo | Rama `definitions` nacida de `develop` | Aplicado | Commit base `772d4e3` |
| 2026-11-08 | Alcance | Primero cuatro familias; después entidades individualmente, iniciando con fauna | Acordado | Este roadmap |
| 2026-11-08 | Trigo silvestre | Se registra como ejemplo de flora, no fauna | Catalogado; desglose pendiente | `plant_southwest_asian_wild_wheat` |
| 2026-11-08 | Reinicio de definiciones | Archivados cuatro JSON originales; activos vacíos; recurso integrado de biomas apunta temporalmente al archivo anterior | Aplicado en rama definitions | Consultar historial Git y carpeta old/ |
| 2026-11-08 | D00 — contratos base | Se documentan dependencias, vocabulario y validaciones | **Aprobado y cerrado** | [D00_CONTRATOS.md](D00_CONTRATOS.md) |
| 2026-11-08 | D01 — biomas principales | Se definen ocho biomas con `subbiomes: []`, sin spawn | Aplicado | [biomes.json](Biomas/biomes.json) |
| 2026-11-08 | D01 — subbiomas iniciales | Se incorporaron 34 subbiomas como primera propuesta, sin spawn | Sustituido por restauración del catálogo histórico | [biomes.json](Biomas/biomes.json) |
| 2026-11-08 | D01 — recuperación histórica con Montaña adaptada | Se recuperan los 8 biomas históricos; 21 subbiomas activos: Montaña rocosa y Cumbres reemplazan los 3 anteriores de Montaña | Aplicado; revisión del director pendiente | [biomes.json](Biomas/biomes.json) |
| 2026-11-09 | D01 — Selva | `forest_tropical` cambia su nombre a Selva y recibe el primer `spawn` declarativo con clima, estación seca y preferencias de altitud, humedad y latitud | Propuesta aplicada; parámetros por confirmar | [biomes.json](Biomas/biomes.json) |
| 2026-11-09 | D01 — Bosques | Los otros cuatro subbiomas de bosque reciben `spawn.required` y `spawn.preferred`; el pantano exige hidrología de humedal | Aplicado; rangos por revisar y tipar | [biomes.json](Biomas/biomes.json) |

## 9. Próximo paso inmediato

**D01 — Revisar los criterios de spawn de los cinco bosques y continuar con los demás biomas.**

Conservar los 21 subbiomas reconstruidos y revisar las reglas iniciales de los 5 bosques. **Los otros 16 permanecen sin `spawn`** hasta su diseño individual. El histórico sigue conservado en `old/`.
