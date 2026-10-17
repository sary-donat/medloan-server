using AutoMapper;
using MedLoan.Domain.Entities;

namespace MedLoan.Application.Copies;

public class CopyProfile : Profile
{
    public CopyProfile()
    {
        CreateMap<ItemCopy, CopyDto>();

        CreateMap<CreateCopyRequest, ItemCopy>()
            .ForMember(c => c.Id, o => o.Ignore())
            .ForMember(c => c.ItemId, o => o.Ignore())
            .ForMember(c => c.Item, o => o.Ignore())
            .ForMember(c => c.Status, o => o.Ignore())
            .ForMember(c => c.Version, o => o.Ignore())
            .ForMember(c => c.Loans, o => o.Ignore());
    }
}
