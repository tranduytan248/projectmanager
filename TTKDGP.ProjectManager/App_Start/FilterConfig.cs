using System.Web.Mvc;
using TTKDGP.ProjectManager.Infrastructure;

namespace TTKDGP.ProjectManager
{
    public static class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new LoggingHandleErrorAttribute());
        }
    }
}
