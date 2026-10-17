using AutoMapper;
using MedLoan.Domain.Entities;

namespace MedLoan.Application.Loans;

public class LoanProfile : Profile
{
    public LoanProfile()
    {
        // Needs ItemCopy.Item and Member loaded (the loan queries include them).
        CreateMap<Loan, LoanDto>()
            .ForCtorParam(nameof(LoanDto.SerialNumber), o => o.MapFrom(l => l.ItemCopy.SerialNumber))
            .ForCtorParam(nameof(LoanDto.ItemId), o => o.MapFrom(l => l.ItemCopy.ItemId))
            .ForCtorParam(nameof(LoanDto.ItemName), o => o.MapFrom(l => l.ItemCopy.Item.Name))
            .ForCtorParam(nameof(LoanDto.MemberFullName), o => o.MapFrom(l => l.Member.FullName));
    }
}
