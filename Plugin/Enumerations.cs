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
    Script,
    SubFlow
}

/// <summary>
/// Form Input Types
/// </summary>
public enum FormInputType
{
    /// <summary>
    /// Text
    /// </summary>
    Text = 1,
    /// <summary>
    /// Swtich
    /// </summary>
    Switch = 2,
    /// <summary>
    /// Select
    /// </summary>
    Select = 3,
    /// <summary>
    /// TextArea
    /// </summary>
    TextArea = 4,
    /// <summary>
    /// Code
    /// </summary>
    Code = 5,
    /// <summary>
    /// Int
    /// </summary>
    Int = 6,
    /// <summary>
    /// Float
    /// </summary>
    Float = 7,
    /// <summary>
    /// String Array
    /// </summary>
    StringArray = 8,
    /// <summary>
    /// File
    /// </summary>
    File = 9,
    /// <summary>
    /// Folder
    /// </summary>
    Folder = 10,
    /// <summary>
    /// Log View
    /// </summary>
    LogView = 11,
    /// <summary>
    /// Regular Expression
    /// </summary>
    RegularExpression = 12,
    /// <summary>
    /// Text Variable
    /// </summary>
    TextVariable = 13,
    /// <summary>
    /// Key Value
    /// </summary>
    KeyValue = 14,
    /// <summary>
    /// Label
    /// </summary>
    Label = 15,
    /// <summary>
    /// Horizontal Rule
    /// </summary>
    HorizontalRule = 16,
    /// <summary>
    /// Schedule
    /// </summary>
    Schedule = 17,
    /// <summary>
    /// Slider
    /// </summary>
    Slider = 18,
    /// <summary>
    /// Checklist
    /// </summary>
    Checklist = 19,
    /// <summary>
    /// Text Label
    /// </summary>
    TextLabel = 20,
    /// <summary>
    /// Password
    /// </summary>
    Password = 21,
    /// <summary>
    /// Executed Nodes
    /// </summary>
    ExecutedNodes = 22,
    /// <summary>
    /// Table
    /// </summary>
    Table = 23,
    /// <summary>
    /// Widget
    /// </summary>
    Widget = 24,
    /// <summary>
    /// Metadata
    /// </summary>
    Metadata = 25,
    /// <summary>
    /// Period
    /// </summary>
    Period = 26,
    /// <summary>
    /// File Size
    /// </summary>
    FileSize = 27,
    /// <summary>
    /// Button
    /// </summary>
    Button = 28
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