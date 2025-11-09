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

Incorporación al proyecto (dentro de FOCA)
- La incorporación de URLs al proyecto actual se hace a través de `PluginsAPI.Import` usando `Operation.AddUrl` (no se escribe directamente en la base de datos de FOCA).
- Requisitos: ejecutar el plugin dentro del host FOCA (compilado con `FOCA_API`).
