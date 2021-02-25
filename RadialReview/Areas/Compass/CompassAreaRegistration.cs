using System.Web.Mvc;

namespace RadialReview.Areas.Compass
{
    public class CompassAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "Compass";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "Compass_default",
                "Compass/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}