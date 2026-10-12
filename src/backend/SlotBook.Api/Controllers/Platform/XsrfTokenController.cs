#pragma warning disable CA1515 // public: discovered by MVC
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SlotBook.Api.Security;

namespace SlotBook.Api.Controllers.Platform;

[ApiController]
[Route("api/xsrf-token")]
public sealed class XsrfTokenController : ControllerBase
{
    /// <summary>Issues the XSRF token pair as cookies. Angular echoes the readable one in <c>X-XSRF-TOKEN</c>.</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Issue()
    {
        AntiforgerySetup.IssueToken(HttpContext);
        return NoContent();
    }
}
