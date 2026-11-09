# Archivo de Game anterior a R02

Estas 16 fuentes C# son copias exactas de sus rutas anteriores en 43222f5f8c2e6f26089c29f02e46bbc3c0428b45. GEvolve.Game.csproj excluye Legacy/**/*.cs. No hay proyecto ejecutable, flag ni fallback que las compile o invoque.

Se conservan el controlador/sesión antiguos, entrada de ventana, layout, renderer, capas, paletas y comparador como evidencia histórica, sin adaptar sus namespaces. Sus referencias al motor archivado no son dependencias activas. Las guías y reportes P03/P09 describen esta implementación, no la interfaz actual.

Las únicas piezas genéricas conservadas activas son UiRenderer, ShaderProgram, MapCamera2D y TextFieldEditor. El nuevo flujo está descrito en [GRID_LABORATORY.md](../GRID_LABORATORY.md). Cualquier rescate adicional requiere evaluación y autorización explícitas.
