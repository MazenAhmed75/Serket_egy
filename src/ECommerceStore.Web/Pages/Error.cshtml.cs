using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;

namespace ECommerceStore.Web.Pages;

/// <summary>
/// The page shown for unhandled errors (500) and for empty 404/429 responses. It never shows technical details;
/// the reference number lets the owner find the matching line in the server log.
/// </summary>
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[IgnoreAntiforgeryToken]
[DisableRateLimiting]
public class ErrorModel : PageModel
{
    public int StatusCode { get; private set; } = StatusCodes.Status500InternalServerError;

    public string Title { get; private set; } = "Something went wrong";

    public string Message { get; private set; } = "We hit a problem on our side. Please try again in a moment.";

    public string RequestId => HttpContext.TraceIdentifier;

    public void OnGet(int? code)
    {
        StatusCode = code ?? StatusCodes.Status500InternalServerError;

        (Title, Message) = StatusCode switch
        {
            StatusCodes.Status404NotFound => ("Page not found", "The page you are looking for doesn't exist or has moved."),
            StatusCodes.Status429TooManyRequests => ("Please slow down", "You have made a lot of requests in a short time. Wait a minute and try again."),
            >= 400 and < 500 => ("We couldn't open that", "Something about that request wasn't right. Please go back and try again."),
            _ => ("Something went wrong", "We hit a problem on our side. Please try again in a moment.")
        };

        // Keep the real status code (404, 500...) on the response, not 200.
        Response.StatusCode = StatusCode;
    }
}
