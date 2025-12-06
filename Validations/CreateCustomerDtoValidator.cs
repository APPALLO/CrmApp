using CrmApp.Models.DTOs;
using FluentValidation;

namespace CrmApp.Validations;

public class CreateCustomerDtoValidator : AbstractValidator<CreateCustomerDto>
{
    public CreateCustomerDtoValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("Ad alanı boş olamaz.")
            .MaximumLength(100).WithMessage("Ad en fazla 100 karakter olabilir.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Soyad alanı boş olamaz.")
            .MaximumLength(100).WithMessage("Soyad en fazla 100 karakter olabilir.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email alanı boş olamaz.")
            .EmailAddress().WithMessage("Geçerli bir email adresi giriniz.")
            .MaximumLength(200);

        RuleFor(x => x.TotalPurchaseAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Toplam tutar negatif olamaz.");
            
        RuleFor(x => x.PhoneNumber)
            .Matches(@"^\+?[1-9][0-9]{7,14}$").When(x => !string.IsNullOrEmpty(x.PhoneNumber))
            .WithMessage("Geçerli bir telefon numarası giriniz.");
    }
}
