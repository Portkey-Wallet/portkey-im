namespace IM.User;

public class ReportUserImMessageCmd
{
    public string UserId { get; set; }
	public string UserAddress { get; set; }
	public string ReportedUserId {get; set; }
	public string ReportedUserAddress {get; set; }
	public int ReportType {get; set; }
	public string Message {get; set; }
	public string MessageId {get; set; }
	public string Description {get; set; }
}