---
description: Repository Information Overview
alwaysApply: true
---

# Repository Information Overview

## Repository Summary

NabTeams is a conversational platform built with ASP.NET Core (.NET 8) backend and Next.js 14 frontend. It implements role-based global chat with Gemini content monitoring, knowledge-based support chat using RAG, disciplinary cycle with negative points, and an appeals module. The system connects to PostgreSQL, supports SSO/JWT authentication, and uses Gemini for moderation and RAG capabilities.

## Repository Structure

- **backend/**: ASP.NET Core (.NET 8) backend with Clean Architecture
  - **NabTeams/src/Domain/**: Domain layer (entities, value objects)
  - **NabTeams/src/Application/**: Application layer (contracts, DTOs, policies)
  - **NabTeams/src/Infrastructure/**: Infrastructure layer (EF Core, Gemini, health checks)
  - **NabTeams/src/Web/**: ASP.NET Core host (Program, Controllers, SignalR)
  - **NabTeams/test/**: Backend unit tests
- **frontend/**: Next.js 14 application with App Router and NextAuth
- **docs/**: Project documentation
- **ops/**: Operations-related scripts (load tests, security)

## Projects

### Backend (ASP.NET Core)

**Configuration File**: backend/global.json, backend/NabTeams/src/Web/NabTeams.Web.csproj

#### Language & Runtime

**Language**: C#
**Version**: .NET 8.0.414
**Build System**: MSBuild
**Package Manager**: NuGet

#### Dependencies

**Main Dependencies**:

- Microsoft.EntityFrameworkCore 8.0.0
- Npgsql.EntityFrameworkCore.PostgreSQL 8.0.0
- Microsoft.AspNetCore.Authentication.JwtBearer 8.0.0
- OpenTelemetry.Extensions.Hosting 1.7.0
- Microsoft.Extensions.Http.Polly 8.0.0

#### Build & Installation

```bash
cd backend/NabTeams
dotnet restore
dotnet build
cd src/Web
dotnet ef database update
dotnet run --urls http://localhost:5000
```

#### Testing

**Framework**: xUnit 2.6.1
**Test Location**: backend/NabTeams/test/NabTeams.Api.Tests
**Configuration**: NabTeams.Api.Tests.csproj
**Run Command**:

```bash
cd backend/NabTeams
dotnet test
```

### Frontend (Next.js)

**Configuration File**: frontend/package.json

#### Language & Runtime

**Language**: TypeScript
**Version**: TypeScript 5.3.3
**Build System**: Next.js
**Package Manager**: npm/yarn

#### Dependencies

**Main Dependencies**:

- next 14.1.4
- react 18.2.0
- next-auth 4.24.5
- @microsoft/signalr 8.0.0

**Development Dependencies**:

- typescript 5.3.3
- eslint 8.56.0
- @types/react 18.2.55

#### Build & Installation

```bash
cd frontend
npm install
npm run build
npm start
```

## Environment Configuration

### Backend Environment Variables

- `ConnectionStrings__DefaultConnection`: PostgreSQL connection string
- `Gemini__ApiKey`: Google Gemini API key
- `Authentication__Authority`: SSO/OIDC server address
- `Authentication__Audience`: JWT token audience
- `Authentication__AdminRole`: Admin role name (default: admin)
- `Authentication__Disabled`: Disable authentication for local development
- `Payments__ApiKey`: Payment gateway API key
- `Notification__Email__*`: SMTP settings for email notifications
- `Notification__Sms__*`: SMS gateway settings

### Frontend Environment Variables

- `NEXTAUTH_URL`: Public Next.js app URL
- `NEXTAUTH_SECRET`: NextAuth session encryption key
- `SSO_ISSUER`, `SSO_CLIENT_ID`, `SSO_CLIENT_SECRET`: OIDC provider settings
- `AUTH_ALLOW_DEV`: Enable test login provider
- `NEXT_PUBLIC_API_URL`: Backend service address (default: http://localhost:5000)
