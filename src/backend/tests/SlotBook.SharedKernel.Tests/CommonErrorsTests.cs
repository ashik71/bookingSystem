using Shouldly;
using SlotBook.SharedKernel.Application;
using SlotBook.SharedKernel.Domain;
using Xunit;

namespace SlotBook.SharedKernel.Tests;

public class CommonErrorsTests
{
    [Fact]
    public void DomainExceptionCarriesItsCode()
    {
        var exception = new DomainException(CommonErrors.NotFound);

        exception.Code.ShouldBe("not_found");
    }
}
