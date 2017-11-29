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
using SimpleMvc.Models.UserViewModels;
using AutoMapper;
using SimpleMvc.Data;

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

        //// GET: User/Create
        //public ActionResult Create()
        //{
        //    return View();
        //}

        //// POST: User/Create
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public ActionResult Create(IFormCollection collection)
        //{
        //    try
        //    {
        //        // TODO: Add insert logic here

        //        return RedirectToAction(nameof(Index));
        //    }
        //    catch
        //    {
        //        return View();
        //    }
        //}

        // GET: User/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                AddErrorMessage("No record found.");
                return RedirectToAction(nameof(Index));
            }

            var model = Mapper.Map<EditViewModel>(user);
            await PopulateAssignedRoleDataAsync(user);

            return View(model);
        }

        // POST: User/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditViewModel model)
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

            await PopulateAssignedRoleDataAsync(user);

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
            await PopulateAssignedRoleDataAsync(user);

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
            var model = new DeleteUserViewModel
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
        /// Check current user is in Super Admin role. if false means is normal admin.
        /// </summary>
        /// <returns></returns>
        private async Task<bool> IsSuperUser()
        {
            var user = await _userManager.GetUserAsync(User);
            return await _userManager.IsInRoleAsync(user, Constants.Roles.SuperAdministrator);
        }

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

        private async Task PopulateAssignedRoleDataAsync(ApplicationUser user)
        {
            var allRoles = await _roleManager.Roles.ToListAsync();
            var userRoles = await _userManager.GetRolesAsync(user);
            bool isSuperUser = await IsSuperUser();

            var data = allRoles.Select(x => new AssignedRoleData
            {
                RoleId = x.Id,
                RoleName = x.Name,
                Assigned = userRoles.Contains(x.Name),
                CanToggle = x.Name == Constants.Roles.SuperAdministrator && !isSuperUser ? false : true
            });

            ViewBag.Roles = data.ToList();
        }

        #endregion
    }
}