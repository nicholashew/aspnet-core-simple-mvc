using AutoMapper;
using SimpleMvc.Models;

namespace SimpleMvc.Mappings
{
    public class ViewModelToDomainMappingProfile : Profile
    {
        public ViewModelToDomainMappingProfile()
        {
            //CreateMap<CreateAttachmentViewModel, Attachment>();

            //CreateMap<CreateTicketViewModel, Ticket>()
            //    .ForMember(vm => vm.Attachments, map => map.MapFrom(a => a.Photos));
        }
    }
}
