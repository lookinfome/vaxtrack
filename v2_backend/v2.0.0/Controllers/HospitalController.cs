using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Vaxtrack.Interfaces;
using Vaxtrack.Dtos.HospitalDtos;

namespace Vaxtrack.Controllers
{
    [Authorize]
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

        // ── helpers ───────────────────────────────────────────────────────────────

        private string CallerUserUid =>
            User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ??
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
        private bool CallerIsAdmin   => User.IsInRole("admin");

        // ── endpoints ─────────────────────────────────────────────────────────────

        [Authorize(Roles = "admin")]
        [HttpPost]
        public async Task<ActionResult<CreateHospitalResponseDto>> CreateHospitalAsync(CreateHospitalRequestDto createHospitalRequestDto)
        {
            try
            {
                var createdHospitalResponse = await _hospitalService.CreateHospitalAsync(createHospitalRequestDto);
                return CreatedAtAction("GetHospitalById", new { hospitalId = createdHospitalResponse.HospitalId }, createdHospitalResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HospitalController: CreateHospitalAsync - {Message}", ex.Message);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        // Platform admin OR hospital-admin scoped to that hospital
        [HttpPut]
        public async Task<ActionResult<UpdateHospitalResponseDto>> UpdateHospitalAsync(UpdateHospitalRequestDto updateHospitalRequest)
        {
            try
            {
                var updatedHospital = await _hospitalService.UpdateHospitalAsync(updateHospitalRequest, CallerUserUid, CallerIsAdmin);
                return Ok(updatedHospital);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HospitalController: UpdateHospitalAsync - {Message}", ex.Message);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        // Platform admin OR hospital-admin scoped to that hospital
        [HttpPut("{hospitalId}/{totalSlots}")]
        public async Task<ActionResult<int>> UpdateTotalSlotsAsync(string hospitalId, int totalSlots)
        {
            try
            {
                int updatedSlots = await _hospitalService.UpdateTotalSlotsAsync(hospitalId, totalSlots, CallerUserUid, CallerIsAdmin);
                return Ok(updatedSlots);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HospitalController: UpdateTotalSlotsAsync - {Message}", ex.Message);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        // Platform admin OR hospital-admin scoped to that hospital
        [HttpPut("{hospitalId}/{slotsToUpdate}")]
        public async Task<ActionResult<int>> UpdateAvailableSlotsAsync(string hospitalId, int slotsToUpdate)
        {
            try
            {
                int updatedSlots = await _hospitalService.UpdateAvailableSlotsAsync(hospitalId, slotsToUpdate, CallerUserUid, CallerIsAdmin);
                return Ok(updatedSlots);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
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

        // ── lifecycle ─────────────────────────────────────────────────────────────

        // Platform admin only — reason required
        [Authorize(Roles = "admin")]
        [HttpPut("{hospitalId}")]
        public async Task<ActionResult<HospitalProfileDataDto>> DisableHospitalAsync(string hospitalId, HospitalActionCommentRequestDto body)
        {
            try
            {
                var updated = await _hospitalService.DisableHospitalAsync(hospitalId, CallerUserUid, CallerIsAdmin, body?.Comment ?? "");
                return Ok(updated);
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HospitalController: DisableHospitalAsync - {Message}", ex.Message);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        // Hospital's own hospital-admin only
        [HttpPut("{hospitalId}")]
        public async Task<ActionResult<HospitalProfileDataDto>> RequestReactivationAsync(string hospitalId, HospitalActionCommentRequestDto? body)
        {
            try
            {
                var updated = await _hospitalService.RequestReactivationAsync(hospitalId, CallerUserUid, CallerIsAdmin, body?.Comment);
                return Ok(updated);
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HospitalController: RequestReactivationAsync - {Message}", ex.Message);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [Authorize(Roles = "admin")]
        [HttpPut("{hospitalId}")]
        public async Task<ActionResult<HospitalProfileDataDto>> ApproveReactivationAsync(string hospitalId, HospitalActionCommentRequestDto? body)
        {
            try
            {
                var updated = await _hospitalService.ApproveReactivationAsync(hospitalId, CallerUserUid, CallerIsAdmin, body?.Comment);
                return Ok(updated);
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HospitalController: ApproveReactivationAsync - {Message}", ex.Message);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [Authorize(Roles = "admin")]
        [HttpPut("{hospitalId}")]
        public async Task<ActionResult<HospitalProfileDataDto>> RejectReactivationAsync(string hospitalId, HospitalActionCommentRequestDto? body)
        {
            try
            {
                var updated = await _hospitalService.RejectReactivationAsync(hospitalId, CallerUserUid, CallerIsAdmin, body?.Comment);
                return Ok(updated);
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HospitalController: RejectReactivationAsync - {Message}", ex.Message);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        // Platform admin only — reason required
        [Authorize(Roles = "admin")]
        [HttpPut("{hospitalId}")]
        public async Task<ActionResult<HospitalProfileDataDto>> RequestUnregisterAsync(string hospitalId, HospitalActionCommentRequestDto body)
        {
            try
            {
                var updated = await _hospitalService.RequestUnregisterAsync(hospitalId, CallerUserUid, CallerIsAdmin, body?.Comment ?? "");
                return Ok(updated);
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HospitalController: RequestUnregisterAsync - {Message}", ex.Message);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [Authorize(Roles = "admin")]
        [HttpPut("{hospitalId}")]
        public async Task<ActionResult<HospitalProfileDataDto>> WithdrawUnregisterRequestAsync(string hospitalId)
        {
            try
            {
                var updated = await _hospitalService.WithdrawUnregisterRequestAsync(hospitalId, CallerUserUid, CallerIsAdmin);
                return Ok(updated);
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HospitalController: WithdrawUnregisterRequestAsync - {Message}", ex.Message);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        // Hospital's own hospital-admin only
        [HttpPut("{hospitalId}")]
        public async Task<ActionResult<HospitalProfileDataDto>> DeclineUnregisterRequestAsync(string hospitalId, HospitalActionCommentRequestDto? body)
        {
            try
            {
                var updated = await _hospitalService.DeclineUnregisterRequestAsync(hospitalId, CallerUserUid, CallerIsAdmin, body?.Comment);
                return Ok(updated);
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HospitalController: DeclineUnregisterRequestAsync - {Message}", ex.Message);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        // Hospital's own hospital-admin re-authenticates with their password to confirm
        // (or the platform admin themselves, if no hospital-admin is assigned)
        [HttpPut("{hospitalId}")]
        public async Task<ActionResult> AuthorizeUnregisterAsync(string hospitalId, AuthorizeUnregisterRequestDto body)
        {
            try
            {
                await _hospitalService.AuthorizeUnregisterAsync(hospitalId, CallerUserUid, CallerIsAdmin, body.Password, body.Comment);
                return NoContent();
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HospitalController: AuthorizeUnregisterAsync - {Message}", ex.Message);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpGet("{hospitalId}")]
        public async Task<ActionResult<List<HospitalAuditLogDto>>> GetHospitalAuditTrailAsync(string hospitalId)
        {
            try
            {
                var entries = await _hospitalService.GetHospitalAuditTrailAsync(hospitalId, CallerUserUid, CallerIsAdmin);
                return Ok(entries);
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HospitalController: GetHospitalAuditTrailAsync - {Message}", ex.Message);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }
    }
}
