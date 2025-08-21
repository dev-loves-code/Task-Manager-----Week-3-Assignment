using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos.Note;
using api.Interfaces;
using api.Models;
using Mapster;
using Microsoft.AspNetCore.Identity;

namespace api.Services
{
    public class NoteService : INoteService
    {
        private readonly IRedisCacheService _redisCacheService;
        private readonly INoteRepository _noteRepository;
        private readonly ITaskService _taskService;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<NoteService> _logger;

        private const string NOTE_CACHE_KEY_PREFIX = "Task_";
        private const string NOTE_CACHE_KEY_SUFFIX = "_Notes_";

        public NoteService(INoteRepository noteRepository, ITaskService taskService, IRedisCacheService redisCacheService,
            UserManager<AppUser> userManager, ILogger<NoteService> logger)
        {
            _logger = logger;
            _redisCacheService = redisCacheService;
            _noteRepository = noteRepository;
            _taskService = taskService;
            _userManager = userManager;
        }

        public async Task<ResponseNoteDto> AddNoteAsync(int taskId, Dtos.Note.NoteDto noteDto, string username)
        {
            _logger.LogInformation("Adding note for task {TaskId} by user {Username}", taskId, username);

            try
            {
                var appUser = await _userManager.FindByNameAsync(username);
                if (appUser == null)
                {
                    _logger.LogWarning("User {Username} not found", username);
                    throw new UnauthorizedAccessException("User not found");
                }


                var task = await _taskService.GetTaskByIdAsync(taskId, username);
                if (task == null)
                {
                    _logger.LogWarning("Task {TaskId} not found or not accessible by user {Username}", taskId, username);
                    throw new UnauthorizedAccessException("Task not found or access denied");
                }

                var note = noteDto.Adapt<Models.Note>();
                note.TaskId = taskId;
                note.CreatedAt = DateTime.UtcNow;

                var noteCreated = await _noteRepository.AddNoteAsync(note);
                if (noteCreated == null)
                {
                    _logger.LogError("Failed to create note for task {TaskId}", taskId);
                    throw new InvalidOperationException("Failed to create note");
                }

                _logger.LogInformation("Successfully created note {NoteId} for task {TaskId} by user {Username}",
                    noteCreated.Id, taskId, username);

                var noteCreatedDto = noteCreated.Adapt<ResponseNoteDto>();
                await InvalidateTaskNoteCacheAsync(taskId, appUser.Id);

                return noteCreatedDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add note for task {TaskId} by user {Username}", taskId, username);
                throw;
            }
        }

        public async Task<ResponseNoteDto> UpdateNoteAsync(int noteId, Dtos.Note.NoteDto noteDto, string username)
        {
            _logger.LogInformation("Updating note {NoteId} by user {Username}", noteId, username);

            try
            {
                var appUser = await _userManager.FindByNameAsync(username);
                if (appUser == null)
                {
                    _logger.LogWarning("User {Username} not found", username);
                    throw new UnauthorizedAccessException("User not found");
                }

                var existingNote = await _noteRepository.GetNoteByIdAsync(noteId, appUser.Id);
                if (existingNote == null)
                {
                    _logger.LogWarning("Note {NoteId} not found or not accessible by user {Username}", noteId, username);
                    throw new UnauthorizedAccessException("Note not found or access denied");
                }

                existingNote.Title = noteDto.Title;
                existingNote.Content = noteDto.Content;

                var updatedNote = await _noteRepository.UpdateNoteAsync(existingNote);

                if (updatedNote == null)
                {
                    _logger.LogError("Failed to update note {NoteId}", noteId);
                    throw new InvalidOperationException("Failed to update note");
                }

                await InvalidateTaskNoteCacheAsync(updatedNote.TaskId, appUser.Id);

                _logger.LogInformation("Successfully updated note {NoteId} by user {Username}", noteId, username);

                var updatedNoteDto = updatedNote.Adapt<ResponseNoteDto>();
                return updatedNoteDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update note {NoteId} by user {Username}", noteId, username);
                throw;
            }
        }

        public async Task<bool> DeleteNoteAsync(int id, string username)
        {
            _logger.LogInformation("Deleting note {NoteId} by user {Username}", id, username);

            try
            {
                var appUser = await _userManager.FindByNameAsync(username);
                if (appUser == null)
                {
                    _logger.LogWarning("User {Username} not found", username);
                    throw new UnauthorizedAccessException("User not found");
                }

                var existingNote = await _noteRepository.GetNoteByIdAsync(id, appUser.Id);
                if (existingNote == null)
                {
                    _logger.LogWarning("Note {NoteId} not found or not accessible by user {Username}", id, username);
                    return false;
                }

                await _noteRepository.DeleteNoteAsync(id);
                await InvalidateTaskNoteCacheAsync(existingNote.TaskId, appUser.Id);

                _logger.LogInformation("Successfully deleted note {NoteId} by user {Username}", id, username);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete note {NoteId} by user {Username}", id, username);
                throw;
            }
        }

        public async Task<IEnumerable<ResponseNoteDto>> GetNotesByTaskIdAsync(int taskId, string username)
        {
            _logger.LogInformation("Retrieving notes for task {TaskId} by user {Username}", taskId, username);

            try
            {
                var appUser = await _userManager.FindByNameAsync(username);
                if (appUser == null)
                {
                    _logger.LogWarning("User {Username} not found", username);
                    throw new UnauthorizedAccessException("User not found");
                }

                string cacheKey = $"{NOTE_CACHE_KEY_PREFIX}{taskId}{NOTE_CACHE_KEY_SUFFIX}{appUser.Id}";

                var cachedNotes = _redisCacheService.GetData<IEnumerable<ResponseNoteDto>>(cacheKey);

                if (cachedNotes != null)
                {
                    _logger.LogInformation("Retrieved notes for task {TaskId} from cache for user {Username}", taskId, username);
                    return cachedNotes;
                }


                if (!await _taskService.TaskExistsAsync(taskId, username))
                {
                    _logger.LogWarning("Task {TaskId} not found or not accessible by user {Username}", taskId, username);
                    throw new UnauthorizedAccessException("Task not found or access denied");
                }

                var notes = await _noteRepository.GetNotesByTaskIdAsync(taskId, appUser.Id);

                _logger.LogInformation("Retrieved {NoteCount} notes for task {TaskId} by user {Username}",
                    notes.Count(), taskId, username);

                var notesDto = notes.Select(note => note.Adapt<ResponseNoteDto>());
                _redisCacheService.SetData(cacheKey, notesDto);

                return notesDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get notes for task {TaskId} by user {Username}", taskId, username);
                throw;
            }
        }

        private async System.Threading.Tasks.Task InvalidateTaskNoteCacheAsync(int taskId, string userId)
        {
            try
            {
                string taskCacheKey = $"{NOTE_CACHE_KEY_PREFIX}{taskId}{NOTE_CACHE_KEY_SUFFIX}{userId}";
                _redisCacheService.RemoveData(taskCacheKey);

                _logger.LogDebug("Invalidated notes cache for task {TaskId} and user {UserId}", taskId, userId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to invalidate notes cache for task {TaskId} and user {UserId}", taskId, userId);
            }
        }
    }
}