# Adding Dynamic Routing, Layouts and RouteViews to the Blazor App Component

> Publish Date: 2021-04-13  - Last Updated: 2024-08-06

## Overview

`App` is the Blazor UI root component.  This article looks at how it works and demonstrates how to:

1. Add Dynamic Layouts - change the default layout at runtime.
2. Add Dynamic Routes - add and remove extra routes at runtime.
3. Add Dynamic RouteViews - change the RouteView component directly without Routing.

![EditForm](https://shauncurtis.github.io/siteimages/Articles/App/Screenshot.png)

## Code and Examples

[The repository for this project is here](https://github.com/ShaunCurtis/CEC.Blazor.RouteView), and is based on my [Blazor AllInOne Template](https://github.com/ShaunCurtis/AllinOne).

You can view a demo of the components running on my Blazor.Database site here [https://cec-blazor-database.azurewebsites.net/](https://cec-blazor-database.azurewebsites.net/) from the highlighted links.

## The Blazor Application

`App` is normally defined in *App.razor*.  The same component is used in both Web Assembly and Server contexts.

In the Web Assembly context the SPA startup page contains an element placeholder which is replaced when `Program` starts in the Web Assembly context.

```html
....
<body>
    <div id="app">Loading...</div>
    ...
</body>
``` 
The code line that defines the replacement in `Program` is:
```csharp
// Replace the app id element with the component App
builder.RootComponents.Add<App>("#app");
```

In the Server context `App` is declared directly as a Razor component in the Razor markup.  It gets pre-rendered by the server and then updated by the Blazor Server client in the browser. 

```html
...
<body>
    <component type="typeof(Blazor.App)" render-mode="ServerPrerendered" />
    ...
</body>
```

## The App Component

The `App` code is shown below.  It's a standard Razor component, inheriting from `ComponentBase`.

`Router` is the local root component and sets `AppAssembly` to the assembly containing `Program`.  On initialization it trawls `Assembly` for all classes with a Route attribute and registers with the `NavigationChanged` event on the NavigationManager Service.  On a navigation event it tries to match the navigation Url to a route.  If it finds one, it renders the `Found` render fragment, otherwise it renders `NotFound`.

```html
<Router AppAssembly="@typeof(Program).Assembly" PreferExactMatches="@true">
    <Found Context="routeData">
        <RouteView RouteData="@routeData" DefaultLayout="@typeof(MainLayout)" />
    </Found>
    <NotFound>
        <LayoutView Layout="@typeof(MainLayout)">
            <p>Sorry, there's nothing at this address.</p>
        </LayoutView>
    </NotFound>
</Router>
```

`RouteView` is declared within `Found`.  `RouteData` is set to the router's current `routeData` object and `DefaultLayout` set to an application Layout `Type`.  `RouteView` renders an instance of `RouteData.Type` as a component within either the a page specific layout or the default layout, and applies any parameters in `RouteData.RouteValues`.

`NotFound` contains a `LayoutView` component, specifying a layout to render any child content in.

## RouteViewService

`RouteViewService` is the state management service for the new components.  It's registered in the WASM and Server Services.  The Server version can be either a Singleton or Scoped, depending on the application needs.  You could have two separate services to manage application and user contexts separately.

```csharp
public class RouteViewService 
{
    ...
}
``` 

In the Server it's added to `Startup` in `ConfigServices`.

```csharp
services.AddSingleton<RouteViewService>();
```

In the Web Assembly context it's added to `Program`.

```csharp
builder.Services.AddScoped<RouteViewService>();
```

### RouteViewManager

`RouteViewManager` replaces `RouteView`.

It's implements `RouteView`'s functionality.  It's too large to show in it's entirety so We'll look at the key functionality in sections.

When a routing event occurs, `RouteViewManager.RouteData` is updated and `Router` re-rendered.  The `Renderer` calls `SetParametersAsync` on  `RouteViewManager`, passing the updated *Parameters*.  `SetParametersAsync` checks it has a valid `RouteData`, sets `_ViewData` to null and renders the component.  `_ViewData` is set to null to ensure the component loads the route. A valid `ViewData` object has precedence over a valid `RouteData` object in the render process.
  
```csharp
public await Task SetParametersAsync(ParameterView parameters)
{
    // Sets the component parameters
    parameters.SetParameterProperties(this);
    // Check if we have either RouteData or ViewData
    if (RouteData == null)
    {
        throw new InvalidOperationException($"The {nameof(RouteView)} component requires a non-null value for the parameter {nameof(RouteData)}.");
    }
    // we've routed and need to clear the ViewData
    this._ViewData = null;
    // Render the component
    await this.RenderAsync();
}
```

`Render` uses InvokeAsync to ensure the render event is run on the correct thread context. `_RenderEventQueued` ensures there's only only one render event in the Renderer's queue.

```csharp
public async Task RenderAsync() => await InvokeAsync(() =>
    {
        if (!this._RenderEventQueued)
        {
            this._RenderEventQueued = true;
            _renderHandle.Render(_renderDelegate);
        }
    }
);
```

For those curious, `InvokeAsync` looks like this.

```csharp
protected Task InvokeAsync(Action workItem) => _renderHandle.Dispatcher.InvokeAsync(workItem);
```

`RouteViewManager`s content is built as a set of components, each defined within a `RenderFragment`.

`_renderDelegate` defines the local root component, cascading itself and adding the `_layoutViewFragment` fragment as it's `ChildContent`.

```csharp
private RenderFragment _renderDelegate => builder =>
{
    // We're being executed so no longer queued
    _RenderEventQueued = false;
    // Adds cascadingvalue for the ViewManager
    builder.OpenComponent<CascadingValue<RouteViewManager>>(0);
    builder.AddAttribute(1, "Value", this);
    // Get the layout render fragment
    builder.AddAttribute(2, "ChildContent", this._layoutViewFragment);
    builder.CloseComponent();
};
```

`_layoutViewFragment` selects the layout, adds it and sets `_renderComponentWithParameters` as it's `ChildContent`.

```csharp
private RenderFragment _layoutViewFragment => builder =>
{
    Type _pageLayoutType = RouteData?.PageType.GetCustomAttribute<LayoutAttribute>()?.LayoutType
        ?? RouteViewService.Layout
        ?? DefaultLayout;

    builder.OpenComponent<LayoutView>(0);
    builder.AddAttribute(1, nameof(LayoutView.Layout), _pageLayoutType);
    builder.AddAttribute(2, nameof(LayoutView.ChildContent), _renderComponentWithParameters);
    builder.CloseComponent();
};
```

`_renderComponentWithParameters` selects the view/route component to render and adds it with the supplied parameters.  A valid view take precedence over a valid route.

```csharp
private RenderFragment _renderComponentWithParameters => builder =>
{
    Type componentType = null;
    IReadOnlyDictionary<string, object> parameters = new Dictionary<string, object>();

    if (_ViewData != null)
    {
        componentType = _ViewData.ViewType;
        parameters = _ViewData.ViewParameters;
    }
    else if (RouteData != null)
    {
        componentType = RouteData.PageType;
        parameters = RouteData.RouteValues;
    }

    if (componentType != null)
    {
        builder.OpenComponent(0, componentType);
        foreach (var kvp in parameters)
        {
            builder.AddAttribute(1, kvp.Key, kvp.Value);
        }
        builder.CloseComponent();
    }
    else
    {
        builder.OpenElement(0, "div");
        builder.AddContent(1, "No Route or View Configured to Display");
        builder.CloseElement();
    }
};
```

## Dynamic Layouts

Out-of-the-box, Blazor layouts are defined and fixed at compile time.  `@Layout` is Razor talk that gets transposed when the Razor is pre-compiled to:

```csharp
[Microsoft.AspNetCore.Components.LayoutAttribute(typeof(MainLayout))]
[Microsoft.AspNetCore.Components.RouteAttribute("/")]
[Microsoft.AspNetCore.Components.RouteAttribute("/index")]
public partial class Index : Microsoft.AspNetCore.Components.ComponentBase
...
```

To change Layouts dynamically we use `RouteViewService` to store the layout. It can be set from any component that injects the service.

```csharp
public class RouteViewService
{
    public Type Layout { get; set; }
    ...
}
```

`_layoutViewFragment` in `RouteViewManager` chooses the layout - `RouteViewService.Layout` is set above the default layout in precedence.
```csharp
private RenderFragment _layoutViewFragment => builder =>
{
    Type _pageLayoutType = RouteData?.PageType.GetCustomAttribute<LayoutAttribute>()?.LayoutType
        ?? RouteViewService.Layout
        ?? DefaultLayout;

    builder.OpenComponent<LayoutView>(0);
    builder.AddAttribute(1, nameof(LayoutView.Layout), _pageLayoutType);
    builder.AddAttribute(2, nameof(LayoutView.ChildContent), _renderComponentWithParameters);
    builder.CloseComponent();
};
```

Changing in the layout is demonstrated in the demo pages.

## Dynamic Routing

Dynamic Routing is a little more complicated.  `Router` is a sealed box, so it's take it or re-write it.  Unless you must, don't re-write it.  We're not looking to change existing routes, just add and remove new dynamic routes.

Routes are defined at compile time and are used internally within the `Router` Component.

RouteView Razor Pages are labelled like this:
```html
@page "/"
@page "/index"
```

This is Razor talk, and gets transposed into the following in the C# class when pre-compiled.

```csharp
[Microsoft.AspNetCore.Components.RouteAttribute("/")]
[Microsoft.AspNetCore.Components.RouteAttribute("/index")]
public partial class Index : Microsoft.AspNetCore.Components.ComponentBase
...
```

When `Router` initializes it trawls any assemblies provided and builds a route dictionary of component/route pairs.

You can get a list of route attribute components like this:

```csharp
static public IEnumerable<Type> GetTypeListWithCustomAttribute(Assembly assembly, Type attribute)
    => assembly.GetTypes().Where(item => (item.GetCustomAttributes(attribute, true).Length > 0));
```

On initial render the Router register a delegate with the `NavigationManager.LocationChanged` event.  This delegate looks up routes and triggers render events on the `Router`. If it finds a route it renders `Found` which renders our new `RouteViewManager`.  `RouteViewManager` builds out the Layout and adds a new instance of the component defined in `RouteData`.

When it doesn't find a route, what happens depends on the `IsNavigationIntercepted` property of the `LocationChangedEventArgs` provided by the event:

1. True if it intercepts navigation in the DOM - anchors, etc.
2. True if a UI component calls it's `NavigateTo` method and sets `ForceLoad`.
3. False if a UI component calls it's `NavigateTo` method and sets `ForceLoad`.

If we can avoid causing a hard navigation events in `Router`, we can add a component in `NotFound` to handle additional dynamic routing.  Not too difficult, it is our code!  There's an enhanced `NavLink` control to help control navigation - covered later.  In the event of a hard navigation event, routing will still work, but the application reloads.  Any rogue navigation events should be detected and fixed during testing.

### CustomRouteData

`CustomRouteData` holds the information needed to make routing decisions.  The class looks like this with inline detailed explanations.  

```csharp
public class CustomRouteData
{
    /// <summary>
    /// The standard RouteData.
    public RouteData RouteData { get; private set; }

    /// <summary>
    /// The PageType to load on a match.
    /// </summary>
    public Type PageType { get; set; }

    /// <summary>
    /// The Regex String to define the route.
    /// </summary>
    public string RouteMatch { get; set; }

    /// <summary>
    /// Parameter values to add to the Route when created name/defaultvalue.
    /// </summary>
    public SortedDictionary<string, object> ComponentParameters { get; set; } = new SortedDictionary<string, object>();

    /// <summary>
    /// Method to check if we have a route match.
    /// </summary>
    public bool IsMatch(string url)
    {
        // get the match
        var match = Regex.Match(url, this.RouteMatch,RegexOptions.IgnoreCase);
        if (match.Success)
        {
            // create new dictionary object to add to the RouteData
            var dict = new Dictionary<string, object>();
            //  check we have the same or fewer groups as parameters to map the to
            if (match.Groups.Count >= ComponentParameters.Count)
            {
                var i = 1;
                // iterate through the parameters and add the next match
                foreach (var pars in ComponentParameters)
                {
                    string matchValue = string.Empty;
                    if (i < match.Groups.Count)
                        matchValue = match.Groups[i].Value;
                    //  Use a StypeSwitch object to do the Type Matching and create the dictionary pair 
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
            // create a new RouteData object and assign it to the RouteData property. 
            this.RouteData = new RouteData(this.PageType, dict);
        }
        return match.Success;
    }

    /// <summary>
    /// Method to check if we have a route match and return the RouteData.
    /// </summary>
    public bool IsMatch(string url, out RouteData routeData)
    {
        routeData = this.RouteData;
        return IsMatch(url);
    }
}
```

For those interested, `TypeSwitch` looks like this (thanks to *cdiggins* on StackOverflow for the code):

```csharp
/// =================================
/// Author: stackoverflow: cdiggins
/// ==================================
public class TypeSwitch
{
    public TypeSwitch Case<T>(Action<T> action) { matches.Add(typeof(T), (x) => action((T)x)); return this; }
    private Dictionary<Type, Action<object>> matches = new Dictionary<Type, Action<object>>();
    public void Switch(object x) { matches[x.GetType()](x); }
}
```

## Updates to the RouteViewService

The updated sections in `RouteViewService` are shown below. `Routes` holds the list of custom routes - it's deliberately left open for customization. 

```csharp
public List<CustomRouteData> Routes { get; private set; } = new List<CustomRouteData>();

public bool GetRouteMatch(string url, out RouteData routeData)
{
    var route = Routes?.FirstOrDefault(item => item.IsMatch(url)) ?? null;
    routeData = route?.RouteData ?? null;
    return route != null;
}
```

## The RouteNotFoundManager Component

`RouteNotFoundManager` is a simple version of `RouteViewManager`.

`SetParametersAsync` is called when the component loads.  It gets the local Url, calls `GetRouteMatch` on `RouteViewService`, and renders the component.  If there's no layout, it just renders the `ChildContent`.

```csharp
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
```

`_ViewFragment` either renders a `RouteViewManager`, setting `RouteData` if it finds a custom route, or the contents of `RouteNotFoundManager`. 
  
```csharp
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
```

## Switching the RouteView Without Routing

Switching the RouteView without routing has several applications.  These are some I've used:

1. Hide direct access to a page.  It can only be accessed within the application.
2. Multipart forms/processes with a single entry point.  The state of the saved form/process dictates which form gets loaded.
3. Context dependant forms or information.  Login/logout/signup is a good example.  The same Url but with a different routeviews loaded depending on the context.

### ViewData

The equivalent to `RouteData`.

```csharp
public class ViewData
{
    /// Gets the type of the View.
    public Type ViewType { get; set; }

    /// Gets the type of the page matching the route.
    public Type LayoutType { get; set; }

    /// Parameter values to add to the Route when created
    public Dictionary<string, object> ViewParameters { get; private set; } = new Dictionary<string, object>();

    /// Constructs an instance of <see cref="ViewData"/>.
    public ViewData(Type viewType, Dictionary<string, object> viewValues = null)
    {
        if (viewType == null) throw new ArgumentNullException(nameof(viewType));
        this.ViewType = viewType;
        if (viewValues != null) this.ViewParameters = viewValues;
    }
}
```

All functionality is implemented in `RouteViewManager`.

### RouteViewManager

First some properties and fields. 
```csharp
/// <summary>
/// The size of the History list used for Views.
/// </summary>
[Parameter] public int ViewHistorySize { get; set; } = 10;

/// <summary>
/// Gets and sets the view data.
/// </summary>
public ViewData ViewData
{
    get => this._ViewData;
    protected set
    {
        this.AddViewToHistory(this._ViewData);
        this._ViewData = value;
    }
}

/// <summary>
/// Property that stores the View History.  It's size is controlled by ViewHistorySize
/// </summary>
public SortedList<DateTime, ViewData> ViewHistory { get; private set; } = new SortedList<DateTime, ViewData>();

/// <summary>
/// Gets the last view data.
/// </summary>
public ViewData LastViewData
{
    get
    {
        var newest = ViewHistory.Max(item => item.Key);
        if (newest != default) return ViewHistory[newest];
        else return null;
    }
}

/// <summary>
/// Method to check if <param name="view"> is the current View.
/// </summary>
public bool IsCurrentView(Type view) => this.ViewData?.ViewType == view;

/// <summary>
/// Boolean to check if we have a View set.
/// </summary>
public bool HasView => this._ViewData?.ViewType != null;

/// <summary>
/// Internal ViewData used by the component.
/// </summary>
private ViewData _ViewData { get; set; }
```

Next a set of `LoadViewAsync` methods to provide a variety of ways to load a new view.  The main method sets the internal `viewData` field and calls `Render` to re-render the component.

```csharp
/// <summary>
/// The main method
/// </summary>
public await Task LoadViewAsync(ViewData viewData = null)
{
    if (viewData != null) this.ViewData = viewData;
    if (ViewData == null)
    {
        throw new InvalidOperationException($"The {nameof(RouteViewManager)} component requires a non-null value for the parameter {nameof(ViewData)}.");
    }
    await this.RenderAsync();
}

public async Task LoadViewAsync(Type viewtype)
    => await this.LoadViewAsync(new ViewData(viewtype, new Dictionary<string, object>()));

public async Task LoadViewAsync<TView>(Dictionary<string, object> data = null)
    => await this.LoadViewAsync(new ViewData(typeof(TView), data));
```

We have already seen `_renderComponentWithParameters`.  With a valid `_ViewData` object, it renders the component using `_ViewData`.

```csharp
private RenderFragment _renderComponentWithParameters => builder =>
{
    Type componentType = null;
    IReadOnlyDictionary<string, object> parameters = new Dictionary<string, object>();

    if (_ViewData != null)
    {
        componentType = _ViewData.ViewType;
        parameters = _ViewData.ViewParameters;
    }
    else if (RouteData != null)
    {
        componentType = RouteData.PageType;
        parameters = RouteData.RouteValues;
    }

    if (componentType != null)
    {
        builder.OpenComponent(0, componentType);
        foreach (var kvp in parameters)
        {
            builder.AddAttribute(1, kvp.Key, kvp.Value);
        }
        builder.CloseComponent();
    }
    else
    {
        builder.OpenElement(0, "div");
        builder.AddContent(1, "No Route or View Configured to Display");
        builder.CloseElement();
    }
};
```

### RouteNavLink

`RouteNavLink` is an enhanced `NavLink` control.  The code is a direct copy with a small amount of added code.  It doesn't inherit because `NavLink` is a black box.  It ensures navigation is through the NavigationManager rather than Html anchor links and provides direct access to RouteView loading.  The code is in the Repo - it's too long to reproduce here.

## Example Pages

The application has RouteViews/Pages to demonstrate the new components.  You can review the source code in the Repo.  You can also see the pages on the Demo Site.

![EditForm](https://shauncurtis.github.io/siteimages/Articles/App/DemoSite.png)

### RouteViewer.razor

[https://cec-blazor-database.azurewebsites.net/routeviewer](https://cec-blazor-database.azurewebsites.net/routeviewer)

This demonstrates:

1. Adding routes dynamically to the Application.  Choose a page to add a custom route for, add a route name and click *Go To Route*.
2. Loading a `RouteView` without navigation.  Choose a Page and click on *Go To View*.  The page is displayed, but the Url doesn't change!  Confusing, but it demos the principle.
3. Changing the default Layout.  Click on *Red Layout* and the layout will change to red.  Basic FetchData has a specific layout defined so it will use the original layout.  Click on *Normal Layout* to change back.

### Form.Razor

[https://cec-blazor-database.azurewebsites.net/form](https://cec-blazor-database.azurewebsites.net/form)

This demonstrates a multipart form.  There are four forms:
1. *Form.Razor* the base and first form.
2.  *Form2.Razor* the second form - inherits from the first form.
3.  *Form3.Razor* the third form - inherits from the first form.
4.  *Form4.Razor* the result form - inherits from the first form.

The forms link to data in the WeathForecastService which maintains the form state.  Try leaving the form part way through and then returning.  State is preserved while the SPA session is maintained.

## Wrap Up

Hopefully I've demonstrated the principles you can use to build the extra functionality into the core Blazor framework.  None of the components are finished articles.  Use them and develop them as you wish.   

If you're reading this article a long time into the future chack [here](https://shauncurtis.github.io/articles/A-Flexible-App.html) for the latest version.
