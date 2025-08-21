using Microsoft.AspNetCore.Mvc;

namespace Backend.NamedPosts;

[ApiController]
[Route("api/[controller]")]
public class NamedPostController(NamedPostService service) : ControllerBase
{
    [EndpointSummary("Get a list of named posts")]
    [EndpointDescription("Retrieves all named posts in the database")]
    [HttpGet("list")]
    [ProducesResponseType(typeof(List<NamedPost>), 200)]
    public async Task<IActionResult> ListModPosts()
    {
        var modPosts = await service.GetAll();
        return Ok(modPosts);
    }

    [EndpointSummary("Get a list of named post tags")]
    [EndpointDescription("Retrieves all named post tags in the database")]
    [HttpGet("tags")]
    [ProducesResponseType(typeof(List<NamedPostTag>), 200)]
    public async Task<IActionResult> ListModPostTags()
    {
        var modPostTags = await service.GetAllTags();
        return Ok(modPostTags);
    }
}