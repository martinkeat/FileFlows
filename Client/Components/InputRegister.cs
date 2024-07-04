using FileFlows.Client.Components.Inputs;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components;

public abstract class InputRegister:ComponentBase
{
    protected readonly Dictionary<Guid, Inputs.IInput> RegisteredInputs = new();


    internal void RegisterInput<T>(Guid uid, Input<T> input)
    {
        if (this.RegisteredInputs.ContainsKey(uid) == false)
            this.RegisteredInputs.Add(uid, input);
        else
            this.RegisteredInputs[uid] = input;
    }

    // internal void RemoveRegisteredInputs(params string[] except)
    // {
    //     var listExcept = except?.ToList() ?? new();
    //     this.RegisteredInputs.RemoveAll(x => listExcept.Contains(x.Field?.Name ?? string.Empty) == false);
    // }

    // internal Inputs.IInput GetRegisteredInput(string name)
    // {
    //     return this.RegisteredInputs.Where(x => x.Field.Name == name).FirstOrDefault();
    // }

    /// <summary>
    /// Validates the registered inputs
    /// </summary>
    /// <returns>if all inputs are valid</returns>
    public async Task<bool> Validate()
    {
        bool valid = true;
        foreach (var ri in RegisteredInputs)
        {
            var input = ri.Value;
            if (input.Disabled || input.Visible == false)
                continue; // don't validate hidden or disabled inputs
            bool iValid = await input.Validate();
            if (iValid == false)
            {
                Logger.Instance.DLog("Invalid input: " + input.Label);
                valid = false;
            }
        }

        return valid;
    }
}