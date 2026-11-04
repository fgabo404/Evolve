# Instrucciones de Codex para Evolve

## Responsabilidades y autorización

Codex implementa cambios de código y pruebas. El usuario ejecuta localmente compilaciones, suites, mediciones de rendimiento y validaciones visuales. Un auditor externo revisa la evidencia técnica; el usuario decide si aprueba y publica cada hito. Codex no aprueba hitos arquitectónicos por sí mismo ni altera decisiones ya aprobadas sin autorización explícita. Trabajar en la rama autorizada; comprobar rama y estado antes de editar y respetar cambios ajenos.

## Ejecución reservada al usuario

Codex **no ejecuta** `dotnet build`, `dotnet run`, `dotnet test`, `dotnet restore`, proyectos, suites, compilaciones equivalentes, benchmarks, perfiles de rendimiento ni scripts o comandos que los ejecuten directa o indirectamente. Tampoco usa otros runtimes para probar el juego. Puede inspeccionar, escribir pruebas y revisar estáticamente código, Git y diffs. No instala dependencias para validar. Al entregar una implementación, proporciona los comandos PowerShell exactos para que el usuario valide. Solo declara compilación o pruebas aprobadas cuando el usuario ha aportado la salida correspondiente.

## Fuente de verdad y alcance

Priorizar: (1) instrucciones explícitas de la tarea; (2) contratos y documentos oficiales vigentes; (3) [memoria operativa](.agents/PROJECT_MEMORY.md); (4) skills pertinentes; (5) código como implementación efectiva. Si la memoria discrepa de documentos o código, verificar el punto y corregirla; una nota antigua nunca autoriza cambiar código aprobado. Respetar [arquitectura](GEvolve.Engine/WORLD_GENERATION_ARCHITECTURE.md) y [roadmap](GEvolve.Engine/WORLD_GENERATION_ROADMAP.md). Preservar contratos públicos y partidas existentes salvo autorización, y no cambiar baselines o validadores para ocultar fallos. Evitar PNG, SVG, JPG, CSV, HTML, mapas externos, carpetas TEST y archivos temporales persistentes.

## Trabajo eficiente

Consultar solo archivos y secciones necesarios; no repetir lecturas o búsquedas si la información sigue vigente. Para tareas complejas, identificar archivos afectados, revisar referencias mínimas, implementar cambios relacionados y examinar el diff al terminar. Ampliar la inspección solo ante evidencia nueva. No reformatear archivos ajenos, duplicar implementaciones, crear abstracciones innecesarias ni regenerar documentación sin cambio sustancial. La eficiencia no justifica omitir contratos, pruebas necesarias o validaciones estáticas, silenciar excepciones ni inventar resultados.

Usar únicamente skills pertinentes. Actualizar la memoria solo ante decisiones o estado relevante, sustituyendo datos obsoletos. No cambiar de rama, hacer pull, commit o push por rutina: publicar solo cuando la tarea lo indique. Antes de publicar, comprobar alcance y archivos temporales. En la entrega indicar mensaje y SHA completo de 40 caracteres del commit cuando exista; si no se creó, decirlo expresamente.
