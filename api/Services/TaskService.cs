using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos.Task;
using api.Interfaces;
using Mapster;
using Microsoft.Extensions.Logging;

namespace api.Services
{
    public class TaskService : ITaskService
    {
        private readonly ITaskRepository _taskRepository;
        private readonly IRedisCacheService _redisCacheService;
        private readonly ILogger<TaskService> _logger;

        private const string ALL_TASKS_CACHE_KEY = "AllTasks";
        private const string TASK_CACHE_KEY_PREFIX = "Task_";


        public TaskService(ITaskRepository taskRepository, IRedisCacheService redisCacheService, ILogger<TaskService> logger)
        {
            _taskRepository = taskRepository;
            _redisCacheService = redisCacheService;
            _logger = logger;
        }

        public async Task<IEnumerable<GetTaskDto>> GetAllTasksAsync()
        {
            _logger.LogInformation("Starting to fetch all tasks");

            try
            {

                var cachedTasks = _redisCacheService.GetData<IEnumerable<GetTaskDto>>(ALL_TASKS_CACHE_KEY);
                if (cachedTasks != null)
                {
                    _logger.LogInformation("Retrieved {TaskCount} tasks from cache", cachedTasks.Count());
                    return cachedTasks;
                }

                _logger.LogInformation("Cache miss - fetching tasks from repository");
                var tasks = await _taskRepository.GetAllTasksAsync();
                var taskDtos = tasks.Adapt<IEnumerable<GetTaskDto>>();


                if (taskDtos.Any())
                {
                    _redisCacheService.SetData(ALL_TASKS_CACHE_KEY, taskDtos);
                    _logger.LogInformation("Cached {TaskCount} tasks ",
                        taskDtos.Count());
                }

                return taskDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve all tasks");
                throw;
            }
        }

        public async Task<GetTaskDto?> GetTaskByIdAsync(int id)
        {
            _logger.LogInformation("Fetching task with ID {TaskId}", id);

            try
            {
                string cacheKey = $"{TASK_CACHE_KEY_PREFIX}{id}";
                var cachedTask = _redisCacheService.GetData<GetTaskDto>(cacheKey);

                if (cachedTask != null)
                {
                    _logger.LogInformation("Retrieved task {TaskId} from cache", id);
                    return cachedTask;
                }

                _logger.LogInformation("Cache miss - fetching task {TaskId} from repository", id);
                var task = await _taskRepository.GetTaskByIdAsync(id);

                if (task == null)
                {
                    _logger.LogWarning("Task {TaskId} not found in repository", id);
                    return null;
                }

                var taskDto = task.Adapt<GetTaskDto>();
                _redisCacheService.SetData(cacheKey, taskDto);
                _logger.LogInformation("Cached task {TaskId}",
                    id);

                return taskDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve task {TaskId}", id);
                throw;
            }
        }

        public async Task<GetTaskDto> CreateTaskAsync(CreateTaskDto taskDto)
        {
            _logger.LogInformation("Creating new task with title: {TaskTitle}", taskDto.Title);



            var task = taskDto.Adapt<Models.Task>();
            var taskCreated = await _taskRepository.CreateTaskAsync(task);

            if (taskCreated == null)
            {
                _logger.LogError("Repository returned null when creating task");
                throw new InvalidOperationException("Failed to create task");
            }

            var resultDto = taskCreated.Adapt<GetTaskDto>();


            _redisCacheService.RemoveData(ALL_TASKS_CACHE_KEY);
            _logger.LogInformation("Invalidated all tasks cache after creating task {TaskId}", taskCreated.Id);


            string cacheKey = $"{TASK_CACHE_KEY_PREFIX}{taskCreated.Id}";
            _redisCacheService.SetData(cacheKey, resultDto);

            _logger.LogInformation("Successfully created task {TaskId} with title: {TaskTitle}",
                taskCreated.Id, taskDto.Title);

            return resultDto;

        }

        public async Task<GetTaskDto> UpdateTaskAsync(int id, UpdateTaskDto taskDto)
        {
            _logger.LogInformation("Updating task {TaskId}", id);


            var existingTask = await _taskRepository.GetTaskByIdAsync(id);
            if (existingTask == null)
            {
                _logger.LogWarning("Task {TaskId} not found for update", id);
                throw new KeyNotFoundException($"Task with ID {id} not found");
            }

            _logger.LogInformation("Updating task {TaskId} - Title: '{OldTitle}' -> '{NewTitle}'",
                id, existingTask.Title, taskDto.Title);


            existingTask.Title = taskDto.Title;
            existingTask.Description = taskDto.Description;
            existingTask.DueDate = taskDto.DueDate;
            existingTask.IsCompleted = taskDto.IsCompleted;

            var updatedTask = await _taskRepository.UpdateTaskAsync(existingTask);
            var resultDto = updatedTask.Adapt<GetTaskDto>();


            await InvalidateTaskCacheAsync(id);
            _logger.LogInformation("Successfully updated task {TaskId}", id);

            return resultDto;

        }

        public async Task<bool> DeleteTaskAsync(int id)
        {
            _logger.LogInformation("Deleting task {TaskId}", id);

            try
            {

                var exists = await _taskRepository.TaskExistsAsync(id);
                if (!exists)
                {
                    _logger.LogWarning("Cannot delete task {TaskId} - task does not exist", id);
                    return false;
                }

                var result = await _taskRepository.DeleteTaskAsync(id);

                if (result)
                {

                    await InvalidateTaskCacheAsync(id);
                    _logger.LogInformation("Successfully deleted task {TaskId}", id);
                }
                else
                {
                    _logger.LogWarning("Failed to delete task {TaskId} - repository operation failed", id);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete task {TaskId}", id);
                throw;
            }
        }

        public async Task<bool> TaskExistsAsync(int id)
        {
            _logger.LogDebug("Checking if task {TaskId} exists", id);

            try
            {

                string cacheKey = $"{TASK_CACHE_KEY_PREFIX}{id}";
                var cachedTask = _redisCacheService.GetData<GetTaskDto>(cacheKey);

                if (cachedTask != null)
                {
                    _logger.LogDebug("Task {TaskId} exists (found in cache)", id);
                    return true;
                }

                var exists = await _taskRepository.TaskExistsAsync(id);
                _logger.LogDebug("Task {TaskId} exists: {Exists}", id, exists);
                return exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check if task {TaskId} exists", id);
                throw;
            }
        }

        private async Task InvalidateTaskCacheAsync(int taskId)
        {
            try
            {

                string taskCacheKey = $"{TASK_CACHE_KEY_PREFIX}{taskId}";
                _redisCacheService.RemoveData(taskCacheKey);


                _redisCacheService.RemoveData(ALL_TASKS_CACHE_KEY);

                _logger.LogDebug("Invalidated cache for task {TaskId} and all tasks list", taskId);
            }
            catch (Exception ex)
            {

                _logger.LogWarning(ex, "Failed to invalidate cache for task {TaskId}", taskId);
            }
        }
    }
}