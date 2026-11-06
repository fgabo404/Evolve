---
name: evolve-architecture
description: Revisar o implementar cambios de arquitectura, integración, persistencia, servicios y contratos de Evolve respetando las decisiones aprobadas.
---

# Arquitectura y contratos de Evolve

Usa esta skill cuando el cambio afecte límites entre proyectos, APIs públicas, persistencia, integración de generadores o servicios. Consulta la sección pertinente de [arquitectura](../../../GEvolve.Engine/WORLD_GENERATION_ARCHITECTURE.md) y el gate correspondiente del [roadmap](../../../GEvolve.Engine/WORLD_GENERATION_ROADMAP.md); no presupongas que una implementación experimental ya está autorizada para producción.

Respeta las responsabilidades de `GEvolve.Definitions`, `GEvolve.State`, `GEvolve.Engine`, `GEvolve.Game` y `GEvolve.Tests`. Definitions describe configuraciones y perfiles; State contiene datos persistibles; Engine ejecuta generación y validación; Game consume los resultados. Mantén la dirección de dependencias vigente y evita ciclos o modelos equivalentes duplicados. `WorldGenerationService` es el único orquestador oficial; cualquier modificación de su comportamiento requiere justificación y autorización específicas.

Prioriza interfaces, tipos y validadores existentes. Un contrato nuevo debe precisar entradas válidas, estados parciales y compatibilidad de carga. No reinterpretes silenciosamente partidas guardadas ni versiones desconocidas. Tras la retirada de H05, `Continents` permanece en `SurfaceGenerationPending` y no tiene contrato cortical experimental activo. Conserva los contratos históricos de persistencia, evita migraciones implícitas y no reinstales `shared-cortex-1` sin autorización. Un vínculo geológico derivado no certifica continuidad de superficie ni un evento histórico simulado.

Acota los cambios a los archivos necesarios, identifica consumidores antes de alterar una API y revisa el diff para detectar efectos laterales. Si el diseño exige una decisión nueva, presenta evidencia y deja el hito pendiente de auditoría. Codex no ejecuta compilaciones o pruebas; proporciona comandos exactos al usuario y no declara validación lograda sin resultados aportados. Si se modifican pruebas o generación, usa además la skill especializada pertinente.
