using Avalonia;

namespace FileFlows.Node.Ui;

/// <summary>
/// Avalonia app for showing a messge box
/// </summary>
internal class MessageApp : Application
{
    /// <inheritdoc />
    public override void Initialize()
    {
        base.Initialize();

        var message = "FileFlows Node is already running.";
        var title = "FileFlows";
        if (DataContext is MessageAppModel model)
        {
            title = model.Title?.EmptyAsNull() ?? title;
            message = model.Message?.EmptyAsNull() ?? message;
        }

        var window = new MessageBox(message, title);
        window.Show();
    }
}

/// <summary>
/// Represents a model for a message in the application.
/// </summary>
public class MessageAppModel
{
    /// <summary>
    /// Gets or sets the title of the message.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the content of the message.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
