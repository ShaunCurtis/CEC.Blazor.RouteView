/// =================================
/// Author: Shaun Curtis, Cold Elm Coders
/// License: MIT
/// ==================================

using Blazor.SPA.Services;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;

namespace Blazor.SPA.Components
{
    /// <summary>
    /// Class to handle custom runtime routing
    /// where the dynamic routes are loaded into a routing table in the RouteViewService
    /// Route matching uses Regular Expressions, so is relatively expensive.
    /// </summary>
    public class RouteNotFoundManager : IComponent
    {
        /// <summary>
        /// Gets or sets the default type of layout to be used 
        /// </summary>
        [Parameter]
        public Type DefaultLayout { get; set; }

        /// <summary>
        /// Child content of the Component
        /// </summary>
        [Parameter] public RenderFragment ChildContent { get; set; }

        /// <summary>
        /// Injected Navigation Manager
        /// </summary>
        [Inject] NavigationManager NavManager { get; set; }

        /// <summary>
        /// Injected RouteViewService
        /// </summary>
        [Inject] RouteViewService RouteViewService { get; set; }

        /// <summary>
        /// RendleHandle from the Attach Process to queue render requests
        /// </summary>
        private RenderHandle _renderHandle;

        /// <summary>
        /// Internal Route data obtained from RouteViewService
        /// </summary>
        private RouteData _routeData = null;

        /// <summary>
        /// The PageLayout to use in rendering the component
        /// </summary>
        private Type _pageLayoutType => _routeData?.PageType.GetCustomAttribute<LayoutAttribute>()?.LayoutType
            ?? RouteViewService.Layout
            ?? DefaultLayout;

        /// <inheritdoc />
        public void Attach(RenderHandle renderHandle)
        {
            _renderHandle = renderHandle;
        }

        /// <inheritdoc />
        public Task SetParametersAsync(ParameterView parameters)
        {
            parameters.SetParameterProperties(this);
            // Get the route url
            var url = $"/{NavManager.Uri.Replace(NavManager.BaseUri, "")}";
            // check if we have a custom route and if so use it
            if (RouteViewService.GetRouteMatch(url, out var routedata))
                _routeData = routedata;
            // if The layout is blank show the ChildContent without a layout 
            if (_pageLayoutType == null)
                _renderHandle.Render(ChildContent);
            // otherwise show the route or ChildContent inside the layout
            else
                _renderHandle.Render(_ViewFragment);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Layouted Render Fragment
        /// </summary>
        private RenderFragment _ViewFragment => builder =>
        {
            // check if we have a RouteData object and if so load the RouteViewManager, otherwise the ChildContent
            if (_routeData != null)
            {
                builder.OpenComponent<RouteViewManager>(0);
                builder.AddAttribute(1, nameof(RouteViewManager.DefaultLayout), _pageLayoutType);
                builder.AddAttribute(1, nameof(RouteViewManager.RouteData), _routeData);
                builder.CloseComponent();
            }
            else
            {
                builder.OpenComponent<LayoutView>(0);
                builder.AddAttribute(1, nameof(LayoutView.Layout), _pageLayoutType);
                builder.AddAttribute(2, nameof(LayoutView.ChildContent), this.ChildContent);
                builder.CloseComponent();
            }
        };

        /// <summary>
        /// Layouted Render Fragment
        /// </summary>
        private RenderFragment _layoutViewFragment => builder =>
        {
            builder.OpenComponent<LayoutView>(0);
            builder.AddAttribute(1, nameof(LayoutView.Layout), _pageLayoutType);
            // check if we have a RouteData object and if so show it, otherwise the ChildContent
            if (_routeData != null)
                builder.AddAttribute(2, nameof(LayoutView.ChildContent), _renderRouteWithParameters);
            else
                builder.AddAttribute(2, nameof(LayoutView.ChildContent), this.ChildContent);
            builder.CloseComponent();
        };

        /// <summary>
        /// Render Fragment built from the RouteData
        /// </summary>
        private RenderFragment _renderRouteWithParameters => builder =>
        {
            builder.OpenComponent(0, _routeData.PageType);
            foreach (var kvp in _routeData.RouteValues)
            {
                builder.AddAttribute(1, kvp.Key, kvp.Value);
            }
            builder.CloseComponent();
        };

    }
}
