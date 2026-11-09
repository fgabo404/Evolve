# Evolve — Roadmap único de generación mundial

> **Rama de trabajo:** `worldgeneration`  
> **Documento rector:** este archivo es la única hoja de ruta activa del generador.  
> **Fecha de reorganización:** 2026-11-08  
> **Estado:** R00 y R01 cerrados por auditoría y aprobación del director, con las limitaciones históricas documentadas. **R02 en curso**: pruebas automatizadas del laboratorio PASS en R01; revisión visual manual y contrato de snapshot pendientes. La suite general permanece en 17 PASS / 2 FAIL históricos aceptados para R01 (NO es PASS).

## Objetivo

Desarrollar el generador oficial de mundos de Evolve por etapas, conservando el código anterior y el laboratorio visual. No se reinicia el conocimiento ni se copian algoritmos indiscriminadamente. Cada componente antiguo se evalúa y, si conviene, se adapta al contrato nuevo.

## Arquitectura y reglas

- **Nuevo motor:** `GEvolve.Engine/World/WorldGeneration/`. No usar nombres con números de versión.
- **Motor anterior:** `GEvolve.Engine/World/worldgenerationold/`. Se conservan temporalmente los namespaces y APIs para que Game y las pruebas antiguas sigan funcionando. Sigue compilándose durante la transición.
- **Cuadrícula compartida:** `GEvolve.Engine/Grid/`; no duplicar geometría, celdas, vecinos ni wrap horizontal.
- **Definitions / State:** conservar contratos y guardados actuales hasta una migración explícita, probada y aprobada.
- **Laboratorio:** conservar `PangaeaLaboratory`, sus vistas, snapshots y renderizador; conectar gradualmente el nuevo generador sin sacrificar el anterior.
- **Separación:** el código nuevo no invoca generadores ni algoritmos dentro de `worldgenerationold/`. Los elementos reutilizados se extraen/adaptan deliberadamente y se validan.
- **Semillas y capas:** generación determinista, identidad de celda estable, sin atribuir al océano valores de una etapa pendiente. Una sola elevación final oficial; campos parciales son diagnósticos.
- **Proceso:** Arquitecto define contratos; Dev implementa; Auditor revisa; Director aprueba. No marcar un hito como validado sin evidencia.
- **Comprobación:** el usuario ejecuta localmente compilación, pruebas y validación visual; no declarar aprobación automática.

## Roadmap técnico

### R00 — Consolidación de ramas e inventario
- [x] Inventariar referencias locales/remotas actuales e identificar el scaffold histórico V2; límites de referencias desaparecidas documentados en el [informe Git](../../../GEvolve.Tests/ValidationReports/R00-R01_GIT_AUDIT.md).
- [x] Crear la rama `worldgeneration` desde la base funcional más reciente de Pangea.
- [x] Incorporar al historial de `worldgeneration` el commit del antiguo scaffold, sin conservar sus nombres V2 en el árbol operativo.
- [x] Sincronizar `develop` por avance rápido con `worldgeneration` (sin alterar `master`).
- [x] Verificar que los tips locales `worldgenerator` y `worldgeneration_pangea` son ancestros de `worldgeneration`; sus referencias remotas ya no existen. No eliminar sus referencias locales ni ninguna rama de respaldo.
- [x] Documentar y aceptar formalmente la limitación histórica: no es posible certificar las puntas finales de `worldgeneration_v2` y ramas de respaldo desaparecidas. Los commits conocidos de scaffold y C1 son alcanzables; no se afirma que posibles commits desconocidos estén preservados. El director acepta esta incertidumbre para cerrar R00 (2026-11-09).
- [x] Revisión del auditor y aprobación del director de R00, con la limitación anterior aceptada. `origin/definitions` contiene trabajo separado y queda fuera de esta consolidación; no declarar todo el repositorio integrado.

### R01 — Aislamiento del generador antiguo
- [x] Trasladar 34 fuentes C# de `GEvolve.Engine/World/` a `World/worldgenerationold/` conservando contenido y namespaces.
- [x] Preservar la geometría compartida en `GEvolve.Engine/Grid/`.
- [x] Archivar documentación técnica antigua y eliminar los roadmaps anteriores de la rama de trabajo.
- [x] Crear `WorldGeneration/` con documentación básica y esta hoja de ruta, sin implementar algoritmos.
- [x] Verificación estática: 34 pares de blobs Git idénticos, namespaces conservados, fuentes originales retiradas de sus rutas previas, inclusión SDK sin exclusiones nuevas y enlaces corregidos; [evidencia](../../../GEvolve.Tests/ValidationReports/R00-R01_GIT_AUDIT.md).
- [x] Compilación local y pruebas focales verificadas por el usuario sobre `d6abbc4ee9651e14b1f6783e5c9f7f4865c06c42`: build PASS (0 advertencias), comparador PASS, Pangea por etapas PASS y laboratorio automatizado PASS (2026-11-09 UTC).
- [x] Diagnóstico estático de los fallos de baselines recibido y revisado por el auditor: `TectonicMotion` afecta 4 casos; `ContinentalGeology`, 12 mundos distintos (los 4 primeros están incluidos). Los mismos conjuntos de fallos aparecen [antes de R01](../../../GEvolve.Tests/ValidationReports/P03-A4_20261106_140522345Z_f747ba65/SUMMARY.md), sobre `d0658fe889edb5c0754893e19d770e8d9ed1eb03`. Codex atribuye la discrepancia a referencias de elevación anteriores a las cordilleras/calibraciones P03; no hay indicios de que R01 la introdujera. Riesgo bajo para el traslado, medio para la garantía global de regresión de elevaciones. El diagnóstico no equivale a validar las nuevas alturas.
- [x] Excepción R01 **aprobada explícitamente por el director (2026-11-09)**: se aceptan para el alcance exclusivo del traslado los dos fallos históricos de baselines (`TectonicMotion`, `ContinentalGeology`). La suite general **sigue fallando: 17 PASS / 2 FAIL**. No se aprueban las nuevas elevaciones ni se actualizan baselines para ocultar discrepancias. El contrato/baselines de elevación queda como deuda técnica pendiente de revisión separada. Evidencia local comunicada por el usuario: `GEvolve.Tests/ValidationReports/R00-R01_20261109_041729114Z_167839a8/SUMMARY.md` (aún no publicada).
- [x] Revisión del auditor concluida; aprobación explícita del director para cerrar R01 con la excepción anterior. No equivale a aprobación visual ni a aprobación de la suite general.

### R02 — Laboratorio preservado
**Estado:** en curso desde 2026-11-09, sin aprobación visual todavía. No modificar el generador histórico durante esta verificación.
- [ ] Verificar apertura, semilla, selección de tamaño, ejecución manual por etapas, vistas, inspector y controles del laboratorio de Pangea.
- [ ] Confirmar que la misma configuración/semilla conserva los resultados y las referencias históricas.
- [ ] Definir con el arquitecto un contrato de snapshot para el generador nuevo, sin modificar todavía la interfaz antigua.

**Protocolo de validación R02:** ejecutar localmente `dotnet run --project GEvolve.Game`, entrar en **LABORATORIO DE PANGEA** y revisar apertura, tamaño y semilla, etapas manuales 01–08, disponibilidad de vistas, inspector, comparador BASE, zoom/desplazamiento y controles de reinicio/cancelación. Repetir la misma semilla y tamaño para observar estabilidad, y cambiar semilla/tamaño para verificar invalidación de capas. Registrar anomalías y evidencia visual; las pruebas automatizadas PASS de R01 no sustituyen esta revisión interactiva.

### R03 — Núcleo oficial mínimo
- [ ] Definir entradas, estados temporales y salidas canónicas con el arquitecto.
- [ ] Construir un único orquestador `WorldGenerationService` en el namespace nuevo.
- [ ] Establecer dependencias e invalidación de etapas, errores, cancelación y semillas aisladas.
- [ ] Añadir tests de contrato y determinismo. No duplicar `PangaeaGenerationSession` sin revisión.

### R04 — Cuadrícula y tectónica
- [ ] Reutilizar Grid y auditar `HexTopology` antes de extraerlo.
- [ ] Adaptar placas, movimientos y contactos necesarios desde `worldgenerationold/`, conservando una sola cinemática oficial.
- [ ] Verificar seam este-oeste, límites norte-sur, identidad por celda, coherencia y determinismo.
- [ ] Inspeccionar placas y esfuerzos en el laboratorio.

### R05 — Corteza y tipos de mundo
- [ ] Definir explícitamente corteza continental y oceánica, diferenciándolas de placas y masas emergidas.
- [ ] Implementar primero la estrategia Pangea y validarla visualmente.
- [ ] Adaptar después continentes y archipiélagos mediante estrategias separadas.
- [ ] Mantener perfiles y reglas en Definitions, no en el renderizador.

### R06 — Relieve y superficie
- [ ] Integrar regiones geológicas, montañas, fosas y una elevación canónica.
- [ ] Separar contribuciones geológicas y resultado final, sin alturas oficiales paralelas.
- [ ] Clasificar tierra/océano desde altura y nivel del mar; pendientes derivadas después.
- [ ] Validar coherencia física, conectividad y resultados visuales.

### R07 — Clima, agua y biomas
- [ ] Adaptar temperatura y precipitación/humedad a los contratos nuevos.
- [ ] Diseñar drenajes, cuencas, ríos y lagos.
- [ ] Clasificar biomas y subbiomas usando clima, relieve e hidrología.
- [ ] Validar catálogos JSON, determinismo y transición entre regiones.

### R08 — Ecología y recursos
- [ ] Integrar minerales, flora y fauna desde sus catálogos existentes.
- [ ] Distinguir disponibilidad potencial de colocación efectiva.
- [ ] Añadir pruebas de distribución y restricciones de hábitat.

### R09 — Integración oficial
- [ ] Conectar el generador nuevo con el laboratorio y, posteriormente, el flujo normal del juego.
- [ ] Validar compatibilidad de partidas o definir una migración explícita.
- [ ] Ejecutar las pruebas de regresión y validar visualmente todos los tipos de mundo.
- [ ] Dejar de usar el generador anterior solo cuando esté aprobado, sin borrar el historial de Git.

## Criterios de aceptación de cada etapa

1. Contrato de entradas, salidas, dependencias y errores documentado.
2. Código implementado sin dependencia nueva hacia `worldgenerationold/`.
3. Pruebas adecuadas y determinismo comprobados por el usuario.
4. Diagnósticos útiles en laboratorio cuando corresponda.
5. Auditoría técnica revisada y aprobación explícita del director.
6. Commit descriptivo y enlace a evidencia incluidos en este documento.

## Registro vivo de decisiones

| Fecha | Decisión | Estado / evidencia |
|---|---|---|
| 2026-11-08 | Conservar namespaces antiguos tras mover archivos | Aplicado; compilación local PASS sobre `d6abbc4e` el 2026-11-09 UTC |
| 2026-11-08 | Mantener el laboratorio y Grid compartidos | Sin cambios funcionales previstos |
| 2026-11-08 | Una sola hoja de ruta sin sufijos de versión | Este archivo |
| 2026-11-08 | Sincronizar `develop` con `worldgeneration` | Avance rápido aplicado; `master` permanece inalterada |
| 2026-11-08 | Construir etapas nuevas solo cuando se necesiten | Aprobado como criterio arquitectónico |
| 2026-11-09 UTC | Verificación Git R00 sobre 772d4e34b596fe7db524b047aced16fd55ba76d2 | develop/worldgeneration sincronizadas al inicio, master preservada; trabajo separado en definitions y puntas históricas ausentes documentados en el [informe](../../../GEvolve.Tests/ValidationReports/R00-R01_GIT_AUDIT.md). R00 no aprobado |
| 2026-11-09 UTC | Revisión estática R01 y corrección de referencias | 34/34 blobs idénticos, siete enlaces reparados y guardia de validación alineada |
| 2026-11-09 UTC | Validación local R00–R01 comunicada por el usuario | Build, comparador, Pangea por etapas y laboratorio automatizado PASS; suite general: 17 PASS / 2 FAIL (`TectonicMotion` y `ContinentalGeology`, mismos fallos de baselines registrados antes de la reorganización). Informe local `R00-R01_20261109_041729114Z_167839a8/SUMMARY.md`; pendiente de publicación y aprobación. No acredita validación visual de la ventana. |
| 2026-11-09 UTC | Diagnóstico de Codex sobre regresión histórica; dictamen del auditor | `TectonicMotion`: 4 casos; `ContinentalGeology`: 12 mundos, incluidos los cuatro anteriores. Mismatches previos a R01 ya registrados en [P03-A4](../../../GEvolve.Tests/ValidationReports/P03-A4_20261106_140522345Z_f747ba65/SUMMARY.md). Codex informa comparación de hashes históricos 32/32 y equivalencia 40/40 contra informe previo; no se ha demostrado regresión nueva por el traslado. **Recomendada** excepción limitada para R01, pendiente de decisión del director; no aprobar calidad global de elevaciones ni modificar baselines. |
| 2026-11-09 | Cierre de R00 aprobado por el director | Se acepta expresamente la incertidumbre sobre los tips desconocidos de ramas históricas desaparecidas, sin atribuirles preservación no probada. Auditoría de consolidación concluida; `develop` y `worldgeneration` sincronizadas en la revisión y `master` preservada. |
| 2026-11-09 | Cierre de R01 aprobado por el director | Aprobado el aislamiento del código histórico con excepción explícita para dos fallos de baselines preexistentes. Compilación y pruebas focales PASS; general **17 PASS / 2 FAIL**. Revisión futura del contrato de elevaciones sin cambios a baselines ni algoritmos. Laboratorio visual pendiente para R02. |
| 2026-11-09 | Inicio autorizado de R02 | Director solicita avanzar a R02. El auditor registra hito en curso y protocolo de validación visual; las tres casillas permanecen abiertas hasta recibir evidencia y definir el contrato de snapshot. |

**Para actualizarlo:** modificar las casillas del hito afectado, añadir commit y evidencia real en el registro y anotar cambios de contrato. Nunca duplicar el roadmap en otro directorio.
