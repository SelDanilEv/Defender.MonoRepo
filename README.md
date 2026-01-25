# Defender.MonoRepo

A comprehensive microservices-based platform providing financial management, gaming, and user management capabilities. This monorepo contains all Defender ecosystem services, libraries, and deployment configurations.

## Table of Contents

- [Technical Overview](#technical-overview)
  - [Architecture](#architecture)
  - [Technology Stack](#technology-stack)
  - [Project Structure](#project-structure)
  - [Development Setup](#development-setup)
  - [Deployment](#deployment)
  - [CI/CD Pipeline](#cicd-pipeline)
- [Services Overview](#services-overview)
  - [Production Services](#production-services)
  - [Supporting Services](#supporting-services)
  - [Shared Libraries](#shared-libraries)
- [Business Context](#business-context)
  - [Service Business Meanings](#service-business-meanings)
  - [Integration Flow](#integration-flow)
  - [Use Cases](#use-cases)

---

## Technical Overview

### Architecture

The Defender platform follows a **microservices architecture** with the following principles:

- **Clean Architecture / Domain-Driven Design (DDD)**: Each service is organized into layers (Domain, Application, Infrastructure, WebApi/WebUI)
- **CQRS Pattern**: Using MediatR for command/query separation
- **API-First Design**: All services expose RESTful APIs with OpenAPI/Swagger documentation
- **Event-Driven Communication**: Kafka-based messaging for asynchronous operations
- **Backend-for-Frontend (BFF)**: Portal service acts as BFF, aggregating calls to backend services
- **Service-to-Service Communication**: HTTP-based inter-service communication
- **Eventual Consistency**: Queue-based transaction processing with retry mechanisms

#### Architecture Layers

Each service follows a consistent layered architecture:

```
┌─────────────────────────────────────┐
│         WebApi / WebUI              │  ← API Layer (Controllers, Middleware)
├─────────────────────────────────────┤
│         Application                 │  ← Business Logic (Services, Handlers, DTOs)
├─────────────────────────────────────┤
│         Domain                      │  ← Domain Models (Entities, Enums, Interfaces)
├─────────────────────────────────────┤
│         Infrastructure              │  ← External Dependencies (Repositories, Clients, DB)
└─────────────────────────────────────┘
```

### Technology Stack

#### Backend Technologies

- **.NET 10.0**: Primary framework for all backend services
- **ASP.NET Core**: Web framework for REST APIs
- **MediatR 14.0.0**: CQRS pattern implementation
- **AutoMapper 16.0.0**: Object-to-object mapping
- **FluentValidation 12.1.1**: Input validation
- **MongoDB.Driver 3.6.0**: NoSQL database for document storage
- **PostgreSQL (Npgsql 10.0.1)**: Relational database for distributed cache
- **Confluent.Kafka 2.13.0**: Message broker for event-driven communication
- **JWT Bearer Authentication**: Token-based authentication
- **Serilog 4.3.0**: Structured logging
- **Swashbuckle/Swagger 10.1.0**: API documentation

#### Frontend Technologies

- **React 17.0.2**: UI framework
- **TypeScript 4.7.3**: Type-safe JavaScript
- **Material-UI (MUI) 5.16.7**: Component library
- **Redux Toolkit 2.2.3**: State management
- **React Router 6.3.0**: Client-side routing
- **i18next**: Internationalization support

#### Infrastructure & DevOps

- **Docker**: Containerization
- **Kubernetes**: Container orchestration
- **ArgoCD**: GitOps continuous deployment
- **Helm**: Kubernetes package management
- **GitHub Actions**: CI/CD automation

#### Third-Party Integrations

- **SendinBlue**: Email delivery service
- **Google OAuth**: Authentication provider
- **Exchange Rate APIs**: Currency conversion data

### Project Structure

```
brl/
├── .github/
│   └── workflows/              # GitHub Actions CI/CD pipelines
├── helm/                       # Kubernetes Helm charts
│   ├── argocd-applications/   # ArgoCD application manifests
│   ├── argocd-config/         # ArgoCD configuration
│   └── service-template/      # Reusable Helm chart template
├── scripts/                    # Automation and utility scripts
└── src/                        # Source code
    ├── Defender.Portal/        # Frontend + BFF service
    ├── Defender.IdentityService/
    ├── Defender.UserManagementService/
    ├── Defender.WalletService/
    ├── Defender.RiskGamesService/
    ├── Defender.NotificationService/
    ├── Defender.JobSchedulerService/
    ├── Defender.BudgetTracker/
    ├── Defender.GeneralTestingService/
    ├── Defender.Common/        # Shared library
    ├── Defender.Kafka/         # Kafka integration library
    ├── Defender.DistributedCache/ # Distributed cache library
    ├── Directory.Build.props   # Common build properties
    └── Directory.Packages.props # Centralized package versions
```

#### Service Structure

Each service follows a consistent structure:

```
Defender.ServiceName/
└── src/
    ├── Application/           # Business logic layer
    │   ├── Common/
    │   │   ├── Interfaces/     # Service interfaces
    │   │   └── Mappings/       # AutoMapper profiles
    │   ├── Modules/            # Feature modules (Commands/Queries)
    │   └── Services/           # Application services
    ├── Domain/                 # Domain layer
    │   ├── Entities/          # Domain entities
    │   ├── Enums/             # Domain enumerations
    │   └── Interfaces/        # Domain interfaces
    ├── Infrastructure/         # Infrastructure layer
    │   ├── Clients/           # External service clients
    │   ├── Repositories/      # Data access
    │   └── ConfigureServices.cs
    └── WebApi/                 # API layer
        ├── Controllers/       # API endpoints
        ├── ConfigureServices.cs
        └── Program.cs         # Application entry point
```

### Development Setup

#### Prerequisites

- **.NET 10.0 SDK**
- **Docker Desktop** (for local development)
- **Node.js 16+** (for Portal frontend)
- **MongoDB** (local or Docker)
- **PostgreSQL** (for distributed cache)
- **Kafka** (for event-driven features)

#### Local Development

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd Defender.MonoRepo
   ```

2. **Restore dependencies**
   ```bash
   cd src
   dotnet restore
   ```

3. **Start infrastructure services** (Docker Compose)
   ```bash
   docker-compose -f local-docker-compose.yml up -d
   ```

4. **Run services locally**
   - Each service can be run independently via `dotnet run` in its WebApi/WebUI project
   - Portal frontend: `cd src/Defender.Portal/src/WebUI/ClientApp && npm start`

5. **Access services**
   - Portal: `https://localhost:5001`
   - API Swagger: `https://localhost:5001/swagger` (for each service)

#### Environment Configuration

Services use `appsettings.json` with environment-specific overrides:
- `appsettings.Local.json` - Local development
- `appsettings.DockerLocal.json` - Docker local
- `appsettings.DockerDev.json` - Docker development
- `appsettings.Production.json` - Production

### Deployment

#### Containerization

All services are containerized using Docker:

- **Dockerfile.Service**: Standard template for API services
- **Dockerfile.Portal**: Specialized build for Portal (includes React frontend)

#### Kubernetes Deployment

Services are deployed to Kubernetes using:

- **Helm Charts**: Reusable chart template in `helm/service-template/`
- **Service-Specific Values**: Each service has its own `values-*.yaml` file
- **ArgoCD Applications**: GitOps-managed application definitions

#### ArgoCD Integration

The platform uses ArgoCD for GitOps-based continuous deployment:

- **Application Definitions**: Stored in `helm/argocd-applications/`
- **Deployment Strategies**: Staging, Production, and Tagged environments
- **Automated Sync**: Automatic deployment on Docker image updates

See [README-ARGOCD.md](./README-ARGOCD.md) for detailed ArgoCD documentation.

#### Deployment Environments

- **Development**: `defender-dev` namespace
- **Staging**: `defender-staging` namespace
- **Production**: `defender` namespace
- **Tagged**: `defender-tagged` namespace (for version-specific deployments)

### CI/CD Pipeline

#### GitHub Actions Workflows

1. **Docker Build and Publish** (`.github/workflows/docker-build-publish.yml`)
   - Triggers on pushes to `main`/`develop` branches
   - Builds Docker images for all services
   - Publishes to Docker Hub
   - Supports matrix builds for parallel service builds
   - Conditional builds based on source changes

2. **ArgoCD Deployment** (`.github/workflows/promote-image-tag.yml`)
   - Automatically triggered after Docker builds
   - Updates ArgoCD applications with new image tags
   - Supports multiple deployment strategies

#### Build Process

1. **Source Change Detection**: Only builds services with changes in `src/` directory
2. **Docker Image Tagging**: Multiple tags (branch, SHA, date-based, latest)
3. **Image Publishing**: Pushed to Docker Hub registry
4. **ArgoCD Sync**: Automatic deployment to Kubernetes clusters

---

## Services Overview

### Production Services

These services are actively used in production and exposed to end users:

1. **Defender.Portal** - Main user interface and BFF
2. **Defender.IdentityService** - Authentication and authorization
3. **Defender.UserManagementService** - User profile management
4. **Defender.WalletService** - Financial transactions and balances
5. **Defender.RiskGamesService** - Lottery and gaming features
6. **Defender.NotificationService** - Email notifications
7. **Defender.BudgetTracker** - Budget and financial tracking

### Supporting Services

These services support the platform but are not directly exposed to end users:

1. **Defender.JobSchedulerService** - Background job scheduling
2. **Defender.GeneralTestingService** - End-to-end testing utilities

### Shared Libraries

1. **Defender.Common** - Shared utilities, helpers, and base classes
2. **Defender.Kafka** - Kafka integration abstractions
3. **Defender.DistributedCache** - Distributed caching with PostgreSQL

---

## Business Context

### Service Business Meanings

#### Defender.Portal

**Business Purpose**: The primary user interface and entry point for all Defender platform features. Acts as a Backend-for-Frontend (BFF) that aggregates data from multiple backend services and provides a unified user experience.

**Key Business Functions**:
- **User Dashboard**: Centralized view of user's financial status, wallet balances, and active games
- **Account Management**: User profile viewing and editing interface
- **Banking Interface**: Wallet management, transaction history, and account operations
- **Gaming Portal**: Lottery ticket purchasing, draw viewing, and ticket management
- **Budget Management**: Financial tracking, position management, and budget reviews
- **Administrative Panel**: Admin tools for user management, wallet administration, and system oversight

**Business Value**: Provides a seamless, responsive user experience across all platform features, enabling users to manage their finances, participate in games, and track budgets from a single interface.

#### Defender.IdentityService

**Business Purpose**: Central authentication and authorization service that manages user identities, access control, and security tokens. Ensures secure access to all platform features.

**Key Business Functions**:
- **User Authentication**: Login via email/password or Google OAuth
- **JWT Token Generation**: Secure token creation for authenticated sessions
- **Access Code Management**: Generation and verification of access codes for:
  - Email verification
  - Password reset
  - Two-factor authentication
- **Role-Based Access Control**: User roles (Guest, User, Admin, SuperAdmin)
- **Login History Tracking**: Audit trail of user authentication events

**Business Value**: Provides the security foundation for the entire platform, ensuring only authorized users can access features and data. Enables secure, scalable authentication without requiring each service to implement its own security layer.

#### Defender.UserManagementService

**Business Purpose**: Manages user profiles, personal information, and account details. Maintains the core user data that other services reference.

**Key Business Functions**:
- **User Profile Management**: Creation, retrieval, and updates of user profiles
- **Personal Information**: Name, email, nickname, and other user attributes
- **Account Information**: User account status and metadata
- **Public User Profiles**: Limited profile information for public-facing features (e.g., wallet owner names)

**Business Value**: Centralizes user data management, ensuring consistency across the platform. Provides a single source of truth for user information that other services can reference.

#### Defender.WalletService

**Business Purpose**: Core financial service managing user wallets, currency accounts, and all financial transactions. Handles the complete financial lifecycle of user funds.

**Key Business Functions**:
- **Wallet Management**: 
  - Multi-currency wallet creation
  - Currency account management (USD, EUR, etc.)
  - Default currency selection
  - Wallet number assignment for transfers
- **Transaction Processing**:
  - Deposit operations
  - Withdrawal operations
  - Transfers between wallets
  - Transaction history and tracking
- **Balance Management**: Real-time balance updates with transaction consistency
- **Reliable Transaction Processing**: Queue-based system with retry mechanisms for infrastructure failures
- **Event-Driven Updates**: Kafka integration for transaction events

**Business Value**: Provides a secure, reliable financial system that ensures transaction integrity even during infrastructure issues. Enables users to manage multiple currencies, transfer funds, and maintain accurate financial records. The queue-based system ensures no transactions are lost, providing eventual consistency guarantees.

#### Defender.RiskGamesService

**Business Purpose**: Gaming platform service that manages risk-based games, currently focused on lottery functionality. Provides a flexible system for creating and managing gaming draws.

**Key Business Functions**:
- **Lottery Management**:
  - Lottery creation with configurable parameters
  - Draw scheduling and management
  - Ticket number range configuration
  - Bet amount limits (min/max)
  - Custom value support
- **Ticket Operations**:
  - Ticket purchasing with specific number selection
  - Available ticket search
  - User ticket history
  - Ticket availability checking
- **Draw Processing**:
  - Active draw management
  - Draw result calculation
  - Revenue calculation and tracking
- **Event Integration**: Kafka events for ticket purchases and draw results

**Business Value**: Provides an engaging gaming experience that generates revenue through lottery ticket sales. The flexible API allows for various lottery configurations, supporting different game types and business models. Integrates seamlessly with WalletService for payment processing.

#### Defender.NotificationService

**Business Purpose**: Communication service that handles all outbound notifications to users, primarily email-based communications.

**Key Business Functions**:
- **Email Notifications**:
  - Email verification messages
  - Verification code delivery
  - Transaction confirmations
  - System notifications
- **Notification Tracking**:
  - Delivery status monitoring
  - Failed notification handling
  - External service integration (SendinBlue)
- **Notification Types**: Support for Email and SMS (SMS infrastructure ready)
- **Template Management**: Structured notification templates

**Business Value**: Ensures users receive timely, important communications about their account activity, security events, and platform updates. Maintains communication history and delivery status for audit and troubleshooting purposes.

#### Defender.BudgetTracker

**Business Purpose**: Personal finance management service that helps users track their financial positions, create budget reviews, and visualize their financial status.

**Key Business Functions**:
- **Position Management**:
  - Financial position creation (e.g., "Bank PKO", "Savings Wallet")
  - Position categorization
  - Balance assignment to positions
- **Budget Reviews**:
  - Periodic financial reviews
  - Multi-position snapshot creation
  - Historical review tracking
- **Group Management**:
  - Position grouping for organization
  - Group-based filtering and highlighting
  - Visual diagram customization
- **Financial Visualization**: Diagram generation for budget overview

**Business Value**: Empowers users to understand and manage their personal finances across multiple accounts and positions. Provides insights into financial health through reviews and visualizations, helping users make informed financial decisions.

#### Defender.JobSchedulerService

**Business Purpose**: Background job scheduling service that manages recurring and scheduled tasks across the platform. Enables automation of periodic operations.

**Key Business Functions**:
- **Job Scheduling**: 
  - Recurring job creation (minute/hourly intervals)
  - One-time scheduled jobs
  - Job metadata and configuration
- **Job Execution**:
  - Automatic job triggering based on schedule
  - Kafka event publishing for job execution
  - Job status tracking
- **Job Management**:
  - Job creation, update, and deletion
  - Force execution capability
  - Job listing and querying

**Business Value**: Enables automation of platform operations such as periodic data processing, scheduled notifications, and maintenance tasks. Reduces manual intervention and ensures timely execution of critical background processes.

### Integration Flow

#### User Registration and Onboarding

1. **User Registration** → `IdentityService` creates account
2. **Email Verification** → `NotificationService` sends verification email
3. **Profile Creation** → `UserManagementService` creates user profile
4. **Wallet Creation** → `WalletService` creates default wallet
5. **Welcome Experience** → `Portal` displays onboarding flow

#### Financial Transaction Flow

1. **Transaction Initiation** → User initiates transaction via `Portal`
2. **Wallet Validation** → `Portal` calls `WalletService` to validate funds
3. **Transaction Processing** → `WalletService` processes transaction via queue
4. **Event Publishing** → Transaction events published to Kafka
5. **Notification** → `NotificationService` sends confirmation email
6. **UI Update** → `Portal` refreshes wallet display

#### Lottery Purchase Flow

1. **Draw Selection** → User selects lottery draw via `Portal`
2. **Ticket Search** → `Portal` calls `RiskGamesService` for available tickets
3. **Ticket Purchase** → User selects tickets and initiates purchase
4. **Payment Processing** → `RiskGamesService` calls `WalletService` for payment
5. **Ticket Creation** → `RiskGamesService` creates ticket records
6. **Confirmation** → `NotificationService` sends purchase confirmation
7. **UI Update** → `Portal` displays updated ticket list

### Use Cases

#### Personal Finance Management

A user can:
- Create and manage multiple currency accounts
- Track balances across different financial positions
- Create budget reviews to monitor financial health
- Visualize financial status through diagrams
- Transfer funds between wallets

**Services Involved**: Portal, WalletService, BudgetTracker, UserManagementService

#### Gaming and Entertainment

A user can:
- Browse active lottery draws
- Purchase lottery tickets with specific numbers
- View ticket purchase history
- Monitor draw results
- Receive winnings in wallet

**Services Involved**: Portal, RiskGamesService, WalletService, NotificationService

#### Account Security

A user can:
- Register with email or Google OAuth
- Verify email address
- Reset password using access codes
- View login history
- Manage account settings

**Services Involved**: Portal, IdentityService, UserManagementService, NotificationService

#### Administrative Operations

An admin can:
- View and manage all users
- Monitor wallet balances and transactions
- Oversee lottery operations
- Manage system configurations
- Access audit logs

**Services Involved**: Portal (Admin UI), All backend services

---

## Additional Resources

- **ArgoCD Documentation**: See [README-ARGOCD.md](./README-ARGOCD.md) for deployment details
- **Service-Specific READMEs**: Individual services may have their own README files
- **API Documentation**: Swagger UI available at `/swagger` endpoint for each service
- **Demo Environment**: https://defender-portal.coded-by-danil.dev

---
