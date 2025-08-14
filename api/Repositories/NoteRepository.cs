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

        public async Task<IEnumerable<Note>> GetNotesByTaskIdAsync(int taskId)
        {
            return await _context.Notes
                .Where(n => n.TaskId == taskId)
                .ToListAsync();
        }

        public async Task<Note> UpdateNoteAsync(Note note)
        {
            _context.Notes.Update(note);
            await _context.SaveChangesAsync();
            return note;
        }

        public async Task<Note> DeleteNoteAsync(int id)
        {
            var note = await GetNoteByIdAsync(id);
            if (note == null) return null;

            _context.Notes.Remove(note);
            await _context.SaveChangesAsync();
            return note;
        }

        public async Task<Note?> GetNoteByIdAsync(int id)
        {
            return await _context.Notes.FindAsync(id);
        }

        public async Task<Note> AddNoteAsync(Note note)
        {
            await _context.Notes.AddAsync(note);
            await _context.SaveChangesAsync();
            return note;

        }
    }
}