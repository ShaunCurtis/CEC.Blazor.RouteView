/// =================================
/// Author: Shaun Curtis, Cold Elm Coders
/// License: MIT
/// ==================================

using System;
using System.Collections.Generic;

namespace Blazor.SPA.Components
{
    public class ViewData
    {
        /// <summary>
        /// Gets the type of the View.
        /// </summary>
        public Type ViewType { get; private set; }

        /// <summary>
        /// Gets the type of the page matching the route.
        /// </summary>
        public Type LayoutType { get; private set; }

        /// <summary>
        /// Parameter values to add to the Route when created
        /// </summary>
        public Dictionary<string, object> ViewParameters { get; private set; } = new Dictionary<string, object>();

        /// <summary>
        /// Constructs an instance of <see cref="ViewData"/>.
        /// </summary>
        /// <param name="viewType">The type of the view.</param>
        /// <param name="viewValues">The view parameter values.</param>
        public ViewData(Type viewType, Dictionary<string, object> viewValues = null)
        {
            if (viewType == null) 
                throw new ArgumentNullException(nameof(viewType));
            this.ViewType = viewType;
            if (viewValues != null) this.ViewParameters = viewValues;
        }

        /// <summary>
        /// Constructs an instance of <see cref="ViewData"/>.
        /// </summary>
        /// <param name="viewType"></param>
        /// <param name="layout"></param>
        /// <param name="viewValues"></param>
        public ViewData(Type viewType, Type layout, Dictionary<string, object> viewValues = null)
        {
            if (viewType == null)
                throw new ArgumentNullException(nameof(viewType));
            this.ViewType = viewType;
            this.LayoutType = layout;
            if (viewValues != null) this.ViewParameters = viewValues;
        }
    }
}