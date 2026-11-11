using FluentAssertions;
using SchoolAdmission.Domain.Entities;
using SchoolAdmission.Domain.Enums;
using SchoolAdmission.Domain.Exceptions;
using Xunit;

namespace SchoolAdmission.UnitTests.Domain;

public sealed class AdmissionHoldTests
{
    private static readonly TimeSpan TenMinutes = TimeSpan.FromMinutes(10);
    private const string StudentId = "12345678901234567";
    private const string OtherStudentId = "98765432109876543";

    private SchoolClass CreateClassWithHold(out AdmissionHold hold, TimeSpan? duration = null)
    {
        var schoolClass = SchoolClass.Create("Class 6 - Test", 10);
        hold = schoolClass.PlaceHold(StudentId, duration ?? TenMinutes);
        return schoolClass;
    }

    // ── Confirm ───────────────────────────────────────────────────────────────

    [Fact]
    public void Confirm_ValidActiveHold_ChangesStatusToConfirmed()
    {
        CreateClassWithHold(out var hold);

        hold.Confirm(StudentId);

        hold.Status.Should().Be(HoldStatus.Confirmed);
        hold.ConfirmedAt.Should().NotBeNull();
    }

    [Fact]
    public void Confirm_AlreadyConfirmed_IsIdempotent()
    {
        CreateClassWithHold(out var hold);
        hold.Confirm(StudentId);

        var act = () => hold.Confirm(StudentId);

        act.Should().NotThrow();
        hold.Status.Should().Be(HoldStatus.Confirmed);
    }

    [Fact]
    public void Confirm_ExpiredHold_ThrowsHoldExpiredException()
    {
        CreateClassWithHold(out var hold, TimeSpan.FromMilliseconds(-1));

        var act = () => hold.Confirm(StudentId);

        act.Should().Throw<HoldExpiredException>();
    }

    [Fact]
    public void Confirm_WrongStudent_ThrowsUnauthorizedHoldActionException()
    {
        CreateClassWithHold(out var hold);

        var act = () => hold.Confirm(OtherStudentId);

        act.Should().Throw<UnauthorizedHoldActionException>();
    }

    // ── Cancel ────────────────────────────────────────────────────────────────

    [Fact]
    public void Cancel_ActiveHeld_ChangesStatusToCancelled()
    {
        CreateClassWithHold(out var hold);

        hold.Cancel(StudentId);

        hold.Status.Should().Be(HoldStatus.Cancelled);
        hold.CancelledAt.Should().NotBeNull();
    }

    [Fact]
    public void Cancel_WrongStudent_ThrowsUnauthorizedHoldActionException()
    {
        CreateClassWithHold(out var hold);

        var act = () => hold.Cancel(OtherStudentId);

        act.Should().Throw<UnauthorizedHoldActionException>();
    }

    [Fact]
    public void Cancel_ConfirmedHold_ThrowsHoldNotCancellableException()
    {
        CreateClassWithHold(out var hold);
        hold.Confirm(StudentId);

        var act = () => hold.Cancel(StudentId);

        act.Should().Throw<HoldNotCancellableException>();
    }

    // ── IsExpired ─────────────────────────────────────────────────────────────

    [Fact]
    public void IsExpired_HeldAndPastExpiry_ReturnsTrue()
    {
        CreateClassWithHold(out var hold, TimeSpan.FromMilliseconds(-1));
        hold.Status.Should().Be(HoldStatus.Held);
        hold.IsExpired.Should().BeTrue();
    }

    [Fact]
    public void IsExpired_ConfirmedHold_ReturnsFalse()
    {
        CreateClassWithHold(out var hold);
        hold.Confirm(StudentId);
        hold.IsExpired.Should().BeFalse();
    }
}
