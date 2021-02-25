using RadialReview.Utilities.DataTypes;
using System;
using System.Collections.Generic;

namespace RadialReview.Models.L10.VM
{
	public class L10VM
	{

		public TinyRecurrence Recurrence { get; set; }
		public TinyCompass Compass { get; set; }
		public bool? IsAttendee { get { return Recurrence.IsAttendee; } }
		//public bool AdminMeeting { get; set; }

		public L10VM(TinyRecurrence recurrence)
		{
			Recurrence = recurrence;
		}

		public L10VM(TinyCompass compass)
		{
			Compass = compass;
		}
	}

	public class TinyRecurrence {
		public long Id { get; set; }
		public string Name { get; set; }
		public long? MeetingInProgress { get; set; }
		public bool IsAttendee { get; set; }
		public List<TinyUser> _DefaultAttendees { get; set; }
		public DateTime? StarDate { get; set; }
	}

	public class TinyCompass { 
		public long Id { get; set; }
		public string Title { get; set; }
		public bool PrimaryCompass { get; set; }
		public int ForMeeting { get; set; }
		public int ForOrganization { get; set; }
	}
}