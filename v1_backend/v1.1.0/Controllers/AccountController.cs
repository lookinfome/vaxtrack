using Microsoft.AspNetCore.Mvc;
using v1Remastered.Services;
using v1Remastered.Dto;

namespace v1Remastered.Controllers
{
    [Route("Account")]
    public class AccountController : Controller
    {
        private readonly IAccountService _accountService;
        private readonly IAuthService _authService;
        private readonly IUserProfileService _userProfileService;

        public AccountController(IAccountService accountService, IAuthService authService, IUserProfileService userProfileService)
        {
            _accountService = accountService;
            _authService = authService;
            _userProfileService = userProfileService;
        }

        [HttpGet("LoginUser")]
        public IActionResult LoginUser()
        {
            return View();
        }

        [HttpPost("LoginUser")]
        public IActionResult LoginUser(UserDetailsDto_Login submittedDetails)
        {
            if(ModelState.IsValid)
            {
                string userId = _accountService.LoginUser(submittedDetails);

                if(!string.IsNullOrEmpty(userId))
                {
                    string userRole = _userProfileService.FetchUserRoleById(userId);
                    if(userRole.ToLower() == "user")
                    {
                        return RedirectToAction("UserProfile", "UserProfile", new {userid = submittedDetails.UserId});
                    }
                    else
                    {
                        return RedirectToAction("AdminPage", "Admin", new {userid = submittedDetails.UserId});
                    }

                }
                else
                {
                    return View(submittedDetails);
                }

            }
            else
            {
                return View(submittedDetails);
            }
        }

        [HttpGet("RegisterUser")]
        public IActionResult RegisterUser()
        {
            return View();
        }

        [HttpPost("RegisterUser")]
        public IActionResult RegisterUser(UserDetailsDto_Register submittedDetails)
        {
        
            if(ModelState.IsValid)
            {
                string userId = _accountService.RegisterUser(submittedDetails);

                if(!string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("UserProfile", "UserProfile", new {userid = submittedDetails.UserId});
                    // return Ok(userId);
                }
                else
                {
                    return View(submittedDetails);
                    // return NotFound(new {systemMessage = $"user not registered"});
                }
            }
            else
            {
                return View(submittedDetails);
                // return NotFound(new {systemMessage = $"{submittedDetails.UserId}, {submittedDetails.UserRole}, invalid registration form inputs"});
            }
        }
    
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogoutUser()
        {
            var result = await _authService.LogoutUserAsync();

            return result!=false ? RedirectToAction("Index", "Home") : NotFound(new {systemMessage = "something went wrong while logging you out"});
        }

    }
}