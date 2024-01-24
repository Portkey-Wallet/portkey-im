using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using IM.Message;
using JetBrains.Annotations;

namespace IM.PinMessage.Dtos;

public class PinMessageParamDto : IValidatableObject
{
    [Required] public string Id { get; set; }
    [Required] public string ChannelUuid { get; set; }

    [Required] public string SendUuid { get; set; }
    [Required] public MessageType Type { get; set; }

    [Required] public long CreateAt { get; set; }

    [Required] public string From { get; set; }

    [Required] public string FromName { get; set; }

    public string FromAvatar { get; set; }

    [CanBeNull] public string Content { get; set; }

    [CanBeNull] public Quote Quote { get; set; }


    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Type != MessageType.TEXT && Type != MessageType.IMAGE)
        {
            yield return new ValidationResult(
                "Invalid message type."
            );
        }
    }
}
