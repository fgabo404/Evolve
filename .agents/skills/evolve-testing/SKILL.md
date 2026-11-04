---
name: evolve-testing
description: Crear o modificar pruebas, regresiones, validadores y diagnósticos de Evolve; revisar su coherencia estáticamente y preparar la ejecución local para el usuario.
---

# Pruebas y diagnósticos de Evolve

Usa esta skill cuando se agreguen o cambien pruebas, validadores o diagnósticos. Localiza primero la suite o clase pertinente de `GEvolve.Tests`; inspecciona sus invariantes y datos oficiales antes de escribir un caso nuevo. Reutiliza estructuras existentes y cubre el comportamiento real con entradas representativas, especialmente errores de contrato, determinismo y preservación de estado cuando corresponda.

Codex puede implementar pruebas, pero no ejecuta `dotnet build`, `dotnet run`, `dotnet test`, `dotnet restore`, proyectos, suites, benchmarks ni métodos equivalentes. Tampoco instala dependencias para poder ejecutarlos. Revisa estáticamente tipos, firmas, condiciones y el diff; deja la ejecución al usuario. Entrega comandos PowerShell exactos para la prueba específica y la suite completa, y marca los resultados como pendientes hasta recibir la salida real. No afirmes que una prueba pasó solo porque el código parece correcto.

Mantén intactos los 12 hashes tectónicos y los 36 baselines geográficos aprobados. No cambies datos esperados, umbrales ni validadores para producir un resultado favorable. Una regresión nueva debe medir el contrato que pretende proteger sin usar el mismo algoritmo para construir el resultado esperado cuando eso ocultaría defectos. Los diagnósticos experimentales deben distinguirse de criterios de aceptación aprobados.

Evita carpetas TEST, suites paralelas y artefactos gráficos o CSV. Si una prueba tarda mucho, identifica cálculos repetidos con evidencia antes de optimizar; conserva cobertura necesaria. Para generación mundial, carga también `evolve-worldgen` y consulta solo la documentación del hito activo. La validación visual del mapa corresponde al usuario dentro del juego.
