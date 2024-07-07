using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace IM.Contact.Dtos;

public class ContactProfileRequestDto : IValidatableObject
{
    public Guid Id { get; set; }
    public string RelationId { get; set; }
    
    public Guid PortkeyId { get; set; }

    public Guid UserId { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Id == Guid.Empty && RelationId.IsNullOrWhiteSpace() && PortkeyId == Guid.Empty)
        {
            yield return new ValidationResult("Invalid input.");
        }
    }
}