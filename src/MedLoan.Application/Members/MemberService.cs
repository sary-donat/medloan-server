using AutoMapper;
using MedLoan.Application.Common.Interfaces;
using MedLoan.Application.Common.Models;
using MedLoan.Domain.Enums;

namespace MedLoan.Application.Members;

public class MemberService(
    IMemberRepository members,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IMapper mapper) : IMemberService
{
    private const string NotFoundMessage = "החבר לא נמצא";

    private readonly IMemberRepository _members = members;
    private readonly ICurrentUser _currentUser = currentUser;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IMapper _mapper = mapper;

    public Task<PagedResult<MemberDto>> GetPagedAsync(int page, CancellationToken ct) =>
        _members.GetPagedAsync(page, ct);

    public async Task<Result<MemberDto>> GetCurrentAsync(CancellationToken ct)
    {
        if (_currentUser.Id is not int id)
            return Result<MemberDto>.Unauthorized("נדרשת התחברות");

        return await GetByIdAsync(id, ct);
    }

    public async Task<Result<MemberDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        var member = await _members.GetByIdAsync(id, ct);
        return member is null
            ? Result<MemberDto>.NotFound(NotFoundMessage)
            : Result<MemberDto>.Success(_mapper.Map<MemberDto>(member));
    }

    public async Task<Result> UpdateAsync(int id, UpdateMemberRequest request, CancellationToken ct)
    {
        // An admin who blocks or demotes themselves could leave the system with no active admin.
        if (id == _currentUser.Id && (request.IsActive is not null || request.Role is not null))
            return Result.Validation("לא ניתן לשנות את הסטטוס או התפקיד של המשתמש המחובר");

        var member = await _members.GetByIdForUpdateAsync(id, ct);
        if (member is null)
            return Result.NotFound(NotFoundMessage);

        if (request.IsActive is bool isActive)
            member.IsActive = isActive;
        if (request.Role is UserRole role)
            member.Role = role;

        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
