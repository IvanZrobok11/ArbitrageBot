
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace ArbitrageBot.Controllers;

[ApiController]
[Route("error")]
public class ErrorController : ControllerBase
{
    [HttpGet, HttpPost, HttpPut, HttpDelete]
    public IActionResult HandleError()
    {
        var context = HttpContext.Features.Get<IExceptionHandlerFeature>();
        if (context == null) return Problem();

        var exception = context.Error;

        return Problem(
            detail: exception.Message, // У продакшені краще приховати!
            statusCode: (int)HttpStatusCode.InternalServerError,
            title: "An unexpected error occurred"
        );
    }
}
