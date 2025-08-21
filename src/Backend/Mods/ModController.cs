using System.ComponentModel;
using Common;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Mods;

[ApiController]
[Route("api/[controller]")]
public class ModController(ModService service) : ControllerBase
{
    [EndpointSummary("Get a list of mods")]
    [EndpointDescription("Retrieves all mods in the database")]
    [HttpGet("list")]
    [ProducesResponseType(typeof(List<Mod>), 200)]
    public async Task<IActionResult> ListMods()
    {
        var mods = await service.GetAll();
        return Ok(mods);
    }

    [EndpointSummary("Get a list of mods by search query and tags")]
    [EndpointDescription("Retrieves a list of mods by the search query and tags.")]
    [HttpGet("search")]
    [ProducesResponseType(typeof(List<Mod>), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> SearchMods(
        [Description("Optional Search Term")] [FromQuery]
        string? search,
        [Description("Optional filter by tag")] [FromBody]
        string[]? tags)
    {
        var sanitizedTags = tags.Sanitize();
        var sanitizedSearch = search.Sanitize();

        var mods = await service.Search(sanitizedSearch, sanitizedTags);

        return Ok(mods);
    }

    [EndpointSummary("Get a list of mods by any tags")]
    [EndpointDescription("Retrieves a list of mods that match any of the provided tags.")]
    [HttpGet("search/any")]
    [ProducesResponseType(typeof(List<Mod>), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> SearchModsByAnyTags(
        [Description("Optional filter by tags")] [FromBody]
        string[]? tags)
    {
        var sanitizedTags = tags.Sanitize();
        var mods = await service.SearchByAnyTags(sanitizedTags);
        return Ok(mods);
    }

    [EndpointSummary("Get a mod by ID")]
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Mod), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetModById(
        [Description("The ID of the mod to retrieve")]
        string id)
    {
        var mod = await service.Get(id.Sanitize());
        if (mod == null) return NotFound();
        return Ok(mod);
    }

    [EndpointSummary("Create a new mod")]
    [HttpPost]
    [ProducesResponseType(typeof(Mod), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CreateMod(
        [Description("New mod Model")] [FromBody]
        Mod mod)
    {
        await service.Create(mod);
        return CreatedAtAction(nameof(GetModById), new { id = mod.Id }, mod);
    }

    [EndpointSummary("Update an existing mod")]
    [ProducesResponseType(typeof(Mod), 200)]
    [ProducesResponseType(400)]
    [HttpPut]
    public async Task<IActionResult> UpdateMod(
        [Description("Updated mod Model")] [FromBody]
        Mod mod)
    {
        var existingMod = await service.Get(mod.Id);
        if (existingMod == null) return NotFound();

        await service.Update(mod);
        return Ok(mod);
    }
    
    [EndpointSummary("Set Mod Compromised Status")]
    [ProducesResponseType(typeof(Mod), 200)]
    [ProducesResponseType(404)]
    [HttpPost("{id}/compromised")]
    public async Task<IActionResult> SetModCompromised(
        [Description("Mod Id")] string id,
        [FromQuery] bool compromised = true)
    {
        var existingMod = await service.Get(id);
        if (existingMod == null) return NotFound();
        
        existingMod.IsCompromised = compromised;
        
        await service.Update(existingMod);
        return Ok(existingMod);
    }

    [EndpointSummary("Delete a mod")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteMod(
        [Description("Mod Id")] string id)
    {
        var existingMod = await service.Get(id);
        if (existingMod == null) return NotFound();

        await service.Delete(id);
        return NoContent();
    }
}