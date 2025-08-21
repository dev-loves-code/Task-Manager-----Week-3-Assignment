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
        Task<IEnumerable<ResponseNoteDto>> GetNotesByTaskIdAsync(int taskId, string username);
        Task<ResponseNoteDto> AddNoteAsync(int taskId, NoteDto note, string username);
        Task<ResponseNoteDto> UpdateNoteAsync(int noteId, NoteDto note, string username);
        Task<bool> DeleteNoteAsync(int id, string username);
    }
}