namespace FileFlows.Client.Components.Inputs
{
    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.Components.Web;

    public partial class InputTextAreaList : Input<List<string>>
    {
        public override bool Focus() => FocusUid();

        private string TextValue { get; set; }

        private bool _UpdatingValue = false;
        protected override void ValueUpdated()
        {
            if (_UpdatingValue)
                return;

            TextValue = String.Join("\n", this.Value ?? new List<string>());
            ClearError();
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            TextValue = String.Join("\n", this.Value ?? new List<string>());
        }

        public override Task<bool> Validate()
        {
            this.UpdateValue();
            return base.Validate();
        }

        private void UpdateValue()
        {
            this._UpdatingValue = true;
            this.Value = TextValue?.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)?.Select(x => x.Trim())?.ToList() ?? new();
            this._UpdatingValue = false;
        }

        private async Task OnKeyDown(KeyboardEventArgs e)
        {
            if (e.Code == "Enter" && e.ShiftKey) // for textarea the shortcut to submit is shift enter
            {
                this.UpdateValue();
                await OnSubmit.InvokeAsync();
            }
            else if (e.Code == "Escape")
                await OnClose.InvokeAsync();
        }
    }
}