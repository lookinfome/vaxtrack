

namespace Vaxtrack.Interfaces.UtilityInterfaces
{
    public interface IUtilityService
    {
        Task<string> GenerateUniqueIdAsync(string name);
        Task<string> GenerateGuidAsync();
        Task<int> CalculateAgeAsync(DateTime date);
    }
}