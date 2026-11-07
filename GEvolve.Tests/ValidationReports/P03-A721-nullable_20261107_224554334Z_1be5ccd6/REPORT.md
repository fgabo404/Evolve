# P03-A.7.2.1 — Comprobación de la guarda nullable de pruebas

Fuente limpia: `5bfbfc96bcb896615679d78dabe091f60d308164`, `fix(tests): guard retained laboratory base before inspection`. [Resumen y entorno](SUMMARY.md), [build completo](build.log), [laboratorio completo](pangaea-laboratory.log), [script](commands.ps1).

La ejecución amplia de [P03-A.7.2.1](../P03-A721_20261107_223547253Z_a9387211/REPORT.md) pasó laboratorio y equivalencia; la general tuvo 16 PASS y los dos fallos históricos, y build emitió CS8602 en una prueba. El único cambio C# posterior es guardar BASE con una comprobación explícita de existencia antes del bucle de lectura. No cambia una aserción de resultado, el código funcional de Game ni ningún generador.

Resultado: build **PASS**, código 0, **20.093 s**, cero advertencias/errores. Laboratorio **PASS**, código 0, **27.922 s**, con sus cuatro tamaños y nuevas comprobaciones de campos, controles y umbral. Las dos suites geográficas amplias no se repitieron en esta ejecución acotada; sus logs y SHA se conservan en el informe anterior. No se certifican baselines históricos como aprobados.

Repetir desde la raíz:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\GEvolve.Tests\ValidationReports\commands.ps1 -ValidationSet LaboratoryControls -Label P03-A721-nullable
```

No se abrió Game, no hubo contexto GL ni evaluación gráfica. La inspección del portapapeles, dado, texto, colores y fluidez corresponde al usuario. **P03-A.7.2.1 implementado y validado técnicamente; pendiente de aprobación visual.** P03-A no aprobado; P03-A.7.3 no iniciado.
