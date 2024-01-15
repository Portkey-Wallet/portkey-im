using IM.Entities.Es;

namespace IM.Grains.State.User;

public class UserState
{
    public Guid Id { get; set; }
    public string CaHash { get; set; }
    public List<CaAddressInfo> CaAddresses { get; set; } = new();
    public string RelationId { get; set; }
    public string Name { get; set; }
    public string Avatar { get; set; }
    public long CreateTime { get; set; }
    public long LastModifyTime { get; set; }
    public bool IsDeleted { get; set; }
}