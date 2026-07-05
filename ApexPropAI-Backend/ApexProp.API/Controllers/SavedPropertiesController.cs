using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApexProp.API.Models;
using ApexProp.Application.DTOs;
using ApexProp.Domain.Interfaces;
using System.Security.Claims;

namespace ApexProp.API.Controllers;

[ApiController]
[Route("api/users/saved-properties")]
[Authorize] // מחייב התחברות לכל הפונקציות כאן
public class SavedPropertiesController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<SavedPropertiesController> _logger;

    public SavedPropertiesController(
        IUserRepository userRepository,
        IMapper mapper,
        ILogger<SavedPropertiesController> logger)
    {
        _userRepository = userRepository;
        _mapper = mapper;
        _logger = logger;
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.Parse(claim!);
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<PropertyDto>>>> GetMySavedProperties()
    {
        try
        {
            var userId = GetCurrentUserId();
            var properties = await _userRepository.GetSavedPropertiesAsync(userId);
            var dtos = _mapper.Map<IEnumerable<PropertyDto>>(properties);

            return Ok(ApiResponse<IEnumerable<PropertyDto>>.CreateSuccess(dtos));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching saved properties");
            return StatusCode(500, ApiResponse<IEnumerable<PropertyDto>>.CreateError("Failed to fetch", "ERROR"));
        }
    }

    [HttpPost("{propertyId}")]
    public async Task<ActionResult<ApiResponse<string>>> SaveProperty(int propertyId)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _userRepository.AddSavedPropertyAsync(userId, propertyId);
            return Ok(ApiResponse<string>.CreateSuccess("Property saved successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<string>.CreateError(ex.Message, "INVALID_REQUEST"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving property {PropertyId}", propertyId);
            return StatusCode(500, ApiResponse<string>.CreateError("Failed to save", "ERROR"));
        }
    }

    [HttpDelete("{propertyId}")]
    public async Task<ActionResult<ApiResponse<string>>> RemoveProperty(int propertyId)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _userRepository.RemoveSavedPropertyAsync(userId, propertyId);
            return Ok(ApiResponse<string>.CreateSuccess("Property removed successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing property {PropertyId}", propertyId);
            return StatusCode(500, ApiResponse<string>.CreateError("Failed to remove", "ERROR"));
        }
    }
}