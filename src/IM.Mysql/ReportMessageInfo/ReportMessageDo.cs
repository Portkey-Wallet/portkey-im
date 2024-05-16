namespace IM.Mysql.ReportMessageInfo;

public class ReportMessageDo
{
    public long id { get; set; };
    uid {get; set; }
    userAddress {get; set; }
    messageId {get; set; }
    reportedUserId {get; set; }
    reportedUserAddress {get; set; }
    reportedType int(1) NOT NULL comment 'reported type',
    reportedMessage {get; set; }
    description {get; set; }
    reportedTime timestamp    default CURRENTTIMESTAMP not null comment 'reported time',
    createTime timestamp    default CURRENTTIMESTAMP not null comment 'create time',
    updateTime timestamp    default CURRENTTIMESTAMP not null comment 'update time',
}