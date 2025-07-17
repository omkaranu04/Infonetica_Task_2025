## Configurable Workflow Engine (State-Machine API)
A minimal backend service built with .NET 8 that enables clients to define configurable workflows as state machines, manage workflow instances, and execute state transitions with full validation.

## Quick Start

### Prerequisites
- .NET 8 SDK installed
- Postman

  ### Running the Application
1. **Clone the repository:**
2. **Run the application:**
  dotnet run
3. **The API will be available at:**
  http://localhost:5000

## API Endpoints

### Workflow Definition Management

#### Create Workflow Definition
- **Method:** POST
- **URL:** `http://localhost:5000/api/workflows`
- **Headers:** `Content-Type: application/json`

#### Get All Workflow Definitions
- **Method:** GET
- **URL:** `http://localhost:5000/api/workflows`

#### Get Specific Workflow Definition
- **Method:** GET
- **URL:** `http://localhost:5000/api/workflows/{definitionId}`

### Workflow Instance Management

#### Start New Workflow Instance
- **Method:** POST
- **URL:** `http://localhost:5000/api/workflows/{definitionId}/instances`
- **Note:** Replace `{definitionId}` with the actual workflow definition ID

#### Get All Workflow Instances
- **Method:** GET
- **URL:** `http://localhost:5000/api/instances`

#### Get Specific Workflow Instance
- **Method:** GET
- **URL:** `http://localhost:5000/api/instances/{instanceId}`

#### Execute Action on Instance
- **Method:** POST
- **URL:** `http://localhost:5000/api/instances/{instanceId}/actions/{actionId}`
- **Note:** Replace `{instanceId}` and `{actionId}` with actual IDs
