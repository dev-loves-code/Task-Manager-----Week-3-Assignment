using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using api.Dtos.Note;
using api.Interfaces;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NoteController : ControllerBase
    {
        private readonly INoteService _noteService;
        private readonly ILogger<NoteController> _logger;

        public NoteController(INoteService noteService, ILogger<NoteController> logger)
        {
            _noteService = noteService;
            _logger = logger;
        }


        private string GetUsername()
        {
            var username = User.FindFirst(ClaimTypes.GivenName)?.Value
                ?? User.FindFirst("given_name")?.Value
                ?? User.Identity?.Name
                ?? User.FindFirst(ClaimTypes.Email)?.Value;

            return username;
        }

        [HttpGet("task/{taskId}/notes")]
        public async Task<IActionResult> GetNotesByTaskIdAsync(int taskId)
        {
            _logger.LogInformation("Processing GET request for notes of task {TaskId} from {RequestPath}", taskId, Request.Path);

            try
            {
                var username = GetUsername();
                if (string.IsNullOrEmpty(username))
                {
                    _logger.LogWarning("Username not found in claims");
                    return Unauthorized("Unable to determine user identity");
                }

                var notesDto = await _noteService.GetNotesByTaskIdAsync(taskId, username);

                _logger.LogInformation("Successfully returned {NoteCount} notes for task {TaskId} by user {Username}",
                    notesDto.Count(), taskId, username);

                return Ok(notesDto);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt for task {TaskId} notes", taskId);
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HTTP request failed for endpoint {Endpoint} with TaskId {TaskId}", Request.Path, taskId);
                return StatusCode(500, "An error occurred while fetching notes.");
            }
        }

        [HttpPost("task/{taskId}")]
        public async Task<IActionResult> AddNoteAsync(int taskId, [FromBody] NoteDto noteDto)
        {
            _logger.LogInformation("Processing POST request to add note for task {TaskId} from {RequestPath}", taskId, Request.Path);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Model validation failed for note creation: {@ValidationErrors}",
                    ModelState.Where(x => x.Value.Errors.Count > 0).ToDictionary(k => k.Key, v => v.Value.Errors));
                return BadRequest(ModelState);
            }

            try
            {
                var username = GetUsername();
                if (string.IsNullOrEmpty(username))
                {
                    _logger.LogWarning("Username not found in claims");
                    return Unauthorized("Unable to determine user identity");
                }

                var createdNoteDto = await _noteService.AddNoteAsync(taskId, noteDto, username);

                _logger.LogInformation("Successfully added note for task {TaskId} by user {Username}", taskId, username);

                return Created($"api/note/task/{taskId}/notes", createdNoteDto);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt for task {TaskId} note creation", taskId);
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HTTP request failed for endpoint {Endpoint} with TaskId {TaskId}", Request.Path, taskId);
                return StatusCode(500, "An error occurred while creating the note.");
            }
        }

        [HttpPut("{noteId}")]
        public async Task<IActionResult> UpdateNoteAsync(int noteId, [FromBody] NoteDto noteDto)
        {
            _logger.LogInformation("Processing PUT request to update note {NoteId} from {RequestPath}", noteId, Request.Path);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Model validation failed for note update: {@ValidationErrors}",
                    ModelState.Where(x => x.Value.Errors.Count > 0).ToDictionary(k => k.Key, v => v.Value.Errors));
                return BadRequest(ModelState);
            }

            try
            {
                var username = GetUsername();
                if (string.IsNullOrEmpty(username))
                {
                    _logger.LogWarning("Username not found in claims");
                    return Unauthorized("Unable to determine user identity");
                }

                var updatedNoteDto = await _noteService.UpdateNoteAsync(noteId, noteDto, username);

                _logger.LogInformation("Successfully updated note {NoteId} by user {Username}", noteId, username);

                return Ok(updatedNoteDto);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt for note {NoteId} update", noteId);
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HTTP request failed for endpoint {Endpoint} with NoteId {NoteId}", Request.Path, noteId);
                return StatusCode(500, "An error occurred while updating the note.");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNoteAsync(int id)
        {
            _logger.LogInformation("Processing DELETE request for note {NoteId} from {RequestPath}", id, Request.Path);

            try
            {
                var username = GetUsername();
                if (string.IsNullOrEmpty(username))
                {
                    _logger.LogWarning("Username not found in claims");
                    return Unauthorized("Unable to determine user identity");
                }

                var result = await _noteService.DeleteNoteAsync(id, username);
                if (!result)
                {
                    _logger.LogWarning("Note {NoteId} not found for deletion by user {Username}, returning 404", id, username);
                    return NotFound($"Note with ID {id} not found.");
                }

                _logger.LogInformation("Successfully deleted note {NoteId} by user {Username}", id, username);
                return NoContent();
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt for note {NoteId} deletion", id);
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HTTP request failed for endpoint {Endpoint} with NoteId {NoteId}", Request.Path, id);
                return StatusCode(500, "An error occurred while deleting the note.");
            }
        }

    }
}