using AutoMapper;
using MedLoan.Domain.Entities;

namespace MedLoan.Application.Categories;

public class CategoryProfile : Profile
{
    public CategoryProfile()
    {
        CreateMap<Category, CategoryDto>();

        CreateMap<CreateCategoryRequest, Category>()
            .ForMember(c => c.Id, o => o.Ignore())
            .ForMember(c => c.Items, o => o.Ignore());

        CreateMap<UpdateCategoryRequest, Category>()
            .ForMember(c => c.Id, o => o.Ignore())
            .ForMember(c => c.Items, o => o.Ignore());
    }
}
