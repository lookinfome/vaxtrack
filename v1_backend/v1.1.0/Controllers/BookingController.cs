using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using v1Remastered.Services;
using v1Remastered.Dto;

namespace v1Remastered.Controllers
{
    [Route("Booking")]
    public class BookingController : Controller
    {
        private readonly IBookingService _bookingService;

        private readonly IHospitalService _hospitalService;
        public BookingController(IBookingService bookingService, IHospitalService hospitalService)
        {
            _bookingService = bookingService;
            _hospitalService = hospitalService;
        }

        [Authorize]
        [HttpGet("{userid}")]
        public IActionResult Booking(string userid)
        {

            // fetch booking details for user
            var _userBookingDetails = _bookingService.FetchBookingDetails(userid);

            // fetch booking id for user
            if(!string.IsNullOrEmpty(_userBookingDetails?.BookingId))
            {

                // return message page
                ViewBag.BookingErrorMsg = $"{userid} - already booked both slots, booking Id: {_userBookingDetails.BookingId}";
                return View("~/Views/Booking/BookingError.cshtml");
            }

            // if booking id not exists
            else
            {
                // fetch username
                @ViewBag.userid = userid;

                if(!_hospitalService.FetchSlotAvailableStatus())
                {
                    ViewBag.BookingErrorMsg = "No vaccine center are available with open slots right now";
                    return View("~/Views/Booking/BookingError.cshtml");
                }
                else
                {
                    // return new slot booking form
                    return View();
                }
            }
        }

        [Authorize]
        [HttpPost("{userid}")]
        public IActionResult Booking(BookingDetailsDto_SlotBook submittedDetails, [FromRoute] string userid)
        {
            ViewBag.userId = userid;

            int result = _bookingService.BookSlot(submittedDetails, userid);

            if(result > 0)
            {
                return RedirectToAction("UserProfile", "UserProfile", new { userid = userid });
            }
            else
            {
                ViewBag.BookingErrorMsg = "Unexpected error occurred";
                return View("~/Views/Booking/BookingError.cshtml");
            }
        }
    }
}