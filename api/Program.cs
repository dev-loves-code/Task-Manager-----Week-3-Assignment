using System.Text.Json.Serialization;
using api.Data;
using api.Dtos.Note;
using api.Dtos.Task;
using api.Interfaces;
using api.Repositories;
using api.Services;
using Mapster;
using Microsoft.EntityFrameworkCore;
using api.Models;
using api.Service;

var builder = WebApplication.CreateBuilder(args);


// Configure Serilog
builder.Host.SerilogConfiguration();

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.ReferenceHandler =
            ReferenceHandler.IgnoreCycles
    );
builder.Services.AddEndpointsApiExplorer(); // Required for Swagger
builder.Services.AddSwaggerGen(); // Adds Swagger generation
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<ITaskRepository, TaskRepository>();
builder.Services.AddScoped<INoteService, NoteService>();
builder.Services.AddScoped<INoteRepository, NoteRepository>();
builder.Services.AddScoped<IRedisCacheService, RedisCacheService>();

//redis
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("RedisConnection");
    options.InstanceName = "Task-Manager-Cache:";
});

//mapster
TypeAdapterConfig.GlobalSettings.Default
    .PreserveReference(true); // ← Breaks circular reference loops


TypeAdapterConfig<api.Models.Task, GetTaskDto>.NewConfig()
    .Map(dest => dest.Notes, src => src.Notes.Adapt<List<ResponseNoteDto>>());

// Configure Entity Framework Core with SQLSERVER
builder.Services.AddDbContext<ApplicationDBContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseStaticFiles();   // <---- Add this line
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
    });
}



app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();

internal class TaskDto
{
}