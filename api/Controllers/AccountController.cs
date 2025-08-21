using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos.Account;
using api.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    public class AccountController : ControllerBase
    {
        private readonly IAccountService _accountService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(IAccountService accountService, ILogger<AccountController> logger)
        {
            _logger = logger;
            _accountService = accountService;

        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            if (registerDto == null)
            {
                return BadRequest("Invalid registration data.");
            }

            try
            {
                var user = await _accountService.RegisterAsync(registerDto);
                if (user == null)
                {
                    return BadRequest("Registration failed.");
                }

                _logger.LogInformation("User {UserName} registered successfully", registerDto.Username);

                return Ok(user);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration failed for user {UserName}", registerDto.Username);
                return StatusCode(500, ex.Message);
            }


        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            if (loginDto == null)
            {
                return BadRequest("Invalid login data.");
            }

            try
            {
                var user = await _accountService.LoginAsync(loginDto);
                if (user == null)
                {
                    return Unauthorized("Login failed.");
                }
                _logger.LogInformation("User {UserName} logged in successfully", loginDto.Username);

                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed for user {UserName}", loginDto.Username);
                return StatusCode(500, ex.Message);
            }


        }

    }

}