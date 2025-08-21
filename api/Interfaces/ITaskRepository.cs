using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos.Task;

namespace api.Interfaces
{
    public interface ITaskRepository
    {
        Task<IEnumerable<Models.Task>> GetAllTasksAsync(string userId);
        Task<Models.Task?> GetTaskByIdAsync(int id, string userId);
        Task<Models.Task> CreateTaskAsync(Models.Task task);
        Task<Models.Task> UpdateTaskAsync(Models.Task task);
        Task<bool> DeleteTaskAsync(int id, string userId);
        Task<bool> TaskExistsAsync(int id, string userId);
        Task<IEnumerable<Models.Task>> GetUpcomingTasksAsync(string userId);
        Task<IEnumerable<Models.Task>> GetPastDueTasksAsync(string userId);
    }
}