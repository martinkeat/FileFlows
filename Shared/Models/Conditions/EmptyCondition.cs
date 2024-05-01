namespace FileFlows.Shared.Models;

/// <summary>
/// Condition to test if a field is empty
/// </summary>
public class EmptyCondition : Condition
{
    /// <summary>
    /// Constructs a empty condition
    /// </summary>
    /// <param name="field">the field this condition is attached to</param>
    /// <param name="initialValue">the initial value of the field</param>
    public EmptyCondition(ElementField field, object initialValue) : base(field, initialValue)
    {

    }

    /// <summary>
    /// Test if the condition matches the given object value
    /// </summary>
    /// <param name="value">the value to test the condition against</param>
    /// <returns>true if the condition is matches</returns>
    public override bool Matches(object? value)
    {
        if (value == null)
        {
            return IsNot ? false : true;
        }
        if (value is string str)
        {
            bool empty = string.IsNullOrWhiteSpace(str);
            if (IsNot)
                empty = !empty;
            return empty;
        }
        if (value is IList list)
        {
            bool empty = list.Count == 0;
            if (IsNot)
                empty = !empty;
            return empty;
        }
        if (value.GetType().IsArray)
        {
            bool empty = ((Array)value).Length == 0;
            if (IsNot)
                empty = !empty;
            return empty;
        }
        if (value is int iValue)
        {
            return IsNot ? iValue > 0 : iValue == 0;
        }
        if (value is Int64 iValue64)
        {
            return IsNot ? iValue64 > 0 : iValue64 == 0;
        }
        if (value is bool bValue)
        {
            if (IsNot)
                bValue = !bValue;
            return bValue;
        }

        return base.Matches(value);
    }
}