````markdown
# Gift of the Givers Foundation Web Application

The **Gift of the Givers Foundation Web Application** is a comprehensive disaster relief management platform designed to streamline operations for managing disaster response efforts, donations, volunteer assignments, and resource allocations. This application serves as a central hub for the foundation’s efforts to help affected communities across South Africa and globally.

## Table of Contents
1. [Project Overview](#project-overview)
2. [Features](#features)
3. [Prerequisites](#prerequisites)
4. [Installation](#installation)
5. [Setup](#setup)
6. [Database Schema](#database-schema)
7. [How to Use](#how-to-use)
8. [Running the Application](#running-the-application)
9. [Contributing](#contributing)
10. [License](#license)

---

## Project Overview

The **Gift of the Givers Web Application** is developed in C# using ASP.NET Core, and the goal is to support disaster relief efforts by tracking donations, assigning volunteers, and managing resources. The application includes the following key modules:
- **User Authentication and Management**: Secure login, registration, and profile management using Azure Active Directory.
- **Donation Management**: Enable users to donate resources (money, food, medical supplies) to support various disaster missions.
- **Mission Management**: Volunteers are assigned to specific relief operations or missions, ensuring tasks are carried out effectively.
- **Resource Management**: Track and allocate physical resources required for disaster relief.
- **Reporting**: Generate reports on donations, missions, and volunteer participation.

---

## Features

1. **User Authentication & Registration**: 
   - Secure user login and registration using Azure Active Directory.
   - Roles include Admin, Coordinator, Volunteer, and Donor.
  
2. **Disaster Incident Reporting**: 
   - Allows users to report and view active disaster incidents.

3. **Donation Management**: 
   - Users can donate money or physical resources.
   - Donations are linked to specific disasters and missions.

4. **Mission & Volunteer Management**: 
   - Volunteers can register, view available missions, and be assigned to tasks.
   - Coordinators can create and manage missions and volunteer assignments.

5. **Resource Allocation**: 
   - Donations are allocated to resource requests for specific disasters or missions.
   - Resources such as medical supplies, food, and water are tracked.

---

## Prerequisites

Before you begin, ensure you have the following tools and software installed:

- **.NET SDK 6.0 or later**
- **Visual Studio 2022** or **Visual Studio Code**
- **SQL Server** (for local development) or **Azure SQL Database** (for cloud deployment)
- **Azure DevOps** (for repository and CI/CD pipeline)
- **Azure Active Directory** (for user authentication)
- **Node.js** (if frontend is built with React, Vue, or another JS framework)

---

## Installation

To install and run the **Gift of the Givers** web application locally, follow these steps:

### Step 1: Clone the Repository
```bash
git clone https://github.com/Leendouh/GiftOfTheGiversApp.git
cd gift-of-the-givers
````

### Step 2: Install Dependencies

Install the .NET dependencies for backend API:

```bash
dotnet restore
```

If you're using a frontend framework (e.g., React, Angular), install those dependencies:

```bash
npm install
```

### Step 3: Set up the Database

1. Create a local SQL Server or use **Azure SQL Database**.
2. Run the migration scripts to create the database schema:

```bash
dotnet ef database update
```

Ensure the `ConnectionStrings` section in the `appsettings.json` is correctly configured to point to your SQL database.

---

## Setup

### 1. **Azure Active Directory Authentication**

Set up authentication through Azure Active Directory (AAD). Create an app registration in the Azure portal to get the `ClientId`, `TenantId`, and `ClientSecret` that will be used for integration in `appsettings.json`.

```json
{
  "AzureAd": {
    "ClientId": "your-client-id",
    "TenantId": "your-tenant-id",
    "ClientSecret": "your-client-secret",
    "CallbackPath": "/signin-oidc"
  }
}
```

### 2. **Configure the Backend**

Make sure all configurations are updated in `appsettings.json`:

* Database connection string
* Authentication settings
* API keys for integration with payment systems (if applicable)

### 3. **Set Up Azure DevOps Pipeline**

* **Create a repository in Azure Repos**.
* **Set up an Azure Pipeline** to automate build and deployment. Configure the pipeline YAML file to trigger builds on changes.

```yaml
trigger:
  branches:
    include:
      - main

pool:
  vmImage: 'windows-latest'

steps:
- task: UseDotNet@2
  inputs:
    packageType: 'sdk'
    version: '6.x'
    installationPath: $(Agent.ToolsDirectory)/dotnet

- task: DotNetCoreCLI@2
  inputs:
    command: 'restore'
    projects: '**/*.csproj'

- task: DotNetCoreCLI@2
  inputs:
    command: 'build'
    projects: '**/*.csproj'

- task: DotNetCoreCLI@2
  inputs:
    command: 'publish'
    publishWebProjects: true
    arguments: '--configuration Release --output $(Build.ArtifactStagingDirectory)'

- task: AzureWebApp@1
  inputs:
    azureSubscription: 'your-azure-subscription'
    appName: 'your-app-name'
    package: '$(Build.ArtifactStagingDirectory)/**/*.zip'
```

---

## Database Schema

The application uses the following key tables in the **Azure SQL Database**:

* `dbo.Users` - Stores user information and roles.
* `dbo.Donations` - Manages donations, including donor details and donation amounts.
* `dbo.Missions` - Tracks active and completed missions.
* `dbo.Assignments` - Assigns volunteers to missions.
* `dbo.Resources` - Tracks the resources available for donation and allocation.
* `dbo.ResourceRequests` - Requests resources needed for missions or disasters.

---

## How to Use

1. **User Registration & Authentication**:

   * Navigate to the registration page, fill in your details, and create an account.
   * Once registered, you can log in using Azure Active Directory.

2. **Reporting a Disaster Incident**:

   * As an admin or coordinator, go to the 'Disasters' section to report new incidents, including location and status.

3. **Making a Donation**:

   * Donors can navigate to the donation section to contribute money or resources.
   * The system tracks donation status and automatically links to resource requests for missions or disasters.

4. **Volunteer Management**:

   * Volunteers can sign up and choose missions they wish to participate in.
   * Assignments are managed through the backend by the coordinators.

---

## Running the Application

To run the application locally, use the following commands:

1. **Run Backend**:

   ```bash
   dotnet run
   ```

2. **Run Frontend** (if applicable):

   ```bash
   npm start
   ```

The application will be hosted at `http://localhost:5000` by default.

---

## Contributing

We welcome contributions! If you want to help improve the **Gift of the Givers** platform, follow these steps:

1. Fork the repository.
2. Clone your fork locally.
3. Create a feature branch (`git checkout -b feature/your-feature`).
4. Commit your changes (`git commit -m 'Add your feature'`).
5. Push your changes (`git push origin feature/your-feature`).
6. Create a pull request to merge your changes.

---

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

Thank you for contributing to **Gift of the Givers**! We are excited to have you onboard in making a real-world impact through technology.

```

### Key Sections Breakdown:

- **Project Overview**: Gives a concise introduction to the project and its purpose.
- **Features**: Summarizes the core features.
- **Installation and Setup**: Instructions for getting the application running on a local machine.
- **Database Schema**: Description of key tables and their relationships.
- **Contributing**: How others can contribute to the project.
- **License**: Mentions the project license, encouraging open-source contributions.

This structure ensures clarity, easy navigation, and clear guidance for both developers and users.
```
