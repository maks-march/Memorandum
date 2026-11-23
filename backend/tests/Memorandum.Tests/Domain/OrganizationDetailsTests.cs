using Memorandum.Domain.Common;
using Memorandum.Domain.Organizations;

namespace Memorandum.Tests.Domain;

/// <summary>Реквизиты организации: ИНН/КПП/ОГРН — только цифры допустимой длины.</summary>
public sealed class OrganizationDetailsTests
{
    private static readonly DateTime Now = new(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData("7712345678", "771234567", "1027700123456")]   // юрлицо
    [InlineData("771234567890", null, "304500116000157")]      // ИП: КПП нет
    public void Valid_details_are_saved(string inn, string? kpp, string ogrn)
    {
        var organization = Organization.Create("ООО «Проект»", Now);

        organization.ChangeDetails(inn, kpp, ogrn, " Екатеринбург, Ленина 1 ", Now);

        Assert.Equal(inn, organization.Inn);
        Assert.Equal(kpp, organization.Kpp);
        Assert.Equal(ogrn, organization.Ogrn);
        Assert.Equal("Екатеринбург, Ленина 1", organization.Address);
    }

    [Theory]
    [InlineData("77-123", "771234567", "1027700123456", "ИНН")]
    [InlineData("7712345678", "12345", "1027700123456", "КПП")]
    [InlineData("7712345678", "771234567", "12345", "ОГРН")]
    public void Invalid_details_are_rejected(string inn, string kpp, string ogrn, string field)
    {
        var organization = Organization.Create("ООО «Проект»", Now);

        var exception = Assert.Throws<ValidationException>(() =>
            organization.ChangeDetails(inn, kpp, ogrn, null, Now));

        Assert.Equal(ErrorCodes.Validation, exception.Code);
        Assert.Contains(field, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Empty_details_become_null()
    {
        var organization = Organization.Create("ООО «Проект»", Now);

        organization.ChangeDetails("  ", null, "", "   ", Now);

        Assert.Null(organization.Inn);
        Assert.Null(organization.Kpp);
        Assert.Null(organization.Ogrn);
        Assert.Null(organization.Address);
    }
}
