using Vaxtrack.Interfaces;
using Vaxtrack.Interfaces.RepositoryInterfaces;
using Vaxtrack.Dtos.HospitalDtos;
using Vaxtrack.Models;

namespace Vaxtrack.Services
{
    public class HospitalService: IHospitalService
    {
        private readonly IHospitalRepository _hospitalRepository;
        
        public HospitalService(IHospitalRepository hospitalRepository)
        {
            _hospitalRepository = hospitalRepository;   
        }

        public async Task<CreateHospitalResponseDto> CreateHospitalAsync(CreateHospitalRequestDto registerNewHospitalRequest)
        {
            ArgumentNullException.ThrowIfNull(registerNewHospitalRequest);

            var newHospital = MapNewHospitalToHospitalModel(registerNewHospitalRequest);
            var createdHospital = await _hospitalRepository.AddHospitalAsync(newHospital);
            return MapToCreateHospitalResponseDto(createdHospital);
        }

        public async Task<HospitalProfileDataDto> GetHospitalByIdAsync(string hospitalId)
        {
            ArgumentNullException.ThrowIfNull(hospitalId);

            if(!await _hospitalRepository.IsHospitalExistingAsync(hospitalId))
            {
                throw new ArgumentException("Hospital not found.");
            }

            var foundHospital =  await _hospitalRepository.GetHospitalByIdAsync(hospitalId);
            return MapToHospitalProfileDataDto(foundHospital);

        }
        
        public async Task<List<HospitalProfileDataDto>> GetAllHospitalsAsync()
        {
            var allHospitals = await _hospitalRepository.GetAllHospitalsAsync();
            ArgumentNullException.ThrowIfNull(allHospitals);

            List<HospitalProfileDataDto> hospitalList = new List<HospitalProfileDataDto>();
            foreach(HospitalModel hospital in allHospitals)
            {
                hospitalList.Add(MapToHospitalProfileDataDto(hospital));
            }
            return hospitalList;
        }
        
        public async Task<UpdateHospitalResponseDto> UpdateHospitalAsync(UpdateHospitalRequestDto updateHospitalRequest)
        {
            ArgumentNullException.ThrowIfNull(updateHospitalRequest);

            if(!await _hospitalRepository.IsHospitalExistingAsync(updateHospitalRequest.HospitalId))
            {
                throw new ArgumentException("Hospital not found.");
            }

            var updateHospital = await MapUpdateHospitalDataToHospitalModel(updateHospitalRequest);
            var updatedHospitalResponse = await _hospitalRepository.UpdateHospitalAsync(updateHospital);
            return MapToUpdateHospitalResponseDto(updatedHospitalResponse);

        }

        public async Task<int> UpdateTotalSlotsAsync(string hospitalId, int totalSlots)
        {
            ArgumentNullException.ThrowIfNull(hospitalId);

            if(!await _hospitalRepository.IsHospitalExistingAsync(hospitalId))
            {
                throw new ArgumentException("Hospital not found.");
            }

            if (totalSlots < 0)
            {
                throw new ArgumentException("Total slots cannot be less than 0.");
            }

            var updatedTotalSlots = await _hospitalRepository.UpdateTotalSlotsAsync(hospitalId, totalSlots);
            return updatedTotalSlots;
        }

        public async Task<int> UpdateAvailableSlotsAsync(string hospitalId, int slotsToUpdate)
        {
            ArgumentNullException.ThrowIfNull(hospitalId);

            if(!await _hospitalRepository.IsHospitalExistingAsync(hospitalId))
            {
                throw new ArgumentException("Hospital not found.");
            }

            if (slotsToUpdate == 0)
            {
                throw new ArgumentException("slotsToUpdate cannot be 0.");
            }

            var foundHospital = await _hospitalRepository.GetHospitalByIdAsync(hospitalId);
            var updatedAvailableSlots = foundHospital.SlotsAvailable + slotsToUpdate;

            if (updatedAvailableSlots < 0)
            {
                throw new ArgumentException("Updated slots cannot be less than 0.");
            }

            if (updatedAvailableSlots > foundHospital.TotalSlots)
            {
                throw new ArgumentException("Updated slots cannot be greater than total slots.");
            }

            var updatedHospital = await _hospitalRepository.UpdateAvailableSlotsAsync(hospitalId, updatedAvailableSlots);
            return updatedHospital.SlotsAvailable;
        }

        public async Task DeleteHospitalAsync(string hospitalId)
        {
            ArgumentNullException.ThrowIfNull(hospitalId);
            if(!await _hospitalRepository.IsHospitalExistingAsync(hospitalId))
            {
                throw new ArgumentException("Hospital not found.");
            }

            var timestamp = DateTime.UtcNow;
            await _hospitalRepository.DeleteHospitalAsync(hospitalId, timestamp, true);
        }

        // utility methods
        private HospitalProfileDataDto MapToHospitalProfileDataDto(HospitalModel hospitalDetails)
        {
            return new HospitalProfileDataDto
            {
                HospitalId = hospitalDetails.HospitalId,
                HospitalName = hospitalDetails.HospitalName,
                HospitalAddress = hospitalDetails.HospitalAddress,
                HospitalEmail = hospitalDetails.HospitalEmail,
                HospitalPhoneNumber = hospitalDetails.HospitalPhoneNumber,
                HospitalPinCode = hospitalDetails.HospitalPinCode,
                TotalSlots = hospitalDetails.TotalSlots,
                SlotsAvailable = hospitalDetails.SlotsAvailable,
                RegisteredDate = hospitalDetails.RegisteredDate,
                UpdatedDate = hospitalDetails.UpdatedDate,
                RemovedDate = hospitalDetails.RemovedDate
            };
        }

        private async Task<HospitalModel> MapUpdateHospitalDataToHospitalModel(UpdateHospitalRequestDto updateHospitalRequest)
        {
            var existingHospital = await _hospitalRepository.GetHospitalByIdAsync(updateHospitalRequest.HospitalId);

            return new HospitalModel
            {
                HospitalId = existingHospital.HospitalId,
                HospitalUid = existingHospital.HospitalUid,
                HospitalName = existingHospital.HospitalName,
                HospitalAddress = updateHospitalRequest.HospitalAddress,
                HospitalPinCode = updateHospitalRequest.HospitalPinCode,
                HospitalPhoneNumber = updateHospitalRequest.HospitalPhoneNumber,
                HospitalEmail = updateHospitalRequest.HospitalEmail,
                TotalSlots = existingHospital.TotalSlots,
                SlotsAvailable = existingHospital.SlotsAvailable,
                RegisteredDate = existingHospital.RegisteredDate,
                UpdatedDate = DateTime.UtcNow,
                RemovedDate = existingHospital.RemovedDate,
                IsDeleted = existingHospital.IsDeleted
            };
        }

        private UpdateHospitalResponseDto MapToUpdateHospitalResponseDto(HospitalModel updatedHospital)
        {
            return new UpdateHospitalResponseDto
            {
                HospitalId = updatedHospital.HospitalId,
                HospitalAddress = updatedHospital.HospitalAddress,
                HospitalPinCode = updatedHospital.HospitalPinCode,
                HospitalEmail = updatedHospital.HospitalEmail,
                HospitalPhoneNumber = updatedHospital.HospitalPhoneNumber
            };
        }

        private HospitalModel MapNewHospitalToHospitalModel(CreateHospitalRequestDto registerNewHospitalRequest)
        {
            var timestamp = DateTime.UtcNow;

            return new HospitalModel
            {
                HospitalId = GenerateHospitalId(registerNewHospitalRequest.HospitalName),
                HospitalUid = Guid.NewGuid().ToString(),
                HospitalName = registerNewHospitalRequest.HospitalName,
                HospitalAddress = "",
                HospitalPhoneNumber = "",
                HospitalPinCode = "",
                HospitalEmail = "",
                TotalSlots = 50,
                SlotsAvailable = 50,
                RegisteredDate = timestamp,
                UpdatedDate = timestamp
            };
        }

        private CreateHospitalResponseDto MapToCreateHospitalResponseDto(HospitalModel createdHospital)
        {
            return new CreateHospitalResponseDto
            {
                HospitalId = createdHospital.HospitalId,
                HospitalName = createdHospital.HospitalName,
                HospitalAddress = createdHospital.HospitalAddress,
                HospitalPinCode = createdHospital.HospitalPinCode,
                HospitalPhoneNumber = createdHospital.HospitalPhoneNumber,
                HospitalEmail = createdHospital.HospitalEmail,
                TotalSlots = createdHospital.TotalSlots,
                SlotsAvailable = createdHospital.SlotsAvailable,
                RegisteredDate = createdHospital.RegisteredDate
            };
        }

        private string GenerateHospitalId(string hospitalName)
        {
            return $"{hospitalName.ToString().Substring(0, 3)}_{Guid.NewGuid().ToString().Substring(0, 8)}";
        }


    }

}
