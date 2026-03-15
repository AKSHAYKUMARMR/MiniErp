using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniErp.Application;

namespace MiniErp.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly IUserAdminService _userAdminService;

    public UsersController(IUserAdminService userAdminService)
    {
        _userAdminService = userAdminService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> GetAll(CancellationToken cancellationToken)
    {
        var users = await _userAdminService.GetUsersAsync(cancellationToken);
        return Ok(users);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var user = await _userAdminService.GetUserByIdAsync(id, cancellationToken);
        if (user == null)
        {
            return NotFound();
        }

        return Ok(user);
    }

    [HttpPost]
    public async Task<ActionResult<int>> Create([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        var id = await _userAdminService.CreateUserAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
    {
        await _userAdminService.UpdateUserAsync(id, request, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _userAdminService.DeleteUserAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:int}/roles")]
    public async Task<ActionResult<IReadOnlyList<UserRoleDto>>> GetUserRoles(int id, CancellationToken cancellationToken)
    {
        var roles = await _userAdminService.GetUserRolesAsync(id, cancellationToken);
        return Ok(roles);
    }

    [HttpPost("{id:int}/roles/{roleId:int}")]
    public async Task<IActionResult> AssignRole(int id, int roleId, CancellationToken cancellationToken)
    {
        await _userAdminService.AssignRoleAsync(id, roleId, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:int}/roles/{roleId:int}")]
    public async Task<IActionResult> RemoveRole(int id, int roleId, CancellationToken cancellationToken)
    {
        await _userAdminService.RemoveRoleAsync(id, roleId, cancellationToken);
        return NoContent();
    }
}

