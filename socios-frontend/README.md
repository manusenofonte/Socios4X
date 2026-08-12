# Asistente virtual — CAI Dolores

Chatbot que responde consultas de los socios del Club Atlético Independiente de Dolores sobre cuotas, trámites, actividades y reglamento. Las respuestas se generan a partir de la documentación real del club.

**Stack:** .NET 10 · Angular · Qdrant · OpenAI (`gpt-5-nano`, `text-embedding-3-small`) · PdfPig · Docker

## Por qué una segunda versión

Esta es la reescritura de [una primera versión](https://github.com/nicolaslicha02/SociosDev.API) que funcionaba con detección de palabras clave sobre un conjunto de FAQs cargadas a mano.

Andaba, pero tenía un techo bastante bajo. Si el socio escribía "¿cuánto sale asociarse?" y la respuesta estaba cargada como "el valor de la cuota mensual es...", no encontraba nada: no comparten una sola palabra. Para cubrir todas las formas de preguntar lo mismo había que anticiparlas y cargarlas una por una.

Esta versión busca por significado en lugar de por coincidencia literal, así que responde preguntas formuladas de maneras que nunca cargué.

## Cómo funciona

Al cargar la documentación:

1. PdfPig extrae el texto de los PDFs.
2. El texto se corta en fragmentos.
3. Cada fragmento se convierte en un vector con `text-embedding-3-small` (1536 dimensiones).
4. Los vectores se guardan en Qdrant.

Cuando un socio pregunta:

1. La pregunta se convierte en vector con el mismo modelo.
2. Qdrant devuelve los fragmentos más cercanos.
3. Esos fragmentos se insertan en el prompt.
4. `gpt-5-nano` genera la respuesta usando ese contexto.

El modelo nunca ve los documentos completos. En cada consulta recibe solamente los fragmentos que hacen falta.

## Estructura

```
Socios.Domain          entidades y reglas de negocio
Socios.Application     casos de uso e interfaces
Socios.Infrastructure  Qdrant, OpenAI, persistencia
Socios.Api             controladores y configuración
socios-frontend        cliente Angular
```

Clean Architecture: `Application` define las interfaces que necesita e `Infrastructure` las implementa. De esa forma la lógica de negocio no depende de Qdrant ni de OpenAI, y cambiar de base vectorial o de proveedor de modelo es escribir otra implementación sin tocar los casos de uso.

## Correrlo en local

Requisitos: .NET 10 SDK, Node 20.19+, y una API key de OpenAI.

Vectores — elegí una opción:

- **Qdrant Cloud** (la que se usa hoy en este proyecto): creá un cluster gratis en [cloud.qdrant.io](https://cloud.qdrant.io) y guardate el endpoint y la API key.
- **Local con Docker**: `docker run -p 6333:6333 qdrant/qdrant`, y usá `http://localhost:6333` como endpoint.

Backend:

```bash
cd Socios.Api
dotnet user-secrets init
dotnet user-secrets set "AI:OpenAI:ApiKey" "tu-api-key"
dotnet user-secrets set "AI:Qdrant:Endpoint" "tu-endpoint-de-qdrant"
dotnet user-secrets set "AI:Qdrant:ApiKey" "tu-api-key-de-qdrant"
dotnet user-secrets set "ConnectionStrings:SociosDevConnection" "tu-connection-string"
dotnet run
```

La connection string real (no la de `appsettings.Development.json`, que es solo un placeholder) hay que pedirla — no está en el repo a propósito.

Frontend:

```bash
cd socios-frontend
npm install
ng serve
```

## Despliegue

Está containerizado con Docker. Empecé buscando hosting compartido y me encontré con que los proveedores que miré solo soportaban .NET Framework 4.x, y el proyecto usa .NET 10. En lugar de bajar la versión del proyecto para que entrara, lo metí en un contenedor con su propio runtime.

## Pendientes

- Registrar las consultas que no encuentran contexto, para detectar qué documentación falta cargar
- Tests sobre los casos de uso de `Application`

## Autoría

Desarrollado por **Nicolás Licha & Manuel Senofonte**: arquitectura, pipeline de RAG, API, integración con el frontend y despliegue.

[GitHub](https://github.com/nicolaslicha02) · [LinkedIn](https://www.linkedin.com/in/nicolas-licha/)
