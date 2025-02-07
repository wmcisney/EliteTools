using Newtonsoft.Json;
using RadialReview.Accessors;
using RadialReview.Accessors.PDF;
using RadialReview.Exceptions;
using RadialReview.Exceptions.MeetingExceptions;
using RadialReview.Models;
using RadialReview.Models.Enums;
using RadialReview.Models.Json;
using RadialReview.Models.L10;
using RadialReview.Models.L10.VM;
using RadialReview.Models.Timeline;
using RadialReview.Models.UserModels;
using RadialReview.Models.VideoConference;
using RadialReview.Models.ViewModels;
using RadialReview.Utilities;
using RadialReview.Utilities.Pdf;
using RadialReview.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using static RadialReview.Models.PermItem;
using static RadialReview.Utilities.SelectExistingOrCreateUtility;

namespace RadialReview.Controllers {
	public partial class L10Controller : BaseController {


		// GET: L10
		[Access(AccessLevel.UserOrganization)]
		public ActionResult Index() {
			var recurrences = L10Accessor.GetVisibleL10RecurrencesVM(GetUser(), GetUser().Id, true)
				.OrderByDescending(x => x.IsAttendee)
				.OrderByDescending(x => x.Recurrence.MeetingInProgress)
				.ToList();

			var model = new L10ListingVM() {
				Recurrences = recurrences,
			};
			return View(model);
		}

		// GET: L10

		[Access(AccessLevel.UserOrganization)]
		public ActionResult Meeting(long? id = null) {
			if (id == null) {
				return Content("Error: url requires a meeting Id");
			}

			var recurrenceId = id.Value;
			var recurrence = L10Accessor.GetL10Recurrence(GetUser(), recurrenceId, new LoadMeeting {
				LoadMeasurables = false,
				LoadRocks = false,
				LoadUsers = true,
				LoadVideos = true,
				LoadNotes = true,
				LoadPages = true,
			});

			ViewBag.VideoChatRoom = new VideoConferenceVM() {
				RoomId = recurrence.VideoId,
				CurrentProviders = recurrence._VideoConferenceProviders.Select(x => x.Provider).ToList(),
				Selected = recurrence.SelectedVideoProvider,
			};

			ViewBag.ViewAccountabilityChart = PermissionsAccessor.IsPermitted(GetUser(), x => x.CanView(ResourceType.AccountabilityHierarchy, GetUser().Organization.AccountabilityChartId));
			ViewBag.ViewPeopleAnalyzer = GetUser().Organization.Settings.EnablePeople;

			if (PaymentAccessor.ShowDelinquent(GetUser(), GetUser().Organization.Id, 7)) {
				//REPLACE_ME
				var dflt = "Your free trial of EliteTools is over. <br/>" +
							"<u><a href='#' class='todoModal' data-method='CreateTodoRecurrence' data-context='L10' data-recurrence='{0}' data-todo='Enter payment information into EliteTools' data-details='Go to https://dlptools.com/Manage/Payment and add a payment method. Contact support@dlptools.com with questions.'>Take a to-do</a></u> " +
							"to enter your payment information?";
				var msg = VariableAccessor.Get<string>(Variable.Names.DELINQUENT_MESSAGE_MEETING, () => dflt);
				msg = msg.Replace("{0}", "" + id);
				if (!string.IsNullOrWhiteSpace(msg)) {
					ViewBag.ShowDelinquentMessage = msg;
				}
			}


			var model = new L10MeetingVM() {
				ForOrganizationId = recurrence.OrganizationId,
				Recurrence = recurrence,
				Meeting = L10Accessor.GetCurrentL10Meeting(GetUser(), recurrenceId, true, loadLogs: true),
				EnableTranscript = recurrence.EnableTranscription,
			};


			if (model.Meeting != null) {
				model.MemberPictures = recurrence._DefaultAttendees.Select(x => new ProfilePictureVM { UserId = x.User.Id, Url = x.User.ImageUrl(true, ImageSize._32), Name = x.User.GetName(), Initials = x.User.GetInitials() }).ToList();
				if (model.EnableTranscript) {
					var transcript = TranscriptAccessor.GetMeetingTranscript(GetUser(), model.Meeting.Id);
					model.CurrentTranscript = transcript.Select(x => new MeetingTranscriptVM() {
						Id = x.Id,
						Message = x.Text,
						Order = x.CreateTime.ToJavascriptMilliseconds(),
						Owner = x._User.GetName()
					}).ToList();
				}
			}

			if (model != null && model.Recurrence != null) {
				model.CanAdmin = PermissionsAccessor.IsPermitted(GetUser(), x => x.CanAdmin(PermItem.ResourceType.L10Recurrence, model.Recurrence.Id));
				model.CanEdit = PermissionsAccessor.IsPermitted(GetUser(), x => x.CanEdit(PermItem.ResourceType.L10Recurrence, model.Recurrence.Id));
				model.VtoId = model.Recurrence.VtoId;

				model.Connected = L10Accessor.GetConnected(GetUser(), id.Value, true);

			}


			try {
				var me = model.Recurrence.NotNull(x => x._DefaultAttendees.FirstOrDefault(y => y.User.Id == GetUser().Id));
				model.SharingPeopleAnalyzer = me.SharePeopleAnalyzer == L10Recurrence.SharePeopleAnalyzer.Yes;
			} catch (Exception) {
			}

			return View(model);
		}

		[Access(AccessLevel.UserOrganization)]
		public ActionResult Create() {
			var m = new L10Recurrence();
			var model = new L10EditVM();


			AddExtras(0, model);
			ViewBag.InfoAlert = "You can use the same L10 meeting each week. No need to create a new on each week.";

			return View("Edit", model);
		}

		[Access(AccessLevel.UserOrganization)]
		public async Task<ActionResult> Delete(long id) {
			var getRecurrence = await L10Accessor.GetOrGenerateAngularRecurrence(GetUser(), id);
			return PartialView(getRecurrence);
		}

		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> Delete(long id, FormCollection model) {

			var rocksKeys = model.AllKeys.Where(x => x.StartsWith("rocks_"));
			var rocksIds = rocksKeys.Where(x => model[x].ToBooleanJS()).Select(x => long.Parse(x.Replace("rocks_", ""))).ToList();

			var measurableKeys = model.AllKeys.Where(x => x.StartsWith("measurables_"));
			var measurableIds = measurableKeys.Where(x => model[x].ToBooleanJS()).Select(x => long.Parse(x.Replace("measurables_", ""))).ToList();

			var getRecurrence = await L10Accessor.GetOrGenerateAngularRecurrence(GetUser(), id);

			var rocks = getRecurrence.Rocks.Where(t => rocksIds.Contains(t.Id));
			//Archive Rocks
			foreach (var item in rocks) {
				await L10Accessor.Remove(GetUser(), item, id);
			}

			var measurables = getRecurrence.Scorecard.Measurables.Where(t => measurableIds.Contains(t.Id));
			//Archive Measurables
			foreach (var item in measurables) {
				await L10Accessor.Remove(GetUser(), item, id);
			}

			await L10Accessor.DeleteL10Recurrence(GetUser(), id);
			return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
		}

		[Access(AccessLevel.Radial)]
		public async Task<JsonResult> Undelete(long id) {
			await L10Accessor.UndeleteL10Recurrence(GetUser(), id);
			return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
		}

		private void AddUnstored(long recurrenceId, L10EditVM model) {
			ViewBag.VtoSharable = L10Accessor.IsVtoSharable(GetUser(), recurrenceId);

		}

		private L10EditVM AddExtras(long recurrenceId, L10EditVM model) {
			var allMeasurables = ScorecardAccessor.GetVisibleMeasurables(GetUser(), GetUser().Organization.Id, true);
			var allMembers = _OrganizationAccessor.GetOrganizationMembers(GetUser(), GetUser().Organization.Id, false, false);
			var allRocks = RockAccessor.GetAllVisibleRocksAtOrganization(GetUser(), GetUser().Organization.Id, true);
			model.PossibleMeasurables = allMeasurables.Where(x => x != null).ToList();
			model.PossibleMembers = allMembers;
			model.PossibleRocks = allRocks;

			if (recurrenceId != 0) {
				var r = L10Accessor.GetL10Recurrence(GetUser(), recurrenceId, LoadMeeting.True());
				allRocks.AddRange(r._DefaultRocks.Where(x => x.Id > 0 && allRocks.All(y => y.Id != x.ForRock.Id)).Select(x => x.ForRock));
				allMeasurables.AddRange(r._DefaultMeasurables.Where(x => x.Id > 0 && allMeasurables.All(y => y != null && y.Id != x.Measurable.NotNull(z => z.Id))).Select(x => x.Measurable));
				model.Recurrence = r;
				model.SelectedMeasurables = model.SelectedMeasurables ?? r._DefaultMeasurables.Where(x => x.Measurable != null).Select(x => x.Measurable.Id).ToArray();
				model.SelectedMembers = model.SelectedMembers ?? r._DefaultAttendees.Select(x => x.User.Id).ToArray();
				model.SelectedRocks = model.SelectedRocks ?? r._DefaultRocks.Select(x => x.ForRock.Id).ToArray();
			} else {
				model.Recurrence = model.Recurrence ?? new L10Recurrence() {
					CreateTime = DateTime.UtcNow,
					OrganizationId = GetUser().Organization.Id,
					VideoId = Guid.NewGuid().ToString(),
					EnableTranscription = false,
					HeadlinesId = Guid.NewGuid().ToString(),
					CountDown = true,
				};

				model.SelectedMeasurables = model.SelectedMeasurables ?? new long[0];
				model.SelectedMembers = model.SelectedMembers ?? new long[0];
				model.SelectedRocks = model.SelectedRocks ?? new long[0];
			}
			AddUnstored(recurrenceId, model);
			return model;
		}

		[Access(AccessLevel.UserOrganization)]
		public ActionResult Edit(long? id = null, string @return = null, bool noheading = false) {
			if (id == null) {
				return RedirectToAction("Wizard", new { @return = @return });
			}

			var recurrenceId = id.Value;

			PermissionsAccessor.Permitted(GetUser(), x => x.CanAdmin(PermItem.ResourceType.L10Recurrence, recurrenceId));

			var model = AddExtras(recurrenceId, new L10EditVM() { Return = @return });
			//ViewBag.VtoSharable = L10Accessor.IsVtoSharable(GetUser(), id);
			ViewBag.NoTitleBar = noheading;
			return View("Edit", model);
		}

		[Access(AccessLevel.UserOrganization)]
		public async Task<ActionResult> Pages(long id, bool noheading = false) {
			ViewBag.NoTitleBar = noheading;
			var now = DateTime.UtcNow.ToJavascriptMilliseconds();
			var model = L10Accessor.GetL10Recurrence(GetUser(), id, LoadMeeting.True());
			return View(model);
		}

		[Access(AccessLevel.UserOrganization)]
		public async Task<ActionResult> Wizard(long? id = null, string @return = null, MeetingType? type = null, bool noheading = false) {
			ViewBag.NoTitleBar = noheading;
			if (id == null) {
				//var m = new L10Recurrence();
				//var model = new L10EditVM();
				//AddExtras(0, model);
				//ViewBag.InfoAlert = "You can use the same L10 meeting each week. No need to create a new on each week.";

				var l10 = await L10Accessor.CreateBlankRecurrence(GetUser(), GetUser().Organization.Id, true, type ?? MeetingType.L10);
				return RedirectToAction("Wizard", new { id = l10.Id, tname = Request["tname"], tmethod = Request["tmethod"] });
			} else {
				//var recurrenceId = id.Value;
				PermissionsAccessor.Permitted(GetUser(), x => x.CanView(PermItem.ResourceType.L10Recurrence, id.Value));

				ViewBag.CanEdit = PermissionsAccessor.IsPermitted(GetUser(), x => x.CanEdit(PermItem.ResourceType.L10Recurrence, id.Value));
				ViewBag.CanAdmin = PermissionsAccessor.IsPermitted(GetUser(), x => x.CanAdmin(PermItem.ResourceType.L10Recurrence, id.Value));

				var now = DateTime.UtcNow.ToJavascriptMilliseconds();
				try {
					var initModel = (await DetailsData(id.Value, false, false, now, now, true, removeWeeks: true)).Data;
					ViewBag.InitialModel = new HtmlString(JsonConvert.SerializeObject(initModel));
				} catch (Exception ex) {
					int a = 0;
				}

				//var model = AddExtras(recurrenceId, new L10EditVM() { Return = @return });
				return View("Wizard", id.Value);
			}
		}

		[Access(AccessLevel.UserOrganization)]
		public async Task<ActionResult> MeetingSummarySettings(long id, bool noheading = false) {
			ViewBag.NoTitleBar = noheading;
			var settings = await L10Accessor.GetMeetingSummarySettings(GetUser(), id);
			return View(settings);
		}


		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public async Task<ActionResult> Edit(L10EditVM model) {
			//ValidateValues(model,
			//    x => x.Recurrence.Id,
			//    x => x.Recurrence.CreateTime,
			//    x => x.Recurrence.OrganizationId,
			//    x => x.Recurrence.MeetingInProgress,
			//    x => x.Recurrence.CreatedById,
			//    x => x.Recurrence.VideoId,
			//    x => x.Recurrence.HeadlinesId,
			//    x => x.Recurrence.OrderIssueBy,
			//    x => x.Recurrence.VtoId);

			if (model.Recurrence == null) {
				throw new PermissionsException("Recurrence was null");
			}

			if (String.IsNullOrWhiteSpace(model.Recurrence.Name)) {
				ModelState.AddModelError("Name", "Meeting name is required");
				// AddExtras(model.Recurrence.Id, model);
				//  return View("Edit", model);
			}
			if (model.SelectedMembers == null || model.SelectedMembers.Length == 0) {
				ModelState.AddModelError("PossibleMembers", "At least one attendee is required");
			}

			var allRocks = RockAccessor.GetAllVisibleRocksAtOrganization(GetUser(), GetUser().Organization.Id, true);
			var allMeasurables = ScorecardAccessor.GetVisibleMeasurables(GetUser(), GetUser().Organization.Id, true);
			var allMembers = _OrganizationAccessor.GetOrganizationMembers(GetUser(), GetUser().Organization.Id, false, false);
			model.SelectedRocks = model.SelectedRocks ?? new long[0];
			model.SelectedMembers = model.SelectedMembers ?? new long[0];
			model.SelectedMeasurables = model.SelectedMeasurables ?? new long[0];

			if (model.Recurrence.Id != 0) {
				var existing = L10Accessor.GetL10Recurrence(GetUser(), model.Recurrence.Id, LoadMeeting.True());
				allRocks.AddRange(existing._DefaultRocks.Where(x => x.Id > 0 && allRocks.All(y => y.Id != x.ForRock.Id)).Select(x => x.ForRock));
				allMeasurables.AddRange(existing._DefaultMeasurables.Where(x => x.Id > 0 && allMeasurables.All(y => y != null && y.Id != x.Measurable.NotNull(z => z.Id))).Select(x => x.Measurable));
			} else {
				PermissionsAccessor.Permitted(GetUser(), x => x.CreateL10Recurrence(model.Recurrence.OrganizationId));
				ViewBag.InfoAlert = "You only need to create one L10 meeting per weekly meeting. In other words, you don't need to create a new L10 each week.";
			}
			if (ModelState.IsValid) {
				model.Recurrence.OrganizationId = GetUser().Organization.Id;
				model.Recurrence._DefaultAttendees = allMembers.Where(x => model.SelectedMembers.Any(y => y == x.Id))
				.Select(x => new L10Recurrence.L10Recurrence_Attendee() {
					L10Recurrence = model.Recurrence,
					User = x
				}).ToList();
				//model.SelectedMeasurables=model.SelectedMeasurables??new long[0]{};
				model.Recurrence._DefaultMeasurables = allMeasurables.Where(x => model.SelectedMeasurables.Any(y => x != null && y == x.Id))
				.Select(x => new L10Recurrence.L10Recurrence_Measurable() {
					L10Recurrence = model.Recurrence,
					Measurable = x
				}).ToList();

				//model.SelectedRocks = model.SelectedRocks ?? new long[0] { };
				model.Recurrence._DefaultRocks = allRocks.Where(x => model.SelectedRocks.Any(y => y == x.Id))
					.Select(x => new L10Recurrence.L10Recurrence_Rocks() {
						L10Recurrence = model.Recurrence,
						ForRock = x
					}).ToList();



				await L10Accessor.EditL10Recurrence(GetUser(), model.Recurrence);


				if (model.Return == "meeting") {
					return RedirectToAction("meeting", new { id = model.Recurrence.Id });
				}

				return RedirectToAction("Index");
				//this will get the page out of that iframe
				//string url = "/L10";
				//Response.Write("<script>top.location='" + url + "';parent.location='" + url + "';</script>");                
			}

			model.PossibleRocks = allRocks;
			model.PossibleMembers = allMembers;
			model.PossibleMeasurables = allMeasurables.Where(x => x != null).ToList();

			if (model.Recurrence.Id != 0) {
				AddUnstored(model.Recurrence.Id, model);
			}
			//model.SelectedRocks = model.SelectedRocks ?? new long[0];
			//model.SelectedMembers = model.SelectedMembers ?? new long[0];
			//model.SelectedMeasurables = model.SelectedMeasurables ?? new long[0];

			return View("Edit", model);
		}

		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> Star(long id, bool star = true) {
			await L10Accessor.AddStarToMeeting(GetUser(), id, GetUser().Id, star);
			return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
		}

		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> SetLeader(long id) {
			var recurrenceId = id;
			await L10Accessor.SetMeetingLeader(GetUser(), GetUser().Id, recurrenceId);
			return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
		}

		[Access(AccessLevel.UserOrganization)]
		public ActionResult External(long id) {
			var recurrence = id;
			var links = L10Accessor.GetExternalLinksForRecurrence(GetUser(), id);
			ViewBag.Recurrence = recurrence;
			return View(links);
		}

		[Access(AccessLevel.UserOrganization)]
		public async Task<ActionResult> MeetingSummary(long id) {
			var summary = await L10Accessor.GetMeetingSummary(GetUser(), id);
			return PartialView(summary);
		}

		[Access(AccessLevel.UserOrganization)]
		public ActionResult Timeline(long id, bool noheading = false) {
			var recurrence = id;
			var audits = L10Accessor.GetL10Audit(GetUser(), recurrence);
			var transcripts = TranscriptAccessor.GetRecurrenceTranscript(GetUser(), recurrence);
			var meetings = L10Accessor.GetL10Meetings(GetUser(), id, false, true);
			var list = new List<MeetingTimeline>();
			var user = GetUser();
			foreach (var m in meetings) {

				var curAudits = audits.Where(x => m.CreateTime <= x.CreateTime && (m.CompleteTime == null || x.CreateTime <= m.CompleteTime)).ToList();
				var curTranscripts = transcripts.Where(x => m.CreateTime <= x.CreateTime && (m.CompleteTime == null || x.CreateTime <= m.CompleteTime)).ToList();

				var allItems = new List<TimelineItem>();
				allItems.AddRange(curAudits.Select(x => TimelineItem.Create(user, x)));
				allItems.AddRange(curTranscripts.Select(x => TimelineItem.Create(user, x)));

				list.Add(new MeetingTimeline() {
					Meeting = m,
					Items = allItems
				});
			}
			ViewBag.NoTitleBar = noheading;
			return View(list);
		}


		[Access(AccessLevel.UserOrganization)]
		public async Task<ActionResult> Printout(long id) {
			var recur = await L10Accessor.GetOrGenerateAngularRecurrence(GetUser(), id);
			var d = L10Accessor.GetLastMeetingEndTime(GetUser(), id);

			var merger = new DocumentMerger();

			var doc = PdfAccessor.CreateDoc(GetUser(), "THE LEVEL 10 MEETING");
			var settings = new PdfSettings(GetUser().Organization.Settings);
			PdfAccessor.AddL10(doc, recur, settings, d);

			merger.AddDoc(doc);

			var doc1 = merger.Flatten("THE LEVEL 10 MEETING", true, true, GetUser().Organization.Settings.GetDateFormat(), recur._Recurrence.Item.Name);

			var now = DateTime.UtcNow.ToJavascriptMilliseconds() + "";
			return Pdf(doc1, now + "_" + recur.Basics.Name + "_L10Meeting.pdf", true);
		}


		[Access(AccessLevel.UserOrganization)]
		public PartialViewResult GetAdmins(long id) {
			var model = L10Accessor.GetAdmins(GetUser(), id);

			return PartialView(model);
		}

		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> DeleteMeeting(long id) {
			//Deletes the meeting not the recurrence
			await L10Accessor.DeleteL10Meeting(GetUser(), id);
			return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
		}

		[Access(AccessLevel.UserOrganization)]
		public JsonResult EditMeetingTime(long id, string type, long? time) {
			//Deletes the meeting not the recurrence
			var meeting = L10Accessor.EditMeetingTimes(GetUser(), id, type, time.NotNull(x => x.Value.ToDateTime()));
			return Json(ResultObject.SilentSuccess(new {
				start = meeting.StartTime,
				end = meeting.CompleteTime,
			}), JsonRequestBehavior.AllowGet);
		}

		[Access(AccessLevel.UserOrganization)]
		public async Task<ActionResult> EditVto(long id, bool noheading = false) {
			//var vtoId = (await L10Accessor.GetOrGenerateAngularRecurrence(GetUser(), id, false, false, false)).VtoId;
			var vtoId = L10Accessor.GetVtoIdForL10(GetUser(), id);

			return RedirectToAction("Edit", "VTO", new { id = vtoId, noheading = noheading });
		}


		#region Modal

		[Access(AccessLevel.UserOrganization)]
		public async Task<PartialViewResult> AddAttendee(long meetingId) {

			var existingAttendees = string.Join(",", L10Accessor.GetAttendees(GetUser(), meetingId).Select(x => x.Id));
			ViewBag.meetingId = meetingId;
			CreateUserOrganizationViewModel obj = null;
			var showCreate = false;
			try {
				obj = UserAccessor.BuildCreateUserVM(GetUser(), ViewBag, meetingId);
				showCreate = true;
			} catch (Exception) {
				//cannot add users
			}
			var meetingName = L10Accessor.GetMeetingName(GetUser(), meetingId) ?? "the meeting";

			var settings = SelectExistingOrCreateUtility.Create<CreateUserOrganizationViewModel>("/User/Search?exclude=" + existingAttendees, "CreateUserOrganizationViewModel", "Add existing attendee", "Create a new user", "Search users to add them to " + meetingName + ":", obj, false, multiple: true, showCreate: showCreate);
			return PartialView(settings);
		}


		[Access(AccessLevel.UserOrganization)]
		public async Task<PartialViewResult> AddMeasurableModal(long meetingId, bool hideMeetings = false) {

			var data = await L10Accessor.GetOrGenerateScorecardDataForRecurrence(GetUser(), meetingId, false, null, null, true, false, null);
			var existingIds = string.Join(",", data.Measurables.Select(x => x.Id));

			var attendees = L10Accessor.GetAttendees(GetUser(), meetingId);

			var obj = ScorecardAccessor.BuildCreateMeasurableVM(GetUser(), ViewBag, null, meetingId, hideMeetings, attendees.ToSelectList(x => x.GetName(), x => x.Id, GetUser().Id));
			var meetingName = L10Accessor.GetMeetingName(GetUser(), meetingId) ?? "the meeting";
			var settings = SelectExistingOrCreateUtility.Create<CreateMeasurableViewModel>("/Measurable/Search?exclude=" + existingIds, "CreateMeasurableViewModel", "Select existing measurable", "Create a new measurable", "Search for measurables to add them to " + meetingName + ":", obj, true, multiple: true);
			ViewBag.meetingId = meetingId;
			return PartialView(settings);
		}

		public class RockStatesVm {
			public string id { get; set; }

			public string name { get; set; }
		}

		public class RockTypesVm {
			public string id { get; set; }

			public string name { get; set; }
		}

		[Access(AccessLevel.UserOrganization)]
		public async Task<PartialViewResult> AddRockModal(long meetingId) {
			var vm = await RockAccessor.BuildRockVM(GetUser(), null, populateManaging: true, recurrenceId: meetingId);
			var existingRocks = L10Accessor.GetRocksForRecurrence(GetUser(), meetingId);
			var existingIds = string.Join(",", existingRocks.Select(x => x.ForRock.Id));

			var meetingName = L10Accessor.GetMeetingName(GetUser(), meetingId)??"the meeting";

			var settings = SelectExistingOrCreateUtility.Create<EditRockViewModel>("/Rocks/Search?exclude=" + existingIds, "RockEditVM", "Select existing rock", "Add a Rock","Search for existing rocks to attach them to "+ meetingName + ":", vm, true, multiple: true);
			ViewBag.meetingId = meetingId;
			return PartialView(settings);
		}

		[Access(AccessLevel.UserOrganization)]
		[HttpPost]
		public async Task<JsonResult> AddAttendee(long meetingId, SelectExistingOrCreateModel<CreateUserOrganizationViewModel> model) {
			if (model.ShouldCreateNew()) {
				var result = await _UserAccessor.CreateUser(GetUser(), model.Object);
				var createdUser = result.Item2;
				foreach (var mId in model.Object.RecurrenceIds ?? new long[0]) {
					try {
						await L10Accessor.AddAttendee(GetUser(), mId, createdUser.Id);
					} catch (Exception) {
						throw new PermissionsException("Could not add to meeting.");
					}
				}
			} else {
				foreach (var userId in model.SelectedValue) {
					await L10Accessor.AddAttendee(GetUser(), meetingId, userId);
				}
			}

			return Json(ResultObject.Success("Attendee added."));

		}

		[Access(AccessLevel.UserOrganization)]
		[HttpPost]
		public async Task<JsonResult> AddMeasurableModal(long meetingId, SelectExistingOrCreateModel<CreateMeasurableViewModel> model,
															DateTime? cumulativeStart = null, DateTime? averageStart = null, string formula = null) {
			if (model.ShouldCreateNew()) {
				/*
				 * * Why won't this model load correctly?
				 * backup solution ensues
				 */
				var lower = Request.Form["Object.Measurable.lower"].TryParseDecimal();
				var upper = Request.Form["Object.Measurable.upper"].TryParseDecimal();
				var enableCumulative = Request.Form["Object.Measurable.enableCumulative"].TryParseBool();
				var enableAverage = Request.Form["Object.Measurable.enableAverage"].TryParseBool();
				var recurrenceIds = Request.Form["Object.Measurable.recurrenceIds"].TryParseLongList();
				//var direction = LessGreater.GreaterThan;
				//if (Enum.TryParse(Request.Form["Object.Measurable.Direction"], out direction)) {
				//}
				//model.Object.Measurable.Direction = direction;
				/*End of Backup solution*/

				await ScorecardAccessor.CreateMeasurableHelper(
					GetUser(), model.Object.Measurable,
					lower, upper,
					enableCumulative, cumulativeStart,
					enableAverage, averageStart,
					formula,
					recurrenceIds);
				return Json(ResultObject.Success("Measurable created."));

			} else {
				foreach (var measurableId in model.SelectedValue) {
					await L10Accessor.AttachMeasurable(GetUser(), meetingId, measurableId);
				}
				if (model.SelectedValue.Length==1)
					return Json(ResultObject.Success("Measurable added."));
				return Json(ResultObject.Success("Measurables added."));
			}


		}

		[Access(AccessLevel.UserOrganization)]
		[HttpPost]
		public async Task<JsonResult> AddRockModal(long meetingId, SelectExistingOrCreateModel<EditRockViewModel> model) {
			string message = null;
			if (model.ShouldCreateNew()) {
				var o = model.Object;
                var toBeAddedtoL10 = meetingId != null && meetingId != 0;
				var rockDueDate = o.DueDate.NotNull(x => (DateTime?)GetUser().GetTimeSettings().ConvertToServerTime(x));
				var result = await RockAccessor.CreateRock(GetUser(), o.AccountableUser, o.Title, notes: o.Notes, toBeAddedToL10: toBeAddedtoL10, dueDate: rockDueDate);
				await RockAccessor.UpdateRock(GetUser(), result.Id, completion: o.Completion);
				foreach (var recurId in o.RecurrenceIds) {
				try {
						await L10Accessor.AttachRock(GetUser(), recurId, result.Id, o.AddToVTO??false, AttachRockType.Create);
					} catch (PermissionsException e) {
					throw new PermissionsException("Could not add to meeting.");
					}
				}
				var modelMilestones = o.Milestones;
				if (modelMilestones != null) {
					foreach (var m in modelMilestones.Where(x => !x.Deleted).ToList()) {
						await RockAccessor.AddMilestone(GetUser(), result.Id, m.Name, m.DueDate, m.Complete);
					}
				}
				if (o.RecurrenceIds.Count() == 0)
					return Json(ResultObject.Success("Rock was created (not added to any meetings)"));

				return Json(ResultObject.Success("Rock created."));
			} else {
				foreach (var rockId in model.SelectedValue) {
					await L10Accessor.AttachRock(GetUser(), meetingId, rockId, false, AttachRockType.Existing);
				}
				if (model.SelectedValue.Count()==1)
					return Json(ResultObject.Success("Rock added."));
				return Json(ResultObject.Success("Rocks added."));
				//return Json(ResultObject.SilentSuccess());
			}


		}

		#endregion

		#region Error
		[Access(AccessLevel.Any)]
		public PartialViewResult Error(MeetingException e) {
			return PartialView("Error", e);
		}
		[Access(AccessLevel.Any)]
		public PartialViewResult ErrorMessage(String message = null, MeetingExceptionType? type = null) {
			return PartialView("Error", new MeetingException(-1, message ?? "An error has occurred.", type ?? MeetingExceptionType.Error));
		}
		#endregion
	}
}
