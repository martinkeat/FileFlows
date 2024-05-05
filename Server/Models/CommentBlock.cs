using System.Text;

namespace FileFlows.Server.Models;

/// <summary>
/// Represents a block of comments with name/value pairs.
/// </summary>
public class CommentBlock
{
    private readonly string _originalComment;
    private readonly Dictionary<string, string> _commentDict;
    private readonly List<string> _nonKeyValueLines;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommentBlock"/> class.
    /// </summary>
    /// <param name="comment">The original comment block string.</param>
    public CommentBlock(string comment)
    {
        comment ??= string.Empty;
        _originalComment = comment;
        _commentDict = new();
        _nonKeyValueLines = new();

        foreach (string line in comment?.Split('\n'))
        {
            if (line.TrimStart().StartsWith("@"))
            {
                string[] parts = line.Split(new[] { '@', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    string key = parts[0];
                    string value = string.Join(" ", parts.Skip(1));
                    _commentDict[key] = value;
                }
            }
            else
            {
                _nonKeyValueLines.Add(line.TrimEnd());
            }
        }
    }

    /// <summary>
    /// Adds or updates a name/value pair in the comment block.
    /// </summary>
    /// <param name="name">The name of the key.</param>
    /// <param name="value">The value of the key.</param>
    public void AddOrUpdate(string name, string value) =>
        _commentDict[name] = value ?? throw new ArgumentNullException(nameof(value));


    /// <summary>
    /// Gets the value of a parameter with the specified name.
    /// </summary>
    /// <param name="name">The name of the parameter.</param>
    /// <returns>The value of the parameter</returns>
    public string GetValue(string name)
    {
        if (_commentDict.TryGetValue(name, out string? value))
            return value;
        return string.Empty;
    }
    
    /// <summary>
    /// Returns the updated comment block string.
    /// </summary>
    /// <returns>The updated comment block string.</returns>
    public override string ToString()
    {
        StringBuilder sb = new();
        sb.AppendLine("/**");
        foreach (KeyValuePair<string, string> kvp in _commentDict)
        {
            sb.AppendFormat(" * {0} {1}\n", kvp.Key, kvp.Value);
        }
        foreach (string line in _nonKeyValueLines)
        {
            sb.AppendFormat(" * {0}\n", line);
        }
        sb.AppendLine(" */");
        return sb.ToString();
    }

    /// <summary>
    /// Determines whether this comment block is equal to another comment block, ignoring any whitespace differences.
    /// </summary>
    /// <param name="other">The other comment block to compare.</param>
    /// <returns><c>true</c> if the comment blocks are equal ignoring whitespace; otherwise, <c>false</c>.</returns>
    public bool EqualsIgnoreWhitespace(CommentBlock other)
    {
        if (other is null)
            return false;

        string thisStr = RemoveWhitespace(_originalComment);
        string otherStr = RemoveWhitespace(other._originalComment);

        return thisStr.Equals(otherStr, StringComparison.OrdinalIgnoreCase);
    }

    private static string RemoveWhitespace(string str)
    {
        return new string(str.ToCharArray()
            .Where(c => !Char.IsWhiteSpace(c))
            .ToArray());
    }
}

