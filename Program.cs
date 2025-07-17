using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddSingleton<IWorkflowService, WorkflowService>();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseRouting();

// Workflow Definition endpoints
app.MapPost("/api/workflows", CreateWorkflowDefinition);
app.MapGet("/api/workflows/{id}", GetWorkflowDefinition);
app.MapGet("/api/workflows", GetAllWorkflowDefinitions);

// Workflow Instance endpoints
app.MapPost("/api/workflows/{definitionId}/instances", StartWorkflowInstance);
app.MapGet("/api/instances/{instanceId}", GetWorkflowInstance);
app.MapGet("/api/instances", GetAllWorkflowInstances);
app.MapPost("/api/instances/{instanceId}/actions/{actionId}", ExecuteAction);

app.Run();

// API Handlers
IResult CreateWorkflowDefinition(
    [FromBody] CreateWorkflowDefinitionRequest request,
    [FromServices] IWorkflowService workflowService)
{
    try
    {
        var definition = workflowService.CreateWorkflowDefinition(request.Name, request.States, request.Actions);
        return Results.Created($"/api/workflows/{definition.Id}", definition);
    }
    catch (ValidationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
}

IResult GetWorkflowDefinition(
    string id,
    [FromServices] IWorkflowService workflowService)
{
    var definition = workflowService.GetWorkflowDefinition(id);
    return definition != null ? Results.Ok(definition) : Results.NotFound();
}

IResult GetAllWorkflowDefinitions([FromServices] IWorkflowService workflowService)
{
    var definitions = workflowService.GetAllWorkflowDefinitions();
    return Results.Ok(definitions);
}

IResult StartWorkflowInstance(
    string definitionId,
    [FromServices] IWorkflowService workflowService)
{
    try
    {
        var instance = workflowService.StartWorkflowInstance(definitionId);
        return Results.Created($"/api/instances/{instance.Id}", instance);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
}

IResult GetWorkflowInstance(
    string instanceId,
    [FromServices] IWorkflowService workflowService)
{
    var instance = workflowService.GetWorkflowInstance(instanceId);
    return instance != null ? Results.Ok(instance) : Results.NotFound();
}

IResult GetAllWorkflowInstances([FromServices] IWorkflowService workflowService)
{
    var instances = workflowService.GetAllWorkflowInstances();
    return Results.Ok(instances);
}

IResult ExecuteAction(
    string instanceId,
    string actionId,
    [FromServices] IWorkflowService workflowService)
{
    try
    {
        var instance = workflowService.ExecuteAction(instanceId, actionId);
        return Results.Ok(instance);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
}

// Models
public class State
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsInitial { get; set; }
    public bool IsFinal { get; set; }
    public bool Enabled { get; set; } = true;
    public string? Description { get; set; }
}

public class WorkflowAction
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public List<string> FromStates { get; set; } = new();
    public string ToState { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class WorkflowDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<State> States { get; set; } = new();
    public List<WorkflowAction> Actions { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class ActionHistoryEntry
{
    public string ActionId { get; set; } = string.Empty;
    public string ActionName { get; set; } = string.Empty;
    public string FromState { get; set; } = string.Empty;
    public string ToState { get; set; } = string.Empty;
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
}

public class WorkflowInstance
{
    public string Id { get; set; } = string.Empty;
    public string DefinitionId { get; set; } = string.Empty;
    public string CurrentStateId { get; set; } = string.Empty;
    public List<ActionHistoryEntry> History { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastModifiedAt { get; set; } = DateTime.UtcNow;
}

// Request models
public class CreateWorkflowDefinitionRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    public List<State> States { get; set; } = new();
    
    [Required]
    public List<WorkflowAction> Actions { get; set; } = new();
}

// Service Interface
public interface IWorkflowService
{
    WorkflowDefinition CreateWorkflowDefinition(string name, List<State> states, List<WorkflowAction> actions);
    WorkflowDefinition? GetWorkflowDefinition(string id);
    List<WorkflowDefinition> GetAllWorkflowDefinitions();
    WorkflowInstance StartWorkflowInstance(string definitionId);
    WorkflowInstance? GetWorkflowInstance(string instanceId);
    List<WorkflowInstance> GetAllWorkflowInstances();
    WorkflowInstance ExecuteAction(string instanceId, string actionId);
}

// Service Implementation
public class WorkflowService : IWorkflowService
{
    private readonly Dictionary<string, WorkflowDefinition> _definitions = new();
    private readonly Dictionary<string, WorkflowInstance> _instances = new();

    public WorkflowDefinition CreateWorkflowDefinition(string name, List<State> states, List<WorkflowAction> actions)
    {
        // Validation
        ValidateWorkflowDefinition(name, states, actions);

        var definition = new WorkflowDefinition
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            States = states,
            Actions = actions
        };

        _definitions[definition.Id] = definition;
        return definition;
    }

    public WorkflowDefinition? GetWorkflowDefinition(string id)
    {
        return _definitions.TryGetValue(id, out var definition) ? definition : null;
    }

    public List<WorkflowDefinition> GetAllWorkflowDefinitions()
    {
        return _definitions.Values.ToList();
    }

    public WorkflowInstance StartWorkflowInstance(string definitionId)
    {
        var definition = GetWorkflowDefinition(definitionId);
        if (definition == null)
        {
            throw new ArgumentException($"Workflow definition '{definitionId}' not found");
        }

        var initialState = definition.States.FirstOrDefault(s => s.IsInitial);
        if (initialState == null)
        {
            throw new InvalidOperationException($"No initial state found in workflow definition '{definitionId}'");
        }

        var instance = new WorkflowInstance
        {
            Id = Guid.NewGuid().ToString(),
            DefinitionId = definitionId,
            CurrentStateId = initialState.Id
        };

        _instances[instance.Id] = instance;
        return instance;
    }

    public WorkflowInstance? GetWorkflowInstance(string instanceId)
    {
        return _instances.TryGetValue(instanceId, out var instance) ? instance : null;
    }

    public List<WorkflowInstance> GetAllWorkflowInstances()
    {
        return _instances.Values.ToList();
    }

    public WorkflowInstance ExecuteAction(string instanceId, string actionId)
    {
        var instance = GetWorkflowInstance(instanceId);
        if (instance == null)
        {
            throw new ArgumentException($"Workflow instance '{instanceId}' not found");
        }

        var definition = GetWorkflowDefinition(instance.DefinitionId);
        if (definition == null)
        {
            throw new InvalidOperationException($"Workflow definition '{instance.DefinitionId}' not found");
        }

        var action = definition.Actions.FirstOrDefault(a => a.Id == actionId);
        if (action == null)
        {
            throw new ArgumentException($"Action '{actionId}' not found in workflow definition");
        }

        var currentState = definition.States.FirstOrDefault(s => s.Id == instance.CurrentStateId);
        if (currentState == null)
        {
            throw new InvalidOperationException($"Current state '{instance.CurrentStateId}' not found in workflow definition");
        }

        // Validation rules
        if (!action.Enabled)
        {
            throw new InvalidOperationException($"Action '{actionId}' is disabled");
        }

        if (currentState.IsFinal)
        {
            throw new InvalidOperationException($"Cannot execute actions on final state '{currentState.Id}'");
        }

        if (!action.FromStates.Contains(instance.CurrentStateId))
        {
            throw new InvalidOperationException($"Action '{actionId}' cannot be executed from current state '{instance.CurrentStateId}'");
        }

        var targetState = definition.States.FirstOrDefault(s => s.Id == action.ToState);
        if (targetState == null)
        {
            throw new InvalidOperationException($"Target state '{action.ToState}' not found in workflow definition");
        }

        // Execute the action
        var historyEntry = new ActionHistoryEntry
        {
            ActionId = action.Id,
            ActionName = action.Name,
            FromState = instance.CurrentStateId,
            ToState = action.ToState
        };

        instance.History.Add(historyEntry);
        instance.CurrentStateId = action.ToState;
        instance.LastModifiedAt = DateTime.UtcNow;

        return instance;
    }

    private void ValidateWorkflowDefinition(string name, List<State> states, List<WorkflowAction> actions)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ValidationException("Workflow name cannot be empty");
        }

        if (!states.Any())
        {
            throw new ValidationException("Workflow must have at least one state");
        }

        // Check for duplicate state IDs
        var stateIds = states.Select(s => s.Id).ToList();
        if (stateIds.Count != stateIds.Distinct().Count())
        {
            throw new ValidationException("Duplicate state IDs found");
        }

        // Check for exactly one initial state
        var initialStates = states.Where(s => s.IsInitial).ToList();
        if (initialStates.Count != 1)
        {
            throw new ValidationException($"Workflow must have exactly one initial state, found {initialStates.Count}");
        }

        // Check for duplicate action IDs
        var actionIds = actions.Select(a => a.Id).ToList();
        if (actionIds.Count != actionIds.Distinct().Count())
        {
            throw new ValidationException("Duplicate action IDs found");
        }

        // Validate actions reference valid states
        foreach (var action in actions)
        {
            if (!stateIds.Contains(action.ToState))
            {
                throw new ValidationException($"Action '{action.Id}' references unknown target state '{action.ToState}'");
            }

            foreach (var fromState in action.FromStates)
            {
                if (!stateIds.Contains(fromState))
                {
                    throw new ValidationException($"Action '{action.Id}' references unknown source state '{fromState}'");
                }
            }

            if (!action.FromStates.Any())
            {
                throw new ValidationException($"Action '{action.Id}' must have at least one source state");
            }
        }
    }
}