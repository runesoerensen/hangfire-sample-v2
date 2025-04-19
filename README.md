# Background job processing on Heroku with Hangfire, MCP and .NET

[Hangfire](https://www.hangfire.io/) is a background job framework for .NET that enables easy enqueuing, scheduling, and processing of background tasks using persistent storage. This guide walks through running Hangfire with Redis on Heroku using a clean, scalable architecture.

## Overview

This sample app demonstrates how to structure a .NET 9.0 project with Hangfire and Redis using a clean separation of concerns:

- **Web** — A minimal ASP.NET Core app that exposes an `/enqueue` endpoint and hosts the Hangfire dashboard.
- **Worker** — A background service that processes Hangfire jobs.
- **Shared** — A class library that provides reusable configuration and dependency injection setup for Redis and Hangfire.

The app also supports ModelContextProtocol (MCP), which provides a way for AI tools like Cursor to interact with the application and trigger jobs.

This structure mirrors production best practices: the web app handles HTTP traffic and triggers jobs, while a separate worker dyno processes them asynchronously.

Note: Make sure to adapt to your needs. For instance, the Hangfire dashboard is configured to enable unauthenticated access, which is not appropriate for a production app.

## Local development

You can run and test the sample app locally before deploying to Heroku. The app is preconfigured to use `launchSettings.json` for environment variables, so you won't need to set them manually.

### 1. Start a Redis server

If you don't already have Redis installed, you can start it via Homebrew or Docker:

**Using Homebrew (macOS):**
```sh
brew install redis
redis-server
```

**Using Docker:**
```sh
docker run --rm -p 6379:6379 redis
```

### 2. Run the apps

From the root of the repo, run each app in a separate terminal:

```sh
dotnet run --project Web
dotnet run --project Worker
```

The Web app will start on [http://localhost:5000](http://localhost:5000) and expose three routes:

- `/enqueue` — Adds a job to the queue
- `/hangfire` — Shows the Hangfire dashboard
- `/mcp/sse` — MCP endpoint for AI tool integration

### 3. Configure Cursor to use MCP

The application supports ModelContextProtocol (MCP), which allows AI tools like Cursor to interact with it. To configure Cursor to use MCP:

1. Create a `.cursor` directory in your project root if it doesn't exist
2. Create `.cursor/mcp.json` with the following content for local development:
```json
{
    "mcpServers": {
        "hangfire-mcp-sample": {
            "url": "http://localhost:5000/mcp/sse"
        }
    }
}
```

Once configured, you can use the following MCP tools:
- `SendMessage` - Send messages to workers
- `GetJobStorageMetrics` - Get real-time metrics about jobs, queues, and workers

You can test the MCP integration directly in Cursor chat by:
1. Opening a new chat in Cursor
2. Asking the AI to "Send a message to the worker" or "Get job metrics"
3. The AI will use the configured MCP tools to interact with your application

## Deploying to Heroku

### 1. Create app and provision Redis

```sh
heroku create
heroku addons:create heroku-redis --wait
```

Heroku will automatically set the `REDIS_URL` environment variable when provisioned.

### 2. Deploy

Push your code to Heroku:

```sh
git push heroku main
```

The buildpack will automatically detect process types:
- `web` - Runs the Web application
- `worker` - Runs the background job processor

### 3. Scale the Worker app

The web process will start automatically. To begin processing jobs, scale up the `worker` process type:

```sh
heroku scale worker=1
```

This runs your background job processor in a separate dyno, letting you scale it independently.

### 4. Configure MCP for Heroku

Update your `.cursor/mcp.json` to point to your Heroku app:
```json
{
    "mcpServers": {
        "hangfire-mcp-sample": {
            "url": "https://YOUR-APP-NAME.herokuapp.com/mcp/sse"
        }
    }
}
```

### 5. Test

Visit these endpoints to test your deployment:
- `heroku open /enqueue` - Enqueue a background job
- `heroku open /hangfire` - View the Hangfire dashboard

You can also test the MCP integration in Cursor chat, just like in local development. The AI will use the Heroku MCP endpoint to interact with your application.

## How the code is organized

The project uses a shared `RedisConfig` type to parse the `REDIS_URL` into a strongly-typed object. It includes logic to:

- Handle SSL and Heroku's self-signed certificates when necessary
- Provide consistent dependency injection across apps
- Make local and cloud setups behave identically

This logic lives in the **Shared** project and is consumed by both **Web** and **Worker**, avoiding duplication and ensuring consistent setup.

### MCP Integration

The Model Context Protocol (MCP) integration is set up in the Web project:

- `ModelContextProtocol` and `ModelContextProtocol.AspNetCore` packages are added
- The MCP server is configured with HTTP transport
- Custom tools like `WorkerMessageTool` are registered to allow external tools to interact with the application
- The `/mcp` endpoint is mapped to handle MCP requests

The MCP architecture allows:
- Registering server-side "tools" that can be discovered and called by clients
- Real-time communication via SSE (Server-Sent Events)
- Integration with AI assistants
- Debugging with the MCP inspector tool

## Summary

Hangfire makes it easy to add robust, persistent background job processing to your .NET apps on Heroku. By structuring your app into Web, Worker, and Shared components like the ones in this example, you get a clean and scalable architecture that works just as well locally as it does on Heroku.

The addition of MCP support allows AI-powered tools like Cursor to interact with your application programmatically, demonstrating a powerful interface for job management and monitoring.

Key benefits:
- Scale your `worker` and `web` process types independently
- Use Redis for reliable job persistence
- Monitor jobs with the built-in Hangfire dashboard
- Interact with your application through the MCP interface
- Graceful handling of worker scaling and job queuing
