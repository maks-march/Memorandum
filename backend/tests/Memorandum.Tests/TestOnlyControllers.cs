using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace Memorandum.Tests;


/// <summary>Всегда падает — проверяем, что исключение превращается в 500 + ProblemDetails.</summary>
[ApiController]
[ApiVersionNeutral]
[Route("test-api/boom")]
public sealed class TestOnlyBoomController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => throw new InvalidOperationException("Тестовое исключение");
}

/// <summary>Две версии одного маршрута — проверяем выбор версии по заголовку.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("test-api/versioned")]
public sealed class TestOnlyVersionedV1Controller : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { version = "1.0" });
}

[ApiController]
[ApiVersion("2.0")]
[Route("test-api/versioned")]
public sealed class TestOnlyVersionedV2Controller : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { version = "2.0" });
}
