using AutoMapper;
using SimpleMvc.Models;
using SimpleMvc.ViewModels;

namespace SimpleMvc.Mappings
{
    public class DomainToViewModelMappingProfile : Profile
    {
        public DomainToViewModelMappingProfile()
        {
            CreateMap<ApplicationUser, Models.UserViewModels.EditViewModel>();

            //CreateMap<Building, BuildingViewModel>();

            //CreateMap<Problem, ProblemViewModel>();

            CreateMap<Attachment, AttachmentViewModel>();

            CreateMap<Ticket, TicketViewModel>()
                .ForMember(vm => vm.Attachments, map => map.MapFrom(t => t.Attachments));
                //.ForMember(vm => vm.ProblemNatureName, map => map.MapFrom(t => t.Problem != null ? t.Problem.ProblemNatureName : ""))
                //.ForMember(vm => vm.BuildingName, map => map.MapFrom(t => t.Building != null ? t.Building.BuildingName : ""));

            //CreateMap<Ticket, TicketListEntryViewModel>()
            //    .ForMember(vm => vm.ProblemNatureName, map => map.MapFrom(t => t.Problem != null ? t.Problem.ProblemNatureName : ""))
            //    .ForMember(vm => vm.BuildingName, map => map.MapFrom(t => t.Building != null ? t.Building.BuildingName : ""));
        }
    }
}
