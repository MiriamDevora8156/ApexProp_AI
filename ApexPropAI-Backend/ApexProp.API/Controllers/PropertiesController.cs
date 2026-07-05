using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ApexProp.API.Models;
using ApexProp.Application.DTOs;
using ApexProp.Domain.Interfaces;
using ApexProp.Domain.Models;

namespace ApexProp.API.Controllers;

/// <summary>
/// PropertiesController - כל ה-Endpoints שקשורים לנכסים
/// אלו ה-"דלתות" שאנגולר דופק עליהן כדי לקבל נתונים
/// </summary>
[ApiController]
[Route("api/[controller]")]
//[Authorize]
public class PropertiesController : ControllerBase
{
    private readonly IPropertyRepository _propertyRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<PropertiesController> _logger;

    public PropertiesController(
        IPropertyRepository propertyRepository,
        IMapper mapper,
        ILogger<PropertiesController> logger)
    {
        _propertyRepository = propertyRepository;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/properties
    /// קבל את כל הנכסים
    /// </summary>
    [AllowAnonymous]
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<IEnumerable<PropertyDto>>>> GetAllProperties()
    {
        try
        {
            var properties = await _propertyRepository.GetAllAsync();
            var dtos = _mapper.Map<IEnumerable<PropertyDto>>(properties);
            return Ok(ApiResponse<IEnumerable<PropertyDto>>.CreateSuccess(dtos));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting properties");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<IEnumerable<PropertyDto>>.CreateError(
                    "An error occurred", "FETCH_FAILED"));
        }
    }

    /// <summary>
    /// GET /api/properties/{id}
    /// קבל נכס בודד לפי ID
    /// </summary>
    [AllowAnonymous]
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<PropertyDto>>> GetPropertyById(int id)
    {
        try
        {
            if (id <= 0)
                return BadRequest(ApiResponse<PropertyDto>.CreateError(
                    "Invalid property ID", "INVALID_ID"));

            var property = await _propertyRepository.GetByIdAsync(id);
            if (property == null)
                return NotFound(ApiResponse<PropertyDto>.CreateError(
                    $"Property with ID {id} not found", "NOT_FOUND"));

            var dto = _mapper.Map<PropertyDto>(property);
            return Ok(ApiResponse<PropertyDto>.CreateSuccess(dto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting property {id}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<PropertyDto>.CreateError(
                    "An error occurred", "FETCH_FAILED"));
        }
    }

    /// <summary>
    /// GET /api/properties/area?lat=31.77&lng=35.21&radiusKm=5
    /// קבל נכסים באזור מסוים (ברדיוס מ-Latitude/Longitude)
    /// זה משמש ל-Heatmap
    /// </summary>
    [HttpGet("area")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<IEnumerable<PropertyDto>>>> GetPropertiesByArea(
        [FromQuery] double lat,
        [FromQuery] double lng,
        [FromQuery] double radiusKm = 5.0)
    {
        try
        {
            if (lat < -90 || lat > 90)
                return BadRequest(ApiResponse<IEnumerable<PropertyDto>>.CreateError(
                    "Latitude must be between -90 and 90", "INVALID_LAT"));

            if (lng < -180 || lng > 180)
                return BadRequest(ApiResponse<IEnumerable<PropertyDto>>.CreateError(
                    "Longitude must be between -180 and 180", "INVALID_LNG"));

            if (radiusKm <= 0)
                return BadRequest(ApiResponse<IEnumerable<PropertyDto>>.CreateError(
                    "Radius must be greater than 0", "INVALID_RADIUS"));

            var properties = await _propertyRepository.GetByAreaAsync(lat, lng, radiusKm);
            var dtos = _mapper.Map<IEnumerable<PropertyDto>>(properties);
            return Ok(ApiResponse<IEnumerable<PropertyDto>>.CreateSuccess(dtos));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting properties by area");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<IEnumerable<PropertyDto>>.CreateError(
                    "An error occurred", "FETCH_FAILED"));
        }
    }

    /// <summary>
    /// GET /api/properties/top?count=10
    /// קבל את הנכסים הטובים ביותר לפי AI Score
    /// </summary>
    [HttpGet("top")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<IEnumerable<PropertyDto>>>> GetTopProperties(
        [FromQuery] int count = 10)
    {
        try
        {
            if (count <= 0 || count > 100)
                count = 10;

            var properties = await _propertyRepository.GetTopPropertiesByScoreAsync(count);
            var dtos = _mapper.Map<IEnumerable<PropertyDto>>(properties);
            return Ok(ApiResponse<IEnumerable<PropertyDto>>.CreateSuccess(dtos));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting top properties");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<IEnumerable<PropertyDto>>.CreateError(
                    "An error occurred", "FETCH_FAILED"));
        }
    }

    /// <summary>
    /// GET /api/properties/price?minPrice=1000000&maxPrice=5000000
    /// קבל נכסים לפי טווח מחיר
    /// </summary>
    [HttpGet("price")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<IEnumerable<PropertyDto>>>> GetPropertiesByPriceRange(
        [FromQuery] decimal minPrice,
        [FromQuery] decimal maxPrice)
    {
        try
        {
            if (minPrice < 0 || maxPrice < 0)
                return BadRequest(ApiResponse<IEnumerable<PropertyDto>>.CreateError(
                    "Prices cannot be negative", "INVALID_PRICE"));

            if (minPrice > maxPrice)
                return BadRequest(ApiResponse<IEnumerable<PropertyDto>>.CreateError(
                    "Minimum price cannot be greater than maximum price", "INVALID_RANGE"));

            var properties = await _propertyRepository.GetByPriceRangeAsync(minPrice, maxPrice);
            var dtos = _mapper.Map<IEnumerable<PropertyDto>>(properties);
            return Ok(ApiResponse<IEnumerable<PropertyDto>>.CreateSuccess(dtos));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting properties by price range");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<IEnumerable<PropertyDto>>.CreateError(
                    "An error occurred", "FETCH_FAILED"));
        }
    }

    /// <summary>
    /// GET /api/properties/rooms/{rooms}
    /// קבל נכסים לפי מספר חדרים
    /// </summary>
    [HttpGet("rooms/{rooms}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<IEnumerable<PropertyDto>>>> GetPropertiesByRooms(int rooms)
    {
        try
        {
            if (rooms <= 0 || rooms > 20)
                return BadRequest(ApiResponse<IEnumerable<PropertyDto>>.CreateError(
                    "Rooms must be between 1 and 20", "INVALID_ROOMS"));

            var properties = await _propertyRepository.GetByRoomsAsync(rooms);
            var dtos = _mapper.Map<IEnumerable<PropertyDto>>(properties);
            return Ok(ApiResponse<IEnumerable<PropertyDto>>.CreateSuccess(dtos));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting properties by rooms");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<IEnumerable<PropertyDto>>.CreateError(
                    "An error occurred", "FETCH_FAILED"));
        }
    }

    /// <summary>
    /// POST /api/properties
    /// צור נכס חדש
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<PropertyDto>>> CreateProperty(
        [FromBody] CreatePropertyDto createDto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<PropertyDto>.CreateError(
                    "Validation failed", "VALIDATION_ERROR"));

            var entity = _mapper.Map<ApexProp.Domain.Entities.Property>(createDto);
            var createdProperty = await _propertyRepository.CreateAsync(entity);
            var dto = _mapper.Map<PropertyDto>(createdProperty);

            return CreatedAtAction(nameof(GetPropertyById),
                new { id = createdProperty.Id },
                ApiResponse<PropertyDto>.CreateSuccess(dto));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid property data");
            return BadRequest(ApiResponse<PropertyDto>.CreateError(ex.Message, "INVALID_DATA"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating property");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<PropertyDto>.CreateError(
                    "An error occurred", "CREATE_FAILED"));
        }
    }

    /// <summary>
    /// PUT /api/properties/{id}
    /// עדכן נכס קיים (מלא)
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> UpdateProperty(int id, [FromBody] CreatePropertyDto updateDto)
    {
        try
        {
            if (id <= 0)
                return BadRequest(ApiResponse.CreateError("Invalid property ID", "INVALID_ID"));

            if (!ModelState.IsValid)
                return BadRequest(ApiResponse.CreateError("Validation failed", "VALIDATION_ERROR"));

            var existingProperty = await _propertyRepository.GetByIdAsync(id);
            if (existingProperty == null)
                return NotFound(ApiResponse.CreateError(
                    $"Property with ID {id} not found", "NOT_FOUND"));

            _mapper.Map(updateDto, existingProperty);
            await _propertyRepository.UpdateAsync(existingProperty);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating property {id}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse.CreateError("An error occurred", "UPDATE_FAILED"));
        }
    }

    /// <summary>
    /// PATCH /api/properties/{id}
    /// עדכון חלקי - רק את השדות שנשלחו
    /// </summary>
    [HttpPatch("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<PropertyDto>>> PatchProperty(
        int id,
        [FromBody] Dictionary<string, object> updates)
    {
        try
        {
            if (id <= 0)
                return BadRequest(ApiResponse<PropertyDto>.CreateError(
                    "Invalid property ID", "INVALID_ID"));

            if (updates == null || updates.Count == 0)
                return BadRequest(ApiResponse<PropertyDto>.CreateError(
                    "No updates provided", "NO_UPDATES"));

            var updatedProperty = await _propertyRepository.PatchUpdateAsync(id, updates);
            var dto = _mapper.Map<PropertyDto>(updatedProperty);

            return Ok(ApiResponse<PropertyDto>.CreateSuccess(dto));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ApiResponse<PropertyDto>.CreateError(ex.Message, "NOT_FOUND"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error patching property {id}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<PropertyDto>.CreateError(
                    "An error occurred", "PATCH_FAILED"));
        }
    }

    /// <summary>
    /// DELETE /api/properties/{id}
    /// מחק נכס
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> DeleteProperty(int id)
    {
        try
        {
            if (id <= 0)
                return BadRequest(ApiResponse.CreateError(
                    "Invalid property ID", "INVALID_ID"));

            var exists = await _propertyRepository.PropertyExistsAsync(id);
            if (!exists)
                return NotFound(ApiResponse.CreateError(
                    $"Property with ID {id} not found", "NOT_FOUND"));

            await _propertyRepository.DeleteAsync(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting property {id}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse.CreateError("An error occurred", "DELETE_FAILED"));
        }
    }

    /// <summary>
    /// GET /api/properties/paged?pageNumber=1&pageSize=20
    /// קבל נכסים עם pagination
    /// </summary>
    [AllowAnonymous]
    [HttpGet("paged")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResponse<PropertyDto>>>> GetPagedProperties(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var pagedResult = await _propertyRepository.GetAllPaginatedAsync(pageNumber, pageSize);
            var dtos = _mapper.Map<IEnumerable<PropertyDto>>(pagedResult.Items);

            var response = new PagedResponse<PropertyDto>
            {
                Items = dtos.ToList(),
                PageNumber = pagedResult.PageNumber,
                PageSize = pagedResult.PageSize,
                TotalCount = pagedResult.TotalCount
            };

            return Ok(ApiResponse<PagedResponse<PropertyDto>>.CreateSuccess(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting paged properties");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<PagedResponse<PropertyDto>>.CreateError(
                    "An error occurred", "FETCH_FAILED"));
        }
    }

    /// <summary>
    /// GET /api/properties/search?searchTerm=...&minPrice=...&maxPrice=...
    /// חיפוש מתקדם
    /// </summary>
    [HttpGet("search")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResponse<PropertyDto>>>> SearchProperties(
        [FromQuery] string? searchTerm = null,
        [FromQuery] decimal? minPrice = null,
        [FromQuery] decimal? maxPrice = null,
        [FromQuery] int? minRooms = null,
        [FromQuery] int? maxRooms = null,
        [FromQuery] double? minScore = null,
        [FromQuery] string? sortBy = "CreatedAt",
        [FromQuery] bool ascending = false,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var pagedResult = await _propertyRepository.SearchAsync(
                searchTerm, minPrice, maxPrice, minRooms, maxRooms, minScore,
                sortBy, ascending, pageNumber, pageSize);

            var dtos = _mapper.Map<IEnumerable<PropertyDto>>(pagedResult.Items);

            var response = new PagedResponse<PropertyDto>
            {
                Items = dtos.ToList(),
                PageNumber = pagedResult.PageNumber,
                PageSize = pagedResult.PageSize,
                TotalCount = pagedResult.TotalCount
            };

            return Ok(ApiResponse<PagedResponse<PropertyDto>>.CreateSuccess(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching properties");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<PagedResponse<PropertyDto>>.CreateError(
                    "An error occurred", "SEARCH_FAILED"));
        }
    }

    /// <summary>
    /// GET /api/properties/{id}/price-history
    /// קבל היסטוריית מחיר לנכס
    /// </summary>
    [HttpGet("{id}/price-history")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<IEnumerable<PriceHistoryDto>>>> GetPriceHistory(int id)
    {
        try
        {
            if (id <= 0)
                return BadRequest(ApiResponse<IEnumerable<PriceHistoryDto>>.CreateError(
                    "Invalid property ID", "INVALID_ID"));

            var exists = await _propertyRepository.PropertyExistsAsync(id);
            if (!exists)
                return NotFound(ApiResponse<IEnumerable<PriceHistoryDto>>.CreateError(
                    $"Property with ID {id} not found", "NOT_FOUND"));

            var history = await _propertyRepository.GetPriceHistoryAsync(id);
            var dtos = _mapper.Map<IEnumerable<PriceHistoryDto>>(history);
            return Ok(ApiResponse<IEnumerable<PriceHistoryDto>>.CreateSuccess(dtos));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting price history for property {id}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<IEnumerable<PriceHistoryDto>>.CreateError(
                    "An error occurred", "FETCH_FAILED"));
        }
    }
}