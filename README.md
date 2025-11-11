# foca-plugin-serpapi-search
Plugin para FOCA que realiza búsquedas de documentos (PDF, DOCX, XLSX, etc.) mediante SerpApi (motores: DuckDuckGo, Google y Bing) y permite volcar las URLs directamente a un proyecto de FOCA.

## Requisitos

- .NET Framework 4.7.1 (o compatible con el host FOCA).
- Cuenta y API Key de SerpApi.
- FOCA (si se desea ejecutar embebido con integración total).

## Instalación y build

- Compilar el proyecto. El DLL resultante se genera en `plugin/SerpApiSearch/`.
- Para FOCA:
  - Copiar el DLL en la carpeta de plugins del host (o ejecutar desde la solución preparada para FOCA).
  - Si está presente `FOCA/Plugins Release/PluginsAPI.dll` o `lib/PluginsAPI.dll`, el plugin se compilará con `FOCA_API` y habilitará integración completa.

## Configuración

- API Key:
  - Variable de entorno `SERPAPI_API_KEY` (prioridad).
  - Fichero local `%APPDATA%\FOCA\Plugins\SerpApiSearch\config.json` (gestión desde la UI).
- App.config:
  - **SerpApiTimeoutSeconds**: timeout HTTP en segundos (p.ej., 20).
  - (No se usa modo async/polling en Bing).
- Ajustes internos leídos del almacenamiento del plugin:
  - `MaxResults`, `MaxPagesPerSearch`, `DelayBetweenPagesMs`, `MaxRequestsPerSearch`, `UseBingDomainFilter` (ya no se envía `filters=domain` en Bing; el filtrado se realiza en cliente).

## Motores soportados

- **DuckDuckGo**: paginación con `pageno`, soporte de `kl` (región), `device=desktop`.
- **Google**: paginación con `start`, `num`, `google_domain`; `hl/gl` mapeados desde `kl`; `as_filetype`/`as_sitesearch`.
- **Bing**:
  - Parámetros: `q`, `setlang`, `cc`, `mkt`, `first`, `count`, `safe_search=Off`, `device=desktop`.
  - Primera página sin `first/count` (mejora cobertura inicial).
  - Paginación a partir de la segunda página con `first` (y opcionalmente `count`) o siguiendo `serpapi_pagination.next`.
  - No se usa `no_cache` ni parámetros no documentados (p. ej. `filters=domain`).

## Cómo usar (UI)

1. Abrir “Búsqueda avanzada”.
2. Introducir la URL raíz o dominio objetivo (puedes pegar una URL con ruta).
3. Seleccionar extensiones.
4. Elegir motor (DuckDuckGo / Google / Bing).
5. (Opc.) `kl` (ej.: `es-es`) para región.
6. Pulsar “Buscar” y esperar el listado de URLs.
7. Insertar al proyecto (existente o nuevo) o exportar CSV.

---

## Estrategia de búsqueda (resumen común)

- Sanitización de entrada:
  - Recorta espacios, elimina comillas sueltas, normaliza a minúsculas y separa ruta/host si el usuario pegó una URL.
- Construcción del dork:
  - `site:"host"`
  - `inurl:"segmento1" inurl:"segmento2"`… (si el usuario pegó ruta; se filtran segmentos muy cortos y códigos lingüísticos).
  - `filetype:ext` (una o varias extensiones unidas con `OR`).
- Región:
  - `kl` → mapeo a `setlang/cc` (Bing), `hl/gl` (Google), y `mkt` cuando aplica.
- Extracción de resultados:
  - Primero `organic_results`.
  - Para Bing, también `inline_results` (solo si pertenecen al dominio objetivo).
- Filtrado y deduplicación en cliente:
  - Por host (apex/www/subdominio exacto).
  - Por ruta (si se indicaron segmentos).
  - Por extensión (si hay selección).
  - Conjunto `seen` para evitar duplicados.

---

## Estrategia de búsqueda en Bing (detallada)

El objetivo fue maximizar orgánicos del dominio correcto y evitar ruido. Se siguió esta estrategia:

- Construcción de `q` (dork):
  - Sanitización de `domain`: recorte, sin comillas sueltas, minúsculas.
  - Reglas de `site:`:
    - Si el usuario introduce apex (ej. `mjusticia.gob.es`):
      - `q = (site:"mjusticia.gob.es" OR site:"www.mjusticia.gob.es") filetype:EXT`
    - Si introduce `www.apex` (ej. `www.mjusticia.gob.es`):
      - `q = site:"www.mjusticia.gob.es" filetype:EXT`
    - Si introduce un subdominio distinto de `www` (ej. `intranet.mjusticia.gob.es`):
      - `q = site:"intranet.mjusticia.gob.es" filetype:EXT`
  - Si se pegó una URL con ruta, se añaden sesgos con `inurl:"segmento"` por cada segmento relevante.

- Llamada a SerpApi (motor Bing):
  - Primera página: no se envían `first`/`count` (se dejan los defaults de SerpApi/Bing).
  - Paginación: a partir de la segunda página se envía `first=…` y, opcionalmente, `count`; también se puede seguir `serpapi_pagination.next`.
  - No se envía `no_cache` para evitar comportamientos inconsistentes.
  - No se usan parámetros no documentados para Bing (p. ej. `filters=domain`).

- Extracción y filtrado:
  - Se leen `organic_results`.
  - Se añaden `inline_results` solo si el host pertenece a apex/www/subdominio objetivo.
  - Filtro estricto por host en el cliente (descarta `bing.com`, `wikipedia.org`, etc.).

- Robustez y diagnóstico:
  - Reintentos con backoff ante 5xx/timeouts.
  - Logging:
    - URL exacta enviada (copiable).
    - `q` decodificada (para inspección rápida).
    - Si `links == 0`, snippet del JSON devuelto.
  - Pitfalls evitados:
    - No anteponer comillas a `site:` (ej. `"%22site:..."`); Bing lo interpreta como texto literal y saca resultados fuera de dominio.

- Ejemplo de URL (sustituye API Key):
  - `https://serpapi.com/search.json?engine=bing&q=site%3A%22www.mjusticia.gob.es%22%20filetype%3Apdf&setlang=es&cc=es&mkt=es-ES&device=desktop&safe_search=Off&api_key=TU_API_KEY`

---

## Estrategia de integración con FOCA (URLs → Proyecto)

Esta parte reproduce el flujo “oficial” de FOCA de forma transparente para el usuario:

1. **Inserción en BD**
   - Nuevo proyecto:
     - Se crea fila en `Projects` (estado inicializado, carpeta de descargas), y se insertan las URLs en `FilesItems` (o `UrlsItems` si existe) con la FK al nuevo `Id`.
   - Proyecto existente:
     - Se insertan las URLs en la tabla correspondiente con la FK del proyecto seleccionado.
   - La cadena de conexión se alinea con la que usa FOCA cuando el plugin corre embebido, evitando desajustes.

2. **Recarga silenciosa del proyecto**
   - El plugin refresca el proyecto y el listado sin navegación de paneles ni diálogos extra.

3. **Post‑proceso idéntico a “Add URLs from file”**
   - Para cada URL:
     - Se asegura el dominio y se alimenta el mapa (`map.AddUrl` / `map.AddDocument`).
     - Se dispara `techAnalysis.eventLinkFoundDetailed`.
   - El árbol y la navegación se comportan igual que en FOCA.

4. **Descargas y consolidación**
   - Se encolan las descargas con el método interno que usa “Download”, respetando `FolderToDownload` y el número de descargas simultáneas.
   - Con esto:
     - Se fija el `Size` real y `Downloaded = •`.
     - Se habilitan inmediatamente pasos posteriores de FOCA (Extract, Analyze).

5. **Reparación automática de 0 KB**
   - Si alguna URL queda a 0 KB, el plugin lanza un GET directo (User‑Agent/Referer/compresión) y sobrescribe el fichero, actualizando `Size` y la fila en la UI. Se hace en segundo plano y no bloquea.

6. **Alternativa (AddUrl vía PluginsAPI)**
   - Cuando el plugin se ejecuta embebido con `FOCA_API`, la incorporación a proyecto actual también puede invocarse mediante `PluginsAPI.Import (Operation.AddUrl)`.

> Resultado: tras “Insertar a proyecto”, el usuario puede continuar con “Extract All Metadata” → “Analyze All Metadata” sin pasos manuales extra.

---

## Diagnóstico y logging

- Se registra:
  - La URL exacta enviada a SerpApi (copiable).
  - La `q` decodificada.
  - En caso de `links == 0`, un snippet del JSON devuelto (truncado).
- Recomendación: probar primero con dominios con muchos PDFs (p. ej., organismos oficiales) para validar la cadena completa.

## Limitaciones conocidas

- El motor puede variar su formato de respuesta (especialmente `inline_results`) con el tiempo; el plugin filtra por host para minimizar el impacto.
- Si SerpApi/Bing no devuelve orgánicos para un dork concreto, se mostrará el snippet JSON para inspección manual y ajustar la consulta.

## Hoja de ruta (ideas)

- Selector “solo apex”, “solo www” o “apex + www”.
- Ajustar `count` dinámicamente por motor y segunda página.
- Botón “Copiar consulta/URL” en la UI.
- Métricas básicas (tiempos por página, aciertos/descartes) en el log.

## Créditos

- Plugin: FOCA SerpApi Search.
- Motores: DuckDuckGo, Google, Bing a través de SerpApi.