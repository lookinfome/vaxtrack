using Vaxtrack.Interfaces.UtilityInterfaces;

namespace Vaxtrack.Utilities
{
    public class UtilityService : IUtilityService
    {
        private readonly ILogger<UtilityService> _logger;

        public UtilityService(ILogger<UtilityService> logger)
        {
            _logger = logger;
        }

        public async Task<string> GenerateUniqueIdAsync(string name)
        {
            try
            {
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var randomPart = Guid.NewGuid().ToString("N").Substring(0, 6);
                var uniqueId = $"{name}-{timestamp.ToString().Substring(0, 2)}-{randomPart.Substring(0, 2)}";
                return uniqueId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UtilityService: GenerateUniqueIdAsync - {Message}", ex.Message);
                throw new Exception($"UtilityService: GenerateUniqueIdAsync - {ex.Message}", ex);
            }
        }

        public async Task<string> GenerateGuidAsync()
        {
            try
            {
                return Guid.NewGuid().ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UtilityService: GenerateGuidAsync - {Message}", ex.Message);
                throw new Exception($"UtilityService: GenerateGuidAsync - {ex.Message}", ex);
            }
        }

        public async Task<int> CalculateAgeAsync(DateTime date)
        {
            try
            {
                var today = DateTime.Today;
                var age = today.Year - date.Year;

                if (date.Date > today.AddYears(-age))
                    age--;

                return age;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UtilityService: CalculateAgeAsync - {Message}", ex.Message);
                throw new Exception($"UtilityService: CalculateAgeAsync - {ex.Message}", ex);
            }
        }
    }
}
