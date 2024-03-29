using System.Collections.Generic;

namespace IM.RedPackage.Dtos;

public class RedPackageConfigOutput
{
    public List<RedPackageTokenInfo> TokenInfo { get; set; }
    public List<ContractAddressInfo>RedPackageContractAddress{ get; set; }

}

public class RedPackageTokenInfo
{
    public string ChainId { get; set; }
    public string Symbol { get; set; }
    public int Decimal { get; set; }
    public string MinAmount { get; set; }
}

public class ContractAddressInfo
{
    public string ChainId{ get; set; }
    public string ContractAddress{ get; set; }
}