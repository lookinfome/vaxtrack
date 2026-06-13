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

        public async Task<CreateHospitalResponseDto> CreateHospitalAsync(CreateHospitalRequestDto createHospitalRequest)
        {
            ArgumentNullException.ThrowIfNull(createHospitalRequest);

            try
            {
                var newHospital = MapHospitalCreateReqeustToHospitalModel(createHospitalRequest);
                var createdHospital = await _hospitalRepository.CreateHospitalAsync(newHospital);

                return MapToCreatedHospitalResponseDto(createdHospital);
            }
            catch(Exception ex)
            {
                throw new Exception($"HospitalService: CreateHospitalAsync - {ex}");
            }
        }

        public async Task<UpdateHospitalResponseDto> UpdateHospitalAsync(UpdateHospitalRequestDto updateHospitalRequest)
        {
            ArgumentNullException.ThrowIfNull(updateHospitalRequest);

            try
            {
                string hospitalId = updateHospitalRequest.HospitalId;
                var foundHospital = await _hospitalRepository.GetHospitalByIdAsync(hospitalId);

                if(foundHospital == null)
                {
                    throw new Exception($"HospitalService: UpdateHospitalAsync - hospital {hospitalId} not found!");
                }

                var mapFoundHospital = MapHospitalUpdateRequestDtoToHospitalModel(foundHospital, updateHospitalRequest);
                var updatedHospital = await _hospitalRepository.UpdateHospitalAsync(mapFoundHospital);

                return MapToUpdateHospitalResponseDto(updatedHospital);
            }
            catch(Exception ex)
            {
                throw new Exception($"{ex}");
            }
        }

        public async Task<int> UpdateTotalSlotsAsync(string hospitalId, int totalSlots)
        {
            ArgumentNullException.ThrowIfNull(hospitalId);

            try
            {
                var foundHospital = await _hospitalRepository.GetHospitalByIdAsync(hospitalId);

                if(foundHospital == null)
                {
                    throw new Exception($"HospitalService: UpdateTotalSlotsAsync - hospital {hospitalId} not found!");
                }

                var mapFoundHospital = MapHospitalUpdateRequestDtoToHospitalModel(foundHospital, null, null, totalSlots);
                var updatedHospital = await _hospitalRepository.UpdateHospitalAsync(mapFoundHospital);

                return updatedHospital.TotalSlots;
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex}");
            }
        }

        public async Task<int> UpdateAvailableSlotsAsync(string hospitalId, int availableSlots)
        {
            ArgumentNullException.ThrowIfNull(hospitalId);

            try
            {
                var foundHospital = await _hospitalRepository.GetHospitalByIdAsync(hospitalId);

                if(foundHospital == null)
                {
                    throw new Exception($"HospitalService: UpdateTotalSlotsAsync - hospital {hospitalId} not found!");
                }

                var mapFoundHospital = MapHospitalUpdateRequestDtoToHospitalModel(foundHospital, null, null, null, availableSlots);
                var updatedHospital = await _hospitalRepository.UpdateHospitalAsync(mapFoundHospital);

                return updatedHospital.SlotsAvailable;
            }
            catch(Exception ex)
            {
                throw new Exception($"{ex}");
            }
        }

        public async Task<HospitalProfileDataDto> GetHospitalByIdAsync(string hospitalId)
        {
            ArgumentNullException.ThrowIfNull(hospitalId);

            try
            {
                var foundHospital = await _hospitalRepository.GetHospitalByIdAsync(hospitalId);

                if(foundHospital == null)
                {
                    throw new Exception($"HospitalService: UpdateTotalSlotsAsync - hospital {hospitalId} not found!");
                }

                return MapToHospitalProfileDataDto(foundHospital);                
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex}");
            }
        }

        public async Task<List<HospitalProfileDataDto>> GetAllHospitalsAsync()
        {
            try
            {
                List<HospitalProfileDataDto> hospitalList = new List<HospitalProfileDataDto>();

                var foundHospitalList = await _hospitalRepository.GetAllHospitalDetailsAsync();

                if(foundHospitalList == null)
                {
                    throw new Exception($"HospitalService: GetAllHospitalAsync - no hospitals found!");
                }

                foreach(var hospital in foundHospitalList)
                {
                    hospitalList.Add(MapToHospitalProfileDataDto(hospital));
                }

                return hospitalList;
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex}");
            }
        }

        public async Task DeleteHospitalAsync(string hospitalId)
        {
            ArgumentNullException.ThrowIfNull(hospitalId);

            try
            {
                var foundHospital = await _hospitalRepository.GetHospitalByIdAsync(hospitalId);

                if(foundHospital == null)
                {
                    throw new Exception($"HospitalService: DeleteHospitalAsync - hospital {hospitalId} not found!");
                }

                var mappedFoundHospital = MapHospitalUpdateRequestDtoToHospitalModel(foundHospital, null, true, null, null);
                await _hospitalRepository.UpdateHospitalAsync(mappedFoundHospital); 
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex}");
            }
        }

        // utility methods

        private HospitalModel MapHospitalCreateReqeustToHospitalModel(CreateHospitalRequestDto createHospitalRequest)
        {
            var timestamp = DateTime.UtcNow;

            return new HospitalModel
            {
                HospitalId = GenerateHospitalId(createHospitalRequest.HospitalName),
                HospitalUid = Guid.NewGuid().ToString(),
                HospitalName = createHospitalRequest.HospitalName,
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
        private HospitalModel MapHospitalUpdateRequestDtoToHospitalModel(HospitalModel foundHospital, UpdateHospitalRequestDto? updateHospitalRequestDto = null, bool? isDeleted = null, int? totalSlots = null, int? availableSlots = null)
        {
            var timestamp = DateTime.UtcNow;

            if(updateHospitalRequestDto != null && isDeleted == null && totalSlots == null && availableSlots == null)
            {
                foundHospital.HospitalAddress = updateHospitalRequestDto.HospitalAddress;
                foundHospital.HospitalPinCode = updateHospitalRequestDto.HospitalPinCode; 
                foundHospital.HospitalPhoneNumber = updateHospitalRequestDto.HospitalPhoneNumber;
                foundHospital.HospitalEmail = updateHospitalRequestDto.HospitalEmail;
            }

            if(isDeleted != null)
            {
                foundHospital.RemovedDate = timestamp;
                foundHospital.IsDeleted = true;   
            }

            if(totalSlots != null)
            {
                foundHospital.TotalSlots = totalSlots.Value;
            }

            if(availableSlots != null)
            {
                foundHospital.SlotsAvailable = availableSlots.Value;   
            }

            foundHospital.UpdatedDate = timestamp;
            return foundHospital;
        }
        private CreateHospitalResponseDto MapToCreatedHospitalResponseDto(HospitalModel createdHospital)
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
        private string GenerateHospitalId(string hospitalName)
        {
            return $"{hospitalName.ToString().Substring(0, 3)}_{Guid.NewGuid().ToString().Substring(0, 8)}";
        }
    }

}
