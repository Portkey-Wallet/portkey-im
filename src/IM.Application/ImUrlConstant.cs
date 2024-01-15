namespace IM;

public class ImUrlConstant
{
    public static readonly string CreateChannel = "api/v1/channel/create";
        
    public static readonly string JoinChannel = "api/v1/channel/join";
    
    public static readonly string MemberRemoveChannel = "api/v1/channel/members/remove";
    
    public static readonly string MemberLeaveChannel = "api/v1/channel/members/leave";

    public static readonly string ChannelOwnerTransfer = "api/v1/channel/ownerTransfer";
    
    public static readonly string DisbandChannel = "api/v1/channel/disband";
    
    public static readonly string ChannelMembers = "api/v1/channel/members";
    public static readonly string AddChannelMembers = "api/v1/channel/members/add";
    
    public static readonly string IsAdminChannel = "api/v1/channel/isAdmin";

    public static readonly string ChannelAnnouncement = "api/v1/channel/announcement/get";
    public static readonly string ChannelSetAnnouncement = "api/v1/channel/announcement/update";
    public static readonly string ChannelSetName = "api/v1/channel/update";
   
    public static readonly string UserInfo = "api/v1/userInfo";
    public static readonly string AddressList = "api/v1/listAddress";
    public static readonly string AddressToken = "api/v1/verify/portkey";
    public static readonly string AuthToken = "api/v1/auth";
    
    public static readonly string ChannelInfo = "api/v1/channel/info";
    public static readonly string Merge = "api/app/contacts/merge";
    public static readonly string Remark = "api/v1/following/remark";
    public static readonly string UpdateImUser = "api/v1/userInfo/update";
    public static readonly string SearchUserInfo = "api/v1/user/search";

}