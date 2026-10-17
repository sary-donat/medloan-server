using AutoMapper;
using MedLoan.Domain.Entities;

namespace MedLoan.Application.Tags;

public class TagProfile : Profile
{
    public TagProfile()
    {
        CreateMap<Tag, TagDto>();

        CreateMap<CreateTagRequest, Tag>()
            .ForMember(t => t.Id, o => o.Ignore())
            .ForMember(t => t.Items, o => o.Ignore());
    }
}
