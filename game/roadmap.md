# Evolve — Roadmap de la interfaz base

**Rama:** `architecture`  
**Área:** `game/`  
**Estado general:** CLOSED — Approved by the project owner.

**Fecha:** 2026-11-16

**Closure date:** 2026-11-18

**Closure version:** `v0.0.08`

**Independent audit verdict:** APPROVE WITH OBSERVATIONS; see `audit.md` for
the audited revision, accepted observations and outstanding manual checks.

## 1. Objetivo

Construir una aplicación gráfica mínima, ejecutable y mantenible para Evolve, con un **menú principal azul de estilo pixelado y botones con relieve**, inspirado en la estética de los menús de Minecraft pero con recursos y diseño propios.

Esta fase es **solo la base de la interfaz**. No incluye generación de mundos, cuadrícula, mapas, simulación, guardado ni lógica del juego.

### Resultado visual esperado

- Fondo en tonos azules oscuros, con detalles discretos.
- Título **EVOLVE** grande y destacado.
- **Versión del juego en texto pequeño inmediatamente debajo del título**, leída de los metadatos de compilación; nunca duplicar la versión en un texto fijo del menú.
- Cuatro botones, en este orden y con estas etiquetas exactas: **INICIAR**, **LAB**, **OPCIONES**, **SALIR**.
- Botones rectangulares con relieve/bordes marcados, tipografía legible de estilo pixelado y estados de interacción.
- Diseño centrado que se adapte al cambio de tamaño de ventana.
- Sin copiar texturas, fuentes ni otros recursos protegidos de Minecraft.

## 2. Technical choice — approved

Approved implementation: C# / .NET 10 with OpenTK 4.9.4. The following proposal
and approval requirement are retained as historical planning context; the gate
was satisfied by the authorized implementation and project owner acceptance.

**Propuesta del Arquitecto:** utilizar **C# / .NET 10 + OpenTK 4.x**, porque la versión anterior de Evolve (`WorldGenerationOLD`) ya usaba OpenTK 4.9.4 para la ventana y la representación 2D.

Esta elección requiere **aprobación explícita del responsable** antes de empezar a programar. El código anterior podrá servir como referencia conceptual, pero **no se migrarán componentes antiguos ni se reintroducirán dependencias de Engine/State/Definitions sin autorización**.

El sistema gráfico debe estar encapsulado en `game/`, con una estructura interna mínima, sin abstraer prematuramente un motor gráfico completo.

## 3. Roadmap por etapas

### UI-01 — Proyecto gráfico mínimo

**Estado:** COMPLETED — Accepted by the project owner.

**Dev / Codex:**
1. Tras la aprobación de la biblioteca gráfica, crear el proyecto C# ejecutable dentro de `game/`.
2. Configurar los metadatos de versión en un único lugar; inicialmente se propone **0.0.1**, sujeto a confirmación.
3. Abrir una ventana titulada `Evolve`, con un fondo azul sólido.
4. Manejar correctamente el cierre de la ventana y la liberación de recursos.

**Criterios de aceptación:** la aplicación inicia, muestra la ventana y termina limpiamente; no depende de `engine/`, `state/`, `saves/` ni `definitions/`.

### UI-02 — Presentación del menú

**Estado:** COMPLETED — Accepted by the project owner.

**Dev / Codex:**
1. Dibujar el título `EVOLVE` en grande.
2. Mostrar la versión real del ensamblado inmediatamente debajo, en tamaño menor.
3. Dibujar los cuatro botones, centrados y en el orden aprobado.
4. Aplicar una identidad visual en tonos azules, relieve rectangular y estilo pixelado original.
5. Garantizar alineación, legibilidad y escalado al redimensionar la ventana.

**Criterios de aceptación:** título, versión y las cuatro opciones se muestran correctamente, sin recortes ni solapamientos en tamaños de ventana razonables.

### UI-03 — Interacción básica

**Estado:** COMPLETED — Accepted by the project owner.

**Dev / Codex:**
1. Implementar estados visuales normales, al pasar el cursor y al pulsar.
2. Implementar clic y navegación básica con teclado, sin colocar lógica de simulación en la interfaz.
3. `INICIAR`, `LAB` y `OPCIONES` deben informar que aún no están disponibles; **no** simular funcionalidades inexistentes ni crear pantallas adicionales.
4. `SALIR` debe cerrar la aplicación de forma ordenada.

**Criterios de aceptación:** las cuatro opciones responden de forma coherente, no desencadenan lógica de juego y `SALIR` funciona.

### UI-04 — Verificación y entrega

**Estado:** COMPLETED — Accepted by the project owner.

**Dev / Codex:**
1. Incorporar verificaciones automatizadas sencillas de cualquier lógica de navegación comprobable sin GPU, si la implementación lo permite sin añadir complejidad artificial.
2. Documentar cómo compilar y ejecutar el menú mediante comandos PowerShell para el responsable del proyecto.
3. Entregar el resumen de archivos cambiados, mensaje y SHA completo del commit, así como limitaciones conocidas.

**Auditor:**
1. Revisar que el alcance se limita a la interfaz base.
2. Verificar que no hay reglas de simulación en `game/` ni cambios innecesarios en otros módulos.
3. Revisar evidencia de funcionamiento, aspecto visual, comportamiento de botones, escalado y cierre, sin afirmar pruebas que no hayan sido ejecutadas.

**Criterios de aceptación:** el Auditor emite su valoración y el responsable del proyecto decide si aprueba la etapa.

## 4. Límites y gobierno

- **Arquitecto:** define y ajusta el diseño y roadmap conforme a decisiones aprobadas.
- **Dev / Codex:** implementa solo etapas autorizadas.
- **Auditor:** comprueba los criterios de aceptación y presenta hallazgos.
- **Responsable del proyecto:** aprueba el diseño, autoriza las etapas y acepta los resultados.
- Se trabaja **una etapa a la vez**.
- No modificar `architecture.MD`, otros módulos o este roadmap sin la autorización correspondiente.
- **No avanzar hacia la generación de mundos** dentro de este roadmap.

## 5. Initial decisions — resolved

- [x] Graphics library: OpenTK 4.9.4 approved and implemented with C# / .NET 10.
- [x] Initial display version: `v0.0.01` adopted in root `VERSION`; numeric .NET
  metadata is normalized separately. The original numeric proposal `0.0.1` above
  is retained as historical context, not a second source of version truth.
- [x] Design and UI-01 implementation authorized; UI-01 through UI-04 completed
  and accepted by the project owner.

Historical planning restriction, superseded by the explicit owner closure below:

**Esta documentación es un plan de trabajo; su publicación no autoriza implementar las etapas.**

## 6. Official closure and approved interface decisions

**CLOSED — Approved by the project owner.** The owner authorized closure after
reviewing and approving the `game/` structure, including the `menu/` and
`rendering/` reorganization. Original stage tasks and acceptance criteria remain
above as the historical baseline.

| Stage | Completed and accepted result |
| --- | --- |
| UI-01 | Minimal graphical window and lifecycle. |
| UI-02 | Approved main-menu presentation and visual refinements. |
| UI-03 | Mouse and keyboard interaction, unavailable notices and graceful exit. |
| UI-04 | Automated checks, technical documentation and audit preparation. |

The following decisions are definitive for this interface roadmap:

- Evolve uses C# and .NET 10; its graphical implementation uses OpenTK 4.9.4.
- The entire application will run inside one main window, `EvolveWindow`.
  Future menus, laboratories and game screens must be presented within that
  same window. This decision does not implement navigation or new screens.
- `game/` remains separate from simulation rules.
- `game/menu/` groups the menu components; `game/rendering/` contains the
  current graphical tools.
- Every C# source file begins with English `//` documentation as required by
  root `AGENTS.md`.
- Root `VERSION` is the single source of truth for the game version, with its
  exact display string preserved in assembly informational metadata.

These decisions approve the interface implementation and its physical layout
only. They do not settle the physical organization of other modules or replace
the logical responsibilities in `architecture.MD`.

The independent audit covered `v0.0.06` at commit
`60b82a54025306367dcba40c8ba84c8ff529a5cc` and returned **APPROVE WITH
OBSERVATIONS**. The subsequent `v0.0.07` structural reorganization was approved
by the owner and verified by development; it is not represented as independently
audited. Observation dispositions and pending manual checks are recorded in
`audit.md`. Closure does not claim that those manual checks were executed and
does not authorize another roadmap or new functionality.
