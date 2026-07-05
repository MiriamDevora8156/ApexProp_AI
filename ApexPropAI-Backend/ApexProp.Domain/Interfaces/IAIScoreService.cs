using ApexProp.Domain.Entities;

namespace ApexProp.Domain.Interfaces;

public interface IAIScoreService
{
    Task<double> CalculateScoreAsync(Property property, IEnumerable<Location> nearbyLocations);
    Task<double> PredictFuturePriceAsync(int propertyId, int yearsAhead);
}