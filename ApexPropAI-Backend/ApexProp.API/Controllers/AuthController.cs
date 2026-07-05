using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApexProp.API.Models;
using ApexProp.Application.DTOs;
using ApexProp.Application.Validators;
using ApexProp.Domain.Entities;
using ApexProp.Domain.Interfaces;
using ApexProp.Infrastructure.Services;
using System.Security.Claims;

namespace ApexProp.API.Controllers;

/// <summary>
/// AuthController - הרשמה, התחברות, Refresh Token
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly PasswordService _passwordService;
    private readonly JwtService _jwtService;
    private readonly IMapper _mapper;
    private readonly ILogger<AuthController> _logger;
    private readonly CreateUserValidator _createUserValidator;
    private readonly IConfiguration _configuration;

    public AuthController(
        IUserRepository userRepository,
        PasswordService passwordService,
        JwtService jwtService,
        IMapper mapper,
        ILogger<AuthController> logger,
        CreateUserValidator createUserValidator,
        IConfiguration configuration)
    {
        _userRepository = userRepository;
        _passwordService = passwordService;
        _jwtService = jwtService;
        _mapper = mapper;
        _logger = logger;
        _createUserValidator = createUserValidator;
        _configuration = configuration;
    }

    /// <summary>
    /// POST /api/auth/register
    /// הרשמה של משתמש חדש
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Register([FromBody] CreateUserDto dto)
    {
        try
        {
            // וודא את הנתונים
            var validationResult = await _createUserValidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return BadRequest(ApiResponse<LoginResponseDto>.CreateError(errors, "VALIDATION_FAILED"));
            }

            // בדוק אם אימייל קיים
            var existingUser = await _userRepository.GetByEmailAsync(dto.Email);
            if (existingUser != null)
                return Conflict(ApiResponse<LoginResponseDto>.CreateError(
                    "User with this email already exists", "EMAIL_EXISTS"));

            // צור משתמש
            var user = _mapper.Map<User>(dto);
            user.PasswordHash = _passwordService.HashPassword(dto.Password);

            var createdUser = await _userRepository.CreateAsync(user);

            // צור tokens
            var accessToken = _jwtService.GenerateAccessToken(user.Id, user.Email, user.Role);
            var refreshToken = _jwtService.GenerateRefreshToken();

            // חזור login response
            var response = new LoginResponseDto
            {
                User = _mapper.Map<UserDto>(createdUser),
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            };

            _logger.LogInformation("User {Email} registered successfully", dto.Email);
            return CreatedAtAction(nameof(Register), ApiResponse<LoginResponseDto>.CreateSuccess(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering user");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<LoginResponseDto>.CreateError("An error occurred during registration", "REGISTRATION_FAILED"));
        }
    }

    /// <summary>
    /// POST /api/auth/login
    /// התחברות
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Login([FromBody] LoginDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
                return BadRequest(ApiResponse<LoginResponseDto>.CreateError(
                    "Email and password are required", "MISSING_CREDENTIALS"));

            // מצא משתמש
            var user = await _userRepository.GetByEmailAsync(dto.Email);
            if (user == null)
                return Unauthorized(ApiResponse<LoginResponseDto>.CreateError(
                    "Invalid email or password", "INVALID_CREDENTIALS"));

            // בדוק סיסמה
            if (!_passwordService.VerifyPassword(dto.Password, user.PasswordHash))
                return Unauthorized(ApiResponse<LoginResponseDto>.CreateError(
                    "Invalid email or password", "INVALID_CREDENTIALS"));

            // צור tokens
            var accessToken = _jwtService.GenerateAccessToken(user.Id, user.Email, user.Role);
            var refreshToken = _jwtService.GenerateRefreshToken();

            // שמור ב-DB
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
            await _userRepository.UpdateAsync(user);

            var response = new LoginResponseDto
            {
                User = _mapper.Map<UserDto>(user),
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            };

            _logger.LogInformation("User {Email} logged in successfully", dto.Email);
            return Ok(ApiResponse<LoginResponseDto>.CreateSuccess(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging in user");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<LoginResponseDto>.CreateError("An error occurred during login", "LOGIN_FAILED"));
        }
    }

    /// <summary>
    /// POST /api/auth/refresh
    /// רענון Access Token בעזרת Refresh Token
    /// </summary>
    [HttpPost("refresh")]
    public async Task<ActionResult<ApiResponse<LoginResponseDto>>> RefreshToken(
    [FromBody] string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return BadRequest(ApiResponse<LoginResponseDto>.CreateError(
                "Refresh token is required", "MISSING_REFRESH_TOKEN"));

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
            return Unauthorized(ApiResponse<LoginResponseDto>.CreateError(
    "Invalid token", "INVALID_TOKEN"));

        var user = await _userRepository.GetByIdAsync(userId);

        // ✅ האימות החשוב — השוואה מול DB
        if (user == null ||
            user.RefreshToken != refreshToken ||
            user.RefreshTokenExpiry < DateTime.UtcNow)
            return Unauthorized(ApiResponse<LoginResponseDto>.CreateError(
                "Invalid or expired refresh token", "INVALID_REFRESH_TOKEN"));

        // צור tokens חדשים
        var newAccessToken = _jwtService.GenerateAccessToken(user.Id, user.Email, user.Role);
        var newRefreshToken = _jwtService.GenerateRefreshToken();

        // עדכן ב-DB (Rotation — כל שימוש מחולל token חדש)
        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        await _userRepository.UpdateAsync(user);

        return Ok(ApiResponse<LoginResponseDto>.CreateSuccess(new LoginResponseDto
        {
            User = _mapper.Map<UserDto>(user),
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        }));
    }


    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
        var user = await _userRepository.GetByIdAsync(userId);
        if (user != null)
        {
            user.RefreshToken = null;
            user.RefreshTokenExpiry = null;
            await _userRepository.UpdateAsync(user);
        }
        return Ok(ApiResponse.CreateSuccess("Logged out successfully"));
    }

    /// <summary>
    /// GET /api/auth/me
    /// קבלת פרטי הפרופיל של המשתמש המחובר
    /// </summary>
    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetMyProfile()
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!);
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) return NotFound();

        return Ok(ApiResponse<UserDto>.CreateSuccess(_mapper.Map<UserDto>(user)));
    }

    [Authorize]
    [HttpPut("me")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateUserDto updateDto)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!);

        // 1. נביא את המשתמש הקיים מה-DB
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) return NotFound();

        // 2. נעדכן רק את השדות מה-DTO
        _mapper.Map(updateDto, user);

        // 3. נשמור באמצעות ה-UpdateAsync הרגיל
        await _userRepository.UpdateAsync(user);

        return Ok(ApiResponse<string>.CreateSuccess("Profile updated"));
    }

    [Authorize(Roles = "Admin")] // רק מנהל קיים יכול לגשת לכאן!
    [HttpPatch("{id}/role")]
    public async Task<ActionResult> UpdateUserRole(int id, [FromBody] string newRole)
    {
        // ולידציה בסיסית - שלא יכניסו תפקיד שלא קיים
        if (newRole != "Admin" && newRole != "User")
            return BadRequest(ApiResponse.CreateError("Invalid role", "INVALID_ROLE"));

        var user = await _userRepository.GetByIdAsync(id);
        if (user == null) return NotFound();

        // עדכון התפקיד
        user.Role = newRole;
        await _userRepository.UpdateAsync(user);

        return Ok(ApiResponse.CreateSuccess("User role updated successfully"));
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<UserDto>>>> GetAllUsers()
    {
        var users = await _userRepository.GetAllAsync();
        var dtos = _mapper.Map<IEnumerable<UserDto>>(users);
        return Ok(ApiResponse<IEnumerable<UserDto>>.CreateSuccess(dtos));
    }
}