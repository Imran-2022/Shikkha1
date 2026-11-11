using FluentAssertions;
using SchoolAdmission.Application.Holds.Queries;
using Xunit;

namespace SchoolAdmission.UnitTests.Application;

public sealed class StudentHoldsValidatorTests
{
    private readonly GetStudentHoldsQueryValidator _validator = new();

    [Theory]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("1234567890123456a")]
    [InlineData("123456789012345678")] // 18 digits
    public void Invalid_StudentId_Fails(string studentId)
        => _validator.Validate(new GetStudentHoldsQuery(studentId)).IsValid.Should().BeFalse();

    [Fact]
    public void Valid_17DigitStudentId_Passes()
        => _validator.Validate(new GetStudentHoldsQuery("12345678901234567")).IsValid.Should().BeTrue();
}
