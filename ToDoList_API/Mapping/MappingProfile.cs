using AutoMapper;
using ToDoList_Core.Domain.Implementation;
using ToDoListAPI.DTOs.Duty;
using ToDoListAPI.DTOs.User;
namespace ToDoListAPI.Mapping
{
    public class MappingProfile: Profile
    {
        public MappingProfile() {
            // User Maps
            CreateMap<RegisterUserDTO, User>()
                .ForMember(dest => dest.PasswordHash, opt => opt.MapFrom(src => src.Password));
            CreateMap<UpdateUserNormalContentDTO, User>();
            CreateMap<UpdateUserPasswordDTO, User>();
            CreateMap<UpdateUserEmailDTO, User>();
            // Duty Maps
            CreateMap<RegisterDutyDTO, Duty>();
            CreateMap<ChangeDutyStatusDTO, Duty>();
        }
    }
}
