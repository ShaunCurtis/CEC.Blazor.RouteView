// ==========================================================
//  Original code:
// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
// ============================================================

/// =================================
/// Mods Author: Shaun Curtis, Cold Elm Coders
/// This would be an Inherits,  but too many of the methods/Properties/Fields are private and not inheritable!!!!!
/// License: MIT
/// ==================================

#nullable disable warnings

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using System.Threading.Tasks;


namespace Blazor.SPA.Components
{
    public class RouteNavLink : ComponentBase, IDisposable
    {
        private const string DefaultActiveClass = "active";

        private bool _isActive;
        private string _hrefAbsolute = null;
        private string _class = null;

        /// <summary>
        /// Gets or sets the CSS class name applied to the NavLink when the
        /// current route matches the NavLink href.
        /// </summary>
        [Parameter]
        public string ActiveClass { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a collection of additional attributes that will be added to the generated
        /// <c>a</c> element.
        /// </summary>
        [Parameter(CaptureUnmatchedValues = true)]
        public IReadOnlyDictionary<string, object> AdditionalAttributes { get; set; }

        /// <summary>
        /// Gets or sets the computed CSS class based on whether or not the link is active.
        /// </summary>
        protected string CssClass { get; set; }

        /// <summary>
        /// Gets or sets the child content of the component.
        /// </summary>
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        /// <summary>
        /// Gets or sets a value representing the URL matching behavior.
        /// </summary>
        [Parameter]
        public NavLinkMatch Match { get; set; }

        [Inject] private NavigationManager NavigationManager { get; set; } = default!;

        // START Added Properties

        /// <summary>
        /// The Url to route to
        /// </summary>
        [Parameter] public string RouteURL { get; set; }

        /// <summary>
        /// The ViewData object to change the RouteView to
        /// </summary>
        [Parameter] public ViewData ViewData { get; set; }

        /// <summary>
        /// The View Type to switch to
        /// </summary>
        [Parameter] public Type View { get; set; }

        /// <summary>
        /// Force a reload of the SPA/Page
        /// </summary>
        [Parameter] public bool ForceLoad { get; set; }

        /// <summary>
        /// Navigation Manager Service
        /// </summary>
        [Inject] NavigationManager NavManager { get; set; }

        /// <summary>
        /// Cascaded RouteViewManager
        /// </summary>
        [CascadingParameter] RouteViewManager RouteViewManager { get; set; }

        // END Added Properties

        /// <inheritdoc />
        protected override void OnInitialized()
        {
            // We'll consider re-rendering on each location change
            NavigationManager.LocationChanged += OnLocationChanged;
        }

        /// <inheritdoc />
        protected override void OnParametersSet()
        {
            // Update computed state
            var href = (string)null;
            if (AdditionalAttributes != null && AdditionalAttributes.TryGetValue("href", out var obj))
            {
                href = Convert.ToString(obj, CultureInfo.InvariantCulture);
            }

            _hrefAbsolute = href == null ? null : NavigationManager.ToAbsoluteUri(href).AbsoluteUri;
            _isActive = ShouldMatch(NavigationManager.Uri);

            _class = (string)null;
            if (AdditionalAttributes != null && AdditionalAttributes.TryGetValue("class", out obj))
            {
                _class = Convert.ToString(obj, CultureInfo.InvariantCulture);
            }

            UpdateCssClass();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            // To avoid leaking memory, it's important to detach any event handlers in Dispose()
            NavigationManager.LocationChanged -= OnLocationChanged;
        }

        /// <summary>
        /// Method to handle OnClick event on the link
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private async Task OnClick(MouseEventArgs e)
        {
            if (this.ViewData != null && this.RouteViewManager != null)
                await this.RouteViewManager.LoadViewAsync(this.ViewData);
            else if (this.View != null && this.RouteViewManager != null)
                await this.RouteViewManager.LoadViewAsync(this.View);
            else if (!string.IsNullOrWhiteSpace(this.RouteURL))
                this.NavManager.NavigateTo(this.RouteURL, this.ForceLoad);
        }

        private void UpdateCssClass()
        {
            CssClass = _isActive ? CombineWithSpace(_class, ActiveClass ?? DefaultActiveClass) : _class;
        }

        private void OnLocationChanged(object sender, LocationChangedEventArgs args)
        {
            // We could just re-render always, but for this component we know the
            // only relevant state change is to the _isActive property.
            var shouldBeActiveNow = ShouldMatch(args.Location);
            if (shouldBeActiveNow != _isActive)
            {
                _isActive = shouldBeActiveNow;
                UpdateCssClass();
                StateHasChanged();
            }
        }

        private bool ShouldMatch(string currentUriAbsolute)
        {
            if (_hrefAbsolute == null)
                return false;

            if (EqualsHrefExactlyOrIfTrailingSlashAdded(currentUriAbsolute))
                return true;

            if (Match == NavLinkMatch.Prefix && IsStrictlyPrefixWithSeparator(currentUriAbsolute, _hrefAbsolute))
                return true;

            // check if we have a view match
            var view = this.ViewData?.ViewType ?? this.View ?? null;
            if (this.RouteViewManager != null && this.RouteViewManager.IsCurrentView(view))
                return true;

            return false;
        }

        private bool EqualsHrefExactlyOrIfTrailingSlashAdded(string currentUriAbsolute)
        {
            Debug.Assert(_hrefAbsolute != null);

            if (string.Equals(currentUriAbsolute, _hrefAbsolute, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (currentUriAbsolute.Length == _hrefAbsolute.Length - 1)
            {
                // Special case: highlight links to http://host/path/ even if you're
                // at http://host/path (with no trailing slash)
                //
                // This is because the router accepts an absolute URI value of "same
                // as base URI but without trailing slash" as equivalent to "base URI",
                // which in turn is because it's common for servers to return the same page
                // for http://host/vdir as they do for host://host/vdir/ as it's no
                // good to display a blank page in that case.
                if (_hrefAbsolute[_hrefAbsolute.Length - 1] == '/'
                    && _hrefAbsolute.StartsWith(currentUriAbsolute, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// <inheritdoc/>
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "a");

            builder.AddMultipleAttributes(1, AdditionalAttributes);
            builder.AddAttribute(2, "class", CssClass);
            builder.AddAttribute(4, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, this.OnClick));
            builder.AddContent(3, ChildContent);
            builder.CloseElement();
        }

        private string CombineWithSpace(string str1, string str2)
            => str1 == null ? str2 : $"{str1} {str2}";

        private static bool IsStrictlyPrefixWithSeparator(string value, string prefix)
        {
            var prefixLength = prefix.Length;
            if (value.Length > prefixLength)
            {
                return value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                    && (
                        // Only match when there's a separator character either at the end of the
                        // prefix or right after it.
                        // Example: "/abc" is treated as a prefix of "/abc/def" but not "/abcdef"
                        // Example: "/abc/" is treated as a prefix of "/abc/def" but not "/abcdef"
                        prefixLength == 0
                        || !char.IsLetterOrDigit(prefix[prefixLength - 1])
                        || !char.IsLetterOrDigit(value[prefixLength])
                    );
            }
            else
            {
                return false;
            }
        }
    }
}
