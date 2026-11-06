# Evolve — Roadmap operativo de Pangea

> **Rama:** `worldgeneration_pangea`  
> **Rama de origen:** `worldgenerator`  
> **Base limpia validada:** `a58d5689c17f1fea045066c10a400dbbe9a35976` — `refactor(worldgen): revert experimental continents generation and preserve pangaea`  
> **Documento general:** [WORLD_GENERATION_ROADMAP.md](WORLD_GENERATION_ROADMAP.md)  
> **Estado al 2026-11-05:** P03-A implementado; pendiente de compilación, suite, revisión visual y rendimiento locales.

## Objetivo

Completar **Pangea** antes de ampliar o adaptar las capas a Continents, Archipelagos y SmallContinents. Conservar la forma actual de Pangea, el rendimiento, los cuatro tamaños hexagonales, el wrap E/O, los límites abiertos N/S, las 16 placas y sus movimientos, y los catálogos JSON. Mantener **una sola generación individual por tipo, tamaño y semilla**, con etapas especializadas y contratos compartidos.

No restaurar los experimentos corticales H05 retirados. `Continents` continúa en estado `SurfaceGenerationPending`; los otros generadores históricos quedan protegidos.

## Cómo llevar el control

Cada hito puede estar en uno de estos estados:

- **Base funcional:** existe código previo; no implica que toda la capa esté finalizada.
- **Pendiente:** aún no comienza el trabajo específico de Pangea.
- **En desarrollo:** implementación autorizada, sin prueba local aprobada.
- **Implementado / pendiente de validación:** existe un commit, falta la comprobación del usuario.
- **Aprobado:** el usuario confirmó compilación, tests, integridad de baselines y revisión visual cuando corresponda.
- **Bloqueado:** hay un fallo documentado que impide continuar.

No marcar `[x]` por el solo hecho de crear una clase o publicar un commit. Para cerrar un hito consignar mensaje de commit, SHA completo, pruebas locales realizadas, resultados y decisión visual. Actualizar **este documento en el repositorio** en cada cierre o cambio de alcance; alinear las síntesis del roadmap general y `.agents/PROJECT_MEMORY.md` sin duplicar especificaciones detalladas.

## Matriz principal

| Hito | Alcance | Estado | Evidencia / próximo paso |
|---|---|---|---|
| P01 | Cuadrícula, 16 placas, límites y movimiento tectónico | **Base funcional aprobada** | H01–H04; conservar hashes y topología |
| P02 | Superficie actual de Pangea, costas y masa conectada | **Base funcional aprobada** | 36 baselines históricos y geometría valorada por usuario; no cambiar |
| P03 | Accidentes geográficos terrestres | **En desarrollo** | P03-A implementado; validación y aprobación pendientes |
| P04 | Accidentes submarinos y batimetría | Pendiente | Plataformas, taludes, llanuras abisales, dorsales, fosas |
| P05 | Consolidar elevaciones y pendientes | Pendiente | Tierra 1–100, océano 0 en contrato actual; profundidad separada |
| P06 | Temperatura sobre geografía definitiva | **Base funcional; integración/calibración pendiente** | Conservar módulos y validar latitud/altura/mar |
| P07 | Precipitaciones y humedad | **Base funcional; integración/calibración pendiente** | Revisar sesgo seco, vientos y sombra de lluvia |
| P08 | Hidrología | Pendiente | Escorrentía, cuencas, ríos, lagos y conectividad |
| P09 | Biomas y subbiomas | **Base funcional; integración/calibración pendiente** | Clasificación desde catálogo JSON, altitud, humedad, pendiente y clima |
| P10 | Recursos naturales | Pendiente | Flora, fauna y minerales desde catálogos y condiciones |
| P11 | Integración, persistencia y cierre | Pendiente | Rendimiento, 4 tamaños, semillas, guardado y auditoría visual |

## P03 — Accidentes geográficos terrestres

| Subhito | Resultado esperado | Estado |
|---|---|---|
| **P03-A — Cordilleras** | Sistemas montañosos regionales continuos, ejes conectados, anchuras y alturas variables, coherentes con evidencia convergente de H04 | **Implementado / pendiente de validación** |
| P03-B — Montañas aisladas | Macizos y cumbres diferenciables de cordilleras | Pendiente |
| P03-C — Mesetas y colinas | Relieve elevado amplio y ondulaciones regionales | Pendiente |
| P03-D — Valles y cuencas | Depresiones coherentes con el relieve circundante | Pendiente |
| P03-E — Rifts, escarpes y fallas | Deformación relacionada con extensión y cizalla verificadas | Pendiente |
| P03-F — Volcanes | Volcanismo coherente, sin inferir subducción sin evidencia cortical suficiente | Pendiente |
| P03-G — Consolidación | Resolver superposiciones y mantener contratos de altura y biomas | Pendiente |

### Contrato de aceptación P03-A

- [ ] Implementación publicada **solo en** `worldgeneration_pangea`, sin modificar los otros generadores.
- [ ] Identificar cordilleras con IDs y ejes conectados por vecinos hexagonales reales; preservar seam E/O y límites N/S.
- [ ] Evidencia tectónica de H04 utilizada sin afirmar colisiones continente–continente no verificables.
- [ ] Superficie de Pangea **idéntica** a la base por tamaño/semilla; ninguna celda pasa de tierra a océano o viceversa.
- [ ] Placas, velocidades, interacciones y campos de esfuerzo **idénticos** a la base.
- [ ] Elevaciones terrestres de 1–100; océano 0. Cumbres y laderas diferenciadas, sin saturación generalizada.
- [ ] No modificar catálogos JSON, clima, precipitación, biomas ni batimetría en este subhito.
- [ ] Documentar cambios previstos en hashes de elevación; **no actualizar baselines históricos sin aprobación explícita**.
- [ ] Compilación local, suite específica y pruebas generales aprobadas por el usuario.
- [ ] Inspección visual dentro de Evolve aprobada: cadenas regionales reconocibles, no líneas artificiales.
- [ ] Rendimiento registrado para los cuatro tamaños; sin solucionadores globales costosos.
- [ ] Commit y SHA documentados; no iniciar P03-B antes del cierre de P03-A.

### Registro de entregas

| Fecha | Hito | Estado / decisión | Commit | Evidencia |
|---|---|---|---|---|
| 2026-11-05 | Base | Limpieza H05 validada; retomamos Pangea | `a58d5689c17f1fea045066c10a400dbbe9a35976` | Build, suite y 36 baselines aprobados por usuario |
| 2026-11-05 | P03-A | Implementado; sin aprobación todavía | `feat(worldgen): add tectonic mountain range systems to Pangaea` (SHA en la entrega) | Pendientes build, suite P03-A, suite general, hashes de elevación, tiempos e inspección visual en Evolve |

### Entrega P03-A pendiente de validación

Los ejes se extraen de contactos H04 convergentes o mixtos con convergencia de al menos 1 cm/año, dominante frente a la cizalla y con tierra a ambos lados. Se agrupan por par de placas y vecindad hexagonal; los componentes cortos se descartan. Cada sistema conserva ID, eje ordenado, anchura local e intensidad. El levantamiento se propaga localmente sobre tierra, se atenúa hacia las laderas y se suma al relieve inicial sin renormalizar el mapa ni alterar la superficie. El modo de geología muestra las áreas y ejes de Pangea.

Limitaciones: H04 no identifica corteza continental ni la frontera causante del máximo de compresión por celda; las cordilleras son sistemas favorecidos por convergencia, no colisiones continentales certificadas. La selección de ejes y su aspecto requieren revisión visual y medición de rendimiento local. Se esperan cambios en hashes de elevación de Pangea; los baselines históricos permanecen intactos hasta aprobación explícita. Los guardados anteriores sin metadatos de cordilleras siguen admitidos.

## Arquitectura protegida y dependencias

```text
Configuración (tipo + tamaño + semilla)
  -> Cuadrícula / topología hexagonal
  -> 16 placas tectónicas
  -> Movimientos, fronteras e interacciones H04
  -> Estrategia de superficie de Pangea [CONSERVAR]
  -> Elevación inicial de Pangea [BASE]
  -> Accidentes geográficos terrestres P03 (en subhitos)
  -> Batimetría P04 y consolidación de relieve P05
  -> Temperatura P06
  -> Precipitaciones / humedad P07
  -> Hidrología P08
  -> Biomas del catálogo P09
  -> Recursos del catálogo P10
  -> Validación, persistencia y consumo por Game P11
```

El diagrama representa la **secuencia objetivo**, no afirma que todos los módulos estén integrados hoy en el orquestador. En la implementación actual, Pangea produce superficie y elevación juntas; temperatura, lluvia y biomas existen como módulos especializados separados. La introducción de P03 debe conservar la salida actual y evitar acoplar los demás tipos de mapa.

## Reglas de trabajo

1. Trabajar en entregas pequeñas: un subhito por autorización.
2. El **usuario** ejecuta `dotnet build`, `dotnet run --project GEvolve.Tests` y `dotnet run --project GEvolve.Game`; agentes de desarrollo no ejecutan compilaciones ni pruebas.
3. Los cambios de superficie y tectónica están prohibidos salvo aprobación expresa. Los hashes de elevación pueden cambiar con el relieve, pero deben auditarse antes de aceptar nuevos baselines.
4. No crear mapas PNG/SVG/CSV/HTML ni laboratorios externos. Inspección visual en Evolve.
5. La escala actual es tierra 1–100, océano 0; la batimetría tendrá representación separada sin redefinir el cero silenciosamente.
6. El cierre del roadmap requiere evidencia técnica **y** evaluación visual. Evitar adoptar un modelo solo porque pasa los tests.
