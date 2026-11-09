# Definiciones de generación del mundo

Este espacio contiene `Configuration/WorldSettings.cs` y `Configuration/WorldPresets.cs`: parámetros y perfiles declarativos del prototipo anterior. `Configuration/HexGridDefinition.cs` contiene los tres tamaños oficiales y las reglas de orientación y continuidad de la Capa 01. Una futura definición podrá seleccionar un algoritmo para capas posteriores, pero su implementación matemática y su ejecución corresponderán a Engine. State conservará la configuración utilizada y los resultados necesarios para recuperar la partida.

Todavía no hay contrato aprobado para seleccionar algoritmos, etapas, ruido, clima o persistencia espacial de las capas siguientes. No se crean `Algorithms/` ni `Stages/` con modelos vacíos, y no se modifican los JSON actuales. El generador procedural anterior permanece aislado de la nueva cuadrícula; Simulator solo solicita la Capa 01.
