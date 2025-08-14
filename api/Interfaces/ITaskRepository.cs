using System;
using System.Collections.Generic;
using System.Linq;
using api.Dtos.Task;
using api.Models;

namespace api.Interfaces
{
    public interface ITaskRepository
    {
        Task<IEnumerable<Models.Task>> GetAllTasksAsync();
        Task<Models.Task?> GetTaskByIdAsync(int id);
        Task<Models.Task> CreateTaskAsync(Models.Task task);
        Task<Models.Task> UpdateTaskAsync(Models.Task task);
        Task<bool> DeleteTaskAsync(int id);
        Task<bool> TaskExistsAsync(int id);

    }
}