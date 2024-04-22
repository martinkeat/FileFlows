namespace FileFlows.Client.Pages;

public partial class Notifications : ListPage<Guid, Notification>
{
    public override string ApiUrl => "/api/notification";
    
    public override Task<bool> Edit(Notification item)
    {
        throw new NotImplementedException();
    }
}