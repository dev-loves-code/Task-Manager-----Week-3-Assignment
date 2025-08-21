using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using api.Dtos.Task;
using api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TaskController : ControllerBase
    {
        private readonly ITaskService _taskService;
        private readonly ILogger<TaskController> _logger;
        private readonly INotificationService _notifications;

        public TaskController(ITaskService taskService, ILogger<TaskController> logger, INotificationService notifications)
        {
            _taskService = taskService;
            _logger = logger;
            _notifications = notifications;
        }


        private string GetUsername()
        {

            var username = User.FindFirst(ClaimTypes.GivenName)?.Value
                ?? User.FindFirst("given_name")?.Value;

            return username;
        }
        private string GetId()
        {

            var id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return id;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllTasks()
        {
            _logger.LogInformation("Processing GET request for all tasks from {RequestPath}", Request.Path);

            try
            {
                var username = GetUsername();
                var id = GetId();
                _logger.LogInformation("userId: {userId}", id);
                await _notifications.SendPrivateMessageAsync(id, "you are accessing the tasks... weeeeeeee");  // i used this for testing the notifications <3

                if (string.IsNullOrEmpty(username))
                {
                    _logger.LogWarning("Username not found in claims");
                    return Unauthorized("Unable to determine user identity");
                }

                var tasks = await _taskService.GetAllTasksAsync(username);
                if (tasks == null || !tasks.Any())
                {
                    _logger.LogInformation("No tasks found for user {Username}, returning empty list", username);
                    return Ok(new List<GetTaskDto>());
                }

                _logger.LogInformation("Successfully returned {TaskCount} tasks for user {Username}", tasks.Count(), username);
                return Ok(tasks);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt");
                return Unauthorized(ex.Message);
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
                var username = GetUsername();
                if (string.IsNullOrEmpty(username))
                {
                    _logger.LogWarning("Username not found in claims");
                    return Unauthorized("Unable to determine user identity");
                }

                var task = await _taskService.GetTaskByIdAsync(id, username);
                if (task == null)
                {
                    _logger.LogInformation("Task {TaskId} not found for user {Username}, returning 404", id, username);
                    return NotFound($"Task with ID {id} not found.");
                }

                _logger.LogInformation("Successfully returned task {TaskId} for user {Username}", id, username);
                return Ok(task);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt for task {TaskId}", id);
                return Unauthorized(ex.Message);
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
                var username = GetUsername();
                if (string.IsNullOrEmpty(username))
                {
                    _logger.LogWarning("Username not found in claims");
                    return Unauthorized("Unable to determine user identity");
                }

                var task = await _taskService.CreateTaskAsync(taskDto, username);

                if (task == null)
                {
                    _logger.LogWarning("Task creation failed - service returned null result for user {Username}", username);
                    return BadRequest("Failed to create task.");
                }

                _logger.LogInformation("Successfully created task with ID {TaskId} for user {Username}", task.Id, username);
                return CreatedAtAction(nameof(GetTaskById), new { id = task.Id }, task);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt during task creation");
                return Unauthorized(ex.Message);
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
                var username = GetUsername();
                if (string.IsNullOrEmpty(username))
                {
                    _logger.LogWarning("Username not found in claims");
                    return Unauthorized("Unable to determine user identity");
                }

                var updatedTask = await _taskService.UpdateTaskAsync(id, taskDto, username);
                _logger.LogInformation("Successfully updated task {TaskId} for user {Username}", id, username);
                return Ok(updatedTask);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogInformation("Task {TaskId} not found for update: {Message}", id, ex.Message);
                return NotFound(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt for task {TaskId} update", id);
                return Unauthorized(ex.Message);
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
                var username = GetUsername();
                if (string.IsNullOrEmpty(username))
                {
                    _logger.LogWarning("Username not found in claims");
                    return Unauthorized("Unable to determine user identity");
                }

                var result = await _taskService.DeleteTaskAsync(id, username);
                if (!result)
                {
                    _logger.LogInformation("Task {TaskId} not found for deletion for user {Username}", id, username);
                    return NotFound($"Task with ID {id} not found.");
                }

                _logger.LogInformation("Successfully deleted task {TaskId} for user {Username}", id, username);
                return NoContent();
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt for task {TaskId} deletion", id);
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HTTP request failed for endpoint {Endpoint} with TaskId {TaskId}", Request.Path, id);
                return StatusCode(500, "An error occurred while deleting the task.");
            }
        }


    }
}