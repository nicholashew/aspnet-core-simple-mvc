using AutoMapper;
using SimpleMvc.Models;
using SimpleMvc.ViewModels;
using SimpleMvc.ViewModels.User;

namespace SimpleMvc.Mappings
{
    public class DomainToViewModelMappingProfile : Profile
    {
        public DomainToViewModelMappingProfile()
        {
            CreateMap<ApplicationUser, UserEditViewModel>();
        }
    }
}
