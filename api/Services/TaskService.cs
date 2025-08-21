using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos.Task;
using api.Interfaces;
using api.Models;
using Mapster;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace api.Services
{
    public class TaskService : ITaskService
    {
        private readonly ITaskRepository _taskRepository;
        private readonly IRedisCacheService _redisCacheService;
        private readonly ILogger<TaskService> _logger;
        private readonly UserManager<AppUser> _userManager;

        private const string ALL_TASKS_CACHE_KEY_PREFIX = "AllTasks_";
        private const string TASK_CACHE_KEY_PREFIX = "Task_";

        public TaskService(ITaskRepository taskRepository, IRedisCacheService redisCacheService, ILogger<TaskService> logger, UserManager<AppUser> userManager)
        {
            _userManager = userManager;
            _taskRepository = taskRepository;
            _redisCacheService = redisCacheService;
            _logger = logger;
        }

        public async Task<IEnumerable<GetTaskDto>> GetAllTasksAsync(string username)
        {
            _logger.LogInformation("Starting to fetch all tasks for user: {Username}", username);

            try
            {
                var appUser = await _userManager.FindByNameAsync(username);
                if (appUser == null)
                {
                    _logger.LogWarning("User {Username} not found", username);
                    throw new UnauthorizedAccessException("User not found");
                }

                string cacheKey = $"{ALL_TASKS_CACHE_KEY_PREFIX}{appUser.Id}";
                var cachedTasks = _redisCacheService.GetData<IEnumerable<GetTaskDto>>(cacheKey);
                if (cachedTasks != null)
                {
                    _logger.LogInformation("Retrieved {TaskCount} tasks from cache for user {Username}", cachedTasks.Count(), username);
                    return cachedTasks;
                }

                _logger.LogInformation("Cache miss - fetching tasks from repository for user {Username}", username);
                var tasks = await _taskRepository.GetAllTasksAsync(appUser.Id);
                var taskDtos = tasks.Adapt<IEnumerable<GetTaskDto>>();

                if (taskDtos.Any())
                {
                    _redisCacheService.SetData(cacheKey, taskDtos);
                    _logger.LogInformation("Cached {TaskCount} tasks for user {Username}", taskDtos.Count(), username);
                }

                return taskDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve all tasks for user {Username}", username);
                throw;
            }
        }

        public async Task<GetTaskDto?> GetTaskByIdAsync(int id, string username)
        {
            _logger.LogInformation("Fetching task with ID {TaskId} for user {Username}", id, username);

            try
            {
                var appUser = await _userManager.FindByNameAsync(username);
                if (appUser == null)
                {
                    _logger.LogWarning("User {Username} not found", username);
                    throw new UnauthorizedAccessException("User not found");
                }

                string cacheKey = $"{TASK_CACHE_KEY_PREFIX}{id}_{appUser.Id}";
                var cachedTask = _redisCacheService.GetData<GetTaskDto>(cacheKey);

                if (cachedTask != null)
                {
                    _logger.LogInformation("Retrieved task {TaskId} from cache for user {Username}", id, username);
                    return cachedTask;
                }

                _logger.LogInformation("Cache miss - fetching task {TaskId} from repository for user {Username}", id, username);
                var task = await _taskRepository.GetTaskByIdAsync(id, appUser.Id);

                if (task == null)
                {
                    _logger.LogWarning("Task {TaskId} not found for user {Username}", id, username);
                    return null;
                }

                var taskDto = task.Adapt<GetTaskDto>();
                _redisCacheService.SetData(cacheKey, taskDto);
                _logger.LogInformation("Cached task {TaskId} for user {Username}", id, username);

                return taskDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve task {TaskId} for user {Username}", id, username);
                throw;
            }
        }

        public async Task<GetTaskDto> CreateTaskAsync(CreateTaskDto taskDto, string username)
        {
            _logger.LogInformation("Creating new task with title: {TaskTitle} for user {Username}", taskDto.Title, username);

            try
            {
                var appUser = await _userManager.FindByNameAsync(username);
                if (appUser == null)
                {
                    _logger.LogWarning("User {Username} not found", username);
                    throw new UnauthorizedAccessException("User not found");
                }

                var task = taskDto.Adapt<Models.Task>();
                task.UserId = appUser.Id;

                var taskCreated = await _taskRepository.CreateTaskAsync(task);

                if (taskCreated == null)
                {
                    _logger.LogError("Repository returned null when creating task");
                    throw new InvalidOperationException("Failed to create task");
                }

                var resultDto = taskCreated.Adapt<GetTaskDto>();


                string allTasksCacheKey = $"{ALL_TASKS_CACHE_KEY_PREFIX}{appUser.Id}";
                _redisCacheService.RemoveData(allTasksCacheKey);
                _logger.LogInformation("Invalidated all tasks cache for user {Username} after creating task {TaskId}", username, taskCreated.Id);


                string taskCacheKey = $"{TASK_CACHE_KEY_PREFIX}{taskCreated.Id}_{appUser.Id}";
                _redisCacheService.SetData(taskCacheKey, resultDto);

                _logger.LogInformation("Successfully created task {TaskId} with title: {TaskTitle} for user {Username}",
                    taskCreated.Id, taskDto.Title, username);

                return resultDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create task for user {Username}", username);
                throw;
            }
        }

        public async Task<GetTaskDto> UpdateTaskAsync(int id, UpdateTaskDto taskDto, string username)
        {
            _logger.LogInformation("Updating task {TaskId} for user {Username}", id, username);

            try
            {
                var appUser = await _userManager.FindByNameAsync(username);
                if (appUser == null)
                {
                    _logger.LogWarning("User {Username} not found", username);
                    throw new UnauthorizedAccessException("User not found");
                }

                var existingTask = await _taskRepository.GetTaskByIdAsync(id, appUser.Id);
                if (existingTask == null)
                {
                    _logger.LogWarning("Task {TaskId} not found for user {Username}", id, username);
                    throw new KeyNotFoundException($"Task with ID {id} not found");
                }

                _logger.LogInformation("Updating task {TaskId} - Title: '{OldTitle}' -> '{NewTitle}' for user {Username}",
                    id, existingTask.Title, taskDto.Title, username);

                existingTask.Title = taskDto.Title;
                existingTask.Description = taskDto.Description;
                existingTask.DueDate = taskDto.DueDate;
                existingTask.IsCompleted = taskDto.IsCompleted;

                var updatedTask = await _taskRepository.UpdateTaskAsync(existingTask);
                var resultDto = updatedTask.Adapt<GetTaskDto>();

                await InvalidateTaskCacheAsync(id, appUser.Id);
                _logger.LogInformation("Successfully updated task {TaskId} for user {Username}", id, username);

                return resultDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update task {TaskId} for user {Username}", id, username);
                throw;
            }
        }

        public async Task<bool> DeleteTaskAsync(int id, string username)
        {
            _logger.LogInformation("Deleting task {TaskId} for user {Username}", id, username);

            try
            {
                var appUser = await _userManager.FindByNameAsync(username);
                if (appUser == null)
                {
                    _logger.LogWarning("User {Username} not found", username);
                    throw new UnauthorizedAccessException("User not found");
                }

                var exists = await _taskRepository.TaskExistsAsync(id, appUser.Id);
                if (!exists)
                {
                    _logger.LogWarning("Cannot delete task {TaskId} - task does not exist for user {Username}", id, username);
                    return false;
                }

                var result = await _taskRepository.DeleteTaskAsync(id, appUser.Id);

                if (result)
                {
                    await InvalidateTaskCacheAsync(id, appUser.Id);
                    _logger.LogInformation("Successfully deleted task {TaskId} for user {Username}", id, username);
                }
                else
                {
                    _logger.LogWarning("Failed to delete task {TaskId} for user {Username} - repository operation failed", id, username);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete task {TaskId} for user {Username}", id, username);
                throw;
            }
        }

        public async Task<bool> TaskExistsAsync(int id, string username)
        {
            _logger.LogDebug("Checking if task {TaskId} exists for user {Username}", id, username);

            try
            {
                var appUser = await _userManager.FindByNameAsync(username);
                if (appUser == null)
                {
                    _logger.LogWarning("User {Username} not found", username);
                    return false;
                }

                string cacheKey = $"{TASK_CACHE_KEY_PREFIX}{id}_{appUser.Id}";
                var cachedTask = _redisCacheService.GetData<GetTaskDto>(cacheKey);

                if (cachedTask != null)
                {
                    _logger.LogDebug("Task {TaskId} exists for user {Username} (found in cache)", id, username);
                    return true;
                }

                var exists = await _taskRepository.TaskExistsAsync(id, appUser.Id);
                _logger.LogDebug("Task {TaskId} exists for user {Username}: {Exists}", id, username, exists);
                return exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check if task {TaskId} exists for user {Username}", id, username);
                throw;
            }
        }

        private async System.Threading.Tasks.Task InvalidateTaskCacheAsync(int taskId, string userId)
        {
            try
            {

                string taskCacheKey = $"{TASK_CACHE_KEY_PREFIX}{taskId}_{userId}";
                _redisCacheService.RemoveData(taskCacheKey);


                string allTasksCacheKey = $"{ALL_TASKS_CACHE_KEY_PREFIX}{userId}";
                _redisCacheService.RemoveData(allTasksCacheKey);

                _logger.LogDebug("Invalidated cache for task {TaskId} and user {UserId} tasks list", taskId, userId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to invalidate cache for task {TaskId} and user {UserId}", taskId, userId);
            }
        }


        public async Task<IEnumerable<GetTaskDto>> GetPastDueTasks(string userId)
        {
            var tasks = await _taskRepository.GetAllTasksAsync(userId);
            var taskDtos = tasks.Adapt<IEnumerable<GetTaskDto>>();
            return taskDtos;
        }

        public async Task<IEnumerable<GetTaskDto>> GetUpcommingTasks(string userId)
        {
            var tasks = await _taskRepository.GetAllTasksAsync(userId);
            var taskDtos = tasks.Adapt<IEnumerable<GetTaskDto>>();
            return taskDtos;
        }
    }
}