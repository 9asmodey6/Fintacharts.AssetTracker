namespace Fintacharts.AssetTracker.Features.GetPrices;

using FluentValidation;

public class GetPricesValidator : AbstractValidator<GetPricesRequest>
{
    public GetPricesValidator()
    {
        RuleForEach(x => x.ids)
            .Must(id => Guid.TryParse(id, out _))
            .WithMessage("Invalid ID format: '{PropertyValue}'. Expected GUID.")
            .When(x => x != null && x.ids.Length > 0);
    }
}