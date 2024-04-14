namespace FileFlows.Shared.Models;


/// <summary>
/// Condition to test if the field matches any of the given values
/// </summary>
public class AnyCondition : Condition
{
    /// <summary>
    /// Constructs a any condition
    /// </summary>
    /// <param name="field">the field this condition is attached to</param>
    /// <param name="initialValue">the initial value of the field</param>
    /// <param name="values">the value to evaluate for</param>
    /// <param name="isNot">if the condition should NOT match the value</param>
    public AnyCondition(ElementField field, object? initialValue, object values = null, bool isNot = false) 
        : base(field, initialValue, values, isNot)
    {
    }

    /// <summary>
    /// Test if the condition matches the given object value
    /// </summary>
    /// <param name="value">the value to test the condition against</param>
    /// <returns>true if the condition is matches</returns>
    public override bool Matches(object? value)
    {
        if (this.Value == null)
            return false;
        
        if (this.Value is IList list)
        {
            if (list.Count == 0)
                return false;
            foreach (var v in list)
            {
                if (Matches(v, value, this.IsNot))
                    return true;
            }

            return false;
        }

        if (this.Value is Array array)
        {
            if (array.Length == 0)
                return false;
            
            foreach (var v in array)
            {
                if (Matches(v, value, this.IsNot))
                    return true;
            }

            return false;
        }
        
        return Matches(this.Value, value, this.IsNot);
    }
}

