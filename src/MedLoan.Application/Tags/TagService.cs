using AutoMapper;
using MedLoan.Application.Common.Exceptions;
using MedLoan.Application.Common.Interfaces;
using MedLoan.Application.Common.Models;
using MedLoan.Domain.Entities;

namespace MedLoan.Application.Tags;

public class TagService(
    ITagRepository tags,
    IUnitOfWork unitOfWork,
    IMapper mapper) : ITagService
{
    private const string DuplicateNameMessage = "קיימת כבר תגית בשם זה";

    private readonly ITagRepository _tags = tags;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IMapper _mapper = mapper;

    public async Task<IReadOnlyList<TagDto>> GetAllAsync(CancellationToken ct)
    {
        var tags = await _tags.GetAllAsync(ct);
        return _mapper.Map<IReadOnlyList<TagDto>>(tags);
    }

    public async Task<Result<TagDto>> CreateAsync(CreateTagRequest request, CancellationToken ct)
    {
        if (await _tags.NameExistsAsync(request.Name, ct))
            return Result<TagDto>.Conflict(DuplicateNameMessage);

        var tag = _mapper.Map<Tag>(request);
        _tags.Add(tag);

        try
        {
            await _unitOfWork.SaveChangesAsync(ct);
        }
        catch (DuplicateValueException)
        {
            // Another request created the same name between the check and the save.
            return Result<TagDto>.Conflict(DuplicateNameMessage);
        }

        return Result<TagDto>.Success(_mapper.Map<TagDto>(tag));
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct)
    {
        var tag = await _tags.GetByIdForUpdateAsync(id, ct);
        if (tag is null)
            return Result.NotFound("התגית לא נמצאה");

        // The tag's ItemTags rows are deleted with it (cascade on the join table).
        _tags.Remove(tag);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
