namespace IM.Auth.Dtos;

public class AuthTokenRequestDto
{
    public string ca_hash { get; set; }
    public string chainId { get; set; }
    
    public string chain_id { get; set; }
    public string client_id { get; set; }
    public string grant_type { get; set; }
    public string pubkey { get; set; }
    public string scope { get; set; }
    public string signature { get; set; }
    public long timestamp { get; set; }
    
}


