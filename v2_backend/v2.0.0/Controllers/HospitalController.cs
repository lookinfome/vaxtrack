
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

        public HospitalController(IHospitalService hospitalService)
        {
            _hospitalService = hospitalService;
        }

        [HttpGet("{hospitalId}")]
        public async Task<ActionResult<HospitalProfileDataDto>> GetHospitalByIdAsync(string hospitalId)
        {
            ArgumentNullException.ThrowIfNull(hospitalId);
            try
            {
                var foundHospital = await _hospitalService.GetHospitalByIdAsync(hospitalId);
                return foundHospital;
            }
            catch (ArgumentException ex)
            {                
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        public async Task<ActionResult<List<HospitalProfileDataDto>>> GetAllHospitals()
        {
            List<HospitalProfileDataDto> allHospitals = await _hospitalService.GetAllHospitalsAsync();
            return Ok(allHospitals);
        }

        [HttpPost]
        public async Task<ActionResult<CreateHospitalResponseDto>> CreateHospitalAsync(CreateHospitalRequestDto createHospitalRequestDto)
        {
            try
            {
                var createdHospitalResponse = await _hospitalService.CreateHospitalAsync(createHospitalRequestDto);
                return Ok(createdHospitalResponse);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut]
        public async Task<ActionResult<UpdateHospitalResponseDto>> UpdateHospitalAsync(UpdateHospitalRequestDto updateHospitalRequest)
        {
            ArgumentNullException.ThrowIfNull(updateHospitalRequest);
            try
            {
                var updateHospital = await _hospitalService.UpdateHospitalAsync(updateHospitalRequest);
                return Ok(updateHospital);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }

        }

        [HttpPut("{hospitalId}/{totalSlots}")]
        public async Task<ActionResult<int>> UpdateTotalSlotsAsync(string hospitalId, int totalSlots)
        {
            try
            {
                int updatedSlots = await _hospitalService.UpdateTotalSlotsAsync(hospitalId, totalSlots);
                return Ok($"Hospital with id {hospitalId} has been updated with {updatedSlots} total slots.");
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{hospitalId}/{slotsToUpdate}")]
        public async Task<ActionResult<int>> UpdateAvailableSlotsAsync(string hospitalId, int slotsToUpdate)
        {
            try
            {
                int updatedSlots = await _hospitalService.UpdateAvailableSlotsAsync(hospitalId, slotsToUpdate);
                return Ok($"Hospital with id {hospitalId} has been updated with {updatedSlots} slots available now.");
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{hospitalId}")]
        public async Task<ActionResult> DeleteHospitalAsync(string hospitalId)
        {
            try
            {
                await _hospitalService.DeleteHospitalAsync(hospitalId);
                return Ok($"Hospital with id {hospitalId} has been deleted.");
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}