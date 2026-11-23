using Memorandum.Application.Invitations;
using Memorandum.Domain.Organizations;

namespace Memorandum.Tests.Domain;

public sealed class InviteCodeTests
{
    [Fact]
    public void Generated_code_is_readable_and_has_no_ambiguous_characters()
    {
        for (var attempt = 0; attempt < 200; attempt++)
        {
            var code = InviteCode.Generate();

            Assert.Equal(InviteCode.Length, code.Length);
            Assert.DoesNotContain(code, character => "0O1IL".Contains(character, StringComparison.Ordinal));
            Assert.Matches("^[A-Z2-9]+$", code);
        }
    }

    [Fact]
    public void Codes_differ_between_issues()
    {
        var codes = Enumerable.Range(0, 50).Select(_ => InviteCode.Generate()).Distinct().Count();

        Assert.True(codes > 45, $"Слишком много повторов: {codes} из 50");
    }

    [Theory]
    [InlineData(" abcd2345 ", "ABCD2345")]
    [InlineData("abcd 2345", "ABCD2345")]
    [InlineData("AbCd2345", "ABCD2345")]
    public void Input_is_normalized(string raw, string expected)
    {
        Assert.Equal(expected, InviteCode.Normalize(raw));
    }

    [Fact]
    public void Hash_depends_on_pepper_and_code()
    {
        var hash = InviteCode.Hash("pepper", "ABCD2345");

        Assert.Equal(hash, InviteCode.Hash("pepper", " abcd 2345"));
        Assert.NotEqual(hash, InviteCode.Hash("other-pepper", "ABCD2345"));
        Assert.NotEqual(hash, InviteCode.Hash("pepper", "ABCD2346"));
    }

    [Theory]
    [InlineData(OrganizationRole.Editor, true)]
    [InlineData(OrganizationRole.Viewer, true)]
    [InlineData(OrganizationRole.OrgAdmin, false)]
    public void Admin_role_is_not_granted_by_code(OrganizationRole role, bool allowed)
    {
        Assert.Equal(allowed, Organization.CanInviteWithRole(role));
    }
}
