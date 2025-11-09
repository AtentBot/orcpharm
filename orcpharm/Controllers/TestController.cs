using Microsoft.AspNetCore.Mvc;
using Isopoh.Cryptography.Argon2;

namespace Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    [HttpGet("hash/{password}")]
    public IActionResult GenerateHash(string password)
    {
        var hash = Argon2.Hash(password);
        return Ok(new
        {
            password = password,
            hash = hash,
            info = "Use este hash no campo PasswordHash"
        });
    }
}
