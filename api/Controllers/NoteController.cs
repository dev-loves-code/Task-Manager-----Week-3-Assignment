using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos.Note;
using api.Interfaces;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NoteController : ControllerBase
    {
        private readonly INoteService _noteService;
        private readonly ILogger<NoteController> _logger;

        public NoteController(INoteService noteService, ILogger<NoteController> logger)
        {
            _noteService = noteService;
            _logger = logger;
        }


        [HttpGet("task/{taskId}/notes")]
        public async Task<IActionResult> GetNotesByTaskIdAsync(int taskId)
        {
            _logger.LogInformation("Processing GET request for notes of task {TaskId} from {RequestPath}", taskId, Request.Path);
            try
            {
                var notesDto = await _noteService.GetNotesByTaskIdAsync(taskId);


                _logger.LogInformation("Successfully returned {NoteCount} notes for task {TaskId}", notesDto.Count(), taskId);


                return Ok(notesDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HTTP request failed for endpoint {Endpoint}", Request.Path);
                return StatusCode(500, ex.Message);
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
                var createdNoteDto = await _noteService.AddNoteAsync(taskId, noteDto);

                _logger.LogInformation("Successfully added note for task {TaskId}", taskId);


                return Created($"tasks/{taskId}/notes", createdNoteDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HTTP request failed for endpoint {Endpoint} with TaskId {TaskId}", Request.Path, taskId);
                return StatusCode(500, ex.Message);
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
                var updatedNoteDto = await _noteService.UpdateNoteAsync(noteId, noteDto);

                _logger.LogInformation("Successfully updated note {NoteId}", noteId);

                return Ok(updatedNoteDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HTTP request failed for endpoint {Endpoint} with NoteId {NoteId}", Request.Path, noteId);
                return StatusCode(500, ex.Message);
            }

        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNoteAsync(int id)
        {
            _logger.LogInformation("Processing DELETE request for note {NoteId} from {RequestPath}", id, Request.Path);
            try
            {
                var result = await _noteService.DeleteNoteAsync(id);
                if (result)
                {
                    _logger.LogInformation("Successfully deleted note {NoteId}", id);
                    return NoContent();
                }

                _logger.LogWarning("Note {NoteId} not found for deletion, returning 404", id);
                return NotFound($"Note with ID {id} not found.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HTTP request failed for endpoint {Endpoint} with NoteId {NoteId}", Request.Path, id);
                return StatusCode(500, ex.Message);
            }
        }


    }
}