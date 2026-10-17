using AutoMapper;
using MedLoan.Domain.Entities;

namespace MedLoan.Application.Members;

public class MemberProfile : Profile
{
    public MemberProfile()
    {
        CreateMap<Member, MemberDto>();
    }
}
