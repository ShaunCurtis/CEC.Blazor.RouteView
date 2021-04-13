/// =================================
/// Author: Shaun Curtis, Cold Elm Coders
/// License: MIT
/// ==================================

using Blazor.SPA.Utilities;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Blazor.SPA.Components
{
    /// <summary>
    /// Class to extend RouteData and Add match functionality for Custom Routing
    /// </summary>
    public class CustomRouteData
    {
        /// <summary>
        /// The standard RouteData.
        /// </summary>
        public RouteData RouteData { get; private set; }

        /// <summary>
        /// The PageType to load when the route matches
        /// </summary>
        public Type PageType { get; set; }

        /// <summary>
        /// The Regex String to define the route
        /// </summary>
        public string RouteMatch { get; set; }

        /// <summary>
        /// Parameter values to add to the Route when created
        /// </summary>
        public SortedDictionary<string, object> ComponentParameters { get; set; } = new SortedDictionary<string, object>();

        /// <summary>
        /// Method to check if we have a route match
        /// </summary>
        /// <param name="url"></param>
        /// <param name="matchstring"></param>
        /// <returns></returns>
        public bool IsMatch(string url)
        {
            // get the match
            var match = Regex.Match(url, this.RouteMatch,RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var dict = new Dictionary<string, object>();
                if (match.Groups.Count >= ComponentParameters.Count)
                {
                    var i = 1;
                    foreach (var pars in ComponentParameters)
                    {
                        string matchValue = string.Empty;
                        if (i < match.Groups.Count)
                            matchValue = match.Groups[i].Value;
                        var ts = new TypeSwitch()
                            .Case((int x) =>
                            {
                                if (int.TryParse(matchValue, out int value))
                                    dict.Add(pars.Key, value);
                                else
                                    dict.Add(pars.Key, pars.Value);
                            })
                            .Case((float x) =>
                            {
                                if (float.TryParse(matchValue, out float value))
                                    dict.Add(pars.Key, value);
                                else
                                    dict.Add(pars.Key, pars.Value);
                            })
                            .Case((decimal x) =>
                            {
                                if (decimal.TryParse(matchValue, out decimal value))
                                    dict.Add(pars.Key, value);
                                else
                                    dict.Add(pars.Key, pars.Value);
                            })
                            .Case((string x) =>
                            {
                                dict.Add(pars.Key, matchValue);
                            });

                        ts.Switch(pars.Value);
                        i++;
                    }
                }
                this.RouteData = new RouteData(this.PageType, dict);
            }
            return match.Success;
        }

        /// <summary>
        /// Method to check if we have a route match and return the RouteData
        /// </summary>
        /// <param name="url"></param>
        /// <param name="matchstring"></param>
        /// <param name="routeData"></param>
        /// <returns></returns>
        public bool IsMatch(string url, out RouteData routeData)
        {
            routeData = this.RouteData;
            return IsMatch(url);
        }

    }
}