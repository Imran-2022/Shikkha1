using MediatR;
using Microsoft.AspNetCore.Mvc;
using SchoolAdmission.Application.Classes.Commands;
using SchoolAdmission.Application.Classes.DTOs;
using SchoolAdmission.Application.Classes.Queries;
using SchoolAdmission.Application.Holds.Commands;
using SchoolAdmission.Application.Holds.DTOs;

namespace SchoolAdmission.API.Controllers;

[ApiController]
[Route("classes")]
[Produces("application/json")]
public sealed class ClassesController : ControllerBase
{
    private readonly IMediator _mediator;

    public ClassesController(IMediator mediator) => _mediator = mediator;

    /// <summary>POST /classes — Create a new class</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ClassCreatedResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateClass(
        [FromBody] CreateClassRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateClassCommand(request.Name, request.TotalSeats);
        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetClass), new { classId = result.Id }, result);
    }

    /// <summary>GET /classes — List all classes with seat counts</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ClassResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetClasses(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetAllClassesQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>GET /classes/{classId} — Get seat status for one class</summary>
    [HttpGet("{classId}")]
    [ProducesResponseType(typeof(ClassResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetClass(
        [FromRoute] string classId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetClassByIdQuery(classId), cancellationToken);
        return Ok(result);
    }

    /// <summary>POST /classes/{classId}/holds — Place a seat hold for a student</summary>
    [HttpPost("{classId}/holds")]
    [ProducesResponseType(typeof(PlaceHoldResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> PlaceHold(
        [FromRoute] string classId,
        [FromHeader(Name = "X-Student-Id")] string? studentId,
        CancellationToken cancellationToken)
    {
        var command = new PlaceHoldCommand(classId, studentId ?? string.Empty);
        var result = await _mediator.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }
}
