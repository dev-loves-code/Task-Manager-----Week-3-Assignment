using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos.Note;
using api.Interfaces;
using api.Models;
using Mapster;

namespace api.Services
{
    public class NoteService : INoteService
    {
        private readonly IRedisCacheService _redisCacheService;
        private readonly INoteRepository _noteRepository;
        private readonly ITaskService _taskService;
        private readonly ILogger<NoteService> _logger;

        public NoteService(INoteRepository noteRepository, ITaskService taskService, IRedisCacheService redisCacheService, ILogger<NoteService> logger)
        {
            _logger = logger;
            _redisCacheService = redisCacheService;
            _noteRepository = noteRepository;
            _taskService = taskService;
        }

        public async Task<ResponseNoteDto> AddNoteAsync(int taskId, Dtos.Note.NoteDto noteDto)
        {
            _logger.LogInformation("Adding note for task {TaskId}", taskId);

            var task = await _taskService.GetTaskByIdAsync(taskId);
            if (task == null)
            {
                _logger.LogWarning("Task {TaskId} not found for note creation", taskId);
                throw new Exception("Task not found");
            }

            var note = noteDto.Adapt<Models.Note>();
            note.TaskId = taskId;
            note.CreatedAt = DateTime.UtcNow;

            var noteCreated = await _noteRepository.AddNoteAsync(note);
            if (noteCreated == null)
            {
                _logger.LogError("Failed to create note for task {TaskId}", taskId);
                throw new Exception("Failed to create note");

            }


            _logger.LogInformation("Successfully created note for task {TaskId}", taskId);


            var noteCreatedDto = noteCreated.Adapt<ResponseNoteDto>();
            await InvalidateTaskCacheAsync(taskId);


            return noteCreatedDto;
        }

        public async Task<ResponseNoteDto> UpdateNoteAsync(int noteId, Dtos.Note.NoteDto noteDto)
        {
            _logger.LogInformation("Updating note {NoteId}", noteId);


            var existingNote = await _noteRepository.GetNoteByIdAsync(noteId);
            if (existingNote == null)
            {
                _logger.LogWarning("Note {NoteId} not found for update", noteId);
                throw new Exception("Note not found");
            }

            existingNote.Title = noteDto.Title;
            existingNote.Content = noteDto.Content;

            var updatedNote = await _noteRepository.UpdateNoteAsync(existingNote);

            if (updatedNote == null)
            {
                _logger.LogError("Failed to update note {NoteId}", noteId);
                throw new Exception("Failed to update note");
            }

            await InvalidateTaskCacheAsync(updatedNote.TaskId);

            _logger.LogInformation("Successfully updated note {NoteId}", noteId);

            var updatedNoteDto = updatedNote.Adapt<ResponseNoteDto>();



            return updatedNoteDto;
        }

        public async Task<bool> DeleteNoteAsync(int id)
        {
            _logger.LogInformation("Deleting note {NoteId}", id);

            try
            {
                var existingNote = await _noteRepository.GetNoteByIdAsync(id);
                if (existingNote == null)
                {
                    _logger.LogWarning("Note {NoteId} not found for deletion", id);
                    throw new Exception("Note not found");
                }

                await _noteRepository.DeleteNoteAsync(id);
                await InvalidateTaskCacheAsync(existingNote.TaskId);

                _logger.LogInformation("Successfully deleted note {NoteId}", id);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete note {NoteId}", id);
                throw;

            }

        }

        public async Task<IEnumerable<ResponseNoteDto>> GetNotesByTaskIdAsync(int taskId)
        {
            _logger.LogInformation("Retrieving notes for task {TaskId}", taskId);
            try
            {
                string cacheKey = $"Task_{taskId}_Notes";

                var cachedNotes = _redisCacheService.GetData<IEnumerable<ResponseNoteDto>>(cacheKey);

                if (cachedNotes != null)
                {
                    _logger.LogInformation("Retrieved notes for task {TaskId} from cache", taskId);
                    return cachedNotes;
                }

                if (!await _taskService.TaskExistsAsync(taskId))
                {
                    _logger.LogWarning("Task {TaskId} not found for retrieving notes", taskId);
                    throw new KeyNotFoundException($"Task {taskId} not found.");
                }

                var notes = await _noteRepository.GetNotesByTaskIdAsync(taskId);

                _logger.LogInformation("Retrieved {NoteCount} notes for task {TaskId}", notes.Count(), taskId);

                var notesDto = notes.Select(note => note.Adapt<ResponseNoteDto>());
                _redisCacheService.SetData(cacheKey, notesDto);

                return notesDto;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get notes for task {TaskId}", taskId);
                throw;
            }

        }

        private async System.Threading.Tasks.Task InvalidateTaskCacheAsync(int taskId)
        {
            try
            {

                string taskCacheKey = $"Task_{taskId}_Notes"; ;
                _redisCacheService.RemoveData(taskCacheKey);


                _logger.LogDebug("Invalidated cache for notes of task {TaskId}", taskId);
            }
            catch (Exception ex)
            {

                _logger.LogWarning(ex, "Failed to invalidate cache for notes of task {TaskId}", taskId);
            }
        }
    }
}