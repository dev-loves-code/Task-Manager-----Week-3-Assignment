using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos.Note;
using api.Models;

namespace api.Interfaces
{
    public interface INoteService
    {
        Task<IEnumerable<ResponseNoteDto>> GetNotesByTaskIdAsync(int taskId);
        Task<ResponseNoteDto> AddNoteAsync(int taskId, NoteDto note);
        Task<ResponseNoteDto> UpdateNoteAsync(int noteId, NoteDto note);
        Task<bool> DeleteNoteAsync(int id);
    }
}