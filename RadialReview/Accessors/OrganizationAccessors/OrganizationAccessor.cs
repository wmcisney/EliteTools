﻿using Microsoft.AspNet.SignalR;
using NHibernate;
using RadialReview.Crosscutting.Flags;
using RadialReview.Crosscutting.Hooks;
using RadialReview.Crosscutting.Hooks.Interfaces;
using RadialReview.Exceptions;
using RadialReview.Hubs;
using RadialReview.Models;
using RadialReview.Models.Accountability;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Angular.CompanyValue;
using RadialReview.Models.Angular.Users;
using RadialReview.Models.Angular.VTO;
using RadialReview.Models.Askables;
using RadialReview.Models.Components;
using RadialReview.Models.Enums;
using RadialReview.Models.Periods;
using RadialReview.Models.Permissions;
using RadialReview.Models.Responsibilities;
using RadialReview.Models.Scorecard;
using RadialReview.Models.UserModels;
using RadialReview.Models.ViewModels;
using RadialReview.Models.VTO;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using RadialReview.Utilities.Hooks;
using RadialReview.Utilities.Query;
using RadialReview.Utilities.RealTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebGrease.Css.Extensions;

namespace RadialReview.Accessors {

	public class OrganizationAccessor : BaseAccessor {


		public class CreateOrganizationOutput {
			public OrganizationModel organization { get; set; }
			public UserOrganizationModel NewUser { get; set; }
			public AccountabilityNode NewUserNode { get; set; }
		}

		public static ShareVtoPages GetVtoSharedPages(UserOrganizationModel caller, long orgId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.ViewOrganization(orgId);
					return s.Get<OrganizationModel>(orgId).Settings.ShareVtoPages;
				}
			}
		}

		/*public async Task<CreateOrganizationOutput> CreateOrganization_Test(ISession s, UserModel user, string name, PaymentPlanType planType, DateTime now,
			bool enableL10, bool enableReview, bool startDeactivated = false, string positionName = null) {
			UserOrganizationModel userOrgModel;
			//OrganizationModel organization;
			OrganizationTeamModel allMemberTeam;
			PermissionsUtility perms;
			AccountabilityChart acChart;
			//node = null;

			var output = new CreateOrganizationOutput();

			using (var tx = s.BeginTransaction()) {

				output.organization = new OrganizationModel() {
					CreationTime = now,
					Name = new LocalizedStringModel() { Standard = name },
					ManagersCanEdit = false,
				};

				#region Set Settings
				if (startDeactivated)
					output.organization.DeleteTime = new DateTime(1, 1, 1);

				output.organization.Settings.EnableL10 = enableL10;
				output.organization.Settings.EnableReview = enableReview;
				s.Save(output.organization);
#pragma warning disable CS0618 // Type or member is obsolete
				var paymentPlan = PaymentAccessor.GeneratePlan(planType, now);
#pragma warning restore CS0618 // Type or member is obsolete
				PaymentAccessor.AttachPlan(s, output.organization, paymentPlan);
				output.organization.PaymentPlan = paymentPlan;
				output.organization.Organization = output.organization;
				s.Update(output.organization);
				#endregion

				#region AddUser to Organization
				user = s.Get<UserModel>(user.Id);

				userOrgModel = new UserOrganizationModel() {
					Organization = output.organization,
					User = user,
					ManagerAtOrganization = true,
					ManagingOrganization = true,
					EmailAtOrganization = user.Email,
					AttachTime = now,
					CreateTime = now,
				};

				s.Save(user);
				s.SaveOrUpdate(userOrgModel);
				#endregion

				#region Set Role
				user.UserOrganization.Add(userOrgModel);
				user.UserOrganizationCount += 1;

				var newArray = new List<long>();
				if (user.UserOrganizationIds != null)
					newArray = user.UserOrganizationIds.ToList();
				newArray.Add(userOrgModel.Id);
				user.UserOrganizationIds = newArray.ToArray();
				user.CurrentRole = userOrgModel.Id;

				output.organization.Members.Add(userOrgModel);
				s.Update(user);
				s.Save(output.organization);
				#endregion

				#region Update OrganizationLookup
				s.Save(new OrganizationLookup() {
					OrgId = output.organization.Id,
					LastUserLogin = userOrgModel.Id,
					LastUserLoginTime = DateTime.UtcNow,
				});
				#endregion

				#region Create/Populate Accountability Chart
				perms = PermissionsUtility.Create(s, userOrgModel);
				acChart = AccountabilityAccessor.CreateChart(s, perms, output.organization.Id, false);
				output.organization.AccountabilityChartId = acChart.Id;

				if (positionName != null) {
					var orgPos = new OrganizationPositionModel() {
						Organization = s.Load<OrganizationModel>(output.organization.Id),
						CreatedBy = userOrgModel.Id,
						CustomName = positionName,
					};
					s.Save(orgPos);
					var posDur = new PositionDurationModel() {
						UserId = userOrgModel.Id,
						Position = orgPos,
						PromotedBy = userOrgModel.Id,
						CreateTime = DateTime.UtcNow,
						OrganizationId = output.organization.Id,
					};
					userOrgModel.Positions.Add(posDur);
					s.Update(userOrgModel);
				}
				#endregion

				#region Create Teams
				//Add team for every member
				allMemberTeam = new OrganizationTeamModel() {
					CreatedBy = userOrgModel.Id,
					Name = output.organization.Name.Translate(),
					OnlyManagersEdit = true,
					Organization = output.organization,
					InterReview = false,
					Type = TeamType.AllMembers
				};
				s.Save(allMemberTeam);
				//Add team for every manager
				var managerTeam = new OrganizationTeamModel() {
					CreatedBy = userOrgModel.Id,
					Name = Config.ManagerName() + "s at " + output.organization.Name.Translate(),
					OnlyManagersEdit = true,
					Organization = output.organization,
					InterReview = false,
					Type = TeamType.Managers
				};
				s.Save(managerTeam);
				#endregion

				#region Update UserLookup
				if (userOrgModel != null)
					userOrgModel.UpdateCache(s);
				#endregion


				#region Add Default Permissions
				PermissionsAccessor.CreatePermItems(s, perms.GetCaller(), PermItem.ResourceType.UpgradeUsersForOrganization, output.organization.Id,
					PermTiny.Admins(),
					PermTiny.RGM(allMemberTeam.Id, admin: false)
				);

				#endregion


				tx.Commit();
			}
			using (var tx = s.BeginTransaction()) {

				var year = DateTime.UtcNow.Year;
				foreach (var q in Enumerable.Range(1, 4)) {
					s.Save(new PeriodModel() {
						Name = year + " Q" + q,
						StartTime = new DateTime(year, 1, 1).AddDays((q - 1) * 13 * 7).StartOfWeek(DayOfWeek.Sunday),
						EndTime = new DateTime(year, 1, 1).AddDays(q * 13 * 7).StartOfWeek(DayOfWeek.Sunday),
						OrganizationId = output.organization.Id,
					});
				}

				foreach (var defaultQ in new[]{
						"What is their greatest contribution to the team?",
						"What should they start or stop doing?"
					}) {
					var r = new ResponsibilityModel() {
						Category = ApplicationAccessor.GetApplicationCategory(s, ApplicationAccessor.FEEDBACK),
						ForOrganizationId = output.organization.Id,
						ForResponsibilityGroup = allMemberTeam.Id,
						CreateTime = now,
						Weight = WeightType.Normal,
						Required = true,
						Responsibility = defaultQ
					};
					r.SetQuestionType(QuestionType.Feedback);
					s.Save(r);

					allMemberTeam.Responsibilities.Add(r);
				}
				s.Update(allMemberTeam);

				output.NewUser = userOrgModel;
				s.Flush();
				userOrgModel.UpdateCache(s);

				tx.Commit();
			}
			using (var tx = s.BeginTransaction()) {
#pragma warning disable CS0618 // Type or member is obsolete
				await HooksRegistry.Each<ICreateUserOrganizationHook>(x => x.CreateUserOrganization(s, userOrgModel));
#pragma warning restore CS0618 // Type or member is obsolete
				tx.Commit();
			}
			using (var tx = s.BeginTransaction()) {
				//Generate Account Age Events 
				EventUtil.GenerateAccountAgeEvents(s, output.organization.Id, now);
				await EventUtil.Trigger(x => x.Create(s, EventType.CreateOrganization, userOrgModel, output.organization, message: output.organization.GetName()));
				if (enableL10)
					await EventUtil.Trigger(x => x.Create(s, EventType.EnableL10, userOrgModel, output.organization));
				if (enableReview)
					await EventUtil.Trigger(x => x.Create(s, EventType.EnableReview, userOrgModel, output.organization));

				tx.Commit();
			}

			s.Flush();
			using (var tx = s.BeginTransaction()) {
				s.Clear();
				var permsAdmin = PermissionsUtility.Create(s, UserOrganizationModel.ADMIN);
				using (var rt = RealTimeUtility.Create(false)) {
					output.NewUserNode = AccountabilityAccessor.AppendNode(s, permsAdmin, rt, acChart.RootId, userId: userOrgModel.Id);
					//AccountabilityAccessor.UpdateAccountabilityNode(s, RealTimeUtility.Create(false), permsAdmin, node.Id, null, );
				}
				tx.Commit();
				s.Flush();
			}
			return output;
		}
		*/

		//public async Task<CreateOrganizationOutput> CreateOrganization(UserModel user, string name, PaymentPlanType planType, DateTime now,
		//		bool enableL10, bool enableReview, bool startDeactivated = false, string positionName = null, bool enableAC = true, AccountType accountType=AccountType.Demo,
		//		OrgCreationData createData = null)			

		public async Task<CreateOrganizationOutput> CreateOrganization(UserModel user, PaymentPlanType planType, DateTime now, OrgCreationData data) {
			using (var s = HibernateSession.GetCurrentSession()) {
				var result = await CreateOrganization(s, user, planType, now, data);
				s.Flush();
				return result;
			}
		}

		public async Task<CreateOrganizationOutput> CreateOrganization(ISession s, UserModel user, PaymentPlanType planType, DateTime now, OrgCreationData data) {
			UserOrganizationModel userOrgModel;
			//OrganizationModel organization;
			OrganizationTeamModel allMemberTeam;
			PermissionsUtility perms;
			AccountabilityChart acChart;
			UserOrganizationModel primaryContact = null;

			var output = new CreateOrganizationOutput() { };

			//using (var s = HibernateSession.GetCurrentSession()) {
			using (var tx = s.BeginTransaction()) {

				output.organization = new OrganizationModel() {
					CreationTime = now,
					Name = new LocalizedStringModel() { Standard = data.Name },
					ManagersCanEdit = false,
					AccountType = data.AccountType
				};

				#region Set Settings
				if (data.StartDeactivated) {
					output.organization.DeleteTime = new DateTime(1, 1, 1);
				}

				output.organization.Settings.EnableL10 = data.EnableL10;
				output.organization.Settings.EnableReview = data.EnableReview;
				output.organization.Settings.DisableAC = !data.EnableAC;
				output.organization.Settings.EnablePeople = data.EnablePeople;
				s.Save(output.organization);
				#endregion

				#region PaymentPlan
#pragma warning disable CS0618 // Type or member is obsolete
				var paymentPlan = PaymentAccessor.GeneratePlan(planType, now, data.TrialEnd);
#pragma warning restore CS0618 // Type or member is obsolete
				PaymentAccessor.AttachPlan(s, output.organization, paymentPlan);
				output.organization.PaymentPlan = paymentPlan;
				output.organization.Organization = output.organization;
				s.Update(output.organization);
				#endregion

				#region AddUser to Organization
				user = s.Get<UserModel>(user.Id);

				userOrgModel = new UserOrganizationModel() {
					Organization = output.organization,
					User = user,
					ManagerAtOrganization = true,
					ManagingOrganization = true,
					EmailAtOrganization = user.Email,
					AttachTime = now,
					CreateTime = now,
				};

				s.Save(user);
				s.SaveOrUpdate(userOrgModel);
				#endregion

				#region Set Role
				user.UserOrganization.Add(userOrgModel);
				user.UserOrganizationCount += 1;

				var newArray = new List<long>();
				if (user.UserOrganizationIds != null) {
					newArray = user.UserOrganizationIds.ToList();
				}

				newArray.Add(userOrgModel.Id);
				user.UserOrganizationIds = newArray.ToArray();
				user.CurrentRole = userOrgModel.Id;

				output.organization.Members.Add(userOrgModel);
				s.Update(user);
				s.Save(output.organization);
				#endregion

				#region Update OrganizationLookup
				s.Save(new OrganizationLookup() {
					OrgId = output.organization.Id,
					LastUserLogin = userOrgModel.Id,
					LastUserLoginTime = DateTime.UtcNow,
				});
				#endregion

				#region Create/Populate Accountability Chart
				perms = PermissionsUtility.Create(s, userOrgModel);
				acChart = AccountabilityAccessor.CreateChart(s, perms, output.organization.Id, false);
				output.organization.AccountabilityChartId = acChart.Id;

				//if (data.ContactPosition != null) {
				var supportOrgPos = new OrganizationPositionModel() {
					Organization = s.Load<OrganizationModel>(output.organization.Id),
					CreatedBy = userOrgModel.Id,
					CustomName = "Elite Tools Support",
				};
				s.Save(supportOrgPos);
				var posDur = new PositionDurationModel() {
					UserId = userOrgModel.Id,
					Position = supportOrgPos,
					PromotedBy = userOrgModel.Id,
					CreateTime = DateTime.UtcNow,
					OrganizationId = output.organization.Id,
				};
				userOrgModel.Positions.Add(posDur);
				s.Update(userOrgModel);
				//}
				#endregion

				#region Create Teams
				//Add team for every member
				allMemberTeam = new OrganizationTeamModel() {
					CreatedBy = userOrgModel.Id,
					Name = output.organization.Name.Translate(),
					OnlyManagersEdit = true,
					Organization = output.organization,
					InterReview = false,
					Type = TeamType.AllMembers
				};
				s.Save(allMemberTeam);
				//Add team for every manager
				var managerTeam = new OrganizationTeamModel() {
					CreatedBy = userOrgModel.Id,
					Name = Config.ManagerName() + "s at " + output.organization.Name.Translate(),
					OnlyManagersEdit = true,
					Organization = output.organization,
					InterReview = false,
					Type = TeamType.Managers
				};
				s.Save(managerTeam);
				#endregion

				#region Update UserLookup
				try {
					if (userOrgModel != null) {
						userOrgModel.UpdateCache(s);
					}
				} catch (Exception) {

				}
				#endregion

				#region Add Default Permissions
				PermissionsAccessor.CreatePermItems(s, perms.GetCaller(), PermItem.ResourceType.UpgradeUsersForOrganization, output.organization.Id,
					PermTiny.Admins(),
					PermTiny.RGM(allMemberTeam.Id, admin: false)
				);
				PermissionsAccessor.CreatePermItems(s, perms.GetCaller(), PermItem.ResourceType.UpdatePaymentForOrganization, output.organization.Id,
					PermTiny.Admins()
				);
				PermissionsAccessor.CreatePermItems(s, perms.GetCaller(), PermItem.ResourceType.EditDeleteUserDataForOrganization, output.organization.Id,
					PermTiny.Admins()
				);

				#endregion


				tx.Commit();
			}


			using (var tx = s.BeginTransaction()) {

				var year = DateTime.UtcNow.Year;
				foreach (var q in Enumerable.Range(1, 4)) {
					s.Save(new PeriodModel() {
						Name = year + " Q" + q,
						StartTime = new DateTime(year, 1, 1).AddDays((q - 1) * 13 * 7).StartOfWeek(DayOfWeek.Sunday),
						EndTime = new DateTime(year, 1, 1).AddDays(q * 13 * 7).StartOfWeek(DayOfWeek.Sunday),
						OrganizationId = output.organization.Id,
					});
				}

				foreach (var defaultQ in new[]{
						"What is their greatest contribution to the team?",
						"What should they start or stop doing?"
					}) {
					var r = new ResponsibilityModel() {
						Category = ApplicationAccessor.GetApplicationCategory(s, ApplicationAccessor.FEEDBACK),
						ForOrganizationId = output.organization.Id,
						ForResponsibilityGroup = allMemberTeam.Id,
						CreateTime = now,
						Weight = WeightType.Normal,
						Required = true,
						Responsibility = defaultQ
					};
					r.SetQuestionType(QuestionType.Feedback);
					s.Save(r);

					allMemberTeam.Responsibilities.Add(r);
				}
				s.Update(allMemberTeam);

				output.NewUser = userOrgModel;
				s.Flush();
				try {
					userOrgModel.UpdateCache(s);
				} catch (Exception e) {
					log.Error(e);
				}

				tx.Commit();
			}


			using (var tx = s.BeginTransaction()) {
				data.OrgId = output.organization.Id;
				s.Save(data);
				tx.Commit();
			}



			s.Flush();
			//}

			//using (var s = HibernateSession.GetCurrentSession()) {
			using (var tx = s.BeginTransaction()) {
				s.Clear();
				var permsAdmin = PermissionsUtility.Create(s, UserOrganizationModel.ADMIN);
				using (var rt = RealTimeUtility.Create(false)) {
					output.NewUserNode = AccountabilityAccessor.AppendNode(s, permsAdmin, rt, acChart.RootId, userId: userOrgModel.Id);
					//AccountabilityAccessor.UpdateAccountabilityNode(s, RealTimeUtility.Create(false), permsAdmin, node.Id, null, );
				}
				tx.Commit();
				s.Flush();
			}
			//}
			if (data != null && (data.ContactEmail != null || data.ContactFN != null || data.ContactLN != null)) {
				//Add Primary contact
				var ua = new UserAccessor();
				var primContact = new CreateUserOrganizationViewModel() {
					Email = data.ContactEmail,
					FirstName = data.ContactFN,
					LastName = data.ContactLN,
					SendEmail = false,
					OrgId = output.organization.Id,
					IsManager = true,
					ManagerNodeId = acChart.RootId,
					Position = new UserPositionViewModel() {
						CustomPosition = data.ContactPosition
					}
				};
				var result = await ua.CreateUser(userOrgModel, primContact);
				primaryContact = result.Item2;
				//using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var org = s.Get<OrganizationModel>(output.organization.Id);
					org.PrimaryContactUserId = primaryContact.Id;
					s.Update(org);

					await EventUtil.Trigger(x => x.Create(s, EventType.CreatePrimaryContact, primaryContact, primaryContact, message: primaryContact.GetName()));

					tx.Commit();
					s.Flush();
				}
				//}
			}

#pragma warning disable CS0618 // Type or member is obsolete
			//using (var s = HibernateSession.GetCurrentSession()) {
			
			// remove due to duplicate of user registered
			//using (var tx = s.BeginTransaction()) {
				//await HooksRegistry.Each<ICreateUserOrganizationHook>((ses, x) => x.CreateUserOrganization(ses, userOrgModel));

				//if (primaryContact != null) {
				//	await HooksRegistry.Each<ICreateUserOrganizationHook>((ses, x) => x.CreateUserOrganization(ses, primaryContact));
				//}
				//tx.Commit();
			//}
			using (var tx = s.BeginTransaction()) {
				//Generate Account Age Events 
				EventUtil.GenerateAccountAgeEvents(s, output.organization.Id, now);
				await EventUtil.Trigger(x => x.Create(s, EventType.CreateOrganization, userOrgModel, output.organization, message: output.organization.GetName()));
				if (data.EnableL10) {
					await EventUtil.Trigger(x => x.Create(s, EventType.EnableL10, userOrgModel, output.organization));
				}

				if (data.EnableReview) {
					await EventUtil.Trigger(x => x.Create(s, EventType.EnableReview, userOrgModel, output.organization));
				}

				tx.Commit();
			}

			using (var tx = s.BeginTransaction()) {
				var org = s.Get<OrganizationModel>(output.organization.Id);
				await HooksRegistry.Each<IOrganizationHook>((ses, x) => x.CreateOrganization(ses, output.NewUser, org));
				tx.Commit();
			}

			s.Flush();
			//}
#pragma warning restore CS0618 // Type or member is obsolete


			return output;
		}



		public static IEnumerable<AngularUser> GetAngularUsers(UserOrganizationModel caller, long id) {
			using (var db = HibernateSession.GetCurrentSession()) {
				using (var tx = db.BeginTransaction()) {
#pragma warning disable CS0618 // Type or member is obsolete
					return GetAllUserOrganizations(db, PermissionsUtility.Create(db, caller), id).Select(x => AngularUser.CreateUser(x));
#pragma warning restore CS0618 // Type or member is obsolete
				}
			}
		}
		public static async Task<UserOrganizationModel> JoinOrganization_Test(ISession db, UserModel user, long managerId, long userOrgPlaceholder) {
			var manager = db.Get<UserOrganizationModel>(managerId);
			var orgId = manager.Organization.Id;
			var organization = db.Get<OrganizationModel>(orgId);
			user = db.Get<UserModel>(user.Id);
			var userOrg = db.Get<UserOrganizationModel>(userOrgPlaceholder);

			userOrg.AttachTime = DateTime.UtcNow;
			userOrg.User = user;
			userOrg.Organization = organization;
			user.CurrentRole = userOrgPlaceholder;

			user.UserOrganization.Add(userOrg);
			user.UserOrganizationCount += 1;

			var newArray = user.UserOrganizationIds.NotNull(x => x.ToList()) ?? new List<long>();
			newArray.Add(userOrg.Id);
			user.UserOrganizationIds = newArray.ToArray();

			if (user.ImageGuid == null && userOrg.TempUser.ImageGuid != null) {
				user.ImageGuid = userOrg.TempUser.ImageGuid;
			}

			db.Delete(userOrg.TempUser);

			if (user.SendTodoTime == -1) {
				user.SendTodoTime = organization.Settings.DefaultSendTodoTime;
			}

			userOrg.TempUser = null;

			db.SaveOrUpdate(user);

			userOrg.UpdateCache(db);

			await HooksRegistry.Each<ICreateUserOrganizationHook>((ses, x) => x.OnUserOrganizationAttach(ses, userOrg));


			return userOrg;
		}

		public static async Task<UserOrganizationModel> JoinOrganization(UserModel user, long managerId, long userOrgPlaceholder) {
			UserOrganizationModel userOrg = null;
			using (var db = HibernateSession.GetCurrentSession()) {
				using (var tx = db.BeginTransaction()) {
					var manager = db.Get<UserOrganizationModel>(managerId);
					var orgId = manager.Organization.Id;
					var organization = db.Get<OrganizationModel>(orgId);
					user = db.Get<UserModel>(user.Id);
					userOrg = db.Get<UserOrganizationModel>(userOrgPlaceholder);

					userOrg.AttachTime = DateTime.UtcNow;
					userOrg.User = user;
					userOrg.Organization = organization;
					user.CurrentRole = userOrgPlaceholder;

					user.UserOrganization.Add(userOrg);
					user.UserOrganizationCount += 1;

					var newArray = user.UserOrganizationIds.NotNull(x => x.ToList()) ?? new List<long>();
					newArray.Add(userOrg.Id);
					user.UserOrganizationIds = newArray.ToArray();

					if (user.ImageGuid == null && userOrg.TempUser.ImageGuid != null) {
						user.ImageGuid = userOrg.TempUser.ImageGuid;
					}

					db.Delete(userOrg.TempUser);

					if (user.SendTodoTime == -1) {
						user.SendTodoTime = organization.Settings.DefaultSendTodoTime;
					}

					userOrg.TempUser = null;

					db.SaveOrUpdate(user);
					userOrg.UpdateCache(db);

					tx.Commit();
					db.Flush();
				}
			}
			using (var db = HibernateSession.GetCurrentSession()) {
				using (var tx = db.BeginTransaction()) {
					await HooksRegistry.Each<ICreateUserOrganizationHook>((ses, x) => x.OnUserOrganizationAttach(ses, userOrg));
					tx.Commit();
					db.Flush();
				}
			}

			return userOrg;
		}

		[Obsolete("Includes dead items")]
		public static List<OrganizationPositionModel> GetOrganizationPositions(ISession s, PermissionsUtility perms, long organizationId) {
			perms.ViewOrganization(organizationId);
			var positions = s.QueryOver<OrganizationPositionModel>().Where(x => x.Organization.Id == organizationId).List().ToList();
			return positions;
		}

		[Obsolete("Includes dead items")]
		public List<OrganizationPositionModel> GetOrganizationPositions(UserOrganizationModel caller, long organizationId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					return GetOrganizationPositions(s, perms, organizationId);
				}
			}
		}

		public OrganizationPositionModel GetOrganizationPosition(UserOrganizationModel caller, long positionId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var position = s.Get<OrganizationPositionModel>(positionId);
					PermissionsUtility.Create(s, caller).ViewOrganization(position.Organization.Id);
					return position;
				}
			}
		}


		[Obsolete("Includes dead items")]
		public List<ManagerDuration> GetOrganizationManagerLinks(UserOrganizationModel caller, long organizationId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).ViewOrganization(organizationId);
					return s.QueryOver<ManagerDuration>().JoinQueryOver(x => x.Manager).Where(x => x.Organization.Id == organizationId).List().ToList();
				}
			}
		}



		public List<long> GetAllOrganizationMemberIdsAcrossTime(UserOrganizationModel caller, long organizationId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).ViewOrganization(organizationId);
					return s.QueryOver<UserOrganizationModel>().Where(x => x.Organization.Id == organizationId).Select(x => x.Id).List<long>().ToList();
				}
			}

		}

		public List<UserOrganizationModel> GetOrganizationMembers(UserOrganizationModel caller, long organizationId, bool teams, bool managers) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).ViewOrganization(organizationId);
					var users = s.QueryOver<UserOrganizationModel>().Where(x => x.Organization.Id == organizationId && x.DeleteTime == null).List().ToList();

					if (managers) {
						var allManagers = s.QueryOver<ManagerDuration>().JoinQueryOver(x => x.Manager).Where(x => x.Organization.Id == organizationId && x.DeleteTime == null).List().ToList();
						foreach (var user in users) {
							user.PopulateManagers(allManagers);
						}
					}

					if (teams) {
						var allTeams = s.QueryOver<OrganizationTeamModel>().Where(x => x.Organization.Id == organizationId && x.DeleteTime == null).List().ToList();
						var allTeamDurations = s.QueryOver<TeamDurationModel>().JoinQueryOver(x => x.Team).Where(x => x.Organization.Id == organizationId).List().ToList();
						foreach (var user in users) {
							user.PopulateTeams(allTeams, allTeamDurations);
						}
					}
					return users;
				}
			}
		}

		public async Task<OrganizationPositionModel> EditOrganizationPosition(UserOrganizationModel caller, long orgPositionId, long organizationId,
			/*long? positionId = null,*/ String customName = null) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).EditPositions(organizationId).ManagingPosition(orgPositionId);

					/*var existing = s.QueryOver<OrganizationPositionModel>()
                        .Where(x=>x.Organization.Id==organizationId && positionId==x.Position.Id)
                        .List().ToList().FirstOrDefault();
                    if (existing!=null)
                        throw new PermissionsException();*/


					OrganizationPositionModel orgPos;
					if (orgPositionId == 0) {
						var org = s.Get<OrganizationModel>(organizationId);
						if ( /*positionId == null ||*/ String.IsNullOrWhiteSpace(customName)) {
							throw new PermissionsException();
						}

						orgPos = new OrganizationPositionModel() { Organization = org, CreatedBy = caller.Id };
						s.Save(orgPos);
					} else {
						orgPos = s.Get<OrganizationPositionModel>(orgPositionId);
					}

					/*
                    if (positionId != null)
                    {
                        var position = s.Get<PositionModel>(positionId);
                        orgPos.Position = position;
                    }
                    */

					if (customName != null && orgPos.CustomName != customName) {
						orgPos.CustomName = customName;

						var aa = s.QueryOver<PositionDurationModel>().Where(x => x.Position.Id == orgPos.Id && x.DeleteTime == null).Select(x => x.UserId).List<long>().ToList();
						var all = s.QueryOver<UserOrganizationModel>().Where(x => x.DeleteTime == null).WhereRestrictionOn(x => x.Id).IsIn(aa).List().ToList();
						foreach (var a in all) {
							a.UpdateCache(s);
						}

						var usersInPosition = s.QueryOver<PositionDurationModel>().Where(x => x.Position.Id == orgPositionId && x.DeleteTime == null)
							.Select(x => x.UserId)
							.List<long>().ToArray();

						if (usersInPosition.Any()) {
							var managersTeams = s.QueryOver<OrganizationTeamModel>().Where(x => x.DeleteTime == null && x.Type == TeamType.Subordinates)
								.WhereRestrictionOn(x => x.ManagedBy).IsIn(usersInPosition)
								.List().ToList();
							foreach (var t in managersTeams) {
								var newName = all.FirstOrDefault(x => x.Id == t.ManagedBy).NotNull(x => x.GetNameAndTitle().Possessive() + " Direct Reports");
								t.Name = newName ?? t.Name;
								s.Update(t);
							}
						}


					}


					s.SaveOrUpdate(orgPos);
					tx.Commit();
					s.Flush();

					await HooksRegistry.Each<IPositionHooks>((ses, x) => x.UpdatePosition(ses, orgPos, new IPositionHookUpdates() { NameChanged = true }));


					return orgPos;
				}
			}
		}
		public OrganizationTeamModel AddOrganizationTeam(UserOrganizationModel caller, long organizationId, string teamName, bool onlyManagersEdit, bool secret) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).EditTeam(0).ViewOrganization(organizationId);

					/*var existing = s.QueryOver<OrganizationPositionModel>()
                        .Where(x => x.Organization.Id == organizationId && positionId == x.Position.Id)
                        .List().ToList().FirstOrDefault();
                    if (existing!=null)
                        throw new PermissionsException();*/

					var org = s.Get<OrganizationModel>(organizationId);

					var orgTeam = new OrganizationTeamModel() {
						Organization = org,
						CreatedBy = caller.Id,
						Name = teamName,
						OnlyManagersEdit = onlyManagersEdit,
						Secret = secret,
					};

					s.Save(orgTeam);
					tx.Commit();
					s.Flush();

					return orgTeam;
				}
			}
		}

		public static async Task Edit(UserOrganizationModel caller, long organizationId, string organizationName = null,
				bool? managersHaveAdmin = null,
				bool? strictHierarchy = null,
				bool? managersCanEditPositions = null,
				bool? sendEmailImmediately = null,
				bool? managersCanEditSelf = null,
				bool? employeesCanEditSelf = null,
				bool? managersCanCreateSurvey = null,
				bool? employeesCanCreateSurvey = null,
				string rockName = null,
				bool? onlySeeRockAndScorecardBelowYou = null,
				string timeZoneId = null,
				DayOfWeek? weekStart = null,
				ScorecardPeriod? scorecardPeriod = null,
				Month? startOfYearMonth = null,
				DateOffset? startOfYearOffset = null,
				string dateFormat = null,
				NumberFormat? numberFormat = null,
				bool? limitFiveState = null,
				int? defaultTodoSendTime = null,
				bool? allowAddClient = null,
				//string imageUrl = null,
				string primaryColorHex = null,
				long? shareVTOFromRecurrenceId = null,
				ShareVtoPages? shareVtoPages = null
			) {
			var updates = new IOrganizationHookUpdates();

			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller).EditOrganization(organizationId).ManagingOrganization(caller.Organization.Id);
					var org = s.Get<OrganizationModel>(organizationId);
					if (managersHaveAdmin != null && managersHaveAdmin.Value != org.ManagersCanEdit) {
						if (caller.ManagingOrganization) {
							org.ManagersCanEdit = managersHaveAdmin.Value;
						} else {
							throw new PermissionsException("You cannot change whether managers are admins at the organization.");
						}
					}
					if (!String.IsNullOrWhiteSpace(organizationName) && org.Name.Standard != organizationName) {
						updates.UpdateName = true;
						org.Name.UpdateDefault(organizationName);
						var managers = s.QueryOver<OrganizationTeamModel>().Where(x => x.Organization.Id == org.Id && x.Type == TeamType.Managers && x.DeleteTime == null).List().FirstOrDefault();
						if (managers != null) {
							managers.Name = Config.ManagerName() + "s at " + organizationName;
							s.Update(managers);
						}
						var allTeam = s.QueryOver<OrganizationTeamModel>().Where(x => x.Organization.Id == org.Id && x.Type == TeamType.AllMembers && x.DeleteTime == null).List().FirstOrDefault();
						if (allTeam != null) {
							allTeam.Name = organizationName;
							s.Update(allTeam);
						}

						var chart = s.Get<AccountabilityChart>(org.AccountabilityChartId);
						if (chart != null) {
							chart.Name = organizationName;
							s.Update(chart);
						}

					}
					if (strictHierarchy != null) {
						updates.Settings.StrictHierarchy = true;
						org.StrictHierarchy = strictHierarchy.Value;
					}

					if (managersCanEditPositions != null) {
						updates.Settings.ManagersCanEditPositions = true;
						org.ManagersCanEditPositions = managersCanEditPositions.Value;
					}

					if (sendEmailImmediately != null) {
						updates.Settings.SendEmailImmediately = true;
						org.SendEmailImmediately = sendEmailImmediately.Value;
					}

					//if (managersCanRemoveUsers != null)
					//	org.ManagersCanRemoveUsers = managersCanRemoveUsers.Value;


					if (managersCanEditSelf != null) {
						updates.Settings.ManagersCanEditSelf = true;
						org.Settings.ManagersCanEditSelf = managersCanEditSelf.Value;
					}

					if (limitFiveState != null) {
						updates.Settings.LimitFiveState = true;
						org.Settings.LimitFiveState = limitFiveState.Value;
					}

					if (employeesCanEditSelf != null) {
						updates.Settings.EmployeesCanEditSelf = true;
						org.Settings.EmployeesCanEditSelf = employeesCanEditSelf.Value;
					}

					if (employeesCanCreateSurvey != null) {
						updates.Settings.EmployeesCanCreateSurvey = true;
						org.Settings.EmployeesCanCreateSurvey = employeesCanCreateSurvey.Value;
					}

					if (onlySeeRockAndScorecardBelowYou != null) {
						updates.Settings.OnlySeeRocksAndScorecardBelowYou = true;
						org.Settings.OnlySeeRocksAndScorecardBelowYou = onlySeeRockAndScorecardBelowYou.Value;
					}

					if (scorecardPeriod != null) {
						updates.Settings.ScorecardPeriod = true;
						org.Settings.ScorecardPeriod = scorecardPeriod.Value;
					}

					if (dateFormat != null) {
						updates.Settings.DateFormat = true;
						org.Settings.DateFormat = dateFormat;
					}

					if (managersCanCreateSurvey != null) {
						updates.Settings.ManagersCanCreateSurvey = true;
						org.Settings.ManagersCanCreateSurvey = managersCanCreateSurvey.Value;
					}

					if (!String.IsNullOrWhiteSpace(rockName)) {
						updates.Settings.RockName = true;
						org.Settings.RockName = rockName;
					}

					if (!String.IsNullOrWhiteSpace(timeZoneId) && TimeZoneInfo.GetSystemTimeZones().Any(x => x.Id == timeZoneId)) {
						updates.Settings.TimeZoneId = true;
						org.Settings.TimeZoneId = timeZoneId;
					}

					if (weekStart != null) {
						updates.Settings.WeekStart = true;
						org.Settings.WeekStart = weekStart.Value;
					}

					if (startOfYearMonth != null) {
						updates.Settings.StartOfYearMonth = true;
						org.Settings.StartOfYearMonth = startOfYearMonth.Value;
					}

					if (startOfYearOffset != null) {
						updates.Settings.StartOfYearOffset = true;
						org.Settings.StartOfYearOffset = startOfYearOffset.Value;
					}

					if (numberFormat != null) {
						updates.Settings.NumberFormat = true;
						org.Settings.NumberFormat = numberFormat.Value;
					}

					if (defaultTodoSendTime != null) {
						updates.Settings.DefaultSendTodoTime = true;
						org.Settings.DefaultSendTodoTime = defaultTodoSendTime.Value;
					}

					if (allowAddClient != null) {
						updates.Settings.AllowAddClient = true;
						org.Settings.AllowAddClient = allowAddClient.Value;
					}

					if (primaryColorHex != null) {
						updates.Settings.PrimaryColor = true;
						org.Settings.PrimaryColor = ColorComponent.FromHex(primaryColorHex);
					}

					if (shareVTOFromRecurrenceId != null) {
						updates.Settings.ShareVto = true;
						await L10Accessor.SetSharedVTOVision(s, perms, org.Id, shareVTOFromRecurrenceId.Value);
					}

					if (shareVtoPages != null) {
						updates.Settings.ShareVtoPages = true;
						org.Settings.ShareVtoPages = shareVtoPages.Value;

					}

					//if (imageUrl != null)
					//	org.Settings.ImageGuid= imageUrl;

					s.Update(org);

#pragma warning disable CS0618 // Type or member is obsolete
					var all = GetAllUserOrganizations(s, perms, organizationId);
#pragma warning restore CS0618 // Type or member is obsolete
					var cache = new Cache();
					foreach (var u in all) {
						cache.InvalidateForUser(u, CacheKeys.USERORGANIZATION);
					}

					await HooksRegistry.Each<IOrganizationHook>((ses, x) => x.UpdateOrganization(s, org.Id, updates));


					tx.Commit();
					s.Flush();
				}
			}
		}

		public List<UserOrganizationModel> GetOrganizationManagers(UserOrganizationModel caller, long organizationId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).ViewOrganization(organizationId);
					var managers = s.QueryOver<UserOrganizationModel>()
											.Where(x =>
												x.Organization.Id == organizationId &&
												(x.ManagerAtOrganization || x.ManagingOrganization) &&
												x.DeleteTime == null
											).List()
											.OrderBy(x => x.GetName())
											.ToList();
					return managers;
				}
			}
		}

		public static Tree GetOrganizationTree(ISession s, PermissionsUtility perms, long orgId, long? parentId = null, bool includeTeams = false, bool includeRoles = false) {
			perms.ViewOrganization(orgId);

			var org = s.Get<OrganizationModel>(orgId);

			List<UserOrganizationModel> managers;

			if (parentId == null) {
				managers = s.QueryOver<UserOrganizationModel>()
							.Where(x => x.Organization.Id == orgId && x.ManagingOrganization)
							//.Fetch(x => x.Teams).Default
							.List()
							.ToListAlive();
			} else {
				var parent = s.Get<UserOrganizationModel>(parentId);

				if (orgId != parent.Organization.Id) {
					throw new PermissionsException("Organizations do not match");
				}

				perms.ViewOrganization(parent.Organization.Id);
				managers = parent.AsList();
			}

			var managerIds = managers.Select(x => x.Id).ToList();

			if (includeTeams) {
				var managerTeams = s.QueryOver<TeamDurationModel>().Where(x => x.DeleteTime == null)
					.WhereRestrictionOn(x => x.UserId).IsIn(managerIds).List().ToList();

				foreach (var t in managerTeams) {
					managers.First(x => x.Id == t.UserId).Teams.Add(t);
				}
			}

			var caller = perms.GetCaller();

			var deep = DeepAccessor.Users.GetSubordinatesAndSelf(s, caller, caller.Id);

			//var classes = "organizations".AsList("admin");

			var managingOrg = caller.ManagingOrganization && orgId == caller.Organization.Id;

			var tree = new Tree() {
				name = org.Name.Translate(),
				@class = "organizations",
				id = -1 * orgId,
				children = managers.Select(x => x.GetTree(s, perms, deep, caller.Id, force: managingOrg, includeRoles: includeRoles)).ToList()
			};

			return tree;
		}

		public Tree GetOrganizationTree(UserOrganizationModel caller, long orgId, bool includeRoles = false) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					return GetOrganizationTree(s, perms, orgId, null, true, includeRoles);
				}
			}
		}
		/*
        private Tree Children(String name, String subtext, String classStr,bool manager, long id, List<UserOrganizationModel> users,List<long> deep,long youId)
        {

            /*var newClasses = classes.ToList();
            if (classes.Count > 0)
                newClasses.RemoveAt(0);*/
		/*var managing =deep.Any(x=>x==id);

        if (managing)
            classStr += " managing";
        if (id == youId)
            classStr += " you";

        return new Tree()
        {
            name = name,
            id = id,
            subtext = subtext,
            @class = classStr,
            managing = managing,
            manager = manager,
            children = users.ToListAlive().Select(x =>{
                var selfClasses = x.Teams.ToListAlive().Select(y=>y.Team.Name).ToList();
                selfClasses.Add("employee");
                if (x.ManagingOrganization)
                    selfClasses.Add("admin");
                if(x.ManagerAtOrganization)
                    selfClasses.Add("manager");


                return Children(
                        x.GetName(),
                        x.GetTitles(),
                        String.Join(" ", selfClasses.Select(y => Regex.Replace(y, "[^a-zA-Z0-9]", "_"))),
                        x.IsManager(),
                        x.Id,
                        x.ManagingUsers.ToListAlive().Select(y => y.Subordinate).ToList(),
                        deep,
                        youId
                    );
            }
                ).ToList()
        };*
    }*/

		public List<QuestionCategoryModel> GetOrganizationCategories(UserOrganizationModel caller, long organizationId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).ViewOrganization(organizationId);
					var orgCategories = s.QueryOver<QuestionCategoryModel>()
									.Where(x => (x.OriginId == organizationId && x.OriginType == OriginType.Organization))
									.List()
									.ToList();

					var appCategories = ApplicationAccessor.GetApplicationCategories(s);

					return orgCategories.Union(appCategories).ToList();
				}
			}
		}

		//Gets all users and populates direct subordinates
		/*
        public List<UserOrganizationModel> GetOrganizationMembersAndSubordinates(UserOrganizationModel caller, long forUserId, bool allSubordinates)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    if (allSubordinates == false)
                        throw new NotImplementedException("All subordinates not implemented. Only direct subordinates.");

                    var perms=PermissionsUtility.Create(s, caller).ViewUserOrganization(forUserId,false);

                    var user=s.Get<UserOrganizationModel>(forUserId);
                    var allOrgUsers=s.QueryOver<UserOrganizationModel>().Where(x => x.Organization.Id == user.Organization.Id).List().ToList();

                    var directReports = s.QueryOver<ManagerDuration>().Where(x => x.ManagerId == forUserId).List().ToList();

                    foreach (var u in allOrgUsers)
                    {
                        u.SetPersonallyManaging(directReports.Any(x => x.SubordinateId == u.Id));
                    }

                    return allOrgUsers;
                }
            }
        }*/

		public OrganizationModel GetOrganization(UserOrganizationModel caller, long organizationId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).ViewOrganization(organizationId);
					return s.Get<OrganizationModel>(organizationId);
				}
			}
		}

		public static IEnumerable<CompanyValueModel> GetCompanyValues_Unsafe(ISession s, long organizationId) {
			return s.QueryOver<CompanyValueModel>().Where(x => x.DeleteTime == null && x.OrganizationId == organizationId).Future();
		}

		public static List<CompanyValueModel> GetCompanyValues(AbstractQuery query, PermissionsUtility perms, long organizationId, DateRange range) {
			perms.ViewOrganization(organizationId);
			return GetCompanyValues_Unsafe(query, organizationId, range);
		}

		public static List<CompanyValueModel> GetCompanyValues_Unsafe(AbstractQuery query, long organizationId, DateRange range) {
			return query.Where<CompanyValueModel>(x => x.OrganizationId == organizationId).FilterRange(range).ToList();
		}

		public List<CompanyValueModel> GetCompanyValues(UserOrganizationModel caller, long organizationId, DateRange range = null) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					return GetCompanyValues(s.ToQueryProvider(true), perms, organizationId, range);
				}
			}
		}

		public static void EditCompanyValues(ISession s, PermissionsUtility perms, long organizationId, List<CompanyValueModel> companyValues) {
			perms.EditCompanyValues(organizationId);
			var category = ApplicationAccessor.GetApplicationCategory(s, ApplicationAccessor.EVALUATION);


			foreach (var r in companyValues) {
				if (r.OrganizationId != organizationId) {
					throw new PermissionsException("You do not have access to this value.");
				}

				r.Category = category;

				//if (r.ValueBarId == null) {
				//	r._ValueBar = r._ValueBar ?? new ValueBar();
				//	s.SaveOrUpdate(bar);
				//	r.ValueBarId = bar.
				//} 

				s.SaveOrUpdate(r);


			}

			var hub = GlobalHost.ConnectionManager.GetHubContext<RealTimeHub>();
			var vtoIds = s.QueryOver<VtoModel>().Where(x => x.Organization.Id == organizationId).Select(x => x.Id).List<long>();
			foreach (var vtoId in vtoIds) {
				var group = hub.Clients.Group(RealTimeHub.Keys.GenerateVtoGroupId(vtoId));
#pragma warning disable CS0618 // Type or member is obsolete
				group.update(new AngularVTO(vtoId) {
					Values = AngularList.Create(AngularListType.ReplaceAll, AngularCompanyValue.Create(companyValues))
				});
#pragma warning restore CS0618 // Type or member is obsolete
			}


		}
		public void EditCompanyValues(UserOrganizationModel caller, long organizationId, List<CompanyValueModel> companyValues) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					EditCompanyValues(s, perms, organizationId, companyValues);
					tx.Commit();
					s.Flush();
				}
			}
		}

		[Obsolete("remove", true)]
		public static List<RockModel> GetCompanyRocks(ISession s, PermissionsUtility perms, long organizationId) {
			throw new PermissionsException("cannot view");
			perms.ViewOrganization(organizationId);
			return s.QueryOver<RockModel>().Where(x => x.DeleteTime == null && x.OrganizationId == organizationId && x.CompanyRock).List().ToList();
		}

		[Obsolete("remove", true)]
		public List<RockModel> GetCompanyRocks(UserOrganizationModel caller, long organizationId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					return GetCompanyRocks(s, perms, organizationId);
				}
			}
		}
		public static IEnumerable<long> GetAllManagerIds(ISession s, PermissionsUtility perm, long organizationId, bool excludeClients = false) {
			perm.ViewOrganization(organizationId);

			var q = s.QueryOver<UserOrganizationModel>()
				.Where(x => x.DeleteTime == null && x.Organization.Id == organizationId && (x.ManagerAtOrganization || x.ManagingOrganization));
			if (excludeClients) {
				q = q.Where(x => !x.IsClient);
			}

			return q.Select(x => x.Id).Future<long>();
		}

		public static IEnumerable<long> GetAllUserOrganizationIds(ISession s, PermissionsUtility perm, long organizationId, bool excludeClients = false) {
			perm.ViewOrganization(organizationId);

			var q = s.QueryOver<UserOrganizationModel>()
				.Where(x => x.DeleteTime == null && x.Organization.Id == organizationId);

			if (excludeClients) {
				q = q.Where(x => !x.IsClient);
			}

			return q.Select(x => x.Id).Future<long>();
		}
		[Obsolete("Dangerous")]
		public static List<UserOrganizationModel> GetAllUserOrganizations(ISession s, PermissionsUtility perm, long organizationId) {
			perm.ViewOrganization(organizationId);
			return s.QueryOver<UserOrganizationModel>()
				.Where(x => x.DeleteTime == null && x.Organization.Id == organizationId)
				.List().ToList();
		}

		public async Task UpdateProducts(UserOrganizationModel caller, bool enableReview, bool enableL10, bool enableSurvey, bool enablePeople, bool enableCP, bool enableZapier, BrandingType branding) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller).ManagingOrganization(caller.Organization.Id);

					var org = s.Get<OrganizationModel>(caller.Organization.Id);

					if (org.Settings.EnableL10 != enableL10) {
						await EventUtil.Trigger(x => x.Create(s, enableL10 ? EventType.EnableL10 : EventType.DisableL10, caller, org));
					}

					if (org.Settings.EnableReview != enableReview) {
						await EventUtil.Trigger(x => x.Create(s, enableReview ? EventType.EnableReview : EventType.DisableReview, caller, org));
					}

					if (org.Settings.EnablePeople != enablePeople) {
						await EventUtil.Trigger(x => x.Create(s, enablePeople ? EventType.EnablePeople : EventType.DisablePeople, caller, org));
					}

					if (org.Settings.EnableCoreProcess != enableCP) {
						await EventUtil.Trigger(x => x.Create(s, enableCP ? EventType.EnableCoreProcess : EventType.DisableCoreProcess, caller, org));
					}

					if (org.Settings.EnableZapier != enableZapier) {
						await EventUtil.Trigger(x => x.Create(s, enableZapier ? EventType.EnableZapier : EventType.DisableZapier, caller, org));
					}

					org.Settings.EnableL10 = enableL10;
					org.Settings.EnableReview = enableReview;
					org.Settings.EnablePeople = enablePeople;
					org.Settings.Branding = branding;
					org.Settings.EnableSurvey = enableSurvey;
					org.Settings.EnableCoreProcess = enableCP;
					org.Settings.EnableZapier = enableZapier;

					s.Update(org);

					tx.Commit();
					s.Flush();

#pragma warning disable CS0618 // Type or member is obsolete
					var all = GetAllUserOrganizations(s, perms, caller.Organization.Id);
#pragma warning restore CS0618 // Type or member is obsolete
					var cache = new Cache();
					foreach (var u in all) {
						cache.InvalidateForUser(u, CacheKeys.USERORGANIZATION);
					}
				}
			}
		}

		public static IEnumerable<Askable> AskablesAboutOrganization(AbstractQuery query, PermissionsUtility perms, long orgId, DateRange range) {
			perms.ViewOrganization(orgId);
			return query.Where<AboutCompanyAskable>(x => x.DeleteTime == null && x.OrganizationId == orgId)
				.FilterRange(range)
				.ToList();
		}

		public static List<AboutCompanyAskable> GetQuestionsAboutCompany(UserOrganizationModel caller, long orgId, DateRange range) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perm = PermissionsUtility.Create(s, caller);
					var q = s.ToQueryProvider(false);
					return AskablesAboutOrganization(q, perm, orgId, range).Cast<AboutCompanyAskable>().ToList();
				}
			}
		}

		public static void EditQuestionsAboutCompany(UserOrganizationModel caller, List<AboutCompanyAskable> questions) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perm = PermissionsUtility.Create(s, caller);
					questions.Select(x => x.OrganizationId)
						.Distinct()
						.ForEach(x =>
							perm.EditOrganizationQuestions(x)
						);

					var cat = ApplicationAccessor.GetApplicationCategory(s, ApplicationAccessor.COMPANY_QUESTION);
					foreach (var q in questions) {
						q.Organization = s.Load<OrganizationModel>(q.OrganizationId);
						q.Category = cat;
						s.SaveOrUpdate(q);
					}

					tx.Commit();
					s.Flush();
				}
			}
		}

		public static List<UserLookup> GetOrganizationMembersLookup(ISession s, PermissionsUtility perms, long organizationId, bool populatePersonallyManaging, PermissionType? type = null) {
			var caller = perms.GetCaller();
			perms.ViewOrganization(organizationId);
			var users = s.QueryOver<UserLookup>().Where(x => x.OrganizationId == organizationId && x.DeleteTime == null).List().ToList();
			var isRadialAdmin = perms.HasRadialAdminFlags();
			if (!isRadialAdmin) {
				users = users.Where(x => !x.Email.NotNull(y => y.ToLower().EndsWith("@dlptools.com"))).ToList();
			}
			if (populatePersonallyManaging) {
				var subs = DeepAccessor.Users.GetSubordinatesAndSelf(s, caller, caller.Id, type);

				var orgManager = PermissionsAccessor.AnyTrue(s, caller, type, x => x.ManagingOrganization);


				users.ForEach(u =>
					u._PersonallyManaging = (isRadialAdmin || (orgManager && u.OrganizationId == organizationId) || subs.Contains(u.UserId)));
			}

			return users;
		}

		public List<UserLookup> GetOrganizationMembersLookup(UserOrganizationModel caller, long organizationId, bool populatePersonallyManaging, PermissionType? type = null) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					return GetOrganizationMembersLookup(s, perms, organizationId, populatePersonallyManaging, type);
				}
			}
		}

		public static List<PositionDurationModel> GetOrganizationUserPositions(ISession s, PermissionsUtility perm, long orgId) {
			perm.ViewOrganization(orgId);
			return s.QueryOver<PositionDurationModel>().JoinQueryOver(x => x.Position).Where(x => x.Organization.Id == orgId && x.DeleteTime == null).List().ToList();
		}

		public static List<UserOrganizationModel> GetUsersWithOrganizationPositions(ISession s, PermissionsUtility perm, long orgId, long orgPositionId) {
			//perm.ViewOrganization(orgId);
			//var ids = s.QueryOver<PositionDurationModel>().Where(x => x.DeleteTime == null).JoinQueryOver(x => x.Position).Where(x => x.Organization.Id == orgId && x.DeleteTime == null && x.Id == orgPositionId).Select(x => x.UserId).List<long>().ToList();
			var ids = GetUserIdsWithOrganizationPositions(s, perm, orgId, orgPositionId);
			return s.QueryOver<UserOrganizationModel>().Where(x => x.DeleteTime == null).WhereRestrictionOn(x => x.Id).IsIn(ids.ToArray()).List().ToList();
		}

		public static IEnumerable<long> GetUserIdsWithOrganizationPositions(ISession s, PermissionsUtility perm, long orgId, long orgPositionId) {
			perm.ViewOrganization(orgId);
			return s.QueryOver<PositionDurationModel>().Where(x => x.DeleteTime == null)
				.JoinQueryOver(x => x.Position)
				.Where(x => x.Organization.Id == orgId && x.DeleteTime == null && x.Id == orgPositionId)
				.Select(x => x.UserId)
				.Future<long>();

			//return s.QueryOver<UserOrganizationModel>().Where(x => x.DeleteTime == null).WhereRestrictionOn(x => x.Id).IsIn(ids).List().ToList();
		}

		public static List<ResponsibilityGroupModel> GetOrganizationResponsibilityGroupModels(UserOrganizationModel caller, long organizationId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).ViewOrganization(organizationId);
					return s.QueryOver<ResponsibilityGroupModel>().Where(x => x.DeleteTime == null && x.Organization.Id == organizationId).List().ToList();
				}
			}
		}
		//public static List<ResponsibilityGroupModel> GetOrganizationResponsibilityGroupModels(UserOrganizationModel caller, long organizationId,string search) {
		//	using (var s = HibernateSession.GetCurrentSession()) {
		//		using (var tx = s.BeginTransaction()) {
		//			PermissionsUtility.Create(s, caller).ViewOrganization(organizationId);
		//			var terms = search.ToLower().Split(' ');
		//			s.CreateCriteria<ResponsibilityGroupModel>().Add(Restrictions.InsensitiveLike(x=>"LastName", "something", MatchMode.Anywhere))

		//			var q = s.QueryOver<ResponsibilityGroupModel>().Where(x => x.DeleteTime == null && x.Organization.Id == organizationId);

		//			q.
		//			.List().ToList();
		//		}
		//	}
		//}

		public void EnsureAllAtOrganization(UserOrganizationModel caller, long organizationId, List<long> userIds, bool includedDeleted = false) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).ViewOrganization(organizationId);
					var q = s.QueryOver<UserOrganizationModel>().Where(x => x.Organization.Id == organizationId);
					if (!includedDeleted) {
						q = q.Where(x => x.DeleteTime == null);
					}

					var foundIds = q.WhereRestrictionOn(x => x.Id).IsIn(userIds).Select(x => x.Id).List<long>().ToList();

					foreach (var id in userIds) {
						if (!foundIds.Any(x => x == id)) {
							throw new PermissionsException("User not part of organization");
						}
					}

				}
			}
		}

		public static async Task SetFlag(UserOrganizationModel caller, long orgId, OrganizationFlagType type, bool enabled) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);

					if (enabled) {
						await AddFlag(s, perms, orgId, type);
					} else {
						await RemoveFlag(s, perms, orgId, type);
					}

					tx.Commit();
					s.Flush();
				}
			}
		}

		public static async Task RemoveFlag(UserOrganizationModel caller, long orgId, OrganizationFlagType type) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					await RemoveFlag(s, perms, orgId, type);
					tx.Commit();
					s.Flush();
				}
			}
		}

		public static async Task AddFlag(UserOrganizationModel caller, long orgId, OrganizationFlagType type) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					await AddFlag(s, perms, orgId, type);
					tx.Commit();
					s.Flush();
				}
			}
		}

		public static async Task AddFlag(ISession s, PermissionsUtility perms, long orgId, OrganizationFlagType type) {
			perms.Or(x => x.ViewOrganization(orgId), x => x.RadialAdmin(true));
			var any = s.QueryOver<OrganizationFlag>().Where(x => x.OrganizationId == orgId && type == x.FlagType && x.DeleteTime == null).RowCount();
			//var user = s.Get<OrganizationModel>(orgId);
			if (any == 0) {
				s.Save(new OrganizationFlag() {
					OrganizationId = orgId,
					FlagType = type,

				});
				await HooksRegistry.Each<IOrganizationFlagHook>((ses, x) => x.AddFlag(ses, orgId, type));
			}
		}

		public static async Task RemoveFlag(ISession s, PermissionsUtility perms, long orgId, OrganizationFlagType type) {
			perms.Or(x => x.ViewOrganization(orgId), x => x.RadialAdmin(true));
			var any = s.QueryOver<OrganizationFlag>().Where(x => x.OrganizationId == orgId && type == x.FlagType && x.DeleteTime == null).List().ToList();
			//var org = s.Get<UserOrganizationModel>(orgId);
			if (any.Count > 0) {
				foreach (var a in any) {
					a.DeleteTime = DateTime.UtcNow;
					s.Update(a);
				}
				await HooksRegistry.Each<IOrganizationFlagHook>((ses, x) => x.RemoveFlag(ses, orgId, type));
			}
		}

		public static async Task<List<OrganizationFlagType>> GetFlags(UserOrganizationModel caller, long orgId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					return await GetFlags(s, perms, orgId);
				}
			}
		}

		public static async Task<List<OrganizationFlagType>> GetFlags(ISession s, PermissionsUtility perms, long orgId) {
			perms.Or(x => x.ViewOrganization(orgId), x => x.RadialAdmin(true));
			return s.QueryOver<OrganizationFlag>().Where(x => x.OrganizationId == orgId && x.DeleteTime == null).Select(x => x.FlagType).List<OrganizationFlagType>().ToList();
		}

	}
}