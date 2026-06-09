
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using v1Remastered.Services;
using v1Remastered.Dto;
using Microsoft.AspNetCore.Identity;

namespace v1Remastered.Controllers
{
    [Route("UserProfile")]
    public class UserProfileController : Controller
    {
        private readonly IAuthService _authService;
        private readonly IUserProfileService _userProfileService;
        private readonly IHospitalService _hospitalService;

        // variable: user manager | for authentication
        private readonly UserManager<AppUserIdentityModel> _userManager;
        public UserProfileController(IAuthService authService, IUserProfileService userProfileService, IHospitalService hospitalService, UserManager<AppUserIdentityModel> userManager)
        {
            _authService = authService;
            _userProfileService = userProfileService;
            _hospitalService = hospitalService;
            _userManager = userManager;
        }

        [Authorize]
        [HttpGet("{userid}")]
        public async Task<IActionResult> UserProfile(string userid)
        {
            if(!string.IsNullOrEmpty(userid))
            {

                // fetch user details from asp-net-user table for authentication
                var loggedInUser = await _userManager.GetUserAsync(User);

                // if user's recor not found
                if (loggedInUser == null || loggedInUser.UserName != userid)
                {
                    await _authService.LogoutUserAsync();
                    return RedirectToAction("LoginUser", "Account");
                }

                UserDetailsDto_UserProfile userProfileDetails = _userProfileService.FetchUserProfileDetails(userid);

                ViewBag.D1HospitalName = _hospitalService.FetchHospitalNameById(userProfileDetails.UserBookingDetails.D1HospitalId);
                ViewBag.D2HospitalName = _hospitalService.FetchHospitalNameById(userProfileDetails.UserBookingDetails.D2HospitalId);

                if(userProfileDetails.UserId != null)
                {
                    return View(userProfileDetails);
                }
            }

            return View("~/Shared/_Error.cshtml");
        }
    }

}