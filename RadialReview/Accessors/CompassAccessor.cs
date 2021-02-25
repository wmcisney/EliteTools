using NHibernate;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Application;
using RadialReview.Models.Compass;
using RadialReview.Models.Dashboard;
using RadialReview.Models.Enums;
using RadialReview.Models.L10;
using RadialReview.Models.ViewModels;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RadialReview.Accessors
{
    public class CompassAccessor 
    {
        public static int TILE_HEIGHT = 5;

		public static async Task SetHomeCompass(UserOrganizationModel caller, long userId, CompassType type, long CompassId)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var perms = PermissionsUtility.Create(s, caller);
					await SetHomeCompass(s, perms, userId, type, CompassId);
					tx.Commit();
					s.Flush();
				}
			}
		}

		public static async Task SetHomeCompass(ISession s, PermissionsUtility perms, long userId, CompassType CompassType, long CompassId)
		{
			perms.ViewCompass(CompassType, CompassId);
			perms.Self(userId);

			var user = s.Get<UserOrganizationModel>(userId);
			user.PrimaryCompass = new UserOrganizationModel.PrimaryCompassModel()
			{
				CompassId = CompassId,
				Type = CompassType,
			};
			s.Update(user);
		}

		public static List<Compass> GetCompassessForUser(ISession s, PermissionsUtility perms, long userId)
		{
			var user = s.Get<UserOrganizationModel>(userId);
			if (user == null || user.User == null)
			{
				throw new PermissionsException("User does not exist.");
			}

			perms.ViewCompassForUser(user.User.Id);
			return s.QueryOver<Compass>().Where(x => x.DeleteTime == null && x.ForUser.Id == user.User.Id).List().ToList();

		}

		public static Compass GetPrimaryCompassForUser(UserOrganizationModel caller, long? userId)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					return GetPrimaryCompassForUser(s, caller, userId);
				}
			}
		}

		public static Compass GetPrimaryCompassForUser(ISession s, UserOrganizationModel caller, long? userId)
		{
			var user = s.Get<UserOrganizationModel>(userId);
			if (user == null || user.User == null)
			{
				throw new PermissionsException("User does not exist.");
			}

			PermissionsUtility.Create(s, caller).ViewCompassForUser(user.User.Id);
			return s.QueryOver<Compass>()
				.Where(x => x.DeleteTime == null && x.ForUser.Id == user.User.Id && x.PrimaryCompass)
				.OrderBy(x => x.CreateTime).Desc
				.Take(1).SingleOrDefault();
		}

		public static List<Compass> GetCompassesForUser(UserOrganizationModel caller, long userId)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var perms = PermissionsUtility.Create(s, caller);
					return GetCompassesForUser(s, perms, userId);

				}
			}
		}

		public static List<Compass> GetCompassesForUser(ISession s, PermissionsUtility perms, long userId)
		{
			var user = s.Get<UserOrganizationModel>(userId);
			if (user == null || user.User == null)
			{
				throw new PermissionsException("User does not exist.");
			}

			perms.ViewCompassForUser(user.User.Id);
			return s.QueryOver<Compass>().Where(x => x.DeleteTime == null && x.ForUser.Id == user.User.Id).List().ToList();

		}

		public static Compass GetCompass(UserOrganizationModel caller, long CompassId)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var comp = s.Get<Compass>(CompassId);
					if (comp == null)
					{
						return null;
					}

					PermissionsUtility.Create(s, caller).ViewCompassForUser(comp.ForUser.Id);
					return comp;
				}
			}
		}

		public static int GetCompassTileId(ISession s) {
			//this is a hack - because of the tx commits we can't use a DB trigger.
			//called from CreateCompassTile

			int rowId = 0;
			var date = DateTime.Today;
			CompassTileModel record = s.QueryOver<CompassTileModel>()
				   .Where(x => x.CreateTime >= DateTime.Now.AddSeconds(-30))
				   .OrderBy(x => x.CreateTime).Desc
				   .Take(1).SingleOrDefault();
			rowId = (int)record.Id;
			return rowId;
		}

		public static int SetYPosition(ISession s, long CompassId)
		{
			//this is a hack - because of the tx commits we can't use a DB trigger.
			//called from CreateCompassTile

			int yPos = 0;
			int rowId = 0;
			var date = DateTime.Today;
			CompassTileModel record = s.QueryOver<CompassTileModel>()
				   .Where(x => (x.Compass.Id == CompassId)&&(x.Hidden==false))
				   .OrderBy(x => x.Y).Desc
				   .Take(1).SingleOrDefault();
			
			yPos = record.Height + record.Y;
			return yPos;
		}

		public static int GetCompassTileCount(ISession s, long CompassId) {
			int tCount = 0;

			List<CompassTileModel> records = GetCompassTiles(s, CompassId);
			tCount = records.Count;

			return tCount;
		}



		public static CompassDropdownVM GetCompassDropdown(UserOrganizationModel caller, long userId)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var perms = PermissionsUtility.Create(s, caller);
					return GetCompassDropdown(s, perms, userId);

				}
			}
		}

		public static CompassDropdownVM GetCompassDropdown(ISession s, PermissionsUtility perms, long userId)
		{
			perms.Self(userId);
			var allCompasses = CompassAccessor.GetCompassesForUser(s, perms, userId);
			//var l10s = L10Accessor.GetVisibleL10Recurrences(s, perms, userId);
			//var l10s = L10Accessor.GetViewableL10Meetings_Tiny(s, perms, userId);
			var l10s = L10Accessor.GetViewableL10Meetings_Tiny(s, perms, userId).OrderByDescending(x => x.StarDate).ThenBy(x => x.Name).ToList();
			var user = s.Get<UserOrganizationModel>(userId);
			var primaryCompass = user.PrimaryCompass ?? new UserOrganizationModel.PrimaryCompassModel()
			{
				CompassId = allCompasses.Where(x => x.PrimaryCompass).Select(x => x.Id).FirstOrDefault(),
				Type = CompassType.Standard
			};
			var custom = allCompasses.Where(x => !x.PrimaryCompass).Select(x => new NameId(x.Title, x.Id)).ToList();

			var defaultCompassName = "Default Compass";
			if (user.UserIds.Length > 1)
				defaultCompassName = "Default Compass (cross-account)";

			var originals = s.QueryOver<Compass>()
				.Where(x => x.DeleteTime == null && x.ForUser.Id == user.User.Id && x.PrimaryCompass)
				.List()
				.Select((x, i) => new NameId(defaultCompassName + (i > 0 ? " (" + i + ")" : ""), x.Id))
				.ToList();


			custom.AddRange(originals);

			return new CompassDropdownVM()
			{
				AllCompasses = l10s,
				CustomCompass = custom,
				DefaultCompassId = allCompasses.FirstOrDefault(x => x.PrimaryCompass).NotNull(x => x.Id),
				PrimaryCompass = primaryCompass
			};
		}

		public static Compass CreateCompass(UserOrganizationModel caller, string title, bool primary, bool defaultCompass = false)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					if (caller.User == null)
					{
						throw new PermissionsException("User does not exist.");
					}

					Compass cps = CreateCompass(s, caller, title, primary, defaultCompass);

					tx.Commit();
					s.Flush();
					return cps;
				}
			}
		}

		public static Compass CreateCompass(ISession s, UserOrganizationModel caller, string title, bool primary, bool defaultCompass)
		{
			if (primary)
			{
				var existing = s.QueryOver<Compass>().Where(x => x.DeleteTime == null && x.ForUser.Id == caller.User.Id && x.PrimaryCompass).List();
				foreach (var e in existing)
				{
					e.PrimaryCompass = false;
					s.Update(e);
				}
			}
			else
			{
				//If this the first one, then override primary to true
				primary = (!s.QueryOver<Compass>().Where(x => x.DeleteTime == null && x.ForUser.Id == caller.User.Id).Select(x => x.Id).List<long>().Any());
			}

			var cps = new Compass()
			{
				ForUser = caller.User,
				Title = title,
				PrimaryCompass = primary,
			};
			s.Save(cps);
			if (defaultCompass)
			{
				var perms = PermissionsUtility.Create(s, caller);
				//x: 0, y: 0, w: 1, h: 1
				CreateTile(s, perms, cps.Id, 1, 1 * TILE_HEIGHT, 0, 0 * TILE_HEIGHT, "/TileData/UserProfile2", "Profile", TileType.Profile);
				CreateTile(s, perms, cps.Id, 1, 1 * TILE_HEIGHT, 0, 1 * TILE_HEIGHT, "/TileData/FAQTips", "FAQ Guide", TileType.FAQGuide);
				//x: 1, y: 2, w: 3, h: 2
				CreateTile(s, perms, cps.Id, 4, 2 * TILE_HEIGHT, 0, 2 * TILE_HEIGHT, "/TileData/UserTodo2", "To-dos", TileType.Todo);
				//x: 1, y: 0, w: 6, h: 2
				CreateTile(s, perms, cps.Id, 6, 2 * TILE_HEIGHT, 1, 0 * TILE_HEIGHT, "/TileData/UserScorecard2", "Scorecard", TileType.Scorecard);
				//x: 4, y: 2, w: 3, h: 2
				CreateTile(s, perms, cps.Id, 3, 2 * TILE_HEIGHT, 4, 2 * TILE_HEIGHT, "/TileData/UserRock2", "Rocks", TileType.Rocks);

			}
            else
            {
				var perms = PermissionsUtility.Create(s, caller);
                CreateCompassTile(s, perms, cps.Id, 1, 2 * TILE_HEIGHT, 0, 0, "/TileData/OrganizationValues", "Core Values", null, CompassTileType.Url, null);
			}

			return cps;
		}
		#region Tiles
		public static TileModel CreateTile(ISession s, PermissionsUtility perms, long dashboardId, int w, int h, int x, int y, string dataUrl, string title, TileType type, string keyId = null)
		{
			perms.EditDashboard(DashboardType.Standard, dashboardId);
			if (type == TileType.Invalid)
			{
				throw new PermissionsException("Invalid tile type");
			}

			var uri = new Uri(dataUrl, UriKind.Relative);
			if (uri.IsAbsoluteUri)
			{
				throw new PermissionsException("Data url must be relative");
			}

			var dashboard = s.Get<Dashboard>(dashboardId);

			var tile = (new TileModel()
			{
				Dashboard = dashboard,
				DataUrl = dataUrl,
				ForUser = dashboard.ForUser,
				Height = h,
				Width = w,
				X = x,
				Y = y,
				Type = type,
				Title = title,
				KeyId = keyId,
			});

			s.Save(tile);
			return tile;
		}

		public static CompassTileModel CreateCompassTile(ISession s, PermissionsUtility perms, long CompassId, int w, int h, int x, int y, string dataUrl, string title,string detail, CompassTileType type, string keyId = null)
		{
			perms.EditCompass(CompassType.Standard, CompassId);
			if (type == CompassTileType.Invalid)
			{
				throw new PermissionsException("Invalid compass tile type");
			}

			var uri = new Uri(dataUrl, UriKind.Relative);
			if (uri.IsAbsoluteUri)
			{
				throw new PermissionsException("Data url must be relative");
			}

			var compass = s.Get<Compass>(CompassId);

			var tile = (new CompassTileModel()
			{
				Compass = compass,
				DataUrl = dataUrl,
				ForUser = compass.ForUser,
				Height = h,
				Width = w,
				X = x,
				Y = y,
				Type = type,
				Title = title,
				Detail = detail,
				KeyId = keyId,
			});

			s.Save(tile);
			return tile;
		}

		public static TileModel CreateTile(UserOrganizationModel caller, long dashboardId, int w, int h, int x, int y, string dataUrl, string title, TileType type, string keyId = null)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{

					var perms = PermissionsUtility.Create(s, caller);
					var tile = CreateTile(s, perms, dashboardId, w, h, x, y, dataUrl, title, type, keyId);
					tx.Commit();
					s.Flush();
					return tile;
				}
			}
		}

		public static CompassTileModel CreateCompassTile(UserOrganizationModel caller, long CompassId, int w, int h, int x, int y, string dataUrl, string title,string detail, CompassTileType type, string keyId = null)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{

					var perms = PermissionsUtility.Create(s, caller);
					var ctile = CreateCompassTile(s, perms, CompassId, w, h, x, 0, dataUrl, title,detail, type, keyId);
					tx.Commit();
					s.Flush();

				}

				using (var tx = s.BeginTransaction())
                {
					//***NEW***//

					var perms = PermissionsUtility.Update(s, caller);
					int records = (GetCompassTileCount(s, CompassId) > 0) ? GetCompassTileCount(s, CompassId) : 0;

					int yPos = (records>0)?SetYPosition(s, CompassId):0;

					int rowId = GetCompassTileId(s);
					string newDataUrl = dataUrl + "/" + rowId.ToString();
					var ctile = EditCompassTile(s, perms, (long)rowId,null,null,null,yPos,null, newDataUrl, null,null);
					tx.Commit();
					s.Flush();

					//*********//

					return ctile;

				}

			}
		}

		public static CompassTileModel  UpdateCompassTile(UserOrganizationModel caller, long CompassTileId, string dataUrl, string title, string detail, CompassTileType type, string keyId = null)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{

					var perms = PermissionsUtility.Update(s, caller);
					var ctile = EditCompassTile(s, perms, CompassTileId, null, null, null, null, null, dataUrl, title, detail);
					tx.Commit();
					s.Flush();
					return ctile;

				}
			}
		}

		public static CompassTileModel EditCompassTile(ISession s, PermissionsUtility perms, long tileId, int? w = null, int? h = null, int? x = null, int? y = null, bool? hidden = null, string dataUrl = null, string title = null, string detail = null)
		{
			var tile = s.Get<CompassTileModel>(tileId);

			tile.Height = h ?? tile.Height;
			tile.Width = w ?? tile.Width;
			tile.X = x ?? tile.X;
			tile.Y = y ?? tile.Y;
			tile.Hidden = hidden ?? tile.Hidden;
			tile.Title = title ?? tile.Title;
			tile.Detail = detail ?? tile.Detail;

			if (dataUrl != null)
			{
				//Ensure relative
				var uri = new Uri(dataUrl, UriKind.Relative);
				if (uri.IsAbsoluteUri)
				{
					throw new PermissionsException("Data url must be relative.");
				}

				tile.DataUrl = dataUrl;
			}

			s.Update(tile);

			return tile;
		}

		public static CompassTileModel EditCompassTile(UserOrganizationModel caller, long tileId, int? h = null, int? w = null, int? x = null, int? y = null, bool? hidden = null, string dataUrl = null, string title = null, string detail=null)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var perms = PermissionsUtility.Create(s, caller).EditCompassTile(tileId);

					var o = EditCompassTile(s, perms, tileId, w, h, x, y, hidden, dataUrl, title, detail);

					tx.Commit();
					s.Flush();
					return o;
				}
			}
		}



		public static void EditTiles(UserOrganizationModel caller, long CompassId, IEnumerable<Controllers.CompassController.CompassTileVM> model)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var perms = PermissionsUtility.Create(s, caller).EditCompass(CompassType.Standard, CompassId);

					var editIds = model.Select(x => x.id).ToList();

					var old = s.QueryOver<CompassTileModel>().Where(x => x.DeleteTime == null).WhereRestrictionOn(x => x.Id).IsIn(editIds).List().ToList();

					if (!SetUtility.AddRemove(editIds, old.Select(x => x.Id)).AreSame())
					{
						throw new PermissionsException("You do not have access to edit some tiles.");
					}

					if (old.Any(x => x.Compass.Id != CompassId))
					{
						throw new PermissionsException("You do not have access to edit this compass.");
					}

					foreach (var o in old)
					{
						var found = model.First(x => x.id == o.Id);
						o.X = found.x;
						o.Y = found.y;
						o.Height = found.h;
						o.Width = found.w;
						s.Update(o);
					}


					tx.Commit();
					s.Flush();
				}
			}
		}

		public static void EditCompassTiles(UserOrganizationModel caller, long CompassId, IEnumerable<Controllers.CompassController.CompassTileVM> model)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var perms = PermissionsUtility.Create(s, caller).EditCompass(CompassType.Standard, CompassId);

					var editIds = model.Select(x => x.id).ToList();

					var old = s.QueryOver<CompassTileModel>().Where(x => x.DeleteTime == null).WhereRestrictionOn(x => x.Id).IsIn(editIds).List().ToList();

					if (!SetUtility.AddRemove(editIds, old.Select(x => x.Id)).AreSame())
					{
						throw new PermissionsException("You do not have access to edit some tiles.");
					}

					if (old.Any(x => x.Compass.Id != CompassId))
					{
						throw new PermissionsException("You do not have access to edit this compass.");
					}

					foreach (var o in old)
					{
						var found = model.First(x => x.id == o.Id);
						o.X = found.x;
						o.Y = found.y;
						o.Height = found.h;
						o.Width = found.w;
						s.Update(o);
					}


					tx.Commit();
					s.Flush();
				}
			}
		}

		//public static CompassTileModel EditCompassTile(ISession s, PermissionsUtility perms, long tileId, int? w = null, int? h = null, int? x = null, int? y = null, bool? hidden = null, string dataUrl = null, string title = null)
		//{
		//	var tile = s.Get<CompassTileModel>(tileId);

		//	tile.Height = h ?? tile.Height;
		//	tile.Width = w ?? tile.Width;
		//	tile.X = x ?? tile.X;
		//	tile.Y = y ?? tile.Y;
		//	tile.Hidden = hidden ?? tile.Hidden;
		//	tile.Title = title ?? tile.Title;

		//	if (dataUrl != null)
		//	{
		//		//Ensure relative
		//		var uri = new Uri(dataUrl, UriKind.Relative);
		//		if (uri.IsAbsoluteUri)
		//		{
		//			throw new PermissionsException("Data url must be relative.");
		//		}

		//		tile.DataUrl = dataUrl;
		//	}

		//	s.Update(tile);

		//	return tile;
		//}


		public static List<TileModel> GetTiles(UserOrganizationModel caller, long dashboardId)
		{
			List<TileModel> tiles;
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					PermissionsUtility.Create(s, caller).ViewDashboard(DashboardType.Standard, dashboardId);

					tiles = GetTiles(s, dashboardId);
					if (tiles.Count == 0)
					{
					}

				}
			}
			//foreach (var tile in tiles) {
			//	tile.ForUser = null;
			//	tile.Dashboard = null;
			//}
			return tiles;
		}

		public static List<CompassTileModel> GetCompassTiles(UserOrganizationModel caller, long CompassId)
		{
			List<CompassTileModel> tiles;
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					PermissionsUtility.Create(s, caller).ViewCompass(CompassType.Standard, CompassId);

					tiles = GetCompassTiles(s, CompassId);
					if (tiles.Count == 0)
					{
					}

				}
			}
			//foreach (var tile in tiles) {
			//	tile.ForUser = null;
			//	tile.Dashboard = null;
			//}
			return tiles;
		}

        public static List<TileModel> GetTiles(ISession s, long dashboardId)
        {
            return s.QueryOver<TileModel>()
                .Where(x => x.DeleteTime == null && x.Dashboard.Id == dashboardId && x.Hidden == false)
                .List().OrderBy(x => x.Y).ThenBy(x => x.X).ToList();
        }

        public static List<CompassTileModel> GetCompassTiles(ISession s, long CompassId)
		{
			return s.QueryOver<CompassTileModel>()
				.Where(x => x.DeleteTime == null && x.Compass.Id == CompassId && x.Hidden == false)
				.List().OrderBy(x => x.Y).ThenBy(x => x.X).ToList();
		}

		public static List<CompassTileModel> GetOneCompassTile(ISession s, long CompassTileId)
		{
			using (var hs = HibernateSession.GetCurrentSession())
			{
				return hs.QueryOver<CompassTileModel>()
					.Where(x => x.DeleteTime == null && x.Id == CompassTileId && x.Hidden == false).List().OrderBy(x => x.Y).ThenBy(x => x.X).ToList();
			}

		}
		#endregion
		#region Tiles
		//public static TileModel CreateTile(ISession s, PermissionsUtility perms, long CompassId, int w, int h, int x, int y, string dataUrl, string title, TileType type, string keyId = null)
		//{
		//	perms.EditCompass(CompassType.Standard, CompassId);
		//	if (type == TileType.Invalid)
		//	{
		//		throw new PermissionsException("Invalid Tile type");
		//	}

		//	var uri = new Uri(dataUrl, UriKind.Relative);
		//	if (uri.IsAbsoluteUri)
		//	{
		//		throw new PermissionsException("Data url must be relative");
		//	}

		//	var compass = s.Get<Compass>(CompassId);

		//	var Tile = (new TileModel()
		//	{
		//		Compass = compass,
		//		DataUrl = dataUrl,
		//		ForUser = compass.ForUser,
		//		Height = h,
		//		Width = w,
		//		X = x,
		//		Y = y,
		//		Type = type,
		//		Title = title,
		//		KeyId = keyId,
		//	});

		//	s.Save(Tile);
		//	return Tile;
		//}

		//public static TileModel CreateTile(UserOrganizationModel caller, long CompassId, int w, int h, int x, int y, string dataUrl, string title, TileType type, string keyId = null)

		//{
		//	using (var s = HibernateSession.GetCurrentSession())
		//	{
		//		using (var tx = s.BeginTransaction())
		//		{

		//			var perms = PermissionsUtility.Create(s, caller);
		//			var Tile = CreateTile(s, perms, CompassId, w, h, x, y, dataUrl, title, type, keyId);
		//			tx.Commit();
		//			s.Flush();
		//			return Tile;
		//		}
		//	}
		//}

		//public static TileModel EditTile(ISession s, PermissionsUtility perms, long TileId, int? w = null, int? h = null, int? x = null, int? y = null, bool? hidden = null, string dataUrl = null, string title = null)
		//{
		//	var Tile = s.Get<TileModel>(TileId);

		//	Tile.Height = h ?? Tile.Height;
		//	Tile.Width = w ?? Tile.Width;
		//	Tile.X = x ?? Tile.X;
		//	Tile.Y = y ?? Tile.Y;
		//	Tile.Hidden = hidden ?? Tile.Hidden;
		//	Tile.Title = title ?? Tile.Title;

		//	if (dataUrl != null)
		//	{
		//		//Ensure relative
		//		var uri = new Uri(dataUrl, UriKind.Relative);
		//		if (uri.IsAbsoluteUri)
		//		{
		//			throw new PermissionsException("Data url must be relative.");
		//		}

		//		Tile.DataUrl = dataUrl;
		//	}

		//	s.Update(Tile);

		//	return Tile;
		//}

		//public static TileModel EditTile(UserOrganizationModel caller, long TileId, int? h = null, int? w = null, int? x = null, int? y = null, bool? hidden = null, string dataUrl = null, string title = null)
		//{
		//	using (var s = HibernateSession.GetCurrentSession())
		//	{
		//		using (var tx = s.BeginTransaction())
		//		{
		//			var perms = PermissionsUtility.Create(s, caller).EditTile(TileId);

		//			var o = EditTile(s, perms, TileId, w, h, x, y, hidden, dataUrl, title);

		//			tx.Commit();
		//			s.Flush();
		//			return o;
		//		}
		//	}
		//}

		//public static void EditTiles(UserOrganizationModel caller, long CompassId, IEnumerable<Controllers.CompassController.TileVM> model)
		//{
		//	using (var s = HibernateSession.GetCurrentSession())
		//	{
		//		using (var tx = s.BeginTransaction())
		//		{
		//			var perms = PermissionsUtility.Create(s, caller).EditCompass(CompassType.Standard, CompassId);

		//			var editIds = model.Select(x => x.id).ToList();

		//			var old = s.QueryOver<TileModel>().Where(x => x.DeleteTime == null).WhereRestrictionOn(x => x.Id).IsIn(editIds).List().ToList();

		//			if (!SetUtility.AddRemove(editIds, old.Select(x => x.Id)).AreSame())
		//			{
		//				throw new PermissionsException("You do not have access to edit some Tiles.");
		//			}

		//			if (old.Any(x => x.Compass.Id != CompassId))
		//			{
		//				throw new PermissionsException("You do not have access to edit this dashboard.");
		//			}

		//			foreach (var o in old)
		//			{
		//				var found = model.First(x => x.id == o.Id);
		//				o.X = found.x;
		//				o.Y = found.y;
		//				o.Height = found.h;
		//				o.Width = found.w;
		//				s.Update(o);
		//			}


		//			tx.Commit();
		//			s.Flush();
		//		}
		//	}
		//}

		//public static List<TileModel> GetTiles(UserOrganizationModel caller, long CompassId)
		//{
		//	List<TileModel> Tiles;
		//	using (var s = HibernateSession.GetCurrentSession())
		//	{
		//		using (var tx = s.BeginTransaction())
		//		{
		//			PermissionsUtility.Create(s, caller).ViewCompass(CompassType.Standard, CompassId);

		//			Tiles = GetTiles(s, CompassId);

		//		}
		//	}
		//	//foreach (var Tile in Tiles) {
		//	//	Tile.ForUser = null;
		//	//	Tile.Dashboard = null;
		//	//}
		//	return Tiles;
		//}

		//public static List<TileModel> GetTiles(ISession s, long CompassId)
		//{
		//	return s.QueryOver<TileModel>()
		//		.Where(x => x.DeleteTime == null && x.Compass.Id == CompassId && x.Hidden == false)
		//		.List().OrderBy(x => x.Y).ThenBy(x => x.X).ToList();
		//}

		#endregion

		public static void RenameCompass(UserOrganizationModel caller, long CompassId, string title)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					PermissionsUtility.Create(s, caller).EditCompass(CompassType.Standard, CompassId);
					var d = s.Get<Compass>(CompassId);
					d.Title = title;
					s.Update(d);
					tx.Commit();
					s.Flush();
				}
			}
		}

		public static void DeleteCompass(UserOrganizationModel caller, long CompassId)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					PermissionsUtility.Create(s, caller).EditCompass(CompassType.Standard, CompassId);
					var d = s.Get<Compass>(CompassId);
					d.DeleteTime = DateTime.UtcNow;
					s.Update(d);
					tx.Commit();
					s.Flush();
				}
			}
		}


		public class CompassAndTiles
		{
			public Compass Compass { get; set; }
			public List<CompassTileModel> Tiles { get; set; }
			public CompassAndTiles(Compass c)
			{
				Compass = c;
				Tiles = new List<CompassTileModel>();
			}
		}


		public static CompassAndTiles GenerateCompass(UserOrganizationModel caller, long id, CompassType type, int? width)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var perms = PermissionsUtility.Create(s, caller);
					switch (type)
					{
                        //case DashboardType.DirectReport:
                        //	(perms.ManagesUserOrganizationOrSelf(id);)>
                        //	return GenerateUserDashboard(s,id);
                        //	break;
                        //case DashboardType.Client:
                        //	(perms.ViewClient(id);)>
                        //	return GenerateClientDashboard(s, id);
                        case CompassType.Standard:
							return GenerateCompass(caller, id,CompassType.Standard, width);
							break;
						//case CompassType.L10:
						//	return GenerateL10Compass(s, perms, id, width);
						default:
							throw new ArgumentOutOfRangeException("CompassType", "" + type);
					}
				}
			}
		}


		//private static CompassAndTiles GenerateL10Compass(ISession s, PermissionsUtility perms, long id, int? width)
		//{
		//	perms.ViewL10Recurrence(id);
		//	var recur = s.Get<L10Recurrence>(id);
		//	var now = DateTime.UtcNow;

		//	var d = new Dashboard()
		//	{
		//		Id = -1,
		//		CreateTime = DateTime.UtcNow,
		//		Title = recur.Name ?? " L10 Compass",
		//	};
		//	var o = new CompassAndTiles(d);

		//	var measurableRowCounts = L10Accessor.GetMeasurableCount(s, perms, id);
		//	//2 is for header and footer...
		//	var scorecardHeight = (int)Math.Ceiling(2.0 + Math.Ceiling((measurableRowCounts.Measurables) * 18.0 / 19.0 + 0.47) + Math.Round(measurableRowCounts.Dividers / 5.0));
		//	var scorecardCount = (double)scorecardHeight / (double)TILE_HEIGHT;

		//	if (measurableRowCounts.Measurables == 0)
		//	{
		//		scorecardHeight = 4;
		//	}

		//	//Rocks, todos, issues
		//	var nonScorecardTileHeight = (int)Math.Max(3 * TILE_HEIGHT, Math.Ceiling((5.0 - scorecardCount) * TILE_HEIGHT));
		//	var issueTileHeight = (int)Math.Ceiling(0.5 * nonScorecardTileHeight);

		//	width = Math.Max(1156, width ?? 1156);

		//	int w = Math.Min(4, (int)Math.Floor(width.Value / 580.0 + 0.33));


		//	//						  x, y									w										h
		//	o.Tiles.Add(new TileModel(0, 0, w * 3, scorecardHeight, "Scorecard", TileTypeBuilder.L10Scorecard(id), c, now));
		//	o.Tiles.Add(new TileModel(0, scorecardHeight, w, issueTileHeight, "Rocks", TileTypeBuilder.L10Rocks(id), c, now));

		//	o.Tiles.Add(new TileModel(0, scorecardHeight + issueTileHeight, w, nonScorecardTileHeight - issueTileHeight, "Stats", TileTypeBuilder.L10Stats(id), c, now));

		//	o.Tiles.Add(new TileModel(w, scorecardHeight, w, nonScorecardTileHeight, "To-dos", TileTypeBuilder.L10Todos(id), c, now));
		//	o.Tiles.Add(new TileModel(w * 2, scorecardHeight, w, issueTileHeight, "Issues", TileTypeBuilder.L10Issues(id), c, now));
		//	o.Tiles.Add(new TileModel(w * 2, scorecardHeight + issueTileHeight, w, nonScorecardTileHeight - issueTileHeight, "People Headlines", TileTypeBuilder.L10PeopleHeadlines(id), c, now));

		//	return o;
		//}

		public static UserOrganizationModel.PrimaryCompassModel GetHomeCompassForUser(UserOrganizationModel caller, long userId)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var perms = PermissionsUtility.Create(s, caller);
					perms.Self(userId);

					var user = s.Get<UserOrganizationModel>(userId);
					var pcomp = user.PrimaryCompass;
					if (pcomp == null)
					{
						var primary = CompassAccessor.GetPrimaryCompassForUser(s, caller, userId);
						if (primary == null)
						{
							primary = CompassAccessor.CreateCompass(s, caller, null, false, true);
							tx.Commit();
							s.Flush();
						}

						pcomp = new UserOrganizationModel.PrimaryCompassModel()
						{
							Type = CompassType.Standard,
							CompassId = primary.Id,
						};

					}
					return pcomp;
				}
			}
		}
	}
}