using System.Collections.Generic;
using IM.Commons;

namespace IM.User.Dtos;


public class CaHolderInfoDto
{
    public List<GuardianDto> CaHolderInfo { get; set; }
}

public class GuardianDto : GuardianBase
{
    public string OriginChainId { get; set; }
    public GuardianBaseListDto GuardianList { get; set; }
    public List<ManagerInfoDBase> ManagerInfos { get; set; }
}

public class GuardianBase : ChainDisplayNameDto
{
    public string CaHash { get; set; }
    public string CaAddress { get; set; }
}

public class GuardianBaseListDto
{
    public List<GuardianInfoBase> Guardians { get; set; }
}

public class ManagerInfoDBase
{
    public string Address { get; set; }
    public string ExtraData { get; set; }
}

public class GuardianInfoBase
{
    public string IdentifierHash { get; set; }
    public string Salt { get; set; }
    public string GuardianIdentifier { get; set; }
    public string VerifierId { get; set; }
    public bool IsLoginGuardian { get; set; }
    public string Type { get; set; }
}