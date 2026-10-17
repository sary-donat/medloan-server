using AutoMapper;
using MedLoan.Application.Common.Exceptions;
using MedLoan.Application.Common.Interfaces;
using MedLoan.Application.Common.Models;
using MedLoan.Domain.Entities;
using MedLoan.Domain.Enums;

namespace MedLoan.Application.Copies;

public class CopyService(
    IItemCopyRepository copies,
    IItemRepository items,
    IUnitOfWork unitOfWork,
    IMapper mapper) : ICopyService
{
    private const string ItemNotFoundMessage = "הפריט לא נמצא";
    private const string DuplicateSerialMessage = "קיים כבר עותק עם מספר סידורי זה";

    private readonly IItemCopyRepository _copies = copies;
    private readonly IItemRepository _items = items;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IMapper _mapper = mapper;

    public async Task<Result<IReadOnlyList<CopyDto>>> GetByItemIdAsync(int itemId, CancellationToken ct)
    {
        if (!await _items.ExistsAsync(itemId, ct))
            return Result<IReadOnlyList<CopyDto>>.NotFound(ItemNotFoundMessage);

        var copies = await _copies.GetByItemIdAsync(itemId, ct);
        return Result<IReadOnlyList<CopyDto>>.Success(_mapper.Map<IReadOnlyList<CopyDto>>(copies));
    }

    public async Task<Result<CopyDto>> CreateAsync(int itemId, CreateCopyRequest request, CancellationToken ct)
    {
        if (!await _items.ExistsAsync(itemId, ct))
            return Result<CopyDto>.NotFound(ItemNotFoundMessage);

        if (await _copies.SerialNumberExistsAsync(request.SerialNumber, ct))
            return Result<CopyDto>.Conflict(DuplicateSerialMessage);

        var copy = _mapper.Map<ItemCopy>(request);
        copy.ItemId = itemId;
        copy.Status = CopyStatus.Available;   // a new copy is always available
        _copies.Add(copy);

        try
        {
            await _unitOfWork.SaveChangesAsync(ct);
        }
        catch (DuplicateValueException)
        {
            // Another request added the same serial number between the check and the save.
            return Result<CopyDto>.Conflict(DuplicateSerialMessage);
        }

        return Result<CopyDto>.Success(_mapper.Map<CopyDto>(copy));
    }

    public async Task<Result> UpdateAsync(int id, UpdateCopyRequest request, CancellationToken ct)
    {
        var copy = await _copies.GetByIdForUpdateAsync(id, ct);
        if (copy is null)
            return Result.NotFound("העותק לא נמצא");

        // Only a loan puts a copy into Loaned, and only a return takes it out.
        if (request.Status == CopyStatus.Loaned)
            return Result.Validation("לא ניתן להגדיר עותק כמושאל ידנית. עותק מושאל רק דרך השאלה.");

        if (request.Status is not null && copy.Status == CopyStatus.Loaned)
            return Result.Conflict("לא ניתן לשנות סטטוס של עותק מושאל");

        if (request.Status is CopyStatus status)
            copy.Status = status;
        if (request.Condition is CopyCondition condition)
            copy.Condition = condition;
        if (request.Notes is not null)
            copy.Notes = request.Notes;

        try
        {
            await _unitOfWork.SaveChangesAsync(ct);
        }
        catch (ConcurrencyConflictException)
        {
            // The copy was loaned or returned between loading and saving.
            return Result.Conflict("העותק עודכן על ידי משתמש אחר. נא לנסות שוב.");
        }

        return Result.Success();
    }
}
