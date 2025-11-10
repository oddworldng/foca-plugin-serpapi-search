# foca-plugin-serpapi-search
Plugin para FOCA que permite realizar una búsqueda ampliada de documentos mediante URLs utilizando SerpApi (Google, DuckDuckGo y Bing) e integrarlos directamente en un proyecto de FOCA.

Resumen de uso rápido
- Menú: Búsqueda avanzada → Configuración de SerpApi / Buscar.
- Configura la API Key (prioridad: SERPAPI_API_KEY > %APPDATA%\FOCA\Plugins\SerpApiSearch\config.json).
- Busca por dominio y extensiones; luego incorpora resultados a proyectos o exporta CSV.

Motores soportados
- DuckDuckGo (paginación con pageno).
- Google (paginación con start, soporte de google_domain, hl/gl automapeados desde kl).
- Bing (paginación con first/count; `kl` opcional en formato lang-cc → `setlang`/`cc`; además `mkt=lang-COUNTRY` y `safe_search=Off`. Se refuerza el dork con `site:`/`inurl:` y, de forma opcional (activada por defecto en la configuración), se añade `filters=domain:<host>` a la petición de SerpApi. Se usa `async=true` + sondeo del endpoint hasta `Success/Cached` para evitar timeouts y se permite caché). 

Configuración (App.config)
- `SerpApiTimeoutSeconds`: recomendado 90 para Bing async.
- `SerpApiBingAsyncPollingIntervalMs` (opcional): intervalo de sondeo, por defecto 1500 ms.
- `SerpApiBingAsyncMaxWaitMs` (opcional): ventana máxima de sondeo, por defecto 180000 ms.
- Opción "Aplicar filtro domain= en búsquedas Bing" (configuración del plugin / `config.json`): permite desactivar `filters=domain` si se necesita relajar el filtro.

Estrategia de integración con FOCA (URLs → Proyecto)

Para que, tras una búsqueda, las URLs queden en el proyecto de FOCA listas para “Download/Extract/Analyze” exactamente igual que si se hubieran añadido desde la interfaz nativa, el plugin sigue estas fases:

1) Inserción por BD (nuevo o existente)
   - Proyecto nuevo: se crea una fila en `Projects` (estado inicializado y carpeta de descargas) y se insertan las URLs en `FilesITems` (o `UrlsItems`) con su FK.
   - Proyecto existente: se insertan las URLs en la tabla correspondiente con su FK.
   - La cadena de conexión se alinea con la que usa FOCA cuando el plugin corre embebido, evitando desajustes de BD.

2) Recarga silenciosa del proyecto
   - Tras insertar, el plugin recarga el proyecto y refresca el combo “Select project” sin cambiar de panel ni mostrar diálogos.

3) Post‑proceso idéntico a “Add URLs from file”
   - Para cada URL insertada: asegura dominio, alimenta el mapa (`map.AddUrl` y `map.AddDocument`) y dispara `techAnalysis.eventLinkFoundDetailed`. Así, árbol y navegación se comportan como en FOCA.

4) Encolado de descargas con el pipeline oficial de FOCA
   - Inmediatamente después, el plugin encola esas URLs con el mismo método interno que usa el menú “Download”, respetando `FolderToDownload` y el número de descargas simultáneas. Con esto se fija el `Size` real y `Downloaded=•`.

5) Reparación automática de 0 KB
   - Si alguna URL queda a 0 KB tras descargar, el plugin lanza un GET directo (User‑Agent/Referer/compresión) y sobrescribe el fichero, actualizando `Size` y la fila en la UI. Se hace en segundo plano y no bloquea al usuario.

Con este flujo, tras “Insertar a proyecto”, puedes continuar con “Extract All Metadata” → “Analyze All Metadata” sin pasos manuales adicionales.

Incorporación al proyecto (dentro de FOCA)
- La incorporación de URLs al proyecto actual se hace a través de `PluginsAPI.Import` usando `Operation.AddUrl` (no se escribe directamente en la base de datos de FOCA).
- Requisitos: ejecutar el plugin dentro del host FOCA (compilado con `FOCA_API`).

Inserción directa en BD (nuevo o existente)
- Tras la búsqueda, puedes:
  - Insertar a un proyecto nuevo: se crea fila en `Projects` y se insertan URLs en `FilesITems` (o `UrlsItems` si existe) vinculadas al nuevo `Id`.
  - Insertar a un proyecto existente: se muestra un selector con los proyectos (`Projects.Id`, `ProjectName`) y se insertan las URLs al seleccionado.
- El inserter detecta dinámicamente tabla y columnas (`URL`, `IdProject`/`ProjectId`) y completa columnas NOT NULL típicas (`Ext`, `Downloaded`, `MetadataExtracted`, `Date`, `ModifiedDate`, `Size`, `DiarioAnalyzed`, `DiarioPrediction`, `Path`) con valores seguros cuando sea necesario.
- Evita duplicados por `(URL, IdProject)` mediante `IF NOT EXISTS`.
- Si se ejecuta fuera de FOCA, asegúrate de definir una cadena de conexión SQL Server válida en `App.config`. Dentro de FOCA se reutiliza la conexión del host.