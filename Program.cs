using Cassandra;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:5223");
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "TodoApp API",
        Version = "v1",
        Description = "API para gestionar tareas con Cassandra"
    });
});


builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();


app.UseCors();


var cluster = Cluster.Builder()
    .AddContactPoint("192.168.100.183") 
    .Build();
var session = cluster.Connect("todo_app");


app.MapGet("/tasks", () =>
{
    var rs = session.Execute("SELECT * FROM tasks;");
    var tasks = rs.Select(row => new
    {
        id = row.GetValue<Guid>("task_id"),
        titulo = row.GetValue<string>("title"),
        completada = row.GetValue<bool>("is_completed")
    });
    return tasks;
});

app.MapPost("/tasks", ([FromBody] TaskItem task) =>
{
    var id = Guid.NewGuid();
    session.Execute(new SimpleStatement("INSERT INTO tasks (task_id, title, is_completed) VALUES (?, ?, ?);",
        id, task.Titulo, task.Completada));
    return Results.Created($"/tasks/{id}", new { id, task.Titulo, task.Completada });
});

app.MapPut("/tasks/{id}", (Guid id, [FromBody] TaskItem task) =>
{
    session.Execute(new SimpleStatement("UPDATE tasks SET title = ?, is_completed = ? WHERE task_id = ?;",
        task.Titulo, task.Completada, id));
    return Results.Ok();
});

app.MapDelete("/tasks/{id}", (Guid id) =>
{
    session.Execute(new SimpleStatement("DELETE FROM tasks WHERE task_id = ?;", id));
    return Results.Ok();
});

app.Run();

public record TaskItem(string Titulo, bool Completada);
