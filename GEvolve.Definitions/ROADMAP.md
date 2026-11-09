# Evolve — Roadmap único de definiciones y entidades

> **Repositorio:** `fgabo404/Evolve`  
> **Rama de trabajo:** `definitions` (creada desde `develop`)  
> **Origen Git:** `772d4e34b596fe7db524b047aced16fd55ba76d2` — `docs(worldgen): record synchronized development branch and pending cleanup`  
> **Fecha de inicio:** 2026-11-08  
> **Estado:** D00 aprobado; D01 en curso con 8 biomas históricos y 21 subbiomas (Montaña adaptada a 2), sin spawn; fauna, flora y minerales vacíos  
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

**Nuevo estado de D01:** se recuperó el catálogo histórico de 8 biomas y sus subbiomas sin condiciones ni relaciones; la única excepción es Montaña, que ahora tiene solo Montaña rocosa (`mountain_rocky`) y Cumbres (`mountain_summits`). Total activo: **21 subbiomas**. Los 8/22 anteriores siguen intactos en `old/`.

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
- [ ] Revisar y aprobar el catálogo recuperado y la excepción Montaña antes de desarrollar spawn.
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

Se mantiene la estructura acordada `Bioma → Subbiomas`. Todos los elementos contienen **solo** `id` y `name` (es/en). No se incluyó `spawn`, condiciones ambientales, fauna, flora ni minerales.

| Bioma | Subbiomas |
|---|---|
| Desierto (`desert`) | Arenoso (dunas) (`desert_sandy`); Rocoso (cañones y mesetas) (`desert_rocky`); Salar (`desert_salt_flat`) |
| Bosque (`forest`) | Tropical (selva) (`forest_tropical`); Subtropical (`forest_subtropical`); Templado (`forest_temperate`); Boreal (taiga) (`forest_boreal`); Pantano arbolado (Everglades) (`forest_wooded_swamp`) |
| Llanuras (`plains`) | Sabana (`plains_savanna`); Pradera (`plains_prairie`); Estepa (`plains_steppe`); Humedal abierto (Pantanal) (`plains_open_wetland`) |
| Marino (`marine`) | Arrecife (`marine_reef`); Estuario o litoral (`marine_estuary_littoral`); Manglar (`marine_mangrove`); Océano abierto (alta mar) (`marine_open_ocean`) |
| Tundra (`tundra`) | Tundra (`tundra_tundra`) |
| Montaña (`mountain`) | Montaña rocosa (`mountain_rocky`); Cumbres (`mountain_summits`) |
| Polo Norte (`polar_north`) | Hielo polar (`polar_north_ice`) |
| Polo Sur (`polar_south`) | Hielo polar (`polar_south_ice`) |

**Único cambio deliberado respecto al histórico:** Montaña deja de tener `mountain_alpine` y `mountain_cliffs` y conserva `mountain_rocky`, que se presenta como Montaña rocosa. Se incorpora `mountain_summits` (Cumbres). Los IDs eliminados del catálogo activo permanecen en `old/`; no se reasignan. Los ecosistemas forestales permanecen en Bosque.

Los 8 biomas históricos / 22 subbiomas siguen archivados íntegramente en `Biomas/old/biomes.json`. La nueva lista activa tiene 8 biomas / 21 subbiomas. La clasificación está en **revisión de diseño**; el motor antiguo continúa usando el archivo histórico integrado hasta una migración aprobada.

**Pendiente:** aprobación del catálogo y, en una etapa posterior, requisitos de `spawn` y reconciliación de vínculos históricos.

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

## 9. Próximo paso inmediato

**D01 — Revisar los subbiomas enumerados; los requisitos de spawn se diseñarán por separado.**

Revisar la lista recuperada de 21 subbiomas: conservar Desierto (arenoso, rocoso, salar) y los restantes grupos históricos; en Montaña mantener únicamente Montaña rocosa y Cumbres. El histórico de 22 se conserva en `old/`. **No añadir `spawn` hasta que se solicite expresamente**.
