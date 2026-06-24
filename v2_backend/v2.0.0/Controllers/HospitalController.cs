using Microsoft.AspNetCore.Mvc;
using Vaxtrack.Interfaces;
using Vaxtrack.Dtos.HospitalDtos;

namespace Vaxtrack.Controllers
{
    [ApiController]
    [Route("/api/vaxtrack/v1/[controller]/[action]")]
    public class HospitalController : ControllerBase
    {
        private readonly IHospitalService _hospitalService;
        private readonly ILogger<HospitalController> _logger;

        public HospitalController(IHospitalService hospitalService, ILogger<HospitalController> logger)
        {
            _hospitalService = hospitalService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<ActionResult<CreateHospitalResponseDto>> CreateHospitalAsync(CreateHospitalRequestDto createHospitalRequestDto)
        {
            try
            {
                var createdHospitalResponse = await _hospitalService.CreateHospitalAsync(createHospitalRequestDto);
                return CreatedAtAction(nameof(GetHospitalByIdAsync), new { hospitalId = createdHospitalResponse.HospitalId }, createdHospitalResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HospitalController: CreateHospitalAsync - {Message}", ex.Message);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpPut]
        public async Task<ActionResult<UpdateHospitalResponseDto>> UpdateHospitalAsync(UpdateHospitalRequestDto updateHospitalRequest)
        {
            try
            {
                var updatedHospital = await _hospitalService.UpdateHospitalAsync(updateHospitalRequest);
                return Ok(updatedHospital);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HospitalController: UpdateHospitalAsync - {Message}", ex.Message);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpPut("{hospitalId}/{totalSlots}")]
        public async Task<ActionResult<int>> UpdateTotalSlotsAsync(string hospitalId, int totalSlots)
        {
            try
            {
                int updatedSlots = await _hospitalService.UpdateTotalSlotsAsync(hospitalId, totalSlots);
                return Ok(updatedSlots);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HospitalController: UpdateTotalSlotsAsync - {Message}", ex.Message);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpPut("{hospitalId}/{slotsToUpdate}")]
        public async Task<ActionResult<int>> UpdateAvailableSlotsAsync(string hospitalId, int slotsToUpdate)
        {
            try
            {
                int updatedSlots = await _hospitalService.UpdateAvailableSlotsAsync(hospitalId, slotsToUpdate);
                return Ok(updatedSlots);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HospitalController: UpdateAvailableSlotsAsync - {Message}", ex.Message);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpGet("{hospitalId}")]
        public async Task<ActionResult<HospitalProfileDataDto>> GetHospitalByIdAsync(string hospitalId)
        {
            try
            {
                var foundHospital = await _hospitalService.GetHospitalByIdAsync(hospitalId);
                return Ok(foundHospital);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HospitalController: GetHospitalByIdAsync - {Message}", ex.Message);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpGet]
        public async Task<ActionResult<List<HospitalProfileDataDto>>> GetAllHospitalsAsync()
        {
            try
            {
                var allHospitals = await _hospitalService.GetAllHospitalsAsync();
                return Ok(allHospitals);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HospitalController: GetAllHospitalsAsync - {Message}", ex.Message);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpDelete("{hospitalId}")]
        public async Task<ActionResult> DeleteHospitalAsync(string hospitalId)
        {
            try
            {
                await _hospitalService.DeleteHospitalAsync(hospitalId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HospitalController: DeleteHospitalAsync - {Message}", ex.Message);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }
    }
}
