/// =================================
/// Author: Shaun Curtis, Cold Elm Coders
/// License: MIT
/// ==================================

using Blazor.Components;
using Blazor.Pages;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Blazor.Services
{
    /// <summary>
    /// Service Class for managing Cusotm Routes and Runtime Layout Changes
    /// </summary>
    public class RouteViewService
    {
        /// <summary>
        /// List of Custom Routes
        /// </summary>
        public List<CustomRouteData> Routes { get; private set; } = new List<CustomRouteData>();

        /// <summary>
        /// Runtime Layout override
        /// </summary>
        public Type Layout { get; set; }

        /// <summary>
        /// Instance Constructor
        /// </summary>
        public RouteViewService()
        {
            var componentParameters = new SortedDictionary<string, object>();
            componentParameters.Add("ID", 0);
            var route = new CustomRouteData() { PageType = typeof(Counter), RouteMatch = @"^\/counted\/(\d+)", ComponentParameters = componentParameters };
            Routes.Add(route);
            Routes.Add(new CustomRouteData() { PageType = typeof(Counter), RouteMatch = @"^\/counters" });
            Routes.Add(new CustomRouteData() { PageType = typeof(RouteViewer), RouteMatch = @"^\/routeviewer" });
        }

        /// <summary>
        /// Method to get a Custom route match if one exists
        /// </summary>
        /// <param name="url"></param>
        /// <param name="routeData"></param>
        /// <returns></returns>
        public bool GetRouteMatch(string url, out RouteData routeData)
        {
            var route = Routes.FirstOrDefault(item => item.IsMatch(url));
            if (route != null && !EqualityComparer<RouteData>.Default.Equals(route))
            {
                routeData = route.RouteData;
                return true;
            }
            else
            {
                routeData = null;
                return false;
            }
        }
    }
}
