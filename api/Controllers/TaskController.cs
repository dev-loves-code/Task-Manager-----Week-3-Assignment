using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos.Task;
using api.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TaskController : ControllerBase
    {
        private readonly ITaskService _taskService;
        private readonly ILogger<TaskController> _logger;

        public TaskController(ITaskService taskService, ILogger<TaskController> logger)
        {
            _taskService = taskService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllTasks()
        {
            _logger.LogInformation("Processing GET request for all tasks from {RequestPath}", Request.Path);

            try
            {
                var tasks = await _taskService.GetAllTasksAsync();
                if (tasks == null || !tasks.Any())
                {
                    _logger.LogInformation("No tasks found, returning 404");
                    return NotFound("No tasks found.");
                }

                _logger.LogInformation("Successfully returned {TaskCount} tasks", tasks.Count());
                return Ok(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HTTP request failed for endpoint {Endpoint}", Request.Path);
                return StatusCode(500, "An error occurred while fetching tasks.");
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTaskById(int id)
        {
            _logger.LogInformation("Processing GET request for task {TaskId} from {RequestPath}", id, Request.Path);

            try
            {
                var task = await _taskService.GetTaskByIdAsync(id);
                if (task == null)
                {
                    _logger.LogInformation("Task {TaskId} not found, returning 404", id);
                    return NotFound($"Task with ID {id} not found.");
                }

                _logger.LogInformation("Successfully returned task {TaskId}", id);
                return Ok(task);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HTTP request failed for endpoint {Endpoint} with TaskId {TaskId}", Request.Path, id);
                return StatusCode(500, "An error occurred while fetching the task.");
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateTask([FromBody] CreateTaskDto taskDto)
        {
            _logger.LogInformation("Processing POST request to create task from {RequestPath}", Request.Path);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Model validation failed for task creation: {@ValidationErrors}",
                    ModelState.Where(x => x.Value.Errors.Count > 0).ToDictionary(k => k.Key, v => v.Value.Errors));
                return BadRequest(ModelState);
            }

            try
            {
                var task = await _taskService.CreateTaskAsync(taskDto);

                if (task == null)
                {
                    _logger.LogWarning("Task creation failed - service returned null result");
                    return BadRequest("Failed to create task.");
                }

                _logger.LogInformation("Successfully created task with ID {TaskId}", task.Id);
                return CreatedAtAction(nameof(GetTaskById), new { id = task.Id }, task);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HTTP request failed for endpoint {Endpoint} during task creation", Request.Path);
                return StatusCode(500, "An error occurred while creating the task.");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTask(int id, [FromBody] UpdateTaskDto taskDto)
        {
            _logger.LogInformation("Processing PUT request for task {TaskId} from {RequestPath}", id, Request.Path);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Model validation failed for task {TaskId} update: {@ValidationErrors}", id,
                    ModelState.Where(x => x.Value.Errors.Count > 0).ToDictionary(k => k.Key, v => v.Value.Errors));
                return BadRequest(ModelState);
            }

            try
            {
                var updatedTask = await _taskService.UpdateTaskAsync(id, taskDto);
                _logger.LogInformation("Successfully updated task {TaskId}", id);
                return Ok(updatedTask);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HTTP request failed for endpoint {Endpoint} with TaskId {TaskId}", Request.Path, id);
                return StatusCode(500, "An error occurred while updating the task.");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTask(int id)
        {
            _logger.LogInformation("Processing DELETE request for task {TaskId} from {RequestPath}", id, Request.Path);

            try
            {
                var result = await _taskService.DeleteTaskAsync(id);
                _logger.LogInformation("Successfully processed delete request for task {TaskId}", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HTTP request failed for endpoint {Endpoint} with TaskId {TaskId}", Request.Path, id);
                return StatusCode(500, "An error occurred while deleting the task.");
            }
        }
    }
}