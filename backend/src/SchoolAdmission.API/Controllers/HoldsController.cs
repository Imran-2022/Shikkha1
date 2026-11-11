using MediatR;
using Microsoft.AspNetCore.Mvc;
using SchoolAdmission.Application.Holds.Commands;
using SchoolAdmission.Application.Holds.DTOs;
using SchoolAdmission.Application.Holds.Queries;

namespace SchoolAdmission.API.Controllers;

[ApiController]
[Produces("application/json")]
public sealed class HoldsController : ControllerBase
{
    private readonly IMediator _mediator;

    public HoldsController(IMediator mediator) => _mediator = mediator;

    /// <summary>GET /holds/{holdId} — Get current hold status</summary>
    [HttpGet("holds/{holdId}")]
    [ProducesResponseType(typeof(HoldStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetHold(
        [FromRoute] string holdId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetHoldByIdQuery(holdId), cancellationToken);
        return Ok(result);
    }

    /// <summary>POST /holds/{holdId}/confirm — Confirm a hold (simulate payment)</summary>
    [HttpPost("holds/{holdId}/confirm")]
    [ProducesResponseType(typeof(ConfirmHoldResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ConfirmHold(
        [FromRoute] string holdId,
        [FromHeader(Name = "X-Student-Id")] string? studentId,
        CancellationToken cancellationToken)
    {
        var command = new ConfirmHoldCommand(holdId, studentId ?? string.Empty);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>DELETE /holds/{holdId} — Cancel a hold</summary>
    [HttpDelete("holds/{holdId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CancelHold(
        [FromRoute] string holdId,
        [FromHeader(Name = "X-Student-Id")] string? studentId,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new CancelHoldCommand(holdId, studentId ?? string.Empty), cancellationToken);
        return NoContent();
    }

    /// <summary>GET /me/holds — Get all holds for the current student</summary>
    [HttpGet("me/holds")]
    [ProducesResponseType(typeof(IReadOnlyList<StudentHoldResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyHolds(
        [FromHeader(Name = "X-Student-Id")] string? studentId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetStudentHoldsQuery(studentId ?? string.Empty), cancellationToken);
        return Ok(result);
    }
}
