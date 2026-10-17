using AutoMapper;
using MedLoan.Domain.Entities;
using MedLoan.Domain.Enums;

namespace MedLoan.Application.Items;

public class ItemProfile : Profile
{
    public ItemProfile()
    {
        // The copies are already loaded with the item, so counting them here adds no query.
        CreateMap<Item, ItemDetailsDto>()
            .ForCtorParam(nameof(ItemDetailsDto.CategoryName), o => o.MapFrom(i => i.Category.Name))
            .ForCtorParam(nameof(ItemDetailsDto.AvailableCopies),
                o => o.MapFrom(i => i.Copies.Count(c => c.Status == CopyStatus.Available)))
            .ForCtorParam(nameof(ItemDetailsDto.Tags), o => o.MapFrom(i => i.Tags.OrderBy(t => t.Name)));

        CreateMap<CreateItemRequest, Item>()
            .ForMember(i => i.Id, o => o.Ignore())
            .ForMember(i => i.Category, o => o.Ignore())
            .ForMember(i => i.Copies, o => o.Ignore())
            .ForMember(i => i.Tags, o => o.Ignore())   // set by ItemService from TagIds
            .ForMember(i => i.CreatedAt, o => o.Ignore());

        CreateMap<UpdateItemRequest, Item>()
            .ForMember(i => i.Id, o => o.Ignore())
            .ForMember(i => i.Category, o => o.Ignore())
            .ForMember(i => i.Copies, o => o.Ignore())
            .ForMember(i => i.Tags, o => o.Ignore())   // set by ItemService from TagIds
            .ForMember(i => i.CreatedAt, o => o.Ignore());
    }
}
