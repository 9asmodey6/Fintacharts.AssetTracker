namespace Fintacharts.AssetTracker.Features.GetPrices;

using FluentValidation;

public class GetPricesValidator : AbstractValidator<GetPricesRequest>
{
    public GetPricesValidator()
    {
        RuleForEach(x => x.ids).NotEqual(Guid.Empty);
    }
}