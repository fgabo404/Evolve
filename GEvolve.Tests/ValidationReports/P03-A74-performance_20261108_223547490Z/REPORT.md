# P03-A.7.4 — Rendimiento normal, comparación intercalada

Se ejecutaron los binarios de Tests previos/actuales, sin reconstruirlos, en orden old/new/new/old, después de terminar la suite general. Cada proceso calienta Small y registra tres muestras por mundo: seis muestras antiguas y seis actuales por cada uno de los ocho mundos. GenerateGeology no activa los nuevos campos; no se ejecuta Game. Los binarios no se publican. Versiones, hashes de binarios, códigos y logs: [SUMMARY.md](SUMMARY.md).

| Mundo | Mediana previa ms | Mediana actual ms | Cambio % | Asignación mediana previa bytes | Cambio asignación bytes | State/BASE/FINAL idénticos |
|---|---:|---:|---:|---:|---:|---|
| Small/-1387338765 | 224.895 | 227.517 | 1.166 | 12860400 | 1272 | True |
| Small/-132790047 | 200.365 | 186.327 | -7.006 | 13045816 | 40 | True |
| Medium/-1387338765 | 307.045 | 291.443 | -5.081 | 20247520 | -2432 | True |
| Medium/-132790047 | 443.088 | 430.368 | -2.871 | 20265140 | -108 | True |
| Large/-1387338765 | 654.775 | 645.824 | -1.367 | 29196756 | -448 | True |
| Large/-132790047 | 628.496 | 608.181 | -3.232 | 28590096 | 1320 | True |
| Enormous/-1387338765 | 1309.298 | 1276.427 | -2.511 | 50600888 | -672 | True |
| Enormous/-132790047 | 1323.170 | 1290.725 | -2.452 | 51253724 | 1028 | True |

Las medianas actuales varían de -7.006% a 1.166% respecto a las previas. No aparece una regresión temporal sistemática en esta comparación. No se interpreta una diferencia negativa como una mejora demostrada ni se afirma coste exactamente cero: no hay aislamiento de CPU/frecuencia, se comparten condiciones del equipo y seis muestras por grupo no dan una prueba estadística universal.

La primera comparación separada había dado cambios de −16.5% a +35.5%. Esa señal no persiste en el orden intercalado; las propias ejecuciones del binario antiguo también varían respecto a la captura inicial. Es compatible con deriva del entorno de medición; no se diagnosticó su causa física. La comparación intercalada es la evidencia más pertinente para evaluar la instrumentación normal, conservando también las cifras iniciales.

Las pequeñas diferencias de asignación están en los logs; no se añade una matriz por celda en modo normal. Fields=null se comprueba en los ocho casos. La instrumentación opt-in sí copia matrices y los snapshots las duplican deliberadamente; esos costes no representan la ruta normal y no se ocultan como tiempo de generación productiva.

Los cuatro procesos terminaron con código 0, manteniendo iguales los tres hashes por mundo. El script [commands.ps1](commands.ps1) requiere los dos directorios de binarios aislados originales, deliberadamente excluidos de Git. Tras limpiar esos artefactos no puede repetirse directamente sin reconstruir/proveer esas versiones; la auditoría canónica sigue siendo reproducible desde el código mediante BaseReliefAudit. No sobrescribir las capturas históricas.
