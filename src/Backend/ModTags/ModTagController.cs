using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;

namespace Backend.ModTags;

[ApiController]
[Route("api/[controller]")]
public class ModTagController(ModTagService service) : ControllerBase
{
    [EndpointSummary("Get a list of mod tags")]
    [EndpointDescription("Retrieves a list of mod tags.")]
    [HttpGet]
    [ProducesResponseType(typeof(List<ModTag>), 200)]
    public async Task<IActionResult> GetModTags()
    {
        var tags = await service.GetAll();
        return Ok(tags);
    }

    [EndpointSummary("Get a mod tag by id")]
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ModTag), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetModTagById(
        [Description("Id of the mod tag to retrieve")]
        string id)
    {
        var tag = await service.GetModTag(id);
        if (tag == null) return NotFound();
        return Ok(tag);
    }

    [EndpointSummary("Create a new mod tag")]
    [HttpPost]
    [ProducesResponseType(typeof(ModTag), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CreateModTag(
        [Description("New mod tag Model")] [FromBody]
        ModTag tag)
    {
        await service.CreateModTag(tag);
        return CreatedAtAction(nameof(GetModTagById), new { name = tag.Id }, tag);
    }

    [EndpointSummary("Update an existing mod tag")]
    [ProducesResponseType(typeof(ModTag), 200)]
    [ProducesResponseType(400)]
    [HttpPut]
    public async Task<IActionResult> UpdateModTag(
        [Description("Updated mod tag Model")] [FromBody]
        ModTag tag)
    {
        var existingTag = await service.GetModTag(tag.Id);
        if (existingTag == null) return NotFound();

        await service.UpdateModTag(tag);
        return Ok(tag);
    }

    [EndpointSummary("Delete a mod tag")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteModTag(
        [Description("Id of the mod tag to delete")]
        string id)
    {
        var existingTag = await service.GetModTag(id);
        if (existingTag == null) return NotFound();

        await service.DeleteModTag(id);
        return NoContent();
    }
}