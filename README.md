## Configurable Workflow Engine (State-Machine API)
A minimal backend service built with .NET 8 that enables clients to define configurable workflows as state machines, manage workflow instances, and execute state transitions with full validation.

### Prerequisites
- .NET 8 SDK installed
- Postman
### Running the Application
1. **Clone the repository**
2. **Run the application:**  dotnet run
3. **The API will be available at:**  http://localhost:5000

## API Endpoints
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

## Quick Testing Guide (Postman)
### Test Sequence

#### 1. Create Workflow Definition
- **Method:** POST
- **URL:** `http://localhost:5000/api/workflows`
- **Headers:** `Content-Type: application/json`
- **Body:** {
"name": "Simple Approval",
"states": [
{
"id": "draft",
"name": "Draft",
"isInitial": true,
"isFinal": false,
"enabled": true
},
{
"id": "approved",
"name": "Approved",
"isInitial": false,
"isFinal": true,
"enabled": true
}
],
"actions": [
{
"id": "approve",
"name": "Approve",
"enabled": true,
"fromStates": ["draft"],
"toState": "approved"
}
]
}
- **Expected:** 201 Created + workflow with ID
- **Note:** Copy the workflow ID from the response

#### 2. Start Workflow Instance
- **Method:** POST
- **URL:** `http://localhost:5000/api/workflows/{WORKFLOW_ID}/instances`
- **Expected:** 201 Created + instance with ID
- **Note:** Copy the instance ID from the response

#### 3. Execute Action
- **Method:** POST
- **URL:** `http://localhost:5000/api/instances/{INSTANCE_ID}/actions/approve`
- **Expected:** 200 OK + updated instance in "approved" state

#### 4. Verify Instance State
- **Method:** GET
- **URL:** `http://localhost:5000/api/instances/{INSTANCE_ID}`
- **Expected:** 200 OK + instance showing currentStateId: "approved" with history

#### 5. Test Invalid Action (Should Fail)
- **Method:** POST
- **URL:** `http://localhost:5000/api/instances/{INSTANCE_ID}/actions/approve`
- **Expected:** 400 Bad Request + error message about final state

### Quick Health Check
- **GET** `http://localhost:5000/api/workflows` → Should return empty array `[]`
- **GET** `http://localhost:5000/api/instances` → Should return your created instances
