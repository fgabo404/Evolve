# Pruebas y utilidades históricas fuera del flujo oficial

La versión previa de commands.ps1 se conserva aquí sin cambios como referencia; no es el procedimiento de validación vigente. Los snapshots de informes tampoco se alteran. Invocar suites históricas con el runner oficial nuevo produce un error explícito, nunca un fallback.

Las fuentes C# antiguas permanecen en sus rutas originales en GEvolve.Tests, incluido Program.cs; no se compilan. GEvolve.Tests.csproj desactiva la inclusión automática y admite exclusivamente Active/**/*.cs. Los baselines y recursos se conservan íntegros. No se crea un proyecto alternativo que vuelva a ejecutar el motor antiguo.

Quedan archivadas las pruebas de mundos/geografía, Grid con HexTopology histórico, tectónica, pendientes, clima, biomas, cordilleras/elevaciones, sesiones Pangea, laboratorio/capas/comparador y edición ligada a los controladores antiguos. Las nuevas pruebas de Grid y edición ejercen los modelos activos, sin copiar algoritmos geográficos.

Los resultados históricos, incluidos 17 PASS/2 FAIL, siguen siendo históricos: no son una validación del sistema nuevo ni se consideran corregidos por excluirlo. Procedimiento actual: [índice de informes](../ValidationReports/README.md).
