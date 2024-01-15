namespace IM.RedPackage.Dtos;

public class GetCreationResultOutputDto
{
    public int Status { get; set; }
    public string Message { get; set; } 
    public string TransactionResult { get; set; }
    public string TransactionId { get; set; }
}