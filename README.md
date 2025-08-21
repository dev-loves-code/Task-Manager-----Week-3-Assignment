# Task Manager API

ASP.NET Core 9 REST API for managing tasks and notes (one-to-many) with full CRUD support, Entity Framework Core, Mapster DTO mapping, Redis caching, and Serilog logging.  
The API also includes user management with JWT authentication, background jobs, email notifications, PDF report generation, and real-time updates.

## Prerequisites

- .NET 9 SDK  
- SQL Server (local or containerized)  
- Docker for Redis  

## Setup & Run

### Database Migrations

```bash
cd api
dotnet ef database update
````

### Start Redis via Docker

```bash
docker-compose up
```

### Run the API

```bash
dotnet run
```

> The default `appsettings.json` uses Windows Authentication for SQL Server. To use SQL username/password, update the `DefaultConnection`.

## Features

* Tasks & Notes CRUD (one-to-many)
* DTO mapping with Mapster
* Redis caching
* Logging with Serilog & `ILogger`
* Controller–Service–Repository architecture
* Full exception handling
* User management (`AppUser`) with JWT authentication
* Background job processing via Hangfire:

  * **Recurring job:** `QueueWeeklyReports()`

    * Fetches all users and calls `GeneratePdf(UserDtoWithID user)` for each
    * Generates PDF reports using QuestPDF
    * Sends SignalR notification to the specific user that the report is ready
    * Triggers a fire-and-forget job to send the report via email
  * **Fire-and-forget job:** Sends welcome email upon registration
* Email notifications via MailKit
* Real-time notifications with SignalR

## Job & Notification Flow

1. **QueueWeeklyReports()** (Recurring Job)

   * Fetches all users from the database
   * Calls `GeneratePdf(UserDtoWithID user)` for each user:

     * Generates a PDF report using QuestPDF
     * Sends a SignalR notification to the specific user that their report is ready
     * Triggers a fire-and-forget job to email the PDF to the user

2. **Fire-and-Forget Jobs**

   * Sends welcome email when a user registers
   * Sends email reports with PDF attachments after report generation

## Architecture Overview

* ASP.NET Core 9 REST API
* EF Core for database management
* Mapster for DTO mapping
* Redis for caching
* Serilog & `ILogger` for logging
* Controller–Service–Repository pattern for clear separation of concerns
* Hangfire for background job management
* MailKit for sending emails
* QuestPDF for PDF report generation
* SignalR for real-time notifications
