using Humanizer.DateTimeHumanizeStrategy;

namespace FileFlows.Client.Components.Inputs
{
    using System.Collections.Generic;
    using Microsoft.AspNetCore.Components;
    using System.Linq;
    using FileFlows.Shared;
    using FileFlows.Shared.Models;
    using System.Text.Json;
    using System.Threading.Tasks;
    using FileFlows.Plugin;
    using System;
    using Microsoft.AspNetCore.Components.Web;

    public partial class InputSelect : Input<object>
    {
        private Dictionary<string, List<ListOption>> Groups = new Dictionary<string, List<ListOption>>();
        private readonly List<ListOption> _Options = new List<ListOption>();

        [Parameter] public bool ShowDescription { get; set; }

        private string Description { get; set; }
        /// <summary>
        /// If the value is currently being updated via UI action and if any update events received should be ignored during this update
        /// </summary>
        private bool UpdatingValue = false;

#pragma warning disable BL0007
        [Parameter]
        public IEnumerable<ListOption> Options
        {
            get => _Options;
            set
            {
                _Options.Clear();
                if (value == null)
                    return;
                string group = string.Empty;
                Groups = new Dictionary<string, List<ListOption>>();
                Groups.Add(string.Empty, new List<ListOption>());
                foreach (var lo in value)
                {
                    if (lo.Value is JsonElement je && je.ValueKind == JsonValueKind.String)
                        lo.Value = je.GetString();  // this can happen from the Templates where the object is a JsonElement

                    if(lo.Value is string && (string)lo.Value == Globals.LIST_OPTION_GROUP)
                    {
                        group = lo.Label!;
                        Groups.Add(group, new List<ListOption>());
                        continue;
                    }
                    if (Translater.NeedsTranslating(lo.Label))
                        lo.Label = Translater.Instant(lo.Label);
                    _Options.Add(lo);
                    Groups[group].Add(lo);
                }
            }
        }
#pragma warning restore BL0007

        public override bool Focus() => FocusUid();

        private string lblSelectOne;
        [Parameter] public bool AllowClear { get; set; }
        
        /// <summary>
        /// Gets or sets if the clear label should be blank
        /// </summary>
        [Parameter] public bool BlankClearLabel { get; set; }

        private int _SelectedIndex = -1;
        public int SelectedIndex
        {
            get => _SelectedIndex;
            set
            {
                _SelectedIndex = value;
                if (value == -1 || Options == null || Options.Any() == false)
                    this.Value = null;
                else
                    this.Value = Options.ToArray()[value].Value;
                UpdateDescription();
            }
        }

        /// <inheritdoc />
        protected override void OnInitialized()
        {
            base.OnInitialized();
            lblSelectOne = BlankClearLabel ? string.Empty : Translater.Instant("Labels.SelectOne");
            ValueUpdated();
        }

        protected override void ValueUpdated()
        {
            if (UpdatingValue)
                return;

            int startIndex = SelectedIndex;
            if (Value != null)
            {
                var opt = Options.ToArray();
                var valueJson = JsonSerializer.Serialize(Value);
                var objReference = TryParseObjectReference(valueJson);
                for (int i = 0; i < opt.Length; i++)
                {
                    if (opt[i].Value == Value)
                    {
                        startIndex = i;
                        break;
                    }
                    string optJson = JsonSerializer.Serialize(opt[i].Value);                    
                    if (optJson.ToLower() == valueJson.ToLower())
                    {
                        startIndex = i;
                        break;
                    }

                    if (objReference.Uid != Guid.Empty)
                    {
                        // incase the object reference name has changed, we look for the UID
                        var otherObjReference = TryParseObjectReference(optJson);
                        if (otherObjReference.Uid == objReference.Uid)
                        {
                            startIndex = i;
                            break;
                        }
                    }
                }
            }

            if (startIndex == -1)
            {
                if (AllowClear)
                {
                    startIndex = -1;
                }
                else
                {
                    startIndex = 0;
                    Value = Options.FirstOrDefault()?.Value;
                }
            }
            if(startIndex != SelectedIndex)
                SelectedIndex = startIndex;
        }

        private ObjectReference TryParseObjectReference(string json)
        {
            try
            {
                return JsonSerializer.Deserialize<ObjectReference>(json);
            }
            catch (Exception)
            {
                return new ObjectReference { Name = string.Empty, Type = string.Empty, Uid = Guid.Empty };
            }
        }

        private void SelectionChanged(ChangeEventArgs args)
        {
            UpdatingValue = true;
            try
            {
                if (int.TryParse(args?.Value?.ToString(), out int index))
                    SelectedIndex = index;
                else
                    Logger.Instance.DLog("Unable to find index of: ",  args?.Value);
                UpdateDescription();
            }
            finally
            {
                UpdatingValue = false;
            }
        }

        public override async Task<bool> Validate()
        {
            if (this.SelectedIndex == -1)
            {
                ErrorMessage = Translater.Instant($"Validators.Required");
                return false;
            }
            return await base.Validate();
        }

        private void UpdateDescription()
        {
            Description = string.Empty;
            if (this.ShowDescription == false)
                return;

            var dict = Value as IDictionary<string, object>;

            if (dict == null)
            {
                try
                {
                    string json = JsonSerializer.Serialize(Value);
                    ExpandoObject? eo = JsonSerializer.Deserialize<System.Dynamic.ExpandoObject>(json);
                    dict = eo == null ? new Dictionary<string, object>() : (IDictionary<string, object>)eo!;
                }
                catch (Exception)
                {
                    // ingored
                }
            }

            if (dict?.TryGetValue("Description", out var value) is true)
                Description = value?.ToString() ?? string.Empty;

            this.Help = Description;
            this.StateHasChanged();
        }


        private async Task OnKeyDown(KeyboardEventArgs e)
        {
            if (e.Code == "Enter")
                await OnSubmit.InvokeAsync();
            else if(e.Code == "Escape")
                await OnClose.InvokeAsync();    
        }
    }
}