using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Models.Compass;
using RadialReview.Models.Dashboard;

namespace RadialReview.Models.Angular.Compass
{
    public class AngularTile : Base.BaseAngular
    {
		public AngularTile(long id) : base(id)
		{
		}
		public AngularTile()
		{
		}

		public DateTime CreateTime { get; set; }
		public DateTime? DeleteTime { get; set; }
		public string DataUrl { get; set; }
		public bool Hidden { get; set; }
		public string Title { get; set; }
		public int Width { get; set; }
		public int Height { get; set; }
		public int X { get; set; }
		public int Y { get; set; }
		public int Type { get; set; }

		public static List<AngularTile> Create(IEnumerable<TileModel> list)
		{
			new AngularTile();
			return list.Select(Create).ToList();
		}

		public static AngularTile Create(TileModel x)
		{
			return new AngularTile()
			{
				CreateTime = x.CreateTime,
				DeleteTime = x.DeleteTime,
				DataUrl = x.DataUrl,
				Hidden = x.Hidden,
				Title = x.Title,
				Width = x.Width,
				Height = x.Height,
				X = x.X,
				Y=x.Y,
				Id = x.Id,
			};
		}
	}
}