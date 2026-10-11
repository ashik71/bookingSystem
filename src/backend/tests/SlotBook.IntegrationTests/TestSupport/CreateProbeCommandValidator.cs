#pragma warning disable CA1812 // instantiated by the dispatcher scan
using FluentValidation;

namespace SlotBook.IntegrationTests.TestSupport;

internal sealed class CreateProbeCommandValidator : AbstractValidator<CreateProbeCommand>
{
    public CreateProbeCommandValidator()
    {
        RuleFor(c => c.DisplayName).NotEmpty();
        RuleFor(c => c.Count).InclusiveBetween(1, 10);
    }
}
