namespace IM.PinMessage.Dtos;

public class PinMessageResponseDto<T>
{
    public int Code { get; set; }
    
    public string Message { get; set; }
    
    public T Data { get; set; }
    
  
}