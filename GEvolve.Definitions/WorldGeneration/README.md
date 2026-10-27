# Definiciones de generación del mundo

Este espacio contiene `Configuration/WorldSettings.cs` y `Configuration/WorldPresets.cs`: parámetros y perfiles declarativos existentes. Una futura definición podrá seleccionar un algoritmo, pero su implementación matemática y su ejecución corresponderán a Engine. State conservará la configuración utilizada y los resultados necesarios para recuperar la partida.

Todavía no hay contrato aprobado para seleccionar algoritmos, etapas, ruido, clima, geometría o persistencia espacial. No se crean `Algorithms/` ni `Stages/` con modelos vacíos, y no se modifican los JSON actuales. El generador existente en Engine permanece operativo para los usos actuales; no se activa desde el Simulator al iniciar.
