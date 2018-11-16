using System.Threading.Tasks;
using DatingApp.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using DatingApp.API.DTOs;
using Microsoft.AspNetCore.Identity;
using DatingApp.API.Models;
using CloudinaryDotNet.Actions;
using CloudinaryDotNet;
using Microsoft.Extensions.Options;
using DatingApp.API.Helpers;
using AutoMapper;
using System.Collections.Generic;

namespace DatingApp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly UserManager<User> _userManager;

        private readonly IDatingRepository _repo;
        private readonly Cloudinary _cloudinary;
        private readonly IOptions<CloudinarySettings> _cloudinaryConfig;
        private readonly IMapper _mapper;

        public AdminController(DataContext context, UserManager<User> userManager,
        IDatingRepository repo, IOptions<CloudinarySettings> cloudinaryConfig,
        IMapper mapper)
        {
            _mapper = mapper;
            _userManager = userManager;
            _context = context;
            _repo = repo;
            _cloudinaryConfig = cloudinaryConfig;
            Account acc = new Account(
                _cloudinaryConfig.Value.CloudName,
                _cloudinaryConfig.Value.ApiKey,
                _cloudinaryConfig.Value.ApiSecret
            );
            _cloudinary = new Cloudinary(acc);
        }

        [Authorize(Policy = "RequireAdminRole")]
        [HttpGet("usersWithRoles")]
        public async Task<IActionResult> GetUsersWithRoles()
        {
            var userList = await (from user in _context.Users
                                  orderby user.UserName
                                  select new
                                  {
                                      Id = user.Id,
                                      UserName = user.UserName,
                                      Roles = (from userRole in user.UserRoles
                                               join role in _context.Roles
                                               on userRole.RoleId equals role.Id
                                               select role.Name).ToList()
                                  }).ToListAsync();

            return Ok(userList);
        }


        [Authorize(Policy = "RequireAdminRole")]
        [HttpPost("editRoles/{userName}")]
        public async Task<IActionResult> EditRoles(string userName, RoleEditDto roleEditDto)
        {
            var user = await _userManager.FindByNameAsync(userName);

            var userRoles = await _userManager.GetRolesAsync(user);

            var selectedRoles = roleEditDto.RoleNames;

            selectedRoles = selectedRoles ?? new string[] { };

            var result = await _userManager.AddToRolesAsync(user, selectedRoles.Except(userRoles));

            if (!result.Succeeded)
                return BadRequest("Failed to add to roles");

            result = await _userManager.RemoveFromRolesAsync(user, userRoles.Except(selectedRoles));

            if (!result.Succeeded)
                return BadRequest("Failed to remove roles");

            return Ok(await _userManager.GetRolesAsync(user));
        }

        [Authorize(Policy = "ModeratePhotoRole")]
        [HttpGet("photosForModeration")]
        public async Task<IActionResult> GetPhotosForModeration([FromQuery]PaginationParams paginationParams)
        {
            var photosForModeration = await _repo.GetPhotosForModeration(paginationParams);

            var photosToReturn = _mapper.Map<IEnumerable<PhotoForModerationDto>>(photosForModeration);

            Response.AddPagination(photosForModeration.CurrentPage, photosForModeration.PageSize,
                photosForModeration.TotalCount, photosForModeration.TotalPages);

            return Ok(photosToReturn);
        }

        [Authorize(Policy = "ModeratePhotoRole")]
        [HttpPost("approvePhoto/{id}")]
        public async Task<IActionResult> ApprovePhoto(int id)
        {
            var photo = await _repo.GetPhoto(id);
            if (photo == null)
                return BadRequest("Photo not exist");

            if (photo.IsApproved)
                return BadRequest("Photo already approved");

            photo.IsApproved = true;

            if (await _repo.SaveAll())
                return NoContent();

            return BadRequest("Error on saving changes");
        }

        [Authorize(Policy = "ModeratePhotoRole")]
        [HttpPost("rejectPhoto/{id}")]
        public async Task<IActionResult> RejectPhoto(int id)
        {
            var photo = await _repo.GetPhoto(id);
            if (photo == null)
                return BadRequest("Photo not exist");

            if (photo.PublicId != null)
            {
                var deleteParams = new DeletionParams(photo.PublicId);
                var result = _cloudinary.Destroy(deleteParams);
                if (result.Result == "ok")
                {
                    _repo.Delete(photo);
                }
            }
            else
            {
                _repo.Delete(photo);
            }

            if (await _repo.SaveAll())
                return NoContent();

            return BadRequest("Failed to delete the photo.");
        }

    }
}