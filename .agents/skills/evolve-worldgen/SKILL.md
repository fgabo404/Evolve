---
name: evolve-worldgen
description: Trabajar en generación mundial de Evolve, incluida topología, tectónica, corteza, continentes, superficie y relieve; aplicar sus invariantes sin activar otras áreas del juego.
---

# Generación mundial de Evolve

Usa esta skill cuando una tarea afecte la generación procedural o sus contratos geográficos. Consulta el tramo del hito activo en [arquitectura](../../../GEvolve.Engine/World/WorldGeneration/README.md) y [roadmap](../../../GEvolve.Engine/World/WorldGeneration/ROADMAP.md); abre secciones adicionales solo cuando una dependencia concreta lo requiera. Verifica el estado vigente antes de tratar una propuesta como aprobada.

R02 excluye worldgenerationold del build y desconecta todas sus rutas. No hay API de generación geográfica activa ni fallback: Game solo muestra la cuadrícula compartida. El futuro orquestador de R03 tendrá namespace GEvolve.Engine.World.WorldGeneration. Ningún código nuevo puede invocar algoritmos históricos sin una revisión y autorización explícitas de rescate. Aprovecha los generadores y contratos especializados existentes; no introduzcas un pipeline paralelo. Las cuadrículas oficiales mantienen la relación lógica aproximada 3:2, hexágonos flat-top, continuidad E/O y bordes N/S abiertos. Usa `HexTopology` para vecindad y distancia cuando el contrato lo requiera. Conserva las 16 placas universales y los hashes tectónicos aprobados.

Distingue siempre placa tectónica, corteza, fragmento, provincia, sistema geológico y masa terrestre emergida. Una placa no determina por sí sola tierra u océano. La corteza continental puede estar sumergida y un sistema de fragmentos no equivale a un continente visible. `Continents` conserva superficie pendiente hasta que una fase aprobada la genere. Los experimentos H05 de corteza y superficie de Continents fueron retirados; no los restaures por inferencia. En R02 el archivo histórico no se compila ni ejecuta: solo se muestra Grid, sin geografía ficticia ni calibraciones. R03 y cualquier rescate de algoritmos requieren contratos y aprobación separados.

Preserva determinismo por semilla e intento. No cambies dimensiones, perfiles, intentos, generadores históricos, baselines geográficos ni fórmulas tectónicas sin autorización específica. Si aparece una discrepancia, registra tamaño, tipo, semilla, IDs y resultado observado antes de proponer cambios. Mantén separadas las rutas vigentes de Pangaea, Archipelagos y SmallContinents mientras su migración no esté aprobada.

Para tareas que también escriban regresiones, aplica `evolve-testing`. Codex prepara el código y la revisión estática; el usuario realiza compilación, pruebas y observación visual dentro del juego. En la entrega identifica las dependencias pendientes de evidencia y los comandos locales necesarios.
