namespace IM.PinMessage.Dtos;

public class UnpinMessageResponseDto<T>
{
    public T Data { get; set; }

    public string Message { get; set; }
}