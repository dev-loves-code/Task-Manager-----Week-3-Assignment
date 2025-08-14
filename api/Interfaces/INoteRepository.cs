using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos.Note;
using api.Models;

namespace api.Interfaces
{
    public interface INoteRepository
    {
        Task<Note?> GetNoteByIdAsync(int id);
        Task<IEnumerable<Note>> GetNotesByTaskIdAsync(int taskId);
        Task<Note> AddNoteAsync(Note note);
        Task<Note> UpdateNoteAsync(Note note);
        Task<Note> DeleteNoteAsync(int id);
    }
}