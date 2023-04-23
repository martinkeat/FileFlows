namespace FileFlows.Plugin;

public enum FlowElementType
{
    Input,
    Output,
    Process,
    Logic,
    BuildStart,
    BuildEnd,
    BuildPart,
    Failure,
    Communication,
    Script
}

public enum FormInputType
{
    Text = 1,
    Switch = 2,
    Select = 3,
    TextArea = 4,
    Code = 5,
    Int = 6,
    Float = 7,
    StringArray = 8,
    File = 9,
    Folder = 10,
    LogView = 11,
    RegularExpression = 12,
    TextVariable = 13,
    KeyValue = 14,
    Label = 15,
    HorizontalRule = 16,
    Schedule = 17,
    Slider = 18,
    Checklist = 19,
    TextLabel = 20,
    Password = 21,
    ExecutedNodes = 22,
    Table = 23,
    Widget = 24,
    Metadata = 25,
    Period = 26,
    FileSize = 27
}


/// <summary>
/// A type of script
/// </summary>
public enum ScriptType
{
    /// <summary>
    /// A script used in a flow
    /// </summary>
    Flow = 0,
    /// <summary>
    /// A script used by the system to process something
    /// </summary>
    System = 1,
    /// <summary>
    /// A shared script which can be imported into other scripts
    /// </summary>
    Shared = 2,
    /// <summary>
    /// Template scripts used in the Function editor
    /// </summary>
    Template = 3,
    /// <summary>
    /// A scripts used by webhooks
    /// </summary>
    Webhook = 4
}