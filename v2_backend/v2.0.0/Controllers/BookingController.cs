using Microsoft.AspNetCore.Mvc;
using Vaxtrack.Interfaces;
using Vaxtrack.Dtos.BookingDtos;

namespace Vaxtrack.Controllers
{
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

        [HttpPost]
        public async Task<ActionResult<CreateBookingResponseDto>> CreateBookingAsync(CreateBookingRequestDto createBookingRequestDto)
        {
            try
            {
                var createdBookingResponse = await _bookingService.CreateBookingAsync(createBookingRequestDto);
                return CreatedAtAction(nameof(GetBookingByBookingIdAsync), new { bookingId = createdBookingResponse.BookingId }, createdBookingResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BookingController: CreateBookingAsync - {Message}", ex.Message);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

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
                var bookDose2Response = await _bookingService.BookDose2Async(bookDose2RequestDto);
                return Ok(bookDose2Response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BookingController: BookDose2Async - {Message}", ex.Message);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpPut("{bookingId}")]
        public async Task<ActionResult<BookingProfileDataDto>> ApproveBookingsAsync(string bookingId)
        {
            try
            {
                var approvedBooking = await _bookingService.ApproveBookingsAsync(bookingId);
                return Ok(approvedBooking);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BookingController: ApproveBookingsAsync - {Message}", ex.Message);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpPut("{bookingId}")]
        public async Task<ActionResult<BookingProfileDataDto>> CancelBookingsAsync(string bookingId)
        {
            try
            {
                var canceledBooking = await _bookingService.CancelBookingsAsync(bookingId);
                return Ok(canceledBooking);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BookingController: CancelBookingsAsync - {Message}", ex.Message);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpGet("{bookingId}")]
        public async Task<ActionResult<BookingProfileDataDto>> GetBookingByBookingIdAsync(string bookingId)
        {
            try
            {
                var foundBooking = await _bookingService.GetBookingByBookingIdAsync(bookingId);
                return Ok(foundBooking);
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
                var foundBooking = await _bookingService.GetBookingsByUserIdAsync(userId);
                return Ok(foundBooking);
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
