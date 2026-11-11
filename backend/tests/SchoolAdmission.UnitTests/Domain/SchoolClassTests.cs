using FluentAssertions;
using SchoolAdmission.Domain.Entities;
using SchoolAdmission.Domain.Enums;
using SchoolAdmission.Domain.Exceptions;
using Xunit;

namespace SchoolAdmission.UnitTests.Domain;

public sealed class SchoolClassTests
{
    private static readonly TimeSpan DefaultHoldDuration = TimeSpan.FromMinutes(10);

    // ── Creation ──────────────────────────────────────────────────────────────

    [Fact]
    public void Create_ValidInputs_ReturnsSchoolClassWithCorrectData()
    {
        var schoolClass = SchoolClass.Create("Class 6 - Morning Shift", 120);

        schoolClass.Name.Should().Be("Class 6 - Morning Shift");
        schoolClass.TotalSeats.Should().Be(120);
        schoolClass.AvailableSeats.Should().Be(120);
        schoolClass.HeldSeats.Should().Be(0);
        schoolClass.ConfirmedSeats.Should().Be(0);
        schoolClass.Id.Should().StartWith("c_");
        schoolClass.DomainEvents.Should().ContainSingle();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void Create_EmptyName_ThrowsArgumentException(string? name)
    {
        var act = () => SchoolClass.Create(name!, 120);
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_ZeroOrNegativeSeats_ThrowsArgumentOutOfRangeException(int seats)
    {
        var act = () => SchoolClass.Create("Class 6", seats);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    // ── PlaceHold ─────────────────────────────────────────────────────────────

    [Fact]
    public void PlaceHold_SeatAvailable_ReturnsHoldWithCorrectData()
    {
        var schoolClass = SchoolClass.Create("Class 6", 2);

        var hold = schoolClass.PlaceHold("12345678901234567", DefaultHoldDuration);

        hold.Should().NotBeNull();
        hold.Status.Should().Be(HoldStatus.Held);
        hold.StudentId.Should().Be("12345678901234567");
        hold.ClassId.Should().Be(schoolClass.Id);
        hold.ExpiresAt.Should().BeCloseTo(DateTimeOffset.UtcNow.Add(DefaultHoldDuration), TimeSpan.FromSeconds(5));
        schoolClass.HeldSeats.Should().Be(1);
        schoolClass.AvailableSeats.Should().Be(1);
    }

    [Fact]
    public void PlaceHold_NoSeatsAvailable_ThrowsNoSeatsAvailableException()
    {
        var schoolClass = SchoolClass.Create("Class 6", 1);
        schoolClass.PlaceHold("12345678901234567", DefaultHoldDuration);

        var act = () => schoolClass.PlaceHold("98765432109876543", DefaultHoldDuration);

        act.Should().Throw<NoSeatsAvailableException>();
    }

    [Fact]
    public void PlaceHold_StudentAlreadyHasActiveHold_ThrowsDuplicateActiveHoldException()
    {
        var schoolClass = SchoolClass.Create("Class 6", 5);
        schoolClass.PlaceHold("12345678901234567", DefaultHoldDuration);

        var act = () => schoolClass.PlaceHold("12345678901234567", DefaultHoldDuration);

        act.Should().Throw<DuplicateActiveHoldException>();
    }

    [Fact]
    public void PlaceHold_ExpiredHoldExists_AllowsNewHold()
    {
        var schoolClass = SchoolClass.Create("Class 6", 2);
        // Place a hold that immediately expires
        var shortDuration = TimeSpan.FromMilliseconds(-1);
        schoolClass.PlaceHold("12345678901234567", shortDuration);

        // Should succeed because previous hold is expired
        var act = () => schoolClass.PlaceHold("12345678901234567", DefaultHoldDuration);

        act.Should().NotThrow();
    }

    [Fact]
    public void PlaceHold_StudentHasConfirmedSeat_ThrowsDuplicateActiveHoldException()
    {
        var schoolClass = SchoolClass.Create("Class 6", 5);
        var hold = schoolClass.PlaceHold("12345678901234567", DefaultHoldDuration);
        hold.Confirm("12345678901234567");

        var act = () => schoolClass.PlaceHold("12345678901234567", DefaultHoldDuration);

        act.Should().Throw<DuplicateActiveHoldException>();
    }

    // ── Seat count accuracy ───────────────────────────────────────────────────

    [Fact]
    public void SeatCounts_AreAccurateAfterMultipleOperations()
    {
        var schoolClass = SchoolClass.Create("Class 6", 5);

        var hold1 = schoolClass.PlaceHold("11111111111111111", DefaultHoldDuration);
        var hold2 = schoolClass.PlaceHold("22222222222222222", DefaultHoldDuration);
        schoolClass.PlaceHold("33333333333333333", DefaultHoldDuration);

        hold1.Confirm("11111111111111111");
        hold2.Cancel("22222222222222222");

        schoolClass.ConfirmedSeats.Should().Be(1);
        schoolClass.HeldSeats.Should().Be(1);     // hold3 still held
        schoolClass.AvailableSeats.Should().Be(3); // 5 - 1 confirmed - 1 held
    }
}
