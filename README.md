# Background job processing with Hangfire and .NET on Heroku

[Hangfire](https://www.hangfire.io/) is a background job framework for .NET that makes it easy to enqueue, schedule, and process background tasks using persistent storage. This guide walks through running Hangfire with Redis on Heroku using a simple, scalable setup.

## Overview

This sample app demonstrates how to structure a .NET 8.0 project with Hangfire and Redis using a clean separation of concerns:

- **Web** — a minimal ASP.NET Core app that exposes an `/enqueue` endpoint and hosts the Hangfire dashboard.
- **Worker** — a background service that processes Hangfire jobs.
- **Shared** — a class library that provides reusable configuration and dependency injection setup for Redis and Hangfire.

This structure mirrors production best practices: the web app handles HTTP traffic and triggers jobs, while a separate worker dyno processes them asynchronously.

Note: Make sure to adapt to your needs. For instance, the Hangfire dashboard is configured to enable unauthenticated access, which is not appropriate for a production app.

## Local development

You can run and test the sample app locally before deploying to Heroku. The app is preconfigured to use `launchSettings.json` for environment variables, so you won’t need to set them manually.

### 1. Start a Redis server

If you don’t already have Redis installed, you can start it via Homebrew or Docker:

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

The Web app will start on [http://localhost:5000](http://localhost:5000) and expose two routes:

- `/enqueue` — adds a job to the queue
- `/hangfire` — shows the Hangfire dashboard

### 3. Enqueue a job

To trigger a background job:

```
http://localhost:5000/enqueue
```

This enqueues a simple console job. The Worker app will process the job using Hangfire, and you’ll see it appear in the dashboard at:

```
http://localhost:5000/hangfire
```

### How configuration works

Both the Web and Worker apps read Redis connection settings from the `REDIS_URL` environment variable.

Locally, this is set via each app’s `launchSettings.json` file:

```json
"environmentVariables": {
  "REDIS_URL": "redis://default:@localhost:6379"
}
```

This is the same format Heroku provides in production. SSL and self-signed certificate handling are automatically configured depending on the URL scheme (`redis://` or `rediss://`).

## Deploying to Heroku

### 1. Create the app and provision Redis

```sh
heroku create
heroku addons:create heroku-redis --wait
```

Heroku will automatically set the `REDIS_URL` environment variable when provisioned.

### 2. Deploy the code

Push your code to Heroku:

```sh
git push heroku main
```

### 3. Scale the Worker process

The web process will start automatically. To begin processing jobs, scale up the `Worker` process type:

```sh
heroku scale Worker=1
```

This runs your background job processor in a separate dyno, letting you scale it independently.

### 4. Enqueue a test job

Visit your app:

```
heroku open /enqueue
```

This will enqueue a background job, and the Worker dyno will pick it up. You can verify that it ran via the dashboard at:

```
heroku open /hangfire
```

## How the code is organized

The project uses a shared `RedisConfig` type to parse the `REDIS_URL` into a strongly-typed object. It includes logic to:

- Handle SSL and Heroku’s self-signed certificates when necessary
- Provide consistent dependency injection across apps
- Make local and cloud setups behave identically

This logic lives in the **Shared** project and is consumed by both **Web** and **Worker**, avoiding duplication and ensuring consistent setup.

## Customizing your job logic

By default, the sample enqueues a simple `Console.WriteLine` task. To create your own jobs:

1. Define a class with a public method (e.g. `EmailService.Send()`).
2. Register it in the shared DI setup.
3. Enqueue it like this:

```csharp
BackgroundJob.Enqueue<EmailService>(s => s.Send("user@example.com"));
```

## Summary

Hangfire makes it easy to add robust, persistent background job processing to your .NET apps on Heroku. By structuring your app into Web, Worker, and Shared components like the ones in this example, you get a clean and scalable architecture that works just as well locally as it does on Heroku.

- Scale your `Worker` and `Web` process types independently.
- Use Heroku Key-Value Store for job persistence.
- Monitor jobs with the built-in Hangfire dashboard.

This approach keeps your apps modular and your infrastructure simple.
