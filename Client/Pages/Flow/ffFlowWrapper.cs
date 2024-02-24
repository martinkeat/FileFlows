
using System.Text.RegularExpressions;
using BlazorMonaco;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using NPoco;
using ffElement = FileFlows.Shared.Models.FlowElement;
using xFlowConnection = FileFlows.Shared.Models.FlowConnection;
using ffPart = FileFlows.Shared.Models.FlowPart;

namespace FileFlows.Client.Pages;

/// <summary>
/// Wrapper for the javascript ffFlow
/// </summary>
public class ffFlowWrapper
{
    /// <summary>
    /// The reference to the ffFlow object created in javasscirpt
    /// </summary>
    private IJSObjectReference jsffFlow;

    private IJSRuntime jsRuntime;
    
    public Func<string, Task<object>> OnAddElement { get; set; }
    public Func<Flow.OpenContextMenuArgs, Task> OnOpenContextMenu { get; set; }
    
    public Func<ffPart, bool, Task<object>> OnEdit { get; set; }
    public Action OnMarkDirty { get; set; }
    public Action<ffPart> OnCtrlDblClick { get; set; }

    
    private ffFlowWrapper(IJSRuntime jsRuntime)
    {
        this.jsRuntime = jsRuntime;
        
    }

    public static async Task<ffFlowWrapper> Create(IJSRuntime jsRuntime, Guid flowUid, bool readOnly = false)
    {
        var instance = new ffFlowWrapper(jsRuntime);
        var dotNetObjRef = DotNetObjectReference.Create(instance);
        instance.jsffFlow = await jsRuntime.InvokeAsync<IJSObjectReference>("createffFlow", dotNetObjRef, flowUid, readOnly);
        return instance;
    }
    
    public async Task ioInitConnections(Dictionary<string, List<xFlowConnection>> connections)
    {
        if(jsffFlow != null)
            await jsffFlow.InvokeVoidAsync("ioInitConnections",  connections);
    }

    public async Task init(List<FlowPart> parts, FlowElement[] available)
        => await jsffFlow.InvokeVoidAsync("init",  parts, available);

    public async Task setVisibility(bool show)
        => await jsffFlow.InvokeVoidAsync("setVisibility", show);
    
    public async Task dispose()
        => await jsffFlow.InvokeVoidAsync("dispose");

    public async Task redrawLines()
        => await jsffFlow.InvokeVoidAsync("redrawLines");

    public async Task zoom(int zoom)
        => await jsffFlow.InvokeVoidAsync("zoom", zoom);

    public async Task<List<FlowPart>> getModel()
        => await jsffFlow.InvokeAsync<List<FlowPart>>("getModel");

    public async Task insertElement(string itemUid)
        => await jsffFlow.InvokeVoidAsync("insertElement", itemUid);

    public async Task focusElement(string uid)
        => await jsffFlow.InvokeVoidAsync("ffFlowPart.focusElement", uid);

    public async Task contextMenu(string action, params object[] parameters)
        => await jsffFlow.InvokeVoidAsync("contextMenu_" + action, parameters);

    public async Task undo()
        => await jsffFlow.InvokeVoidAsync("History.undo");
    public async Task redo()
        => await jsffFlow.InvokeVoidAsync("History.redo");

    public async Task addElementActual(string uid, int xPos, int yPos)
        => await jsffFlow.InvokeVoidAsync("addElementActual", uid, xPos, yPos);

    public async Task dragElementStart(string uid)
        => await jsffFlow.InvokeVoidAsync("Mouse.dragElementStart", uid);

    public async Task focusName()
        => await jsffFlow.InvokeVoidAsync("focusName");


    [JSInvokable]
    public string NewGuid() => Guid.NewGuid().ToString();

    [JSInvokable]
    public async Task<object> AddElement(string uid)
        => await OnAddElement(uid);

    [JSInvokable]
    public async Task OpenContextMenu(Flow.OpenContextMenuArgs args)
        => await OnOpenContextMenu(args);

    [JSInvokable]
    public async Task<object> Edit(ffPart part, bool isNew = false)
        => await OnEdit(part, isNew);

    [JSInvokable]
    public void MarkDirty()
        => OnMarkDirty();

    [JSInvokable]
    public void CtrlDblClick(ffPart part)
        => OnCtrlDblClick(part);

    /// <summary>
    /// Translates a string
    /// </summary>
    /// <param name="key">the string key to translate</param>
    /// <param name="model">the model to pass into the translation</param>
    /// <returns>the translated string</returns>
    [JSInvokable]
    public string Translate(string key, ExpandoObject model)
    {
        string prefix = string.Empty;
        if (key.Contains(".Outputs."))
        {
            prefix = Translater.Instant("Labels.Output", suppressWarnings: true) + " " + key.Substring(key.LastIndexOf(".", StringComparison.Ordinal) + 1) + ": ";
        }

        var dict = model?.Where(x => x.Value != null)?.ToDictionary(x => x.Key, x => x.Value)
                   ?? new();

        string translated = Translater.Instant(key, dict, suppressWarnings: true);
        if (Regex.IsMatch(key, "^[\\d]+$"))
            return string.Empty;
        return prefix + translated;
    }
}