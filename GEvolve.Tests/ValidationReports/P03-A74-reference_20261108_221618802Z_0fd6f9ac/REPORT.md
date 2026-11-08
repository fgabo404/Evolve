# P03-A.7.4 — Referencia anterior a la instrumentación

Captura de los ocho mundos obligatorios antes de modificar cualquier archivo del motor. Engine corresponde a `84932b9396001f0c883ce2d4c6a4d4d3dcd29f45` en `worldgeneration_pangea`. Solo se había añadido el comando de captura en Tests y el conjunto operativo del script. El SUMMARY indica fuente rastreada distinta de HEAD por ese arnés; no atribuye ese arnés al SHA de la base.

[base-reference.log](base-reference.log) conserva State/BASE/FINAL SHA256 y tres muestras de tiempo/asignación por mundo, con un calentamiento previo Small. La captura terminó antes de editar la instrumentación. Build y captura PASS. No se generaron imágenes ni archivos baseline; este log es evidencia inmutable, no aprobación de alturas.

La auditoría posterior verifica esos hashes directamente, además de comparar modos con/sin instrumentación y automático/manual. Los tiempos son de GenerateGeology (no clima/biomas), medidos secuencialmente en la misma máquina; tres repeticiones no constituyen un estudio de rendimiento estadísticamente concluyente. Las asignaciones se miden con GC.GetAllocatedBytesForCurrentThread. No se cambian fórmulas ni parámetros.

Ver [SUMMARY.md](SUMMARY.md), [build.log](build.log) y la instantánea [commands.ps1](commands.ps1). Ejecutar de nuevo la instantánea crea otra ejecución; no vuelve a producir la versión antigua de Engine. Para reconstruir la referencia antigua debe conservarse su versión de motor/armazón indicada arriba. No sobrescribir esta evidencia.
