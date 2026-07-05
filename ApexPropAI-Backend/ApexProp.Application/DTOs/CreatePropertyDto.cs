using ApexProp.Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace ApexProp.Application.DTOs;

/// <summary>
/// CreatePropertyDto - מה Angular שולח לנו כשרוצה ליצור נכס חדש
/// הבדיקות הן בשכבה ראשונה - לפני שהקוד בעצם מעבד
/// </summary>
public class CreatePropertyDto
{
    [Required(ErrorMessage = "Title is required")]
    [StringLength(200, MinimumLength = 5, ErrorMessage = "Title must be between 5 and 200 characters")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Address is required")]
    [StringLength(500, MinimumLength = 5, ErrorMessage = "Address must be between 5 and 500 characters")]
    public string Address { get; set; } = string.Empty;

    [Required(ErrorMessage = "Description is required")]
    [StringLength(200, MinimumLength = 5, ErrorMessage = "Description must be between 5 and 200 characters")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Latitude is required")]
    [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90")]
    public double Latitude { get; set; }

    [Required(ErrorMessage = "Longitude is required")]
    [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180")]
    public double Longitude { get; set; }

    [Required(ErrorMessage = "Price is required")]
    [Range(0.01, 999999999.99, ErrorMessage = "Price must be greater than 0")]
    public decimal Price { get; set; }

    [Required(ErrorMessage = "Rooms is required")]
    [Range(1, 20, ErrorMessage = "Rooms must be between 1 and 20")]
    public int Rooms { get; set; }

    [Required(ErrorMessage = "AreaSqm is required")]
    [Range(0.1, 10000, ErrorMessage = "AreaSqm must be between 0.1 and 10000")]
    public double AreaSqm { get; set; }
}