﻿using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Angular.CompanyValue;
using RadialReview.Models.Angular.Compass;
using RadialReview.Models.Angular.Dashboard;
using RadialReview.Models.Angular.DataType;
using RadialReview.Models.Angular.Headlines;
using RadialReview.Models.Angular.Issues;
using RadialReview.Models.Angular.Meeting;
using RadialReview.Models.Angular.Notifications;
using RadialReview.Models.Angular.Rocks;
using RadialReview.Models.Angular.Roles;
using RadialReview.Models.Angular.Scorecard;
using RadialReview.Models.Angular.Todos;
using RadialReview.Models.Angular.Users;
using RadialReview.Models.Compass;
using RadialReview.Models.Dashboard;
using RadialReview.Models.Enums;
using RadialReview.Models.Json;
using RadialReview.Models.L10;
using RadialReview.Models.Permissions;
using RadialReview.Models.ViewModels;
using RadialReview.Notifications;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using System.Web.SessionState;

namespace RadialReview.Controllers {
	[SessionState(SessionStateBehavior.ReadOnly)]
	public class CompassDataController : BaseController
	{
		protected static void ProcessDeadTile(Exception e)
		{
			//  int a = 0;
		}

		//[OutputCache(Duration = 3, VaryByParam = "id", Location = OutputCacheLocation.Client, NoStore = true)]
		//[OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
		[Access(AccessLevel.UserOrganization)]
		[OutputCache(NoStore = true, Duration = 0)]
		public async Task<JsonResult> Data2(long id, bool completed = false, string name = null, long? start = null, long? end = null, bool fullScorecard = false, long? CompassId = null)
		{
			long cId = 0;
			//Response.AddHeader("Content-Encoding", "gzip");
			var userId = id;
			Compass comp;
			if (CompassId == null)
			{
				comp = CompassAccessor.GetPrimaryCompassForUser(GetUser(), CompassId);
			}
			else
			{
				comp = CompassAccessor.GetCompass(GetUser(), CompassId.Value);
			}

			List<CompassTileModel> tiles = new List<CompassTileModel>();
			if (comp != null)
			{
				tiles = CompassAccessor.GetCompassTiles(GetUser(), comp.Id);
			}

			if (CompassId != null)
			{
				cId = CompassId.Value;
			}

			ListDataVM output = await GetCompassTileData(GetUser(), cId, userId, tiles, completed, name, start, end, fullScorecard);

			return Json(output, JsonRequestBehavior.AllowGet);
		}

		public static async Task<ListDataVM> GetTileData(UserOrganizationModel caller, long CompassId, long userId, List<TileModel> tiles, bool completed = false, string name = null, long? start = null, long? end = null, bool fullScorecard = false)
		{
			DateTime startRange;
			DateTime endRange;

			if (start == null)
			{
				startRange = TimingUtility.PeriodsAgo(DateTime.UtcNow, 13, caller.Organization.Settings.ScorecardPeriod);
			}
			else
			{
				startRange = start.Value.ToDateTime();
			}

			if (end == null)
			{
				endRange = DateTime.UtcNow/*.AddDays(14);//*/.StartOfWeek(DayOfWeek.Sunday);
			}
			else
			{
				endRange = end.Value.ToDateTime();
			}

			if (completed)
			{
				startRange = Math2.Min(DateTime.UtcNow.AddDays(-1), startRange);
				endRange = Math2.Max(DateTime.UtcNow.AddDays(2), endRange);
			}
			var dateRange = new DateRange(startRange, endRange);

			var output = new ListDataVM(CompassId)
			{
				Name = name,
				date = new AngularDateRange() { startDate = startRange, endDate = endRange },
				dataDateRange = new AngularDateRange() { startDate = startRange, endDate = endRange },

			};

			var dayDateRange = new DateRange(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow);
			var nowDateRange = new DateRange(DateTime.UtcNow, DateTime.UtcNow);


			if (tiles.Any(x => x.Type == TileType.Heading || (x.DataUrl ?? "").Contains("FocusArea")))
			{
				try
				{
					var compTiles = CompassAccessor.GetCompassTiles(caller, CompassId);
					var m = UserAccessor.GetUserOrganization(caller, CompassId, false, true, PermissionType.ViewTodos);
					output.CompassTiles = compTiles.OrderBy(x => x.Y).ThenBy(x => x.X).ToList<CompassTileModel>();

				}
				catch (Exception e)
				{
					ProcessDeadTile(e);
				}
			}


			if (tiles.Any(x => x.Type == TileType.Todo || (x.DataUrl ?? "").Contains("UserTodo")))
			{
				try
				{
					//Todos
					var todos = TodoAccessor.GetMyTodosAndMilestones(caller, CompassId, !completed, dayDateRange/*dateRange*/, includeTodos: true, includeMilestones: false);//.Select(x => new AngularTodo(x));
					var m = UserAccessor.GetUserOrganization(caller, CompassId, false, true, PermissionType.ViewTodos);
					output.Todos = todos.OrderByDescending(x => x.CompleteTime ?? DateTime.MaxValue).ThenBy(x => x.DueDate);
				}
				catch (Exception e)
				{
					ProcessDeadTile(e);
				}
			}
			if (tiles.Any(x => x.Type == TileType.Milestones || (x.DataUrl ?? "").Contains("Milestones")))
			{
				try
				{
					//Milestones
					var milestones = TodoAccessor.GetMyTodosAndMilestones(caller, CompassId, !completed, nowDateRange, includeTodos: false, includeMilestones: true);//.Select(x => new AngularTodo(x));
					var m = UserAccessor.GetUserOrganization(caller, CompassId, false, true, PermissionType.ViewTodos);
					output.Milestones = milestones.OrderByDescending(x => x.CompleteTime ?? DateTime.MaxValue).ThenBy(x => x.DueDate);
				}
				catch (Exception e)
				{
					ProcessDeadTile(e);
				}
			}

			if (tiles.Any(x => x.Type == TileType.Scorecard || (x.DataUrl ?? "").Contains("UserScorecard")))
			{
				var startEnd = "";
				//if (start != null)
				startEnd += "&start=" + startRange.ToJsMs();//start;
															//if (end != null)
				startEnd += "&end=" + endRange.ToJsMs();//end;

				//output.Scorecard = await ScorecardAccessor.GetAngularScorecardForUser(caller, caller.Id, dateRange,true,true,null,false);
				//output.Scorecard.Weeks = null;

				output.LoadUrls.Add(new AngularString(-15291127 * userId, $"/DashboardData/UserScorecardData/{CompassId}?userId={userId}&completed={completed}&fullScorecard={fullScorecard}" + startEnd));
			}

			if (tiles.Any(x => x.Type == TileType.Rocks || (x.DataUrl ?? "").Contains("UserRock")))
			{
				try
				{
					var now = DateTime.UtcNow;
					var rocks = L10Accessor.GetAllMyL10Rocks(caller, caller.Id).Select(x => new AngularRock(x, false));
					output.Rocks = rocks;
				}
				catch (Exception e)
				{
					ProcessDeadTile(e);
				}
			}

			if (tiles.Any(x => x.Type == TileType.Manage || (x.DataUrl ?? "").Contains("UserManage")))
			{
				try
				{
					//var directReports = _OrganizationAccessor.GetOrganizationMembersLookup(caller, caller.Organization.Id, true, PermissionType.EditEmployeeDetails)
					//    .Select(x => AngularUser.CreateUser(x, managing: true)).ToList();
					//var managingIds = DeepAccessor.Users.GetSubordinatesAndSelf(caller, caller.Id);
					//directReports = directReports.Where(x => managingIds.Contains(x.Id)).ToList();
					output.Members = DeepAccessor.Users.GetDirectReportsAndSelfModels(caller, caller.Id).Select(x => AngularUser.CreateUser(x, managing: true));
				}
				catch (Exception e)
				{
					ProcessDeadTile(e);
				}
			}

			if (tiles.Any(x => x.Type == TileType.Roles || (x.DataUrl ?? "").Contains("UserRoles")))
			{
				try
				{
					var roles = _RoleAccessor.GetRoles(caller, caller.Id).Select(x => new AngularRole(x)).ToList();
					output.Roles = roles;
				}
				catch (Exception e)
				{
					ProcessDeadTile(e);
				}
			}

			if (tiles.Any(x => x.Type == TileType.Values || (x.DataUrl ?? "").Contains("OrganizationValues")))
			{
				try
				{
					var values = _OrganizationAccessor.GetCompanyValues(caller, caller.Organization.Id).Select(x => AngularCompanyValue.Create(x)).ToList();
					output.CoreValues = values;
				}
				catch (Exception e)
				{
					ProcessDeadTile(e);
				}
			}

			if (tiles.Any(x => x.Type == TileType.Notifications || (x.DataUrl ?? "").Contains("UserNotifications")))
			{
				try
				{
					var notifications = AngularNotification.Create(PubSub.ListUnseen(caller, caller.Id)).ToList();
					output.Notifications = notifications;
				}
				catch (Exception e)
				{
					ProcessDeadTile(e);
				}
			}



			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var perms = PermissionsUtility.Create(s, caller);

					//if (tiles.Any(x => x.Type == TileType.Tasks || (x.DataUrl ?? "").Contains("Tasks"))) {
					//	try {
					//		var tasks = (await (new ProcessDefAccessor()).GetVisibleTasksForUser(s, perms, caller.Id)).Select(x => AngularTask.Create(x));
					//		output.CoreProcess.Tasks = tasks;
					//	} catch (Exception e) {
					//		ProcessDeadTile(e);
					//	}
					//}

					//if (tiles.Any(x => x.Type == TileType.CoreProcesses || (x.DataUrl ?? "").Contains("CoreProcesses"))) {
					//	try {
					//		var cps = ((new ProcessDefAccessor()).GetVisibleProcessDefinitionList(s, perms, caller.Organization.Id)).Select(x => AngularCoreProcess.Create(x));
					//		output.CoreProcess.Processes = cps;
					//	} catch (Exception e) {
					//		ProcessDeadTile(e);
					//	}
					//}


					var l10Lookup = new DefaultDictionary<long, L10Recurrence>(x => L10Accessor.GetL10Recurrence(s, perms, x, LoadMeeting.False()));

					//L10 Todos
					foreach (var todo in tiles.Where(x => x.Type == TileType.L10Todos || (x.DataUrl ?? "").Contains("L10Todos")).Distinct(x => x.KeyId))
					{
						long l10Id = 0;
						if (long.TryParse(todo.KeyId, out l10Id))
						{
							try
							{
								var tile = new AngularTileId<List<AngularTodo>>(todo.Id, l10Id, l10Lookup[l10Id].Name + " to-dos", AngularTileKeys.L10TodoList(l10Id));
								tile.Contents = L10Accessor.GetAllTodosForRecurrence(s, perms, l10Id, false).Select(x => new AngularTodo(x)).ToList();
								output.L10Todos.Add(tile);
							}
							catch (Exception e)
							{
								output.L10Todos.Add(AngularTileId<List<AngularTodo>>.Error(todo.Id, l10Id, e));
							}
						}
					}

					//L10 Headlines
					foreach (var headlines in tiles.Where(x => x.Type == TileType.Headlines || (x.DataUrl ?? "").Contains("L10Headlines")).Distinct(x => x.KeyId))
					{
						long l10Id = 0;
						if (long.TryParse(headlines.KeyId, out l10Id))
						{
							try
							{
								var tile = new AngularTileId<List<AngularHeadline>>(headlines.Id, l10Id, l10Lookup[l10Id].Name + " headlines", AngularTileKeys.L10HeadlineList(l10Id));
								tile.Contents = L10Accessor.GetAllHeadlinesForRecurrence(s, perms, l10Id, false, null).Select(x => new AngularHeadline(x)).ToList();
								output.L10Headlines.Add(tile);
							}
							catch (Exception e)
							{
								output.L10Todos.Add(AngularTileId<List<AngularTodo>>.Error(headlines.Id, l10Id, e));
							}
						}
					}

					//L10 Issues
					foreach (var issue in tiles.Where(x => x.Type == TileType.L10Issues || (x.DataUrl ?? "").Contains("L10Issues")).Distinct(x => x.KeyId))
					{
						long l10Id = 0;
						if (long.TryParse(issue.KeyId, out l10Id))
						{
							try
							{
								var tile = new AngularTileId<AngularIssuesList>(issue.Id, l10Id, l10Lookup[l10Id].Name + " issues", AngularTileKeys.L10IssuesList(l10Id));
								tile.Contents = new AngularIssuesList(l10Id)
								{
									Issues = L10Accessor.GetIssuesForRecurrence(s, perms, l10Id).Select(x => new AngularIssue(x)).ToList(),
									Prioritization = l10Lookup[l10Id].Prioritization,
								};
								output.L10Issues.Add(tile);
							}
							catch (Exception e)
							{
								output.L10Issues.Add(AngularTileId<AngularIssuesList>.Error(issue.Id, l10Id, e));
							}
						}
					}

					//L10 SOLVED Issues
					foreach (var issue in tiles.Where(x => x.Type == TileType.L10SolvedIssues || (x.DataUrl ?? "").Contains("L10SolvedIssues")).Distinct(x => x.KeyId))
					{
						long l10Id = 0;
						if (long.TryParse(issue.KeyId, out l10Id))
						{
							try
							{
								var tile = new AngularTileId<AngularIssuesSolved>(issue.Id, l10Id, l10Lookup[l10Id].Name + " recently solved issues", AngularTileKeys.L10IssuesSolvedList(l10Id));
								var recent = new DateRange(DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);
								tile.Contents = new AngularIssuesSolved(l10Id)
								{
									Issues = L10Accessor.GetSolvedIssuesForRecurrence(s, perms, l10Id, recent).Select(x => new AngularIssue(x)).ToList(),
									Prioritization = l10Lookup[l10Id].Prioritization,
								};
								output.L10SolvedIssues.Add(tile);
							}
							catch (Exception e)
							{
								output.L10SolvedIssues.Add(AngularTileId<AngularIssuesSolved>.Error(issue.Id, l10Id, e));
							}
						}
					}

					//L10 Rocks
					foreach (var rock in tiles.Where(x => x.Type == TileType.L10Rocks || (x.DataUrl ?? "").Contains("L10Rocks")).Distinct(x => x.KeyId))
					{
						long l10Id = 0;
						if (long.TryParse(rock.KeyId, out l10Id))
						{
							try
							{
								var tile = new AngularTileId<List<AngularRock>>(rock.Id, l10Id, l10Lookup[l10Id].Name + " rocks", AngularTileKeys.L10RocksList(l10Id));
								tile.Contents = L10Accessor.GetRocksForRecurrence(s, perms, l10Id).Select(x => new AngularRock(x.ForRock, x.VtoRock)).ToList();
								output.L10Rocks.Add(tile);
							}
							catch (Exception e)
							{
								output.L10Rocks.Add(AngularTileId<List<AngularRock>>.Error(rock.Id, l10Id, e));
							}
						}
					}

					//L10 Scorecard
					foreach (var scorecard in tiles.Where(x => x.Type == TileType.L10Scorecard || (x.DataUrl ?? "").Contains("L10Scorecard")).Distinct(x => x.KeyId))
					{
						long l10Id = 0;
						if (long.TryParse(scorecard.KeyId, out l10Id))
						{
							try
							{
								var scname = "L10 Scorecard";
								try
								{
									scname = l10Lookup[l10Id].Name;
								}
								catch (Exception)
								{
								}
								var startEnd = "";
								// if (startRange != null)
								startEnd += "&start=" + startRange.ToJsMs();//start;
																			//if (end != null)
								startEnd += "&end=" + endRange.ToJsMs();//end;
																		//random prime
								output.LoadUrls.Add(new AngularString(15291127 * l10Id, $"/DashboardData/L10ScorecardData/{CompassId}?name={scname}&scorecardTileId={scorecard.Id}&l10Id={l10Id}&completed={completed}&fullScorecard={fullScorecard}" + startEnd));
							}
							catch (Exception e)
							{
								output.L10Scorecards.Add(AngularTileId<AngularScorecard>.Error(scorecard.Id, l10Id, e));
							}
						}
					}
				}
			}

			return output;
		}

		public static async Task<ListDataVM> GetCompassTileData(UserOrganizationModel caller, long CompassId, long userId, List<CompassTileModel> tiles, bool completed = false, string name = null, long? start = null, long? end = null, bool fullScorecard = false)
		{
			DateTime startRange;
			DateTime endRange;

			if (start == null)
			{
				startRange = TimingUtility.PeriodsAgo(DateTime.UtcNow, 13, caller.Organization.Settings.ScorecardPeriod);
			}
			else
			{
				startRange = start.Value.ToDateTime();
			}

			if (end == null)
			{
				endRange = DateTime.UtcNow/*.AddDays(14);//*/.StartOfWeek(DayOfWeek.Sunday);
			}
			else
			{
				endRange = end.Value.ToDateTime();
			}

			if (completed)
			{
				startRange = Math2.Min(DateTime.UtcNow.AddDays(-1), startRange);
				endRange = Math2.Max(DateTime.UtcNow.AddDays(2), endRange);
			}
			var dateRange = new DateRange(startRange, endRange);

			var output = new ListDataVM(CompassId)
			{
				Name = name,
				date = new AngularDateRange() { startDate = startRange, endDate = endRange },
				dataDateRange = new AngularDateRange() { startDate = startRange, endDate = endRange },

			};

			var dayDateRange = new DateRange(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow);
			var nowDateRange = new DateRange(DateTime.UtcNow, DateTime.UtcNow);


			if (tiles.Any(x => x.Type == CompassTileType.Heading || (x.DataUrl ?? "").Contains("FocusArea")))
			{
				try
				{
					var compTiles = CompassAccessor.GetCompassTiles(caller, CompassId);
					var m = UserAccessor.GetUserOrganization(caller, 2, false, true, PermissionType.ViewTodos);
					output.CompassTiles = compTiles.OrderBy(x => x.Y).ThenBy(x => x.X).ToList<CompassTileModel>();

				}
				catch (Exception e)
				{
					ProcessDeadTile(e);
				}
			}


			if (tiles.Any(x => x.Type == CompassTileType.Todo || (x.DataUrl ?? "").Contains("UserTodo")))
			{
				try
				{
					//Todos
					var todos = TodoAccessor.GetMyTodosAndMilestones(caller, CompassId, !completed, dayDateRange/*dateRange*/, includeTodos: true, includeMilestones: false);//.Select(x => new AngularTodo(x));
					var m = UserAccessor.GetUserOrganization(caller, CompassId, false, true, PermissionType.ViewTodos);
					output.Todos = todos.OrderByDescending(x => x.CompleteTime ?? DateTime.MaxValue).ThenBy(x => x.DueDate);
				}
				catch (Exception e)
				{
					ProcessDeadTile(e);
				}
			}
			if (tiles.Any(x => x.Type == CompassTileType.Milestones || (x.DataUrl ?? "").Contains("Milestones")))
			{
				try
				{
					//Milestones
					var milestones = TodoAccessor.GetMyTodosAndMilestones(caller, CompassId, !completed, nowDateRange, includeTodos: false, includeMilestones: true);//.Select(x => new AngularTodo(x));
					var m = UserAccessor.GetUserOrganization(caller, CompassId, false, true, PermissionType.ViewTodos);
					output.Milestones = milestones.OrderByDescending(x => x.CompleteTime ?? DateTime.MaxValue).ThenBy(x => x.DueDate);
				}
				catch (Exception e)
				{
					ProcessDeadTile(e);
				}
			}

			if (tiles.Any(x => x.Type == CompassTileType.Scorecard || (x.DataUrl ?? "").Contains("UserScorecard")))
			{
				var startEnd = "";
				//if (start != null)
				startEnd += "&start=" + startRange.ToJsMs();//start;
															//if (end != null)
				startEnd += "&end=" + endRange.ToJsMs();//end;

				//output.Scorecard = await ScorecardAccessor.GetAngularScorecardForUser(caller, caller.Id, dateRange,true,true,null,false);
				//output.Scorecard.Weeks = null;

				output.LoadUrls.Add(new AngularString(-15291127 * userId, $"/DashboardData/UserScorecardData/{CompassId}?userId={userId}&completed={completed}&fullScorecard={fullScorecard}" + startEnd));
			}

			if (tiles.Any(x => x.Type == CompassTileType.Rocks || (x.DataUrl ?? "").Contains("UserRock")))
			{
				try
				{
					var now = DateTime.UtcNow;
					var rocks = L10Accessor.GetAllMyL10Rocks(caller, caller.Id).Select(x => new AngularRock(x, false));
					output.Rocks = rocks;
				}
				catch (Exception e)
				{
					ProcessDeadTile(e);
				}
			}

			if (tiles.Any(x => x.Type == CompassTileType.Manage || (x.DataUrl ?? "").Contains("UserManage")))
			{
				try
				{
					//var directReports = _OrganizationAccessor.GetOrganizationMembersLookup(caller, caller.Organization.Id, true, PermissionType.EditEmployeeDetails)
					//    .Select(x => AngularUser.CreateUser(x, managing: true)).ToList();
					//var managingIds = DeepAccessor.Users.GetSubordinatesAndSelf(caller, caller.Id);
					//directReports = directReports.Where(x => managingIds.Contains(x.Id)).ToList();
					output.Members = DeepAccessor.Users.GetDirectReportsAndSelfModels(caller, caller.Id).Select(x => AngularUser.CreateUser(x, managing: true));
				}
				catch (Exception e)
				{
					ProcessDeadTile(e);
				}
			}

			if (tiles.Any(x => x.Type == CompassTileType.Roles || (x.DataUrl ?? "").Contains("UserRoles")))
			{
				try
				{
					var roles = _RoleAccessor.GetRoles(caller, caller.Id).Select(x => new AngularRole(x)).ToList();
					output.Roles = roles;
				}
				catch (Exception e)
				{
					ProcessDeadTile(e);
				}
			}

			if (tiles.Any(x => x.Type == CompassTileType.Values || (x.DataUrl ?? "").Contains("OrganizationValues")))
			{
				try
				{
					var values = _OrganizationAccessor.GetCompanyValues(caller, caller.Organization.Id).Select(x => AngularCompanyValue.Create(x)).ToList();
					output.CoreValues = values;
				}
				catch (Exception e)
				{
					ProcessDeadTile(e);
				}
			}

			if (tiles.Any(x => x.Type == CompassTileType.Notifications || (x.DataUrl ?? "").Contains("UserNotifications")))
			{
				try
				{
					var notifications = AngularNotification.Create(PubSub.ListUnseen(caller, caller.Id)).ToList();
					output.Notifications = notifications;
				}
				catch (Exception e)
				{
					ProcessDeadTile(e);
				}
			}



			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var perms = PermissionsUtility.Create(s, caller);

					//if (tiles.Any(x => x.Type == TileType.Tasks || (x.DataUrl ?? "").Contains("Tasks"))) {
					//	try {
					//		var tasks = (await (new ProcessDefAccessor()).GetVisibleTasksForUser(s, perms, caller.Id)).Select(x => AngularTask.Create(x));
					//		output.CoreProcess.Tasks = tasks;
					//	} catch (Exception e) {
					//		ProcessDeadTile(e);
					//	}
					//}

					//if (tiles.Any(x => x.Type == TileType.CoreProcesses || (x.DataUrl ?? "").Contains("CoreProcesses"))) {
					//	try {
					//		var cps = ((new ProcessDefAccessor()).GetVisibleProcessDefinitionList(s, perms, caller.Organization.Id)).Select(x => AngularCoreProcess.Create(x));
					//		output.CoreProcess.Processes = cps;
					//	} catch (Exception e) {
					//		ProcessDeadTile(e);
					//	}
					//}


					var l10Lookup = new DefaultDictionary<long, L10Recurrence>(x => L10Accessor.GetL10Recurrence(s, perms, x, LoadMeeting.False()));

					//L10 Todos
					foreach (var todo in tiles.Where(x => x.Type == CompassTileType.L10Todos || (x.DataUrl ?? "").Contains("L10Todos")).Distinct(x => x.KeyId))
					{
						long l10Id = 0;
						if (long.TryParse(todo.KeyId, out l10Id))
						{
							try
							{
								var tile = new AngularTileId<List<AngularTodo>>(todo.Id, l10Id, l10Lookup[l10Id].Name + " to-dos", AngularTileKeys.L10TodoList(l10Id));
								tile.Contents = L10Accessor.GetAllTodosForRecurrence(s, perms, l10Id, false).Select(x => new AngularTodo(x)).ToList();
								output.L10Todos.Add(tile);
							}
							catch (Exception e)
							{
								output.L10Todos.Add(AngularTileId<List<AngularTodo>>.Error(todo.Id, l10Id, e));
							}
						}
					}

					//L10 Headlines
					foreach (var headlines in tiles.Where(x => x.Type == CompassTileType.Headlines || (x.DataUrl ?? "").Contains("L10Headlines")).Distinct(x => x.KeyId))
					{
						long l10Id = 0;
						if (long.TryParse(headlines.KeyId, out l10Id))
						{
							try
							{
								var tile = new AngularTileId<List<AngularHeadline>>(headlines.Id, l10Id, l10Lookup[l10Id].Name + " headlines", AngularTileKeys.L10HeadlineList(l10Id));
								tile.Contents = L10Accessor.GetAllHeadlinesForRecurrence(s, perms, l10Id, false, null).Select(x => new AngularHeadline(x)).ToList();
								output.L10Headlines.Add(tile);
							}
							catch (Exception e)
							{
								output.L10Todos.Add(AngularTileId<List<AngularTodo>>.Error(headlines.Id, l10Id, e));
							}
						}
					}

					//L10 Issues
					foreach (var issue in tiles.Where(x => x.Type == CompassTileType.L10Issues || (x.DataUrl ?? "").Contains("L10Issues")).Distinct(x => x.KeyId))
					{
						long l10Id = 0;
						if (long.TryParse(issue.KeyId, out l10Id))
						{
							try
							{
								var tile = new AngularTileId<AngularIssuesList>(issue.Id, l10Id, l10Lookup[l10Id].Name + " issues", AngularTileKeys.L10IssuesList(l10Id));
								tile.Contents = new AngularIssuesList(l10Id)
								{
									Issues = L10Accessor.GetIssuesForRecurrence(s, perms, l10Id).Select(x => new AngularIssue(x)).ToList(),
									Prioritization = l10Lookup[l10Id].Prioritization,
								};
								output.L10Issues.Add(tile);
							}
							catch (Exception e)
							{
								output.L10Issues.Add(AngularTileId<AngularIssuesList>.Error(issue.Id, l10Id, e));
							}
						}
					}

					//L10 SOLVED Issues
					foreach (var issue in tiles.Where(x => x.Type == CompassTileType.L10SolvedIssues || (x.DataUrl ?? "").Contains("L10SolvedIssues")).Distinct(x => x.KeyId))
					{
						long l10Id = 0;
						if (long.TryParse(issue.KeyId, out l10Id))
						{
							try
							{
								var tile = new AngularTileId<AngularIssuesSolved>(issue.Id, l10Id, l10Lookup[l10Id].Name + " recently solved issues", AngularTileKeys.L10IssuesSolvedList(l10Id));
								var recent = new DateRange(DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);
								tile.Contents = new AngularIssuesSolved(l10Id)
								{
									Issues = L10Accessor.GetSolvedIssuesForRecurrence(s, perms, l10Id, recent).Select(x => new AngularIssue(x)).ToList(),
									Prioritization = l10Lookup[l10Id].Prioritization,
								};
								output.L10SolvedIssues.Add(tile);
							}
							catch (Exception e)
							{
								output.L10SolvedIssues.Add(AngularTileId<AngularIssuesSolved>.Error(issue.Id, l10Id, e));
							}
						}
					}

					//L10 Rocks
					foreach (var rock in tiles.Where(x => x.Type == CompassTileType.L10Rocks || (x.DataUrl ?? "").Contains("L10Rocks")).Distinct(x => x.KeyId))
					{
						long l10Id = 0;
						if (long.TryParse(rock.KeyId, out l10Id))
						{
							try
							{
								var tile = new AngularTileId<List<AngularRock>>(rock.Id, l10Id, l10Lookup[l10Id].Name + " rocks", AngularTileKeys.L10RocksList(l10Id));
								tile.Contents = L10Accessor.GetRocksForRecurrence(s, perms, l10Id).Select(x => new AngularRock(x.ForRock, x.VtoRock)).ToList();
								output.L10Rocks.Add(tile);
							}
							catch (Exception e)
							{
								output.L10Rocks.Add(AngularTileId<List<AngularRock>>.Error(rock.Id, l10Id, e));
							}
						}
					}

					//L10 Scorecard
					foreach (var scorecard in tiles.Where(x => x.Type == CompassTileType.L10Scorecard || (x.DataUrl ?? "").Contains("L10Scorecard")).Distinct(x => x.KeyId))
					{
						long l10Id = 0;
						if (long.TryParse(scorecard.KeyId, out l10Id))
						{
							try
							{
								var scname = "L10 Scorecard";
								try
								{
									scname = l10Lookup[l10Id].Name;
								}
								catch (Exception)
								{
								}
								var startEnd = "";
								// if (startRange != null)
								startEnd += "&start=" + startRange.ToJsMs();//start;
																			//if (end != null)
								startEnd += "&end=" + endRange.ToJsMs();//end;
																		//random prime
								output.LoadUrls.Add(new AngularString(15291127 * l10Id, $"/DashboardData/L10ScorecardData/{CompassId}?name={scname}&scorecardTileId={scorecard.Id}&l10Id={l10Id}&completed={completed}&fullScorecard={fullScorecard}" + startEnd));
							}
							catch (Exception e)
							{
								output.L10Scorecards.Add(AngularTileId<AngularScorecard>.Error(scorecard.Id, l10Id, e));
							}
						}
					}
				}
			}

			return output;
		}

		[Access(AccessLevel.UserOrganization)]
		[OutputCache(NoStore = true, Duration = 0)]
		public async Task<JsonResult> L10ScorecardData(long id, string name, long scorecardTileId, long l10Id, bool completed = false, bool fullScorecard = false, long? start = null, long? end = null)
		{
			DateTime startRange;
			DateTime endRange;

			if (start == null)
			{
				startRange = TimingUtility.PeriodsAgo(DateTime.UtcNow, 13, GetUser().Organization.Settings.ScorecardPeriod);
			}
			else
			{
				startRange = start.Value.ToDateTime().AddDays(-8);
			}

			if (end == null)
			{
				endRange = DateTime.UtcNow.AddDays(14);
			}
			else
			{
				endRange = end.Value.ToDateTime().AddDays(8);
			}

			if (completed)
			{
				startRange = Math2.Min(DateTime.UtcNow.AddDays(-1), startRange);
				endRange = Math2.Max(DateTime.UtcNow.AddDays(8), endRange);
			}
			var dateRange = new DateRange(startRange, endRange);

			var output = new ListDataVM(id)
			{
				date = new AngularDateRange() { startDate = startRange, endDate = endRange }
			};
			try
			{
				var tile = new AngularTileId<AngularScorecard>(scorecardTileId, l10Id, name + " scorecard", AngularTileKeys.L10Scorecard(l10Id));
				using (var s = HibernateSession.GetCurrentSession())
				{
					using (var tx = s.BeginTransaction())
					{
						var perms = PermissionsUtility.Create(s, GetUser());
						var scoredata = await L10Accessor.GetOrGenerateScorecardDataForRecurrence(s, perms, l10Id, includeAutoGenerated: false, range: dateRange, getMeasurables: true);
						var scores = scoredata.Scores;
						var measurables = scoredata.Measurables;

						tx.Commit();
						s.Flush();

						// var orders = L10Accessor.GetMeasurableOrdering(GetUser(), l10Id);
						// var ts = GetUser().GetTimeSettings();
						//var recur = L10Accessor.GetL10Recurrence(GetUser(), l10Id, false);
						// ts.WeekStart = recur.StartOfWeekOverride ?? ts.WeekStart;
						tile.Contents = AngularScorecard.Create(/*scorecardTileId*/l10Id, scoredata.TimeSettings,
							scoredata.MeasurablesAndDividers,
							scores.ToList(), DateTime.UtcNow, reverseScorecard: scoredata.TimeSettings.Descending);

						//if (scoredata.TimeSettings.Period == ScorecardPeriod.Monthly || scoredata.TimeSettings.Period == ScorecardPeriod.Quarterly) {
						//	output.date = new AngularDateRange() {
						//		startDate = Math2.Min(TimingUtility.PeriodsAgo(DateTime.UtcNow, 13, GetUser().Organization.Settings.ScorecardPeriod), startRange),
						//		endDate = endRange
						//	};
						//}

						output.L10Scorecards.Add(tile);
					}
				}
			}
			catch (Exception e)
			{
				output.L10Scorecards.Add(AngularTileId<AngularScorecard>.Error(scorecardTileId, l10Id, e));
			}

			return Json(output, JsonRequestBehavior.AllowGet);
		}

		[Access(AccessLevel.UserOrganization)]
		[OutputCache(NoStore = true, Duration = 0)]
		public async Task<JsonResult> UserScorecardData(long id, long userId, bool completed = false, bool fullScorecard = false, long? start = null, long? end = null)
		{
			DateTime startRange;
			DateTime endRange;

			if (start == null)
			{
				startRange = TimingUtility.PeriodsAgo(DateTime.UtcNow, 13, GetUser().Organization.Settings.ScorecardPeriod);
			}
			else
			{
				startRange = start.Value.ToDateTime();
			}

			if (end == null)
			{
				endRange = DateTime.UtcNow.AddDays(14);
			}
			else
			{
				endRange = end.Value.ToDateTime();
			}

			if (completed)
			{
				startRange = Math2.Min(DateTime.UtcNow.AddDays(-1), startRange);
				endRange = Math2.Max(DateTime.UtcNow.AddDays(2), endRange);
			}
			var dateRange = new DateRange(startRange, endRange);

			var output = new ListDataVM(id)
			{
				date = new AngularDateRange() { startDate = startRange, endDate = endRange }
			};
			try
			{//Scorecard
				var scorecardStart = fullScorecard ? TimingUtility.PeriodsAgo(DateTime.UtcNow, 13, GetUser().Organization.Settings.ScorecardPeriod) : startRange;
				var scorecardEnd = fullScorecard ? DateTime.UtcNow.AddDays(14) : endRange;
				output.Scorecard = await ScorecardAccessor.GetAngularScorecardForUser(GetUser(), userId, new DateRange(scorecardStart, scorecardEnd), true, now: DateTime.UtcNow);
				output.Scorecard.ReverseScorecard = GetUser().NotNull(x => x.User.ReverseScorecard);

			}
			catch (Exception e)
			{
				ProcessDeadTile(e);
			}
			return Json(output, JsonRequestBehavior.AllowGet);
		}
	}

	public class CompassController : BaseController
	{
		public class CompassVM
		{
			public string Title { get; set; }
			public long CompassId { get; set; }
			public long CompassModelId { get; set; }
			public CompassType CompassModelType { get; set; }
			public String TileJson { get; set; }
			public List<L10> L10s { get; set; }
			public List<string> TileUrls { get; set; }

			public List<SelectListItem> Compasses { get; set; }
			public List<string> DataUrls { get; set; }

			public bool ShowV2 { get; set; }
			public bool ShowMigrateV2 { get; set; }

			public class L10
			{
				public DateTime StarDate { get; set; }
				public bool Selected { get; set; }
				public string Text { get; set; }
				public string Value { get; set; }
				public List<SelectListItem> Notes { get; set; }
				public L10()
				{
					Notes = new List<SelectListItem>();
				}
			}

			public CompassVM()
			{
				L10s = new List<L10>();
				Compasses = new List<SelectListItem>();
			}
		}

		public class CompassTileVM
		{
			public string title { get; set; }
			public string detail { get; set; }
			public int w { get; set; }
			public int h { get; set; }
			public int x { get; set; }
			public int y { get; set; }
			public long id { get; set; }
		}

		[Access(AccessLevel.Radial)]
		public JsonResult TestTile()
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var t = s.Get<TileModel>(1L);
					return Json(t, JsonRequestBehavior.AllowGet);
				}
			}
		}
		[Access(AccessLevel.Radial)]
		public JsonResult TestCompassUser()
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					//var t = s.Get<TileModel>(1L);
					return Json(GetUser(), JsonRequestBehavior.AllowGet);
				}
			}
		}

		[Access(AccessLevel.UserOrganization)]
		public JsonResult Tiles(long id)
		{
			var CompassId = id;
			var tiles = CompassAccessor.GetCompassTiles(GetUser(), CompassId);
			return Json(ResultObject.SilentSuccess(tiles), JsonRequestBehavior.AllowGet);
		}
		[Access(AccessLevel.UserOrganization)]
		[HttpPost]
		public JsonResult UpdateTiles(long id, IEnumerable<CompassTileVM> model)
		{
			var CompassId = id;
			CompassAccessor.EditCompassTiles(GetUser(), CompassId, model);
			return Json(ResultObject.SilentSuccess());
		}



		[Access(AccessLevel.UserOrganization)]
		public JsonResult Tile(long id, bool? hidden = null, int? w = null, int? h = null, int? x = null, int? y = null, string dataurl = null, string title = null)
		{
			var tile = CompassAccessor.EditCompassTile(GetUser(), id, h, w, x, y, hidden, dataurl, title);
			tile.ForUser = null;
			tile.Compass = null;
			return Json(ResultObject.SilentSuccess(tile), JsonRequestBehavior.AllowGet);
		}

		[Access(AccessLevel.UserOrganization)]
		public JsonResult CreateCompass(string title = null, bool primary = false, bool prepopulate = false)
		{
			var dash = CompassAccessor.CreateCompass(GetUser(), title, primary, prepopulate);
			return Json(ResultObject.SilentSuccess(dash.Id));
		}

		[Access(AccessLevel.UserOrganization)]
		public JsonResult EditCompass(long id, string title, bool delete = false)
		{
			if (delete == true)
			{
				CompassAccessor.DeleteCompass(GetUser(), id);
				return Json(ResultObject.SilentSuccess(new { deleted = true }), JsonRequestBehavior.AllowGet);
			}
			CompassAccessor.RenameCompass(GetUser(), id, title);
			return Json(ResultObject.SilentSuccess(new { title = title }), JsonRequestBehavior.AllowGet);
		}

		[Access(AccessLevel.UserOrganization)]
		public PartialViewResult CreateTileModal()
		{
			return PartialView();
		}

		[Access(AccessLevel.UserOrganization)]
		public JsonResult UpdateCompassTile(long id, bool? hidden = null, int w = 2, int h = 5, int x = 0, int y = 0, CompassTileType type = CompassTileType.Heading, string dataurl = null, string title = null, string detail = null, string keyId = null)
		{
			//using (var s = HibernateSession.GetCurrentSession()) { y = CompassAccessor.SetYPosition(s, id); }
			var tile = CompassAccessor.UpdateCompassTile(GetUser(), id, dataurl, title, detail, type, null);//(GetUser(), id, w, h, x, y, dataurl, title, detail, type, keyId);
			tile.ForUser = null;
			tile.Compass = null;
			return Json(ResultObject.SilentSuccess(tile), JsonRequestBehavior.AllowGet);
		}

		[Access(AccessLevel.UserOrganization)]
		public JsonResult CreateCompassTile(long id, bool? hidden = null, int w = 2, int h = 5, int x = 0, int y = 0, CompassTileType type = CompassTileType.Heading, string dataurl = null, string title = null, string detail=null, string keyId = null) { 
			using (var s = HibernateSession.GetCurrentSession()){ y = CompassAccessor.SetYPosition(s, id); }
			var tile = CompassAccessor.CreateCompassTile(GetUser(), id, w, h, x, y, dataurl, title,detail, type, keyId);
			tile.ForUser = null;
			tile.Compass = null;
			return Json(ResultObject.SilentSuccess(tile), JsonRequestBehavior.AllowGet);
		}

		private void ShowCreateCompass()
		{
			if (ViewBag.WorkspaceDropdown is CompassDropdownVM)
			{
				var wd = ViewBag.WorkspaceDropdown as CompassDropdownVM;
				wd.DisplayCreate = true;
			}
		}

		[Access(AccessLevel.UserOrganization)]
		public async Task<ActionResult> Index(long? id = null)
		{

			ShowCreateCompass();

			var useDefault = id == null;
			UserOrganizationModel.PrimaryCompassModel pcomp;

			if (id == null)
			{
				//Use default workspace
				ViewBag.AddAgileCrmTracker = true;
				pcomp = CompassAccessor.GetHomeCompassForUser(GetUser(), GetUser().Id);
				switch (pcomp.Type)
				{
					case CompassType.Standard:
						id = pcomp.CompassId;
						break;
					case CompassType.L10:
						return await Generate(pcomp.CompassId, pcomp.Type);
						break;
					//case DashboardType.Compass:
					//	return await Generate(pwm.WorkspaceId, pwm.Type);
					//	break;
					default:
						throw new PermissionsException("Unhandled workspace type");
				}
			}
			else if (id == 0)
			{
				//create it 
				id = CompassAccessor.CreateCompass(GetUser(), null, false, true).Id;
				return RedirectToAction("Index", new { id = id });
			}
			else
			{
				//Use specified workspace
				pcomp = new UserOrganizationModel.PrimaryCompassModel()
				{
					Type = CompassType.Standard,
					CompassId = id.Value
				};
			}

			var Tiles = CompassAccessor.GetCompassTiles(GetUser(), id.Value);
			foreach (var item in Tiles)
			{
				if (item.DataUrl.Contains("L10Todos"))
				{
					item.ShowPrintButton = true;
				}
			}
			CompassVM compass = await GenerateCompassViewModel(id, pcomp, useDefault, Tiles);
			return View(compass);
		}

		private async Task<CompassVM> GenerateCompassViewModel(long? id, UserOrganizationModel.PrimaryCompassModel primaryCompass, bool useDefault, List<CompassTileModel> tiles, string compassName = null)
		{
			var l10s = L10Accessor.GetVisibleL10Meetings_Tiny(GetUser(), GetUser().Id, onlyDashboardRecurrences: true);
			var notes = L10Accessor.GetVisibleL10Notes_Unsafe(l10s.Select(x => x.Id).ToList());

			var starred = (await L10Accessor.GetStarredRecurrences(GetUser(), GetUser().Id)).ToDefaultDictionary(x => x.RecurrenceId, x => x.StarDate, x => DateTime.MaxValue);

			var jsonTiles = Json(ResultObject.SilentSuccess(tiles), JsonRequestBehavior.AllowGet);
			var jsonTilesStr = new JavaScriptSerializer().Serialize(jsonTiles.Data);
			var tileUrls = tiles.Select(x => x.DataUrl).ToList();

			ViewBag.UserId = GetUser().Id;

			var compass = new CompassVM()
			{
				CompassId = id.Value,
				CompassModelId = primaryCompass.CompassId,
				CompassModelType = primaryCompass.Type,
				TileJson = jsonTilesStr,
				TileUrls = tileUrls,
				L10s = l10s.Select(x => new CompassVM.L10()
				{
					StarDate = starred[x.Id],
					Value = "" + x.Id,
					Text = x.Name,
					Notes = notes.Where(y => y.Recurrence.Id == x.Id).Select(z => new SelectListItem()
					{
						Text = z.Name,
						Value = "" + z.Id
					}).ToList()
				}).ToList()
			};



			var allCompasses = CompassAccessor.GetCompassesForUser(GetUser(), GetUser().Id);

			compass.Compasses = allCompasses
				.OrderByDescending(x => x.PrimaryCompass)
				.Select(x => new SelectListItem()
				{
					Selected = x.PrimaryCompass,
					Text = string.IsNullOrWhiteSpace(x.Title) ? "Default Compass" : x.Title,
					Value = "" + x.Id
				}).ToList();

			if (!useDefault)
			{
				ViewBag.WorkspaceName = compass.Compasses.FirstOrDefault(x => x.Value == "" + id).NotNull(x => x.Text);
			}
			ViewBag.CompassName = compassName ?? ViewBag.CompassName;

			var v2Status = GetUser().Cache.V2StatusBar;
			compass.ShowV2 = v2Status == Models.UserModels.V2StatusBar.ShowAllowSignup || v2Status == Models.UserModels.V2StatusBar.ShowDoNotAllowSignup;
			compass.ShowMigrateV2 = v2Status == Models.UserModels.V2StatusBar.ShowAllowSignup;

			return compass;
		}

		[Access(AccessLevel.UserOrganization)]
		public async Task<ActionResult> Generate(long id, CompassType type, int? w = null)
		{

			ShowCreateCompass();
			try
			{
				var o = CompassAccessor.GenerateCompass(GetUser(), id, type, w);

				var pcomp = new UserOrganizationModel.PrimaryCompassModel()
				{
					CompassId = id,
					Type = type
				};

				var compass = await GenerateCompassViewModel(o.Compass.Id, pcomp, false, o.Tiles, o.Compass.Title);

				var jsonTiles = Json(await CompassDataController.GetCompassTileData(GetUser(), o.Compass.Id, GetUser().Id, o.Tiles), JsonRequestBehavior.AllowGet);
				var jsonTilesStr = new JavaScriptSerializer().Serialize(jsonTiles.Data);


				ViewBag.InitialModel = jsonTilesStr;
				ViewBag.DisableEditTiles = true;
				//if (type == CompassType.L10) {
				//	ViewBag.CurrentRecurrenceId = id;
				//}

				return View("Index", compass);
			}
			catch (PermissionsException)
			{
				throw new PermissionsException("You can no longer view this compass.");
			}
		}

		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> SetHome(long id, CompassType type)
		{
			await CompassAccessor.SetHomeCompass(GetUser(), GetUser().Id, type, id);
			return Json(ResultObject.Success("Set as primary compass"), JsonRequestBehavior.AllowGet);
		}


		[Access(AccessLevel.UserOrganization)]
		public async Task<PartialViewResult> Dropdown()
		{
			var user = GetUser();
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var perms = PermissionsUtility.Create(s, user);
					var WorkspaceDropdown = DashboardAccessor.GetWorkspaceDropdown(s, perms, user.Id);
					WorkspaceDropdown.DisplayCreate = true;
					return PartialView(WorkspaceDropdown);
				}
			}
		}

		[Access(AccessLevel.UserOrganization)]
		public async Task<PartialViewResult> CompassDd()
		{
			var user = GetUser();
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var perms = PermissionsUtility.Create(s, user);
					var CompassDropdown = CompassAccessor.GetCompassDropdown(s, perms, user.Id);
					CompassDropdown.DisplayCreate = true;
					return PartialView(CompassDropdown);
				}
			}
		}
	}
}