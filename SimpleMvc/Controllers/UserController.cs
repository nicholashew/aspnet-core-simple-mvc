using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SimpleMvc.Models;
using SimpleMvc.Models.AccountViewModels;
using SimpleMvc.Services;
using SimpleMvc.Common;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using SimpleMvc.Data;
using Microsoft.AspNetCore.Http;
using SimpleMvc.ViewModels.User;

namespace SimpleMvc.Controllers
{
    [AuthorizeRoles(Constants.Roles.SuperAdministrator, Constants.Roles.Administrator)]
    //[Route("[controller]/[action]")]
    public class UserController : BaseController
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IEmailService _emailSender;
        private readonly ILogger _logger;

        public UserController(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            IEmailService emailSender,
            ILogger<UserController> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _emailSender = emailSender;
            _logger = logger;
        }

        // GET: User/Index
        public async Task<IActionResult> Index()
        {
            return View(await _userManager.Users.ToListAsync());
        }

        // GET: User/Create
        public async Task<IActionResult> Create()
        {
            await PopulateUserRoleDataAsync(null);
            return View();
        }

        // POST: User/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);

                if (user == null)
                {
                    user = new ApplicationUser
                    {
                        UserName = model.Email,
                        Email = model.Email,
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        EmailConfirmed = !model.SendEmailVerification,
                        CreatedDate = DateTime.UtcNow,
                        CreatedBy = Convert.ToInt32(_userManager.GetUserId(User))
                    };

                    var result = await _userManager.CreateAsync(user, model.Password);
                    if (result.Succeeded)
                    {
                        _logger.LogInformation($"Admin created a new account '{model.Email}' with password.");

                        if (model.SelectedRoles.Any())
                        {
                            foreach (var roleId in model.SelectedRoles)
                            {
                                var role = await _roleManager.FindByIdAsync(roleId.ToString());
                                await _userManager.AddToRoleAsync(user, role.Name);
                            }
                        }

                        if (model.SendEmailVerification)
                        {
                            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                            var callbackUrl = Url.EmailConfirmationLink(user.Id, code, Request.Scheme);
                            await _emailSender.SendEmailConfirmationAsync(model.Email, callbackUrl);
                        }

                        AddSuccessMessage("User create successfully." + user.Id);
                        return RedirectToAction(nameof(Edit), new { id = user.Id });
                    }
                    AddIdentityErrors(result);
                }
                else
                {
                    ModelState.AddModelError("Email", "There's already a user with the provided email.");
                }
            }

            await PopulateUserRoleDataAsync(model.SelectedRoles?.ToList());
            return View(model);
        }

        // GET: User/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                AddErrorMessage("No record found.");
                return RedirectToAction(nameof(Index));
            }

            var model = Mapper.Map<UserEditViewModel>(user);
            await PopulateAssignedRolesDataAsync(user);

            return View(model);
        }

        // POST: User/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserEditViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.Id.ToString());
            if (user == null)
            {
                AddErrorMessage("No record found.");
                return RedirectToAction(nameof(Index));
            }

            if (ModelState.IsValid)
            {
                if (!await CanManageUser(user))
                {
                    AddErrorMessage("Permission denied, you do not have permission to manage the user.");
                    return RedirectToAction(nameof(Edit), new { model.Id });
                }

                var email = user.Email;
                if (model.Email != email)
                {
                    var setEmailResult = await _userManager.SetEmailAsync(user, model.Email);
                    if (!setEmailResult.Succeeded)
                    {
                        AddErrorMessage("Failed to update user email.");
                        _logger.LogError($"Unexpected error occurred setting email for user with ID '{user.Id}'.");
                    }
                }

                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.BirthDate = model.BirthDate;
                user.Address1 = model.Address1;
                user.Address2 = model.Address2;
                user.State = model.State;
                user.City = model.City;
                user.PostalCode = model.PostalCode;
                user.Country = model.Country;
                user.ModifiedDate = DateTime.UtcNow;
                user.ModifiedBy = Convert.ToInt32(_userManager.GetUserId(User));

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    AddSuccessMessage("User profile has been updated", true);
                    return RedirectToAction(nameof(Edit), new { model.Id });
                }
                else
                {
                    AddIdentityErrors(result);
                }
            }

            await PopulateAssignedRolesDataAsync(user);

            return View(model);
        }

        // GET: User/AssignRole/5
        public async Task<IActionResult> AssignRole(int? id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                AddErrorMessage("No record found.");
                return RedirectToAction(nameof(Index));
            }

            var model = new AssignedRoleViewModel
            {
                Id = user.Id,
                UserName = user.UserName
            };
                        
            await PopulateUserRoleDataByUserAsync(user);
            return View(model);
        }

        // POST: User/AssignRole/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignRole(int? id, string[] selectedRoles)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                AddErrorMessage("No record found.");
                return RedirectToAction(nameof(Index));
            }

            if (!await CanManageUser(user))
            {
                AddErrorMessage("Permission denied, you do not have permission to manage the user.");
                return RedirectToAction(nameof(AssignRole), new { id });
            }

            bool isCurrentSuper = await IsSuperUser();
            int[] selectedRoleIds = selectedRoles.Select(int.Parse).ToArray();

            var allRoles = await _roleManager.Roles.ToListAsync();
            var userRoleNames = await _userManager.GetRolesAsync(user);
            var userRoles = allRoles.Where(x => userRoleNames.Contains(x.Name)).ToList();

            var rolesToInsert = allRoles.Where(x => selectedRoleIds.Contains(x.Id) && !userRoles.Any(y => y.Id == x.Id));
            var rolesToDelete = userRoles.Where(x => !selectedRoleIds.Contains(x.Id));

            if (rolesToInsert.Any())
            {
                foreach (var role in rolesToInsert.Distinct())
                {
                    if (role.Name == Constants.Roles.SuperAdministrator && !isCurrentSuper)
                        continue;

                    await _userManager.AddToRoleAsync(user, role.Name);
                }
            }

            if (rolesToDelete.Any())
            {
                foreach (var role in rolesToDelete.Distinct())
                {
                    if (role.Name == Constants.Roles.SuperAdministrator)
                    {
                        if (!isCurrentSuper)
                            continue;

                        var superUsers = await _userManager.GetUsersInRoleAsync(Constants.Roles.SuperAdministrator);
                        if (superUsers.Count == 1)
                        {
                            AddWarningMessage("Unable to demote Super Administrator account, please promote another user before demoting.");
                            continue;
                        }
                    }
                    await _userManager.RemoveFromRoleAsync(user, role.Name);
                }
            }

            AddSuccessMessage("User roles has been updated", true);

            return RedirectToAction(nameof(AssignRole), new { id });
        }

        // GET: User/Delete/5
        public async Task<ActionResult> Delete(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                AddErrorMessage("No record found.");
                return RedirectToAction(nameof(Index));
            }

            var roles = await _userManager.GetRolesAsync(user);
            var model = new UserDeleteViewModel
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                Roles = roles.Any() ? string.Join(',', roles) : "-"
            };

            return View(model);
        }

        // POST: User/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id.ToString());
                if (user == null)
                {
                    AddErrorMessage("No record found.");
                    return RedirectToAction(nameof(Index));
                }

                if (!await CanManageUser(user))
                {
                    AddErrorMessage("Permission denied, you do not have permission to manage the user.");
                }
                else
                {
                    user.IsDeleted = true;
                    user.LastDeletedDate = DateTime.UtcNow;

                    var result = await _userManager.UpdateAsync(user);
                    if (!result.Succeeded)
                    {
                        AddErrorMessage("Failed to delete user.");
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to delete user {id}");
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: User/HardDelete/5
        public async Task<ActionResult> HardDelete(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                AddErrorMessage("No record found.");
                return RedirectToAction(nameof(Index));
            }

            if (!await CanManageUser(user))
            {
                AddErrorMessage("Permission denied, you do not have permission to manage the user.");
            }
            else
            {
                var result = await _userManager.DeleteAsync(user);
                if (!result.Succeeded)
                {
                    AddErrorMessage("Failed to permanent delete user.");
                }
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: User/Restore/5
        public async Task<ActionResult> Restore(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                AddErrorMessage("No record found.");
                return RedirectToAction(nameof(Index));
            }

            if (!await CanManageUser(user))
            {
                AddErrorMessage("Permission denied, you do not have permission to manage the user.");
            }
            else
            {
                user.IsDeleted = false;
                user.LastRestoredDate = DateTime.UtcNow;

                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    AddErrorMessage("Failed to restore user.");
                }
            }

            return RedirectToAction(nameof(Index));
        }

        #region Helpers

        //private void PopulateDepartmentsDropDownList(object selectedDepartment = null)
        //{
        //    var departmentsQuery = from d in _context.Departments
        //                           orderby d.Name
        //                           select d;
        //    ViewBag.DepartmentID = new SelectList(departmentsQuery.AsNoTracking(), "DepartmentID", "Name", selectedDepartment);
        //}

        /// <summary>
        /// Gets a value indicating whether cureent user <see cref= "User" /> is Super Administrator
        /// </summary>
        /// <returns>true, if current user in role of Super Administrator</returns>
        private async Task<bool> IsSuperUser()
        {
            var user = await _userManager.GetUserAsync(User);
            return await _userManager.IsInRoleAsync(user, Constants.Roles.SuperAdministrator);
        }

        /// <summary>
        /// Gets a value indicating whether cureent user can manage the specified user
        /// </summary>
        /// <param name="user"></param>
        /// <returns>true, if has higher permission than the specified user</returns>
        private async Task<bool> CanManageUser(ApplicationUser user)
        {
            bool isCurrentSuper = await IsSuperUser();
            bool isSuper = await _userManager.IsInRoleAsync(user, Constants.Roles.SuperAdministrator);
            bool isAdmin = isSuper ? true : await _userManager.IsInRoleAsync(user, Constants.Roles.Administrator);

            if (!isCurrentSuper && (isSuper || isAdmin))
            {
                return false;
            }

            return true;
        }
        
        /// <summary>
        /// Gets the user roles.
        /// </summary>
        /// <param name="user"></param>
        /// <returns>List of Role Id.</returns>
        private async Task<List<int>> GetUserRoles(ApplicationUser user)
        {
            var allRoles = await _roleManager.Roles.ToListAsync();
            var userRoles = await _userManager.GetRolesAsync(user);

            return allRoles
                .Where(x => userRoles.Contains(x.Name))
                .Select(x => x.Id)
                .ToList();
        }

        private async Task PopulateUserRoleDataAsync(List<int> selectedRoles)
        {
            selectedRoles = selectedRoles ?? new List<int>();

            var isSuperUser = await IsSuperUser();
            var roles = await _roleManager.Roles.ToListAsync();

            ViewBag.Roles = roles.Select(r => new UserRoleDataViewModel
            {
                Id = r.Id,
                Name = r.Name,
                Check = selectedRoles.Contains(r.Id),
                Disabled = r.Name == Constants.Roles.SuperAdministrator && !isSuperUser ? true : false
            }).ToList();
        }

        private async Task PopulateUserRoleDataByUserAsync(ApplicationUser user)
        {
            var selectedRoles = await GetUserRoles(user);
            await PopulateUserRoleDataAsync(selectedRoles);
        }

        private async Task PopulateAssignedRolesDataAsync(ApplicationUser user)
        {
            ViewBag.AssignedRoles = await _userManager.GetRolesAsync(user);
        }

        #endregion
    }
}