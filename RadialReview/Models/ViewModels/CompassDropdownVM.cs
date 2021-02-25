using RadialReview.Models.Application;
using RadialReview.Models.L10.VM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using static RadialReview.Models.UserOrganizationModel;

namespace RadialReview.Models.ViewModels{
	public class CompassDropdownVM	{   
		public PrimaryCompassModel PrimaryCompass { get; set; }
		public long DefaultCompassId { get; set; }
		public List<TinyRecurrence> AllCompasses{ get; set; }
		//public List<TinyCompass> AllCompasses { get; set; }
		public List<NameId> CustomCompass { get; set; }
		public bool DisplayCreate { get; set; }

		public CompassDropdownVM() {
			AllCompasses= new List<TinyRecurrence>();
			//AllCompasses = new List<TinyCompass>();
			CustomCompass = new List<NameId>();
		}

		public bool DisplayMeetings() {
			return AllCompasses != null && AllCompasses.Any();
		}

		public bool DisplayCustom() {
			return (CustomCompass != null && CustomCompass.Any());
		}

	}
}
