using RadialReview.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Models.ViewModels
{
    public class EditFocusAreaViewModel
    {
        public long? Id { get; set; }
        public string Title { get; set; }
        public string Detail { get; set; }
        public bool CanEdit { get; set; }
        public bool CanCreate { get; set; }
        public bool IsCreate { get; set; }
        public bool CanArchive { get; set; }
        public bool HideMeetings { get; set; }
        public List<SelectListItem> PotentialUsers { get; set; }
        public List<SelectListItem> PossibleRecurrences { get; set; }
        public long[] RecurrenceIds { get; set; }

        public EditFocusAreaViewModel()
        {
            CanEdit = true;
            CanCreate = true;
            RecurrenceIds = new long[] { };
        }
    }
}