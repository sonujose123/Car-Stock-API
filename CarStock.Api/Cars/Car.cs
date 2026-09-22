using FastEndpoints;
using FluentValidation;

namespace CarStock.Api.Cars;

public sealed record Car(long Id, string Make, string Model, long Year, long StockLevel);

public sealed class AddCarRequest
{
    public string Make { get; set; } = "";
    public string Model { get; set; } = "";
    public int? Year { get; set; }
    public int? StockLevel { get; set; }
}

public sealed class AddCarValidator : Validator<AddCarRequest>
{
    public AddCarValidator()
    {
        RuleFor(x => x.Make).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Model).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Year).NotNull().Must(year => year is null || (year >= 1886 && year <= DateTime.UtcNow.Year + 1))
            .WithMessage("Year must be between 1886 and next year.");
        RuleFor(x => x.StockLevel).NotNull().GreaterThanOrEqualTo(0);
    }
}

public sealed class UpdateStockRequest
{
    public int? StockLevel { get; set; }
}

public sealed class UpdateStockValidator : Validator<UpdateStockRequest>
{
    public UpdateStockValidator() => RuleFor(x => x.StockLevel).NotNull().GreaterThanOrEqualTo(0);
}
