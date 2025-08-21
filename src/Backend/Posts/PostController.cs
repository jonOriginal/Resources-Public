using System.ComponentModel;
using Common;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Posts;

[ApiController]
[Route("api/[controller]")]
public class PostController(PostService postService) : ControllerBase
{
    [EndpointSummary("Get a list of posts")]
    [HttpGet("list")]
    [ProducesResponseType(typeof(List<Post>), 200)]
    public async Task<IActionResult> GetPosts()
    {
        var forums = await postService.GetAll();
        return Ok(forums);
    }

    [EndpointSummary("Get a list of post tags")]
    [HttpGet("tags")]
    [ProducesResponseType(typeof(List<PostTag>), 200)]
    public async Task<IActionResult> GetPostTags()
    {
        var tags = await postService.GetAllTags();
        return Ok(tags);
    }

    [EndpointSummary("Get a post by mod ID")]
    [HttpGet("mod/{modId}")]
    [ProducesResponseType(typeof(Post), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetPostByModId(
        [Description("The ID of the mod")] string modId)
    {
        var post = await postService.GetPostByModId(modId.Sanitize());
        if (post == null) return NotFound();
        return Ok(post);
    }
    
    [EndpointSummary("Get a post by ID")]
    [HttpGet("{postId}")]
    [ProducesResponseType(typeof(Post), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetPostById(
        [Description("The ID of the post")] string postId)
    {
        var post = await postService.GetPost(postId.Sanitize());
        if (post == null) return NotFound();
        return Ok(post);
    }
    
    
    [EndpointSummary("Force Sync a mod to post service")]
    [HttpGet("sync/{modId}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> SyncModToPost(
        [Description("The ID of the mod to sync")] string modId)
    {
        var post = await postService.GetPostByModId(modId.Sanitize());
        if (post == null) return NotFound();

        await postService.SyncPost(modId.Sanitize());
        return NoContent();
    }

    [EndpointSummary("Create a new post")]
    [HttpPost]
    [ProducesResponseType(201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CreateModPost(
        [Description("The post to create")] [FromBody]
        Post post)
    {
        var existingPost = await postService.GetPostByModId(post.ModId);
        if (existingPost != null) return BadRequest($"A post with mod ID {post.ModId} already exists.");
        post.UpdatedAt = DateTime.Now;
        post.CreatedAt = DateTime.Now;
        await postService.CreatePost(post);
        return CreatedAtAction(nameof(GetPostByModId), new { post.ModId }, post);
    }

    [EndpointSummary("Delete a post by ID")]
    [HttpDelete("{postId}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeleteModPost(
        [Description("The ID of the post to delete")]
        string postId)
    {
        var existingPost = await postService.GetPost(postId);
        if (existingPost == null) return NotFound();

        await postService.DeletePost(postId);
        return NoContent();
    }

    [EndpointSummary("Update a post")]
    [HttpPut]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> UpdatePost(
        [Description("Updated post Model")] [FromBody]
        Post post)
    {
        var existingPost = await postService.GetPostByModId(post.ModId);
        if (existingPost == null) return NotFound();

        post.UpdatedAt = DateTimeOffset.Now;
        await postService.UpdatePost(post);
        return NoContent();
    }

    [EndpointSummary("Updates or creates post tag")]
    [HttpPut("tag")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> UpdatePostTag(
        [Description("Updated post tag Model")] [FromBody]
        PostTag tag)
    {
        await postService.UpdateTag(tag);
        return NoContent();
    }

    [EndpointSummary("Delete a post tag by ID")]
    [HttpDelete("tag/{tagId}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeletePostTag(
        [Description("The ID of the post tag to delete")]
        string tagId)
    {
        var existingTag = await postService.GetPostTag(tagId);
        if (existingTag == null) return NotFound();

        await postService.DeleteTag(tagId);
        return NoContent();
    }
}