namespace Fintacharts.AssetTracker.Features.GetPriceHistory;

using FluentValidation;

public class GetPriceHistoryValidator : AbstractValidator<GetPriceHistoryRequest>
{
    public GetPriceHistoryValidator()
    {
        RuleFor(x => x.id) 
            .Must(id => Guid.TryParse(id, out _))
            .WithMessage("Invalid ID format: '{PropertyValue}'. Expected GUID.");
        
        RuleFor(x => x.barsCount) 
            .GreaterThan(0)
            .WithMessage("Bars count must be greater than zero.")
            .LessThanOrEqualTo(100)
            .WithMessage("Bars count must be less than or equal 100.");
    }
}