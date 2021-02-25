using System;
using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;

namespace RadialReview.Models.Compass
{
	public class Compass : ILongIdentifiable, IHistorical
	{
		public virtual long Id { get; set; }
		public virtual string Title { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
		public virtual UserModel ForUser { get; set; }
		public virtual int ForMeeting { get; set; }
		public virtual int ForOrganization { get; set; }
		/// <summary>
		/// The original workspace
		/// </summary>
		public virtual bool PrimaryCompass { get; set; }

		public Compass()
		{
			CreateTime = DateTime.UtcNow;
		}
    }
	public class CompassMap : ClassMap<Compass>
	{
		public CompassMap()
		{
			Id(x => x.Id);
			Map(x => x.Title);
			Map(x => x.PrimaryCompass);
			Map(x => x.CreateTime);
			Map(x => x.DeleteTime);
			Map(x => x.ForMeeting);
			Map(x => x.ForOrganization);
			References(x => x.ForUser).Nullable();
		}
	}
}