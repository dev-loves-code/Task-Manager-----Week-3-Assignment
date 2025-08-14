using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos.Task;

namespace api.Interfaces
{
    public interface ITaskService
    {
        Task<IEnumerable<GetTaskDto>> GetAllTasksAsync();
        Task<GetTaskDto?> GetTaskByIdAsync(int id);
        Task<GetTaskDto> CreateTaskAsync(CreateTaskDto task);
        Task<GetTaskDto> UpdateTaskAsync(int id, UpdateTaskDto task);
        Task<bool> DeleteTaskAsync(int id);
        Task<bool> TaskExistsAsync(int id);
    }
}