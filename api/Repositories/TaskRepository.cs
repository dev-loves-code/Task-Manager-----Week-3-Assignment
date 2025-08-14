using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Data;
using api.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace api.Repositories
{
    public class TaskRepository : ITaskRepository
    {
        private readonly ApplicationDBContext _context;

        public TaskRepository(ApplicationDBContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Models.Task>> GetAllTasksAsync()
        {
            return await _context.Tasks.Include(c => c.Notes).ToListAsync();
        }

        public async Task<Models.Task?> GetTaskByIdAsync(int id)
        {
            return await _context.Tasks.Include(c => c.Notes).FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<Models.Task> CreateTaskAsync(Models.Task task)
        {
            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();
            return task;
        }

        public async Task<Models.Task> UpdateTaskAsync(Models.Task task)
        {
            _context.Tasks.Update(task);
            await _context.SaveChangesAsync();
            return task;
        }

        public async Task<bool> DeleteTaskAsync(int id)
        {
            var task = await GetTaskByIdAsync(id);
            if (task == null) return false;

            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> TaskExistsAsync(int id)
        {
            return await _context.Tasks.AnyAsync(t => t.Id == id);
        }
    }

}