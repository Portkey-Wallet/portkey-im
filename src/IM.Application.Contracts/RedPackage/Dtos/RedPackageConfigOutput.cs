using System.Collections.Generic;
using IM.Commons;

namespace IM.RedPackage.Dtos;

public class RedPackageConfigOutput
{
    public List<RedPackageTokenInfo> TokenInfo { get; set; }
    public List<ContractAddressInfo>RedPackageContractAddress{ get; set; }

}

public class RedPackageTokenInfo : ChainDisplayNameDto
{
    public string Symbol { get; set; }
    public int Decimal { get; set; }
    public string MinAmount { get; set; }
}

public class ContractAddressInfo : ChainDisplayNameDto
{
    public string ContractAddress{ get; set; }
}