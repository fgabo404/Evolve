# D00 — Contratos, dependencias y trazabilidad

> **Rama:** `definitions` · **Fecha:** 2026-11-08  
> **Estado:** **aprobado por el director el 2026-11-08** (D00).  
> **Alcance:** diseño de datos, no implementación de reglas nuevas.  
> **Roadmap rector:** [ROADMAP.md](ROADMAP.md).

## 1. Estado y fuente de verdad

Los cuatro catálogos están **activos y en reconstrucción**. D01 ha incorporado ocho biomas y 34 subbiomas, sin requisitos de spawn:

| Catálogo activo | Campo raíz | Archivo original conservado |
|---|---|---|
| `Biomas/biomes.json` | Ocho biomas y 34 subbiomas (`id`, `name`; sin `spawn`) | `Biomas/old/biomes.json` |
| `Resources/fauna.json` | `animals: []` | `Resources/old/fauna.json` |
| `Resources/flora.json` | `plants: []` | `Resources/old/flora.json` |
| `Resources/minerals.json` | `minerals: []` | `Resources/old/minerals.json` |

El archivo histórico contiene 8 biomas / 22 subbiomas, 92 entradas de fauna, 91 de flora y 45 de minerales. Son **referencia**, no elementos activos ni cuotas que deban reintroducirse automáticamente.

`Resources/resources.json` **no se reinició**: contiene 121 recursos, 582 `sourceBindings` y 43 `processingLinks`, con la política `loadAsGameDefinitions: false`. `Technologies/agriculture.tech.json` conserva 3 tecnologías y 3 ramas visuales. Las definiciones de tecnología más antiguas están en `Technologies/agriculture.technologies.json` como prototipo histórico; no activan efectos de juego.

**Límite de responsabilidades:** el JSON declara definiciones y relaciones; los modelos C# tipados representan y validan el esquema una vez aprobado; Engine evalúa condiciones y ejecuta sistemas; State guarda lo generado o modificado por la simulación; Game y la wiki lo presentan. El generador histórico de Pangea continúa leyendo temporalmente `Biomas/old/biomes.json` embebido como `Evolve.biomes.json`. **No conectar los JSON vacíos al clasificador antiguo**.

## 2. Mapa de dependencias

```text
Biomas/biomes.json
  └─ biomes[] → subbiomes[] → spawn (gramática pendiente de D01)
                    ↑               ↑
            fauna / flora      clima, relieve e hidrología
            (hábitat futuro)   (valores calculados por Engine)

Resources/fauna.json   ──┐
Resources/flora.json   ──┼──→ Resources/resources.json
Resources/minerals.json──┘       ├─ resources[] (IDs de productos)
                                  ├─ sourceBindings[] (candidatos de origen)
                                  └─ processingLinks[] (candidatos de proceso)
                                                ↑
Technologies/agriculture.tech.json ───── requisitos y capacidades futuras
                                                ↑
                                          Engine / State / Game
```

**Tipos de referencia que deben reconciliarse al reconstruir cada ficha:**

| Origen | Destino | Significado | Estado actual |
|---|---|---|---|
| Subbioma `id` | `fauna[]`, `flora[]` | Especies que podrían habitar el entorno | Relación histórica archivada, no activa |
| Fauna / flora `spawn[]` | Subbioma `id` | Entornos de aparición potencial | Histórico, sujeto a nuevo esquema |
| Subbioma `minerals[]` y mineral `spawn[]` | Entorno | Asociación histórica de presencia | Revisar: depósito depende ante todo de geología |
| Fauna / flora / minerales `resources[].resourceId` | `resources[].id` | Posibles productos y métodos | Histórico, no implica cantidades ni inventario |
| `sourceBindings[].sourceId` | Entidad de origen | Recurso atribuido a fuente candidata | 582 vínculos de diseño; no reglas ejecutables |
| `processingLinks[]` | Recursos y tecnologías | Entradas, salidas y requisitos candidatos | 43 procesos de guía, mayoritariamente no implementados |
| `prerequisites` de tecnologías | Otra tecnología por ID | Dependencia de investigación | El nuevo árbol contiene `language`, `basic_foraging` y `plant_knowledge` |

**Trazabilidad comprobada en el archivo histórico (sin extrapolarla al nuevo):** 219 asociaciones subbioma–fauna, 122 subbioma–flora y 414 subbioma–minerales. En los catálogos archivados se corresponden con las entradas `spawn[]`; no se detectaron IDs de subbioma desconocidos ni referencias directas a entidades faltantes en esas listas.

**Referencias pendientes relevantes:** de los 582 `sourceBindings` vigentes, 498 se declaran para fauna, 15 para flora, 45 para minerales y 24 para candidatos agrícolas. Frente a los archivos históricos, **96 vínculos de fauna, procedentes de 20 IDs distintos, no coinciden directamente con las 92 entradas archivadas**; 24 candidatos agrícolas tampoco forman parte de las cuatro listas activas nuevas. Son asuntos de identidad/procedencia por revisar, **no prueba de que deban eliminarse**. Todas las cuatro listas nuevas están vacías, de modo que ningún vínculo a ellas puede considerarse reconciliado por ahora.

**Regla:** la relación `spawn` describe elegibilidad o preferencia, no una aparición garantizada. Un recurso citado describe una posibilidad de obtención; su producción requiere acciones, reglas y cantidades aprobadas.

## 3. Vocabulario canónico

| Término | Uso en Evolve |
|---|---|
| **Entidad** | Definición identificable por un ID estable: organismo, grupo biológico, mineral, recurso o tecnología. |
| **Especie** | Taxón biológico concreto; no debe confundirse con una agrupación genérica de animales o plantas. |
| **Grupo taxonómico** | Agrupación de múltiples especies usada deliberadamente cuando el detalle por especie aún no existe. |
| **Bioma** | Gran categoría ecológica basada en condiciones ambientales y comunidades características. |
| **Subbioma** | Unidad ecológica más específica dentro de un bioma; contendrá condiciones de `spawn` en el JSON. |
| **Hábitat** | Conjunto de condiciones y entornos compatibles con una entidad; no afirma población presente. |
| **Población** | Individuos de una entidad biológica existentes en un lugar y tiempo concretos; corresponde al estado del juego. |
| **Depósito** | Concentración geológica situada en un lugar, con composición y disponibilidad; no se deduce únicamente del bioma. |
| **Mineral / roca / sedimento** | Materiales geológicos de naturaleza distinta; no deben tratarse como sinónimos automáticos. |
| **Recurso** | Material o producto susceptible de obtención, transformación o almacenamiento; separado de su fuente natural. |
| **Proceso** | Transformación con entradas, condiciones y salidas; una descripción histórica no es una receta ejecutable. |
| **Condición** | Requisito evaluable frente al entorno o estado, con unidades y semántica definidas. |
| **Efecto** | Resultado autorizado de una regla, acción o tecnología; no ocurre solo por existir en el catálogo. |
| **Spawn** | Regla declarativa de **elegibilidad de aparición** (o compatibilidad), que Engine evaluará; no es necesariamente un evento de creación. |
| **Evolución / domesticación** | Cambios heredables y selección biológica a lo largo de generaciones cuando apliquen; las tecnologías habilitan prácticas, no mutaciones instantáneas. |

## 4. Validaciones mínimas y criterio de cierre

### 4.1 Validaciones estructurales de los catálogos

1. Los cuatro archivos activos deben ser JSON válido, con un arreglo raíz esperado (`biomes`, `animals`, `plants`, `minerals`). **Arreglos vacíos son válidos durante el reinicio**, no datos completos.
2. Cada ficha incorporada debe tener ID no vacío y único **dentro de su catálogo**, nombre de presentación y tipo/ámbito suficientemente claro para no confundir una especie con un grupo. Los formatos concretos nuevos se deciden en D01–D04, sin reutilizar IDs históricos para entidades diferentes.
3. Cada subbioma debe vincularse con exactamente un bioma padre. Se validarán IDs de padres y subbiomas y que no haya duplicados.
4. El bloque `spawn` solo se validará de forma estricta cuando aprobemos su gramática en D01: tipos, campos obligatorios, unidades, límites, `min ≤ max`, compatibilidad tierra/agua y semántica de valores ausentes. **No inventar umbrales ahora**.
5. Todas las referencias nuevas a subbiomas, entidades, recursos y tecnologías deben tener destinos existentes en el catálogo activo **o estar registradas explícitamente como pendientes**, sin tomarlas por relaciones jugables válidas.
6. **No activar** `sourceBindings`, `processingLinks`, tecnologías ni aparición de especies por el hecho de deserializar el JSON.
7. La copia `old/` es inmutable como referencia. Ningún cargador de definiciones activas ni la wiki debe mezclarla de forma implícita; se permite la dependencia temporal documentada del generador histórico.

### 4.2 Criterio para cerrar una ficha individual

- [ ] Identidad canónica, nombre y nivel de clasificación definidos.
- [ ] Relaciones pertinentes con ambiente/hábitat, identificadores y condiciones documentadas.
- [ ] Recursos o productos relacionados revisados, o ausencia justificada; cantidades no investigadas quedan pendientes.
- [ ] Ciclo de vida / reproducción / evolución **solo para seres vivos**, o formación geológica **solo para materiales**, cuando corresponda.
- [ ] Referencias cruzadas resueltas o enumeradas como deuda pendiente explícita, sin identificadores inventados.
- [ ] Reglas y unidades revisadas; diferenciadas entre datos, candidatos y efectos ejecutables.
- [ ] Propuesta aprobada por el director. La implementación C#, las pruebas y la auditoría son hitos posteriores y no se marcan implícitamente.

**Nota:** se puede cerrar la **fase de diseño** de una ficha con pendientes explícitos que no bloqueen su identidad y relaciones básicas. No puede declararse **lista para ejecución** si le faltan requisitos esenciales o referencias exigidas por el motor.

### 4.3 Comprobaciones futuras por implementación

Cuando exista contrato JSON tipado, agregar pruebas de deserialización y rechazo de datos malformados, rangos imposibles, duplicados, referencias activas faltantes, archivos históricos cargados por error y sustituciones indebidas de IDs. La auditoría debe verificar que Pangea y los guardados antiguos conserven sus contratos durante la transición. **No se afirma que esas pruebas nuevas existan o se hayan ejecutado hoy.**

## 5. Decisiones aún pendientes de aprobación

- **Aprobado:** vocabulario y reglas mínimas como contrato D00 por decisión explícita del director (2026-11-08).
- Diseñar en D01 el esquema definitivo `Bioma → Subbioma → spawn`, incluidas unidades ambientales, requisitos obligatorios/preferentes y selección cuando varios subbiomas sean compatibles.
- Decidir, al diseñar cada catálogo, qué propiedades específicas requieren fauna, flora y geología; no copiar automáticamente el esquema anterior.
- Reconciliar los vínculos de `resources.json` con cada entidad nueva antes de habilitar acciones.

La aprobación de D00 no aprueba automáticamente nuevas mecánicas, subbiomas ni requisitos de spawn, ni convierte guías históricas en datos ejecutables.
