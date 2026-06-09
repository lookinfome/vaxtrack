using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using v1Remastered.Services;
using v1Remastered.Models;
using v1Remastered.Dto;
using System.Linq;

namespace v1Remastered.Services
{
    public interface IUserVaccineDetailsService
    {
        public string GenerateUserVaccineId(string userid);
        public string FetchUserVaccinationID(string userid);
        public UserVaccineDetailsDto_VaccineDetails FetchUserVaccineDetails(string userid);
    }

    public class UserVaccineDetailsService : IUserVaccineDetailsService
    {
        private readonly AppDbContext _v1RemDb;

        public UserVaccineDetailsService(AppDbContext v1RemDb)
        {
            _v1RemDb = v1RemDb;
        }

        public UserVaccineDetailsDto_VaccineDetails FetchUserVaccineDetails(string userid)
        {
            var userVaccineDetails = _v1RemDb.UserVaccineDetails.FirstOrDefault(record => record.UserId == userid);

            if (userVaccineDetails != null && !string.IsNullOrEmpty(userVaccineDetails.UserId))
            {
                return new UserVaccineDetailsDto_VaccineDetails()
                {
                    UserId = userVaccineDetails.UserId,
                    UserVaccinationId = userVaccineDetails.UserVaccinationId,
                    UserVaccinationStatus = userVaccineDetails.UserVaccinationStatus == 0 ? "Not Vaccinated" : "Vaccinated"
                };
            }

            return new UserVaccineDetailsDto_VaccineDetails();
        }

        public string FetchUserVaccinationID(string userid)
        {
            var userVaccinationId = _v1RemDb.UserVaccineDetails.FirstOrDefault(record=>record.UserId == userid);

            if(userVaccinationId != null && !string.IsNullOrEmpty(userVaccinationId.UserVaccinationId))
            {
                return userVaccinationId.UserVaccinationId;
            }

            return "";
        
        }

        public string GenerateUserVaccineId(string userid)
        {
            Random rnd = new Random();
            int randomNum = rnd.Next(0,99);

            return $"{userid}_{randomNum}";
        }
    }
}