using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using v1Remastered.Services;
using v1Remastered.Models;
using v1Remastered.Dto;
using System.Linq;


namespace v1Remastered.Services
{
    public interface IAccountService
    {
        public string LoginUser(UserDetailsDto_Login submittedDetails);
        public string RegisterUser(UserDetailsDto_Register submittedDetails);
    }

    public class AccountService : IAccountService
    {
        private readonly AppDbContext _v1RemDb;
        private readonly IAuthService _authService;
        private readonly IUserVaccineDetailsService _userVaccineDetailsService;
        
        public AccountService(AppDbContext v1RemDb, IAuthService authService, IUserVaccineDetailsService userVaccineDetailsService)
        {
            _v1RemDb = v1RemDb;
            _authService = authService;
            _userVaccineDetailsService = userVaccineDetailsService;
        }

        // login new user
        public string LoginUser(UserDetailsDto_Login submittedDetails)
        {
            Task<string> result = _authService.LoginUserAsync(submittedDetails.UserId, submittedDetails.UserPassword);

            return !string.IsNullOrEmpty(result.Result.ToString()) && result.Result.ToString() == submittedDetails.UserId ? submittedDetails.UserId : "";
        }

        // register new user
        public string RegisterUser(UserDetailsDto_Register submittedDetails)
        {
            // user id
            submittedDetails.UserId = GenerateUserId(submittedDetails.UserName, submittedDetails.UserUid);
            
            Console.WriteLine($"----------registeration form::::::::{submittedDetails.UserRole}----------");

            // user role
            string userRole = submittedDetails.UserRole == false ? "user" : "admin";

            // register user and login
            Task<string> result = _authService.RegisterUserAsync(submittedDetails.UserId, submittedDetails.UserPassword, userRole);

            // save user details in DB
            if(!string.IsNullOrEmpty(result.Result.ToString()) && result.Result.ToString() == submittedDetails.UserId)
            {
                UserDetailsModel userDetails = new UserDetailsModel()
                {
                    UserId = submittedDetails.UserId,
                    UserName = submittedDetails.UserName,
                    UserUid = submittedDetails.UserUid,
                    UserPhone = submittedDetails.UserPhone,
                    UserGender = submittedDetails.UserGender,
                    UserBirthdate = submittedDetails.UserBirthdate,
                    UserRole = submittedDetails.UserRole
                };

                // update DB
                _v1RemDb.UserDetails.Add(userDetails);
                int userDetailsSavedStatus = _v1RemDb.SaveChanges();

                if(userDetailsSavedStatus >= 0)
                {
                    string userVaccinationId = _userVaccineDetailsService.GenerateUserVaccineId(submittedDetails.UserId);

                    // save user vaccination detail in DB
                    UserVaccineDetailsModel userVaccineDetails = new UserVaccineDetailsModel()
                    {
                        UserId = submittedDetails.UserId,
                        UserVaccinationId = userVaccinationId,
                        UserVaccinationStatus = 0
                    };

                    // update DB
                    _v1RemDb.UserVaccineDetails.Add(userVaccineDetails);
                }

                int userVaccineDetailsSavedStatus= _v1RemDb.SaveChanges();

                return userVaccineDetailsSavedStatus <=0 ? "" : submittedDetails.UserId;
            }

            return "";
        
        }


        private string GenerateUserId(string username, string userUid)
        {
            Random rnd = new Random();
            string rndNum = rnd.Next(0,99).ToString();
            string userId = username.Substring(0,2) + userUid.Substring(4,6) + rndNum; 

            return userId.ToUpper();
        }





    }
}