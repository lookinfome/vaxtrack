using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using v1Remastered.Services;
using v1Remastered.Models;
using v1Remastered.Dto;
using System.Linq;


namespace v1Remastered.Services
{
    public interface IUserProfileService
    {
        public UserDetailsDto_UserProfile FetchUserProfileDetails(string userid);
        public string FetchUserRoleById(string userid);
    }

    public class UserProfileService : IUserProfileService
    {
        private readonly AppDbContext _v1RemDb;
        private readonly IUserVaccineDetailsService _userVaccineDetailsService;
        private readonly IBookingService _bookingService;

        private readonly IHospitalService _hospitalService;
        
        public UserProfileService(AppDbContext v1RemDb, IUserVaccineDetailsService userVaccineDetailsService, IBookingService bookingService, IHospitalService hospitalService)
        {
            _v1RemDb = v1RemDb;
            _userVaccineDetailsService = userVaccineDetailsService;
            _bookingService = bookingService; 
            _hospitalService = hospitalService;
        }

        public UserDetailsDto_UserProfile FetchUserProfileDetails(string userid)
        {
            var userDetails = _v1RemDb.UserDetails.FirstOrDefault(record => record.UserId == userid);

            if (userDetails != null && !string.IsNullOrEmpty(userDetails.UserId))
            {
                var userVaccineDetails = _userVaccineDetailsService.FetchUserVaccineDetails(userid);
                var userBookingDetails = _bookingService.FetchBookingDetails(userid);

                var userProfileDetails = new UserDetailsDto_UserProfile()
                {
                    UserId = userDetails.UserId,
                    Username = userDetails.UserName,
                    UserBirthdate = userDetails.UserBirthdate,
                    UserGender = userDetails.UserGender,
                    UserPhone = userDetails.UserPhone,
                    UserRole = userDetails.UserRole,
                    UserUid = userDetails.UserUid,
                    UserVaccineDetails = userVaccineDetails,
                    UserBookingDetails = userBookingDetails
                };

                return userProfileDetails;
            }

            return new UserDetailsDto_UserProfile();
        }

        public string FetchUserRoleById(string userid)
        {
            var userRecord = _v1RemDb.UserDetails.FirstOrDefault(record => record.UserId == userid);

            if (userRecord != null)
            {
                return userRecord.UserRole ? "admin" : "user";
            }

            return "user"; // Default role if userRecord is null
        }
    
    }
}