using MedLoan.Domain.Common;
using MedLoan.Domain.Entities;
using MedLoan.Domain.Enums;

namespace MedLoan.Application.Tests;

public class ItemCopyTests
{
    [Fact]
    public void ItemCopy_CheckOut_WhenLoaned_Throws()
    {
        var copy = new ItemCopy { Status = CopyStatus.Loaned };

        var ex = Assert.Throws<DomainException>(copy.CheckOut);

        Assert.Equal("העותק אינו זמין", ex.Message);
        Assert.Equal(CopyStatus.Loaned, copy.Status);
    }
}
