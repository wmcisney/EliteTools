using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;
using System.Web.Script.Serialization;
using System.Runtime.Serialization;

namespace RadialReview.Models.Compass
{
	public enum CompassTileType
	{
		//DO NOT REORDER
		Invalid = 0,
		Heading,
		Profile,
		Scorecard,
		Todo,
		Roles,
		Rocks,
		Values,
		Manage,
		Url,
		L10Todos,
		L10Scorecard,
		L10Rocks,
		Headlines,
		L10Issues,
		FAQGuide,
		Notifications,
		L10SolvedIssues,
		Tasks,
		CoreProcesses,
		Milestones,
		L10Stats
	}


	public class CompassTileTypeBuilder
	{


		public CompassTileType Type { get; private set; }
		public string DataUrl { get; private set; }
		public string KeyId { get; private set; }

		private CompassTileTypeBuilder(CompassTileType type, string dataUrl, string keyId = null)
		{
			Type = type;
			DataUrl = dataUrl;
			KeyId = keyId;
		}
		public static CompassTileTypeBuilder Heading(long CompassId)
		{
			return new CompassTileTypeBuilder(CompassTileType.Heading, "/TileData/FocusArea/" + CompassId, "" + CompassId);
		}
		public static CompassTileTypeBuilder L10Scorecard(long recurrenceId)
		{
			return new CompassTileTypeBuilder(CompassTileType.L10Scorecard, "/TileData/L10Scorecard/" + recurrenceId, "" + recurrenceId);
		}
		public static CompassTileTypeBuilder L10Rocks(long recurrenceId)
		{
			return new CompassTileTypeBuilder(CompassTileType.L10Rocks, "/TileData/L10Rocks/" + recurrenceId, "" + recurrenceId);
		}
		public static CompassTileTypeBuilder L10Todos(long recurrenceId)
		{
			return new CompassTileTypeBuilder(CompassTileType.L10Todos, "/TileData/L10Todos/" + recurrenceId, "" + recurrenceId);
		}
		public static CompassTileTypeBuilder L10Issues(long recurrenceId)
		{
			return new CompassTileTypeBuilder(CompassTileType.L10Issues, "/TileData/L10Issues/" + recurrenceId, "" + recurrenceId);
		}
		public static CompassTileTypeBuilder L10PeopleHeadlines(long recurrenceId)
		{
			return new CompassTileTypeBuilder(CompassTileType.L10Issues, "/TileData/L10Headlines/" + recurrenceId, "" + recurrenceId);
		}
		public static CompassTileTypeBuilder L10Stats(long recurrenceId)
		{
			return new CompassTileTypeBuilder(CompassTileType.L10Stats, "/TileData/L10Stats/" + recurrenceId, "" + recurrenceId);
		}
	}


	public class CompassTileModel : ILongIdentifiable, IHistorical
	{
		public virtual long Id { get; set; }
		public virtual bool Hidden { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual string DataUrl { get; set; }
		public virtual string Title { get; set; }
		public virtual string Detail { get; set; }
		public virtual int Width { get; set; }
		public virtual int Height { get; set; }
		public virtual int X { get; set; }
		public virtual int Y { get; set; }
		public virtual CompassTileType Type { get; set; }
		[ScriptIgnore]
		[IgnoreDataMember]
		public virtual Compass Compass { get; set; }
		[ScriptIgnore]
		[IgnoreDataMember]
		public virtual UserModel ForUser { get; set; }
		public virtual string KeyId { get; set; }
		public virtual bool ShowPrintButton { get; set; }

		public CompassTileModel()
		{
			CreateTime = DateTime.UtcNow;
		}

		public CompassTileModel(int x, int y, int width, int height, string title,string detail, CompassTileTypeBuilder type, Compass compass, DateTime createTime)
		{
			CreateTime = createTime;
			Compass = compass;
			Title = title;
			Detail = detail;
			Width = width;
			Height = height;
			X = x;
			Y = y;
			Type = type.Type;
			KeyId = type.KeyId;
			DataUrl = type.DataUrl;
		}

		public class CompassTileMap : ClassMap<CompassTileModel>
		{
			public CompassTileMap()
			{
				Id(x => x.Id);
				Map(x => x.KeyId);
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
				Map(x => x.DataUrl);
				Map(x => x.Hidden);
				Map(x => x.Title);
				Map(x => x.Detail);
				Map(x => x.Width);
				Map(x => x.Height);
				Map(x => x.X);
				Map(x => x.Y);
				Map(x => x.Type).CustomType<CompassTileType>();
				References(x => x.ForUser).LazyLoad();
				References(x => x.Compass).LazyLoad();


			}
		}

	}
}