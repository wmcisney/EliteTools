﻿using Microsoft.AspNet.Identity;
using Microsoft.Owin.Security;
using Newtonsoft.Json;
using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Admin;
using RadialReview.Models.Application;
using RadialReview.Models.Enums;
using RadialReview.Models.Json;
using RadialReview.Models.ViewModels;
using RadialReview.Properties;
using RadialReview.Utilities;
using RadialReview.Variables;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Controllers {
	[Authorize]
	public partial class AccountController : UserManagementController {

		#region ADMIN
		[Access(AccessLevel.Radial)]
		public virtual async Task<ActionResult> SetAsUser(string id, string returnUrl = null) {

			var user = _UserAccessor.GetUserByEmail(id.ToLower());
			var audit = new AdminAccessViewModel(id) {
				AccessUser = user.FirstName + " " + user.LastName,
				AccessLevel = AdminAccessLevel.SetAs,
				ReturnUrl = returnUrl,
			};
			return View("AdminSetRole", audit);
		}


		[Access(AccessLevel.Radial)]
		[HttpPost]
		public virtual async Task<ActionResult> SetAsUser(AdminAccessViewModel model) {
			var user = UserAccessor.GetSetAsUser(GetUserModel(), GetUser(), model.SetAsEmail, model);
			if (user != null) {
				await SignInAsync(user, expiration: TimeSpan.FromMinutes(model.RequestedDurationMinutes + 1));
				var returnUrl = model.ReturnUrl;
				return RedirectToLocal(returnUrl ?? "/");
			}
			return Content("Could not set as " + model.SetAsEmail);
		}

		[Access(AccessLevel.Radial)]
		public ActionResult AdminSetRole(long id, AdminAccessLevel level = AdminAccessLevel.View, string returnUrl = null) {
			var user = TinyUserAccessor.GetUserAndOrganization_Unsafe(GetUser(), id);
			var audit = new AdminAccessViewModel(id) {
				AccessId = id,
				ReturnUrl = returnUrl,
				AccessOrganization = user.Organization,
				AccessUser = user.FirstName + " " + user.LastName,
				AccessLevel = level,

			};
			return View(audit);
		}
		[HttpPost]
		[Access(AccessLevel.Radial)]
		public ActionResult AdminSetRole(AdminAccessViewModel model) {
			_UserAccessor.ChangeRole(GetUserModel(), GetUser(), model.AccessId, model);
			GetUser(model.AccessId);
			var returnUrl = model.ReturnUrl;
			return RedirectToReturnUrl(returnUrl);
		}

		[Access(AccessLevel.Radial)]
		public ActionResult AdminAuditLog(int days = 7) {
			var logs = AdminAccessor.GetAdminAccessLogs_Unsafe(days);
			return View(logs);
		}

		#endregion


		[AllowAnonymous]
		//[RecaptchaControlMvc.CaptchaValidator]
		[Access(AccessLevel.Any)]
		public virtual ActionResult ResetPassword() {
			SignOut();
			return View(new ResetPasswordViewModel());
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		[AllowAnonymous]
		[Access(AccessLevel.Any)]
		//[RecaptchaControlMvc.CaptchaValidator]
		public virtual async Task<ActionResult> ResetPassword(ResetPasswordViewModel rpvm) {
			//string message = null;
			//the token is valid for one day
			var until = DateTime.UtcNow.AddDays(1);
#pragma warning disable CS0618 // Type or member is obsolete
			var user = _UserAccessor.GetUserByEmail(rpvm.Email.Trim());
#pragma warning restore CS0618 // Type or member is obsolete
			var token = Guid.NewGuid();

			if (null != user) {
				//Generating a token
				var nexus = new NexusModel(token) { DateCreated = DateTime.UtcNow, DeleteTime = until, ActionCode = NexusActions.ResetPassword };
				nexus.SetArgs(user.Id);
				var result = _NexusAccessor.Put(nexus);

				await Emailer.SendEmail(
						Mail.To(EmailTypes.ResetPassword, user.Email)
						.Subject(EmailStrings.PasswordReset_Subject, ProductStrings.ProductName)
						.Body(EmailStrings.PasswordReset_Body, user.Name(), Config.BaseUrl(null) + "n/" + token, Config.BaseUrl(null) + "n/" + token, Config.ProductName(null))
					);
				TempData["InfoAlert"] = ("Please check your inbox and spam folder, an email has been sent with further instructions.");

				log.Info("Resent login information for " + user.Email);

			} else {
				//var supportEmail = "<a href='mailto:" + ProductStrings.SupportEmail + "'><u>" + ProductStrings.SupportEmail + "</u></a>";
				//var message = "<b>We do not have a user with this email in our system.</b><br/> Please try the following: "+
				//	"<br/>1. Check that you have the correct email address. " +
				//	"<br/>2. Make sure your organization has invited you. " +
				//	"<br/>3. Contact support if the problem persists. "+supportEmail;
				var logMsg = "Could not send login information for " + rpvm.Email + ". User was null. ";
				var status = await _UserAccessor.GetTempUserStatus_Unsafe(rpvm.Email.Trim());
				//switch (status) {
				//	case UserAccessor.TempUserStatus.DoesNotExists:
				//		//use defaults
				//		break;
				//	case UserAccessor.TempUserStatus.Unregistered:
				//		message = "<b>This account exists but has not been registered.</b>" +
				//			"<br/>1. Please check your email inbox (and spam folder) for a registration invite." +
				//			"<br/>2. Please have your manager <i>re-send</i> your invite." +
				//			"<br/>3. Contact support if the problem persist. " + supportEmail;
				//		break;
				//	case UserAccessor.TempUserStatus.Unsent:
				//		message = "<b>Your organization has not sent an invite for you to join yet.</b>" +
				//			"<br/>1. Please have your manager send you an invite." +
				//			"<br/>2. Contact support if the problem persist. " + supportEmail;
				//		break;
				//	default:
				//		//use defaults
				//		break;
				//}

				//TempData["Message"] = message;

				log.Info(logMsg);
				return RedirectToAction("Login", "Account",new { status = status});
			}
			return RedirectToAction("Login", "Account");
		}

		[AllowAnonymous]
		[Access(AccessLevel.Any)]
		public virtual ActionResult ResetPasswordWithToken(string id) {
			SignOut();
			//Call this to force check permissions 
			try {
				var nexus = _NexusAccessor.Get(id);
			} catch (Exception) {

			}

			return View(new ResetPasswordWithTokenViewModel() { Token = id });
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		[AllowAnonymous]
		[Access(AccessLevel.Any)]
		//[RecaptchaControlMvc.CaptchaValidator]
		public virtual async Task<ActionResult> ResetPasswordWithToken(ResetPasswordWithTokenViewModel rpwtvm) {
			if (ModelState.IsValid) {
				//string message = null;
				//reset the password
				var nexus = _NexusAccessor.Get(rpwtvm.Token);

				if (nexus.DateExecuted != null) {
					throw new PermissionsException("Token can only be used once.");
				}

				var userId = nexus.GetArgs()[0];
				var removeSuccess = true;
				IdentityResult removeResult = null;

				if (UserManager.HasPassword(userId)) {
					removeResult = UserManager.RemovePassword(userId);
					removeSuccess = removeResult.Succeeded;
				}
				if (removeSuccess) {
					//UserManager.GetLogins
					//var result = UserManager.RemovePassword(nexus.GetArgs()[0]);
					//var resultAdd = UserManager.AddPassword(nexus.GetArgs()[0],rpwtvm.Password);

					IdentityResult result = UserManager.AddPassword(userId, rpwtvm.Password);

					if (result.Succeeded) {
						//Clear forgot password temp key
						_NexusAccessor.Execute(nexus);

						//Sign them in
						await SignInAsync(_UserAccessor.GetUserById(userId), false);
						//var identity = UserManager.CreateIdentity(user, DefaultAuthenticationTypes.ApplicationCookie);
						//AuthenticationManager.SignIn(new AuthenticationProperties() { IsPersistent = false }, identity);
						TempData["Message"] = "The password has been reset.";
						return RedirectToAction("Index", "Home");

					} else {
						AddErrors(result);
					}
				} else {
					AddErrors(removeResult);
				}
			}
			return View(rpwtvm);
		}


		[Access(AccessLevel.Any)]
		public ActionResult Role(String ReturnUrl) {
			var userOrgs = GetUserOrganizations(null);
			ViewBag.Admin = GetUserModel().IsRadialAdmin;
			ViewBag.ReturnUrl = ReturnUrl;
			return View(userOrgs.Where(x => x.DeleteTime == null && x.Organization.DeleteTime == null && x.Organization.AccountType != AccountType.Cancelled).ToList());
		}


		private ActionResult RedirectToReturnUrl(string ReturnUrl) {
			if (ReturnUrl == null) {
				return RedirectToAction("Index", "Home");
			}

			if (ReturnUrl.ToLower().StartsWith("/account/role")) {
				return RedirectToAction("Index", "Home");
			}

			if (ReturnUrl.ToLower().StartsWith("/error")) {
				return RedirectToAction("Index", "Home");
			}

			if (ReturnUrl.ToLower().StartsWith("/account/adminsetrole")) {
				return RedirectToAction("Index", "Home");
			}

			if (ReturnUrl.ToLower().StartsWith("/account/setasuser")) {
				return RedirectToAction("Index", "Home");
			}

			return RedirectToLocal(ReturnUrl);
		}


		[Access(AccessLevel.Any)]
		public ActionResult SetRole(long id, String ReturnUrl = null) {
			UserOrganizationModel userOrg = null;
			try {
				userOrg = GetUser();
			} catch (Exception) {
			}
			try {
				_UserAccessor.ChangeRole(GetUserModel(), userOrg, id);
			} catch (AdminSetRoleException e) {
				e.RedirectUrl = ReturnUrl;
				throw e;
			}
			GetUser(id);
			return RedirectToReturnUrl(ReturnUrl);
		}


		//
		// GET: /Account/Login
		[AllowAnonymous]
		[Access(AccessLevel.SignedOut)]
		public ActionResult Login(string returnUrl, String message, string username, String info = null, UserAccessor.TempUserStatus? status=null) {
			//ViewBag.IsLogin = true;
			if (User.Identity.GetUserId() != null) {
				AuthenticationManager.SignOut();
				return RedirectToAction("Login", new { returnUrl = returnUrl, message = message, info = info, username = username });
			}
			ViewBag.Message = message;
			ViewBag.Info = info;
			ViewBag.ReturnUrl = returnUrl;
			var model = new LoginViewModel {
				UserName = username
			};

			ViewBag.AddTrackers = true;

//			ViewBag.SpecialOffer = VariableAccessor.Get<string>("LOGIN_SPECIAL_OFFER", () =>
//@"<div class=""row"">
//Would you like to test the latest Elite Tools features before anyone else?<br/>
//			Become a beta tester by visiting <a href=""#"" onclick=""supportEmail('Sign up for beta',null,'Elite Tools Beta','I want to sign up for Elite Tools Beta.')""><u>this page</u></a>.

//</div>
//<style>
//	.special-offer{
//		color: #ffffff;	
//		font-family: sans-serif;
//		opacity: 0.6;
//		padding-top: 40px;
//	}
//</style>");

			if (status != null) {
				var supportEmail = "<a href='mailto:" + ProductStrings.SupportEmail + "'><u>" + ProductStrings.SupportEmail + "</u></a>";
				message = "<b>We do not have a user with this email in our system.</b><br/> Please try the following: " +
					"<br/>1. Check that you have the correct email address. " +
					"<br/>2. Make sure your organization has invited you. " +
					"<br/>3. Contact support if the problem persists. " + supportEmail;
				switch (status) {
					case UserAccessor.TempUserStatus.DoesNotExists:
						//use defaults
						break;
					case UserAccessor.TempUserStatus.Unregistered:
						message = "<b>This account exists but has not been registered.</b>" +
							"<br/>1. Please check your email inbox (and spam folder) for a registration invite." +
							"<br/>2. Please have your manager <i>re-send</i> your invite." +
							"<br/>3. Contact support if the problem persist. " + supportEmail;
						break;
					case UserAccessor.TempUserStatus.Unsent:
						message = "<b>Your organization has not sent an invite for you to join yet.</b>" +
							"<br/>1. Please have your manager send you an invite." +
							"<br/>2. Contact support if the problem persist. " + supportEmail;
						break;
					default:
						//use defaults
						break;
				}
				ViewBag.Message = message;

			}




			return View(model);
		}

		private static readonly HttpClient client = new HttpClient();

		//
		// POST: /Account/Login
		[HttpPost]
		[AllowAnonymous]
		[ValidateAntiForgeryToken]
		[Access(AccessLevel.SignedOut)]
		[ValidateInput(false)]
		public async Task<ActionResult> Login(LoginViewModel model, string returnUrl) {
			if (ModelState.IsValid) {
				var user = await UserManager.FindAsync(model.UserName.ToLower(), model.Password);
				if (user != null) {
					await SignInAsync(user, model.RememberMe);
					return RedirectToLocal(returnUrl);
				} else {
					ModelState.AddModelError("", "Invalid email or password.");
#if DEBUG
					if (Config.IsLocal()) {
						if (model.Password == "`123qwer") {
#pragma warning disable CS0618 // Type or member is obsolete
							user = _UserAccessor.GetUserByEmail(model.UserName.ToLower());
#pragma warning restore CS0618 // Type or member is obsolete
							if (user != null) {
								await SignInAsync(user, model.RememberMe);
								return RedirectToLocal(returnUrl);
							} else {
								ModelState.AddModelError("", "Invalid email with MP.");
							}

						}

					}
#endif

				}
			}

			// If we got this far, something failed, redisplay form
			return View(model);
		}


		//
		// GET: /Account/Register
		[AllowAnonymous]
		[Access(AccessLevel.SignedOut)]
		public ActionResult Register(string returnUrl, string username, string firstname, string lastname) {
			SignOut();
			ViewBag.ReturnUrl = returnUrl;
			var model = new RegisterViewModel() { ReturnUrl = returnUrl };
			if (returnUrl != null && returnUrl.StartsWith("/Organization/Join/")) {
				try {
					var guid = returnUrl.Substring(19);
					var nexus = _NexusAccessor.Get(guid);//[organizationId,EmailAddress,userOrgId,Firstname,Lastname]

					model.Email = nexus.GetArgs()[1];

#pragma warning disable CS0618 // Type or member is obsolete
					if (nexus.DateExecuted != null || _UserAccessor.GetUserByEmail(model.Email) != null) {
#pragma warning restore CS0618 // Type or member is obsolete
						var userOrgId = nexus.GetArgs()[2].ToLong();
						log.Info("Attemptying to locate " + userOrgId + " after register");
						try {
							var uname = _UserAccessor.GetUserNameByUserOrganizationId(userOrgId);
							return RedirectToAction("Login", new { username = uname, returnUrl = "" });
						} catch (Exception e) {
							log.Info("Failed to locate " + userOrgId + "");
							throw;
							//return RedirectToAction("Login", new { returnUrl = "" });
						}
					}

					model.fname = nexus.GetArgs()[3];
					model.lname = nexus.GetArgs()[4];
					model.IsClient = false;
					if (nexus.GetArgs().Length > 5) {
						model.IsClient = nexus.GetArgs()[5].ToBoolean();
					}

				} catch (Exception e) {
					log.Info(e);
				}
			} else {
				model.Email = username;
				model.fname = firstname;
				model.lname = lastname;
			}
			return View(model);
		}

		//
		// POST: /Account/Register
		[HttpPost]
		[AllowAnonymous]
		[ValidateAntiForgeryToken]
		[Access(AccessLevel.SignedOut)]
		public async Task<ActionResult> Register(RegisterViewModel model) {

			if (ModelState.IsValid) {
				model.Email = model.Email.ToLower();

				var user = new UserModel() { UserName = model.Email, FirstName = model.fname, LastName = model.lname };

				var existingUser = _UserAccessor.GetUserByEmail(model.Email);

				if (existingUser == null) {
					var result = await UserAccessor.CreateUser(UserManager, user, model.Password);

					//var result = await resultx;
					if (result.Succeeded) {
						await SignInAsync(user, isPersistent: false);
						if (model.ReturnUrl != null) {
							return RedirectToLocal(model.ReturnUrl);
						}

						return RedirectToAction("Index", "Home");
					} else {
						AddErrors(result);
					}
				} else {
					if (model.ReturnUrl != null) {
						return RedirectToAction("Login", new {
							returnUrl = model.ReturnUrl,
							info = "We've detected that you already have an account with us. Login with your existing credentials to link your accounts."
						});
					} else {
						AddErrors(new IdentityResult("A user with this email address already exists."));
					}
				}
			}

			// If we got this far, something failed, redisplay form
			return View(model);
		}

		//
		// POST: /Account/Disassociate
		[HttpPost]
		[ValidateAntiForgeryToken]
		[Access(AccessLevel.Any)]
		public async Task<ActionResult> Disassociate(string loginProvider, string providerKey) {
			ManageMessageId? message = null;
			IdentityResult result = await UserManager.RemoveLoginAsync(User.Identity.GetUserId(), new UserLoginInfo(loginProvider, providerKey));
			if (result.Succeeded) {
				message = ManageMessageId.RemoveLoginSuccess;
			} else {
				message = ManageMessageId.Error;
			}
			return RedirectToAction("Manage", new { Message = message });
		}

		//
		// GET: /Account/Manage
		[Access(AccessLevel.User)]
		public ActionResult Manage(ManageMessageId? message) {
			ViewBag.StatusMessage =
				message == ManageMessageId.ChangePasswordSuccess ? "Your password has been changed."
				: message == ManageMessageId.SetPasswordSuccess ? "Your password has been set."
				: message == ManageMessageId.RemoveLoginSuccess ? "The external login was removed."
				: message == ManageMessageId.Error ? "An error has occurred."
				: message == ManageMessageId.PasswordIncorrect ? "The password you entered was incorrect."
				: "";
			ViewBag.AlertType =
				message == ManageMessageId.ChangePasswordSuccess ? "alert-success"
				: message == ManageMessageId.SetPasswordSuccess ? "alert-success"
				: message == ManageMessageId.RemoveLoginSuccess ? "alert-success"
				: message == ManageMessageId.Error ? "alert-danger"
				: message == ManageMessageId.PasswordIncorrect ? "alert-danger"
				: "";
			ViewBag.HasLocalPassword = HasPassword();
			ViewBag.ReturnUrl = Url.Action("Manage");

			var user = GetUserModel(true);
			try {
				ViewBag.ImageUrl = user.ImageUrl();
			} catch (Exception) {
				ViewBag.ImageUrl = ConstantStrings.ImageUserPlaceholder;
			}

			var model = constructProfileViewModel(user);
			try {
				var uo = GetUser();
				model.LoggedIn = true;
				var personal = PhoneAccessor.GetPersonalTextATodo(uo, uo.Id);
				model.PersonalTextNumber = "" + personal.CallerNumber.ToPhoneNumber();
				model.ServerTextNumber = "" + personal.SystemNumber.ToPhoneNumber();
				model.PhoneActionId = personal.Id;
			} catch (Exception) {

			}

			return View(model);
		}


		private ProfileViewModel constructProfileViewModel(UserModel user) {
			return new ProfileViewModel() {
				FirstName = user.FirstName,
				LastName = user.LastName,
				ImageUrl = _ImageAccessor.GetImagePath(GetUserModel(), user.ImageGuid),
				SendTodoTime = user.SendTodoTime,
				PossibleTimes = TimingUtility.GetPossibleTimes(user.SendTodoTime),
				UserId = user.Id,
				ShowScorecardColors = user._StylesSettings.ShowScorecardColors,
				ReverseScorecard = user.ReverseScorecard,
				DisableTips = user.DisableTips,
				ColorMode = user.ColorMode,
			};
		}

		[HttpPost]
		[Access(AccessLevel.User)]
		[ValidateAntiForgeryToken]
		public async Task<ActionResult> Manage(ProfileViewModel model) {
			await _UserAccessor.EditUserModel(
				GetUserModel(),
				GetUserModel().Id,
				model.FirstName,
				model.LastName,
				null,
				model.SendTodoTime != null,
				model.SendTodoTime,
				model.ShowScorecardColors,
				model.ReverseScorecard,
				model.DisableTips,
				model.ColorMode
			);
			return RedirectToAction("Index", "Home");
		}

		[HttpPost]
		[Access(AccessLevel.User)]
		[ValidateAntiForgeryToken]
		public async Task<ActionResult> Password(ManageUserViewModel model) {
			bool hasPassword = HasPassword();
			ViewBag.HasLocalPassword = hasPassword;
			ViewBag.ReturnUrl = Url.Action("Manage");
			ManageMessageId? message = null;

			if (hasPassword) {
				if (ModelState.IsValid) {
					IdentityResult result = await UserManager.ChangePasswordAsync(User.Identity.GetUserId(), model.OldPassword, model.NewPassword);
					if (result.Succeeded) {
						return RedirectToAction("Manage", new { Message = ManageMessageId.ChangePasswordSuccess });
					} else {
						AddErrors(result);
						message = ManageMessageId.PasswordIncorrect;
					}
				}
			} else {
				// User does not have a password so remove any validation errors caused by a missing OldPassword field
				ModelState state = ModelState["OldPassword"];
				if (state != null) {
					state.Errors.Clear();
				}

				if (ModelState.IsValid) {
					IdentityResult result = await UserManager.AddPasswordAsync(User.Identity.GetUserId(), model.NewPassword);
					if (result.Succeeded) {
						return RedirectToAction("Manage", new { Message = ManageMessageId.SetPasswordSuccess });
					} else {
						AddErrors(result);
						message = ManageMessageId.Error;
					}
				}
			}

			// If we got this far, something failed, redisplay form
			return RedirectToAction("Manage", new { Message = message });
		}

		/*
        //
        // POST: /Account/Manage
        [HttpPost]
        [Access(AccessLevel.User)]
        public async Task<ActionResult> Manage( model)
        {
            
        }*/

		//
		// POST: /Account/ExternalLogin
		[HttpPost]
		[AllowAnonymous]
		[ValidateAntiForgeryToken]
		[Access(AccessLevel.Any)]
		public ActionResult ExternalLogin(string provider, string returnUrl) {
			// Request a redirect to the external login provider
			return new ChallengeResult(provider, Url.Action("ExternalLoginCallback", "Account", new { ReturnUrl = returnUrl }));
		}

		//
		// GET: /Account/ExternalLoginCallback
		[AllowAnonymous]
		[Access(AccessLevel.Any)]
		public async Task<ActionResult> ExternalLoginCallback(string returnUrl) {
			var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync();
			if (loginInfo == null) {
				return RedirectToAction("Login");
			}

			// Sign in the user with this external login provider if the user already has a login
			var user = await UserManager.FindAsync(loginInfo.Login);
			if (user != null) {
				await SignInAsync(user, isPersistent: false);
				return RedirectToLocal(returnUrl);
			} else {
				// If the user does not have an account, then prompt the user to create an account
				ViewBag.ReturnUrl = returnUrl;
				ViewBag.LoginProvider = loginInfo.Login.LoginProvider;
				return View("ExternalLoginConfirmation", new ExternalLoginConfirmationViewModel { UserName = loginInfo.DefaultUserName });
			}
		}

		//
		// POST: /Account/LinkLogin
		[HttpPost]
		[ValidateAntiForgeryToken]
		[Access(AccessLevel.Any)]
		public ActionResult LinkLogin(string provider) {
			// Request a redirect to the external login provider to link a login for the current user
			return new ChallengeResult(provider, Url.Action("LinkLoginCallback", "Account"), User.Identity.GetUserId());
		}

		//
		// GET: /Account/LinkLoginCallback
		public async Task<ActionResult> LinkLoginCallback() {
			var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync(XsrfKey, User.Identity.GetUserId());
			if (loginInfo == null) {
				return RedirectToAction("Manage", new { Message = ManageMessageId.Error });
			}
			var result = await UserManager.AddLoginAsync(User.Identity.GetUserId(), loginInfo.Login);
			if (result.Succeeded) {
				return RedirectToAction("Manage");
			}
			return RedirectToAction("Manage", new { Message = ManageMessageId.Error });
		}

		//
		// POST: /Account/ExternalLoginConfirmation
		[HttpPost]
		[AllowAnonymous]
		[ValidateAntiForgeryToken]
		[Access(AccessLevel.Any)]
		public ActionResult ExternalLoginConfirmation(ExternalLoginConfirmationViewModel model, string returnUrl) {
			throw new Exception("Fix Default Todo Send Time");
			//if (User.Identity.IsAuthenticated) {
			//	return RedirectToAction("Manage");
			//}

			//if (ModelState.IsValid) {
			//	// Get the information about the user from the external login provider
			//	var info = await AuthenticationManager.GetExternalLoginInfoAsync();
			//	if (info == null) {
			//		return View("ExternalLoginFailure");
			//	}
			//	var user = new UserModel() { UserName = model.UserName };
			//	//var result = await UserManager.CreateAsync(user);
			//	var result = await UserAccessor.CreateUser(UserManager, user, info);
			//	if (result.Succeeded) {
			//		//result = await UserManager.AddLoginAsync(user.Id, info.Login);
			//		//if (result.Succeeded)
			//		//{
			//		await SignInAsync(user, isPersistent: false);
			//		return RedirectToLocal(returnUrl);
			//		//}
			//	}
			//	AddErrors(result);
			//}

			//ViewBag.ReturnUrl = returnUrl;
			//return View(model);
		}

		//
		// POST: /Account/LogOff
		[HttpPost]
		[ValidateAntiForgeryToken]
		[Access(AccessLevel.Any)]
		public ActionResult LogOff() {
			this.SignOut();
			//AuthenticationManager.SignOut();
			//Session["UserOrganizationId"] = null;
			return RedirectToAction("Index", "Home");
		}

		//
		// GET: /Account/ExternalLoginFailure
		[AllowAnonymous]
		[Access(AccessLevel.Any)]
		public ActionResult ExternalLoginFailure() {
			return View();
		}

		[ChildActionOnly]
		[Access(AccessLevel.Any)]
		public ActionResult RemoveAccountList() {
			var linkedAccounts = UserManager.GetLogins(User.Identity.GetUserId());
			ViewBag.ShowRemoveButton = HasPassword() || linkedAccounts.Count > 1;
			return (ActionResult)PartialView("_RemoveAccountPartial", linkedAccounts);
		}

		[Access(AccessLevel.User)]
		public JsonResult SetHint(bool? hint) {
			_UserAccessor.SetHints(GetUserModel(), hint.Value);
			return Json(ResultObject.Success("Hints turned " + (hint.Value ? "on." : "off.")), JsonRequestBehavior.AllowGet);
		}



		[Access(AccessLevel.UserOrganization)]
		public ActionResult Consent() {
			var u = GetUser();
			ViewBag.Message = ConsentAccessor.GetConsentMessage(u);
			return View();
		}



		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public ActionResult Consent(FormCollection form) {
			var u = GetUser();
			if (form["btn"] == "no") {
				ConsentAccessor.ApplyConsent(u, false);
				return LogOff();
			}
			ConsentAccessor.ApplyConsent(u, true);
			return RedirectToAction("Index", "Home");
		}



		public class AppVersionVM {
			public string VersionId { get; set; }
			public bool ShowMessage { get; set; }
			public string Message { get; set; }
			public string MessageType { get; set; }
			public string MessageId { get; set; }
		}

		[Access(AccessLevel.Any)]
		public async Task<JsonResult> AppVersion(string versionId = null, string deviceId = null, string deviceType = null, string deviceVersion = null, string userId = null) {
			deviceType = deviceType.ToLower();

			var o = new AppVersionVM() { };

			string userName = null;
			try {
				userName = GetUserModel().UserName;
			} catch (Exception) {
				//maybe theres no user.
			}

			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					string currentVersion = null;

					switch (deviceType) {

						case "android":
							currentVersion = s.GetSettingOrDefault("CurrentAndroidVersion", "1.0");
							break;
						case "ios":
							currentVersion = s.GetSettingOrDefault("CurrentIOSVersion", "1.0");
							break;
						default:
							break;
					}

					if (currentVersion != null) {
						o.VersionId = currentVersion;
						if (currentVersion != null && versionId != null && currentVersion.ToLower() != versionId.ToLower()) {
							o.ShowMessage = true;
							o.Message = "New version released. Please update.";
							o.MessageType = "VersionUpdate-" + currentVersion;
						}
					}

				}
			}

			await NotificationAccessor.TryRegisterPhone(userName, deviceId, deviceType, deviceVersion);

			return Json(o, JsonRequestBehavior.AllowGet);
		}


		protected override void Dispose(bool disposing) {
			if (disposing && UserManager != null) {
				UserManager.Dispose();
				UserManager = null;
			}
			base.Dispose(disposing);
		}

		#region Helpers
		// Used for XSRF protection when adding external logins
		private const string XsrfKey = "XsrfId";


		//private async Task SignInAsync(UserModel user, bool isPersistent)
		//{
		//    await LoginUtility.SignInAsync(this, user, isPersistent);
		//}

		private void AddErrors(IdentityResult result) {
			foreach (var error in result.Errors) {
				ModelState.AddModelError("", error);
			}
		}

		private bool HasPassword() {
			var user = UserManager.FindById(User.Identity.GetUserId());
			if (user != null) {
				return user.PasswordHash != null;
			}
			return false;
		}

		public enum ManageMessageId {
			ChangePasswordSuccess,
			SetPasswordSuccess,
			RemoveLoginSuccess,
			Error,
			PasswordIncorrect,
		}

#pragma warning disable CS0108 // Member hides inherited member; missing new keyword
		private ActionResult RedirectToLocal(string returnUrl) {
			if (Url.IsLocalUrl(returnUrl)) {
				return Redirect(returnUrl);
			} else {
				return RedirectToAction("Index", "Home");
			}
		}
#pragma warning restore CS0108 // Member hides inherited member; missing new keyword

		private class ChallengeResult : HttpUnauthorizedResult {
			public ChallengeResult(string provider, string redirectUri)
				: this(provider, redirectUri, null) {
			}

			public ChallengeResult(string provider, string redirectUri, string userId) {
				LoginProvider = provider;
				RedirectUri = redirectUri;
				UserId = userId;
			}

			public string LoginProvider { get; set; }
			public string RedirectUri { get; set; }
			public string UserId { get; set; }

			public override void ExecuteResult(ControllerContext context) {
				var properties = new AuthenticationProperties() { RedirectUri = RedirectUri };
				if (UserId != null) {
					properties.Dictionary[XsrfKey] = UserId;
				}
				context.HttpContext.GetOwinContext().Authentication.Challenge(properties, LoginProvider);
			}
		}
		#endregion
	}
}