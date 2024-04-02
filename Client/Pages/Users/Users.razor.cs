using FileFlows.Client.Components;
using FileFlows.Client.Components.Inputs;
using FileFlows.Plugin;
using Humanizer;

namespace FileFlows.Client.Pages;

/// <summary>
/// Page for revisions 
/// </summary>
public partial class Users: ListPage<Guid, User>
{
    public override string ApiUrl => "/api/user";

    public override string FetchUrl => $"{ApiUrl}";

    /// <summary>
    /// Gets if they are licensed for this page
    /// </summary>
    /// <returns>if they are licensed for this page</returns>
    protected override bool Licensed()
        => App.Instance.FileFlowsSystem.LicenseUserSecurity; 

    /// <summary>
    /// Adds a new user
    /// </summary>
    private async Task Add()
    {
        await Edit(new User()
        {
        });
    }
    
    public override async Task<bool> Edit(User item)
    {
        Blocker.Show();
        var isUser = item.Uid != Guid.Empty && (await HttpHelper.Post<bool>("/authorize/is-user/" + item.Uid)).Data;
        
        List<ElementField> fields = new List<ElementField>();

        var model = new UserEditModel()
        {
            Name = item.Name,
            IsAdmin = item.Role == UserRole.Admin,
            Password = item.Password,
            Uid = item.Uid,
            Email = item.Email
        };

        fields.Add(new ElementField
        {
            InputType = FormInputType.Text,
            Name = nameof(item.Name),
            Validators = new List<FileFlows.Shared.Validators.Validator> {
                new FileFlows.Shared.Validators.Required()
            }
        });
        fields.Add(new ElementField
        {
            InputType = FormInputType.Text,
            Name = nameof(item.Email),
            Validators = new List<FileFlows.Shared.Validators.Validator> {
                new FileFlows.Shared.Validators.Required()
            }
        });

        if (App.Instance.FileFlowsSystem.Security != SecurityMode.OpenIdConnect && isUser == false)
        {
            fields.Add(new ElementField
            {
                InputType = FormInputType.Password,
                Name = nameof(item.Password),
                Validators = new List<FileFlows.Shared.Validators.Validator> {
                    new FileFlows.Shared.Validators.Required()
                }
            });
        }
        fields.Add(new ElementField
        {
            InputType = FormInputType.Switch,
            Name = nameof(model.IsAdmin),
            Parameters = new ()
            {
                { nameof(InputSwitch.ReadOnly), isUser }
            }
        });
        
        Blocker.Hide();
        await Editor.Open(new()
        {
            TypeName = "Pages.Users", Title = "Pages.Users.Single", Fields = fields, Model = model,
            SaveCallback = Save
        });
        
        return false;
    }
    
    
    async Task<bool> Save(ExpandoObject model)
    {
        Blocker.Show();
        this.StateHasChanged();

        try
        {
            var user = new User();
            var dict = model as IDictionary<string, object>;
            user.Uid = (Guid)dict[nameof(UserEditModel.Uid)];
            user.Name = dict[nameof(UserEditModel.Name)].ToString() ?? string.Empty;
            user.Email = dict[nameof(UserEditModel.Email)].ToString() ?? string.Empty;
            if (dict.TryGetValue(nameof(UserEditModel.Password), out object oPassword) && oPassword is string password)
                user.Password = password;
            user.Role = dict[nameof(UserEditModel.IsAdmin)] as bool? == true ? UserRole.Admin : UserRole.Basic;
            
            var saveResult = await HttpHelper.Post<User>($"{ApiUrl}", user);
            if (saveResult.Success == false)
            {
                Toast.ShowError( saveResult.Body?.EmptyAsNull() ?? Translater.Instant("ErrorMessages.SaveFailed"));
                return false;
            }

            int index = this.Data.FindIndex(x => x.Uid == saveResult.Data.Uid);
            if (index < 0)
                this.Data.Add(saveResult.Data);
            else
                this.Data[index] = saveResult.Data;
            await this.Load(saveResult.Data.Uid);

            return true;
        }
        finally
        {
            Blocker.Hide();
            this.StateHasChanged();
        }
    }

    /// <summary>
    /// User edit model
    /// </summary>
    class UserEditModel : User
    {
        /// <summary>
        /// Gets or sets if the user is an admin
        /// </summary>
        public bool IsAdmin { get; set; }
    }
}