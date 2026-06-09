using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using v1Remastered.Services;
using v1Remastered.Models;
using v1Remastered.Dto;
using System.Linq;


namespace v1Remastered.Services
{
    public interface IBookingService
    {
        public int BookSlot(BookingDetailsDto_SlotBook submittedDetails, string userid);
        public BookingDetailsDto_UserBookingDetails FetchBookingDetails(string userid);
    }

    public class BookingService : IBookingService
    {
        private readonly AppDbContext _v1RemDb;
        private readonly IUserVaccineDetailsService _userVaccineDeailsService;
        private readonly IHospitalService _hospitalService;
        public BookingService(AppDbContext v1RemDb, IUserVaccineDetailsService userVaccineDetailsService, IHospitalService hospitalService)
        {
            _v1RemDb = v1RemDb;
            _userVaccineDeailsService = userVaccineDetailsService;
            _hospitalService = hospitalService;
        }

        
        public int BookSlot(BookingDetailsDto_SlotBook submittedDetails, string userid)
        {
            int result = 0;

            // map dose 1 and dose 2
            BookingDetailsModel bookingDetails = new BookingDetailsModel()
            {
                Dose1BookDate = submittedDetails.Dose1BookDate,
                Dose2BookDate = submittedDetails.Dose2BookDate
            };

            // map boooking ID
            bookingDetails.BookingId = GenerateBookingId(userid);

            // map user id and user vaccination id
            bookingDetails.UserId = userid;
            bookingDetails.UserVaccinationId = _userVaccineDeailsService.FetchUserVaccinationID(userid);

            // map hospital details and slots available

                // extract available slot details
                HospitalDetailsModel availableCenters = _hospitalService.FetchCentersWith2Slots();
                List<HospitalDetailsModel> availableCentersList = _hospitalService.FetchCentersWith1Slots();

                // check available details
                if(!string.IsNullOrEmpty(availableCenters.HospitalId))
                {
                    bookingDetails.D1HospitalId = availableCenters.HospitalId;
                    bookingDetails.D2HospitalId = availableCenters.HospitalId;
                    bookingDetails.D1SlotNumber = availableCenters.HospitalAvailableSlots;
                    bookingDetails.D2SlotNumber = availableCenters.HospitalAvailableSlots - 1;

                    // update hospital details
                    availableCenters.HospitalAvailableSlots = availableCenters.HospitalAvailableSlots - 2;

                    // save booking details
                    _v1RemDb.BookingDetails.Add(bookingDetails);

                    // update DB
                    int bookingDetailsSaveStatus = _v1RemDb.SaveChanges();

                    if(bookingDetailsSaveStatus >0)
                    {
                        // update hospital details DB
                        _v1RemDb.HospitalDetails.Update(availableCenters);

                        // update DB
                        result = _v1RemDb.SaveChanges();

                    }
                    else
                    {
                        Console.WriteLine($"-----booking deatils not saved successfully-----");
                    }

                }
                else if(availableCentersList.Count > 0)
                {
                    bookingDetails.D1HospitalId = availableCentersList[0].HospitalId;
                    bookingDetails.D2HospitalId = availableCentersList[1].HospitalId;
                    bookingDetails.D1SlotNumber = availableCentersList[0].HospitalAvailableSlots;
                    bookingDetails.D2SlotNumber = availableCentersList[1].HospitalAvailableSlots;

                    // update hospital details
                    availableCentersList[0].HospitalAvailableSlots = availableCentersList[0].HospitalAvailableSlots -1;
                    availableCentersList[1].HospitalAvailableSlots = availableCentersList[1].HospitalAvailableSlots -1;

                    // save booking details
                    _v1RemDb.BookingDetails.Add(bookingDetails);

                    // update DB       
                    int bookingDetailsSaveStatus = _v1RemDb.SaveChanges(); 

                    if(bookingDetailsSaveStatus >0)
                    {
                        // update hospital details DB
                        _v1RemDb.HospitalDetails.Update(availableCentersList[0]);
                        _v1RemDb.HospitalDetails.Update(availableCentersList[1]);

                        // update DB
                        result = _v1RemDb.SaveChanges();

                    }
                    else
                    {
                        // result = 500;
                        Console.WriteLine($"-----booking deatils not saved successfully-----");
                    }

                }
                else
                {
                    // result = 404;
                    Console.WriteLine($"-----no centers available right now-----");
                }
                
            return result;


        }

        public BookingDetailsDto_UserBookingDetails FetchBookingDetails(string userid)
        {
            var bookingDetails = _v1RemDb.BookingDetails.FirstOrDefault(record => record.UserId == userid);

            if (bookingDetails != null)
            {
                var fetchedBookingDetails = new BookingDetailsDto_UserBookingDetails()
                {
                    BookingId = bookingDetails.BookingId,
                    Dose1BookDate = bookingDetails.Dose1BookDate, // Corrected this line
                    Dose2BookDate = bookingDetails.Dose2BookDate,
                    D1SlotNumber = bookingDetails.D1SlotNumber,
                    D2SlotNumber = bookingDetails.D2SlotNumber,
                    D1HospitalId = bookingDetails.D1HospitalId,
                    D2HospitalId = bookingDetails.D2HospitalId,
                    Dose1ApproveDate = bookingDetails.Dose1ApproveDate,
                    Dose2ApproveDate = bookingDetails.Dose2ApproveDate,
                };

                return fetchedBookingDetails;
            }

            return new BookingDetailsDto_UserBookingDetails();
        }

        private string GenerateBookingId(string userid)
        {
            Random rnd = new Random();
            int randomNum = rnd.Next(100,1000);

            return $"{userid}_B{randomNum}";
        }


    } 

}