using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Data;
using api.Interfaces;
using api.Models;
using Microsoft.EntityFrameworkCore;

namespace api.Repositories
{
    public class NoteRepository : INoteRepository
    {
        private readonly ApplicationDBContext _context;

        public NoteRepository(ApplicationDBContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Note>> GetNotesByTaskIdAsync(int taskId, string userId)
        {

            return await _context.Notes
                .Include(n => n.Task)
                .Where(n => n.TaskId == taskId && n.Task.UserId == userId)
                .OrderBy(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task<Note?> GetNoteByIdAsync(int id, string userId)
        {

            return await _context.Notes
                .Include(n => n.Task)
                .FirstOrDefaultAsync(n => n.Id == id && n.Task.UserId == userId);
        }

        public async Task<Note> AddNoteAsync(Note note)
        {
            await _context.Notes.AddAsync(note);
            await _context.SaveChangesAsync();
            return note;
        }

        public async Task<Note> UpdateNoteAsync(Note note)
        {
            _context.Notes.Update(note);
            await _context.SaveChangesAsync();
            return note;
        }

        public async Task<Note> DeleteNoteAsync(int id)
        {
            var note = await _context.Notes.FindAsync(id);
            if (note == null) return null;

            _context.Notes.Remove(note);
            await _context.SaveChangesAsync();
            return note;
        }

        public async Task<bool> CanUserAccessNote(int noteId, string userId)
        {
            return await _context.Notes
                .Include(n => n.Task)
                .AnyAsync(n => n.Id == noteId && n.Task.UserId == userId);
        }

        public async Task<bool> CanUserAccessTaskNotes(int taskId, string userId)
        {
            return await _context.Tasks
                .AnyAsync(t => t.Id == taskId && t.UserId == userId);
        }
    }
}