using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Vaxtrack.Interfaces;
using Vaxtrack.Dtos.BookingDtos;

namespace Vaxtrack.Controllers
{
    [Authorize]
    [ApiController]
    [Route("/api/vaxtrack/v1/[controller]/[action]")]
    public class BookingController : ControllerBase
    {
        private readonly IBookingService _bookingService;
        private readonly ILogger<BookingController> _logger;

        public BookingController(IBookingService bookingService, ILogger<BookingController> logger)
        {
            _bookingService = bookingService;
            _logger = logger;
        }

        // ── helpers ───────────────────────────────────────────────────────────────

        private string CallerUserUid =>
            User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ??
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
        private bool CallerIsAdmin   => User.IsInRole("admin");

        // ── endpoints ─────────────────────────────────────────────────────────────

        // Public — anyone holding the bookingId/link can verify a completed vaccination,
        // mirroring how a real certificate's QR code works. Backs both the downloadable PDF
        // (called from the owner's own /booking page) and the public /certificate verify page.
        [AllowAnonymous]
        [HttpGet("{bookingId}")]
        public async Task<ActionResult<CertificateDto>> GetCertificateAsync(string bookingId)
        {
            try
            {
                var certificate = await _bookingService.GetCertificateAsync(bookingId);
                return Ok(certificate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BookingController: GetCertificateAsync - {Message}", ex.Message);
                return NotFound(new { message = "Certificate not found or vaccination not yet complete." });
            }
        }

        [HttpPost]
        public async Task<ActionResult<CreateBookingResponseDto>> CreateBookingAsync(CreateBookingRequestDto createBookingRequestDto)
        {
            try
            {
                var createdBookingResponse = await _bookingService.CreateBookingAsync(createBookingRequestDto, CallerUserUid);
                return CreatedAtAction("GetBookingByBookingId", new { bookingId = createdBookingResponse.BookingId }, createdBookingResponse);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BookingController: CreateBookingAsync - {Message}", ex.Message);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        // Admin-only — allows full field override (dose completion flags, dates, etc.)
        [Authorize(Roles = "admin")]
        [HttpPut]
        public async Task<ActionResult<UpdateBookingResponseDto>> UpdateBookingAsync(UpdateBookingRequestDto updateBookingRequestDto)
        {
            try
            {
                var updatedBookingResponse = await _bookingService.UpdateBookingAsync(updateBookingRequestDto);
                return Ok(updatedBookingResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BookingController: UpdateBookingAsync - {Message}", ex.Message);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpPut]
        public async Task<ActionResult<BookDose2ResponseDto>> BookDose2Async(BookDose2RequestDto bookDose2RequestDto)
        {
            try
            {
                var bookDose2Response = await _bookingService.BookDose2Async(bookDose2RequestDto, CallerUserUid);
                return Ok(bookDose2Response);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BookingController: BookDose2Async - {Message}", ex.Message);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpPut]
        public async Task<ActionResult<BookingProfileDataDto>> EditBookingAsync(EditBookingRequestDto editBookingRequestDto)
        {
            try
            {
                var updatedBooking = await _bookingService.EditBookingAsync(editBookingRequestDto, CallerUserUid);
                return Ok(updatedBooking);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BookingController: EditBookingAsync - {Message}", ex.Message);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        // Rebooks a cancelled/rejected Dose 1 — owner only
        [HttpPut]
        public async Task<ActionResult<BookingProfileDataDto>> RebookDose1Async(RebookDose1RequestDto rebookDose1RequestDto)
        {
            try
            {
                var updatedBooking = await _bookingService.RebookDose1Async(rebookDose1RequestDto, CallerUserUid);
                return Ok(updatedBooking);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BookingController: RebookDose1Async - {Message}", ex.Message);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        // Platform admin OR hospital-admin scoped to the booking's hospital
        [HttpPut("{bookingId}")]
        public async Task<ActionResult<BookingProfileDataDto>> ApproveBookingsAsync(string bookingId, BookingActionCommentRequestDto? body)
        {
            try
            {
                var approvedBooking = await _bookingService.ApproveBookingsAsync(bookingId, CallerUserUid, CallerIsAdmin, body?.Comment);
                return Ok(approvedBooking);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BookingController: ApproveBookingsAsync - {Message}", ex.Message);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpPut("{bookingId}")]
        public async Task<ActionResult<BookingProfileDataDto>> CancelBookingsAsync(string bookingId, BookingActionCommentRequestDto? body)
        {
            try
            {
                var canceledBooking = await _bookingService.CancelBookingsAsync(bookingId, CallerUserUid, CallerIsAdmin, body?.Comment);
                return Ok(canceledBooking);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BookingController: CancelBookingsAsync - {Message}", ex.Message);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        // Admin/hospital-admin only — distinct from Cancel (see RejectBookingAsync semantics in BookingService)
        [HttpPut("{bookingId}")]
        public async Task<ActionResult<BookingProfileDataDto>> RejectBookingsAsync(string bookingId, BookingActionCommentRequestDto? body)
        {
            try
            {
                var rejectedBooking = await _bookingService.RejectBookingAsync(bookingId, CallerUserUid, CallerIsAdmin, body?.Comment);
                return Ok(rejectedBooking);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BookingController: RejectBookingsAsync - {Message}", ex.Message);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpGet("{bookingId}")]
        public async Task<ActionResult<List<BookingAuditLogDto>>> GetBookingAuditTrailAsync(string bookingId)
        {
            try
            {
                var entries = await _bookingService.GetBookingAuditTrailAsync(bookingId, CallerUserUid, CallerIsAdmin);
                return Ok(entries);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BookingController: GetBookingAuditTrailAsync - {Message}", ex.Message);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        // Bookings still needing action — scoped internally to the caller's hospital-admin
        // assignments (or all bookings for a platform admin); a caller with no such assignment
        // simply gets an empty list back, which is a safe no-op, not a security gap.
        [HttpGet]
        public async Task<ActionResult<List<BookingProfileDataDto>>> GetActionableBookingsAsync()
        {
            try
            {
                var bookings = await _bookingService.GetActionableBookingsAsync(CallerUserUid, CallerIsAdmin);
                return Ok(bookings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BookingController: GetActionableBookingsAsync - {Message}", ex.Message);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpGet("{bookingId}")]
        public async Task<ActionResult<BookingProfileDataDto>> GetBookingByBookingIdAsync(string bookingId)
        {
            try
            {
                var foundBooking = await _bookingService.GetBookingByBookingIdAsync(bookingId, CallerUserUid, CallerIsAdmin);
                return Ok(foundBooking);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BookingController: GetBookingByBookingIdAsync - {Message}", ex.Message);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpGet("{userId}")]
        public async Task<ActionResult<BookingProfileDataDto>> GetBookingsByUserIdAsync(string userId)
        {
            try
            {
                var foundBooking = await _bookingService.GetBookingsByUserIdAsync(userId, CallerUserUid, CallerIsAdmin);
                return Ok(foundBooking);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BookingController: GetBookingsByUserIdAsync - {Message}", ex.Message);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpGet("{hospitalId}")]
        public async Task<ActionResult<List<BookingProfileDataDto>>> GetBookingsByHospitalIdAsync(string hospitalId)
        {
            try
            {
                var foundBookings = await _bookingService.GetBookingsByHospitalIdAsync(hospitalId);
                return Ok(foundBookings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BookingController: GetBookingsByHospitalIdAsync - {Message}", ex.Message);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [Authorize(Roles = "admin")]
        [HttpGet]
        public async Task<ActionResult<List<BookingProfileDataDto>>> GetAllBookingsAsync()
        {
            try
            {
                var allBookings = await _bookingService.GetAllBookingsAsync();
                return Ok(allBookings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BookingController: GetAllBookingsAsync - {Message}", ex.Message);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        // Read-only preview of the slot that would be assigned for this hospital+date — used by the
        // frontend to show the info card before the user actually submits the booking.
        [HttpGet("{hospitalId}/{date}")]
        public async Task<ActionResult<NextAvailableSlotResponseDto>> GetNextAvailableSlotAsync(string hospitalId, DateTime date)
        {
            try
            {
                var nextSlot = await _bookingService.GetNextAvailableSlotAsync(hospitalId, date);
                return Ok(nextSlot);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BookingController: GetNextAvailableSlotAsync - {Message}", ex.Message);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpGet("{bookingId}")]
        public async Task<ActionResult<bool>> IsBookingExistsAsync(string bookingId)
        {
            try
            {
                var exists = await _bookingService.IsBookingExists(bookingId);
                return Ok(exists);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BookingController: IsBookingExistsAsync - {Message}", ex.Message);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [Authorize(Roles = "admin")]
        [HttpDelete("{bookingId}")]
        public async Task<ActionResult> DeleteBookingAsync(string bookingId)
        {
            try
            {
                await _bookingService.DeleteBookingAsync(bookingId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BookingController: DeleteBookingAsync - {Message}", ex.Message);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }
    }
}
