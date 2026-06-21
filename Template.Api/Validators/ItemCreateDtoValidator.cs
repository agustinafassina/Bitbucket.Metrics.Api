using FluentValidation;
using Template.Models.Dto;

namespace Template.Api.Validators
{
    public class ItemCreateDtoValidator : AbstractValidator<ItemCreateDto>
    {
        public ItemCreateDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MaximumLength(200).WithMessage("Name must not exceed 200 characters.");
        }
    }
}
