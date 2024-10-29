using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using IM.Commons;
using IM.Message;
using JetBrains.Annotations;

namespace IM.User;

public class ReportUserImMessageCmd : IValidatableObject
{
	[Required(ErrorMessage = "please input the user id")]
    public string UserId { get; set; }
    
	[Required(ErrorMessage = "please input the report type")]
	[Range(typeof(int), "1", "8")]
	public int ReportType {get; set; }
	
	[Required(ErrorMessage = "please input the original message")]
	[StringLength(512, MinimumLength = 1, ErrorMessage = "The field Message must be between 1 and 512.")]
	public string Message {get; set; }
	
	[Required(ErrorMessage = "please input the message id")]
	public string MessageId {get; set; }
	
	[CanBeNull]
	public string Description {get; set; }
	
	[Required(ErrorMessage = "please input the relation id")]
	public string ReportedRelationId { get; set; }
	
	[Required(ErrorMessage = "please input the channel uuid")]
	public string ChannelUuid { get; set; }
	
	public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
	{
		if (ReportType == (int)ReportedMessageType.Other && String.IsNullOrEmpty(Description))
		{
			yield return new ValidationResult("please input some description");
		}
		if (ReportType == (int)ReportedMessageType.Other && !String.IsNullOrEmpty(Description) && Description.Length > 200)
		{
			yield return new ValidationResult("your description is not allowed more");
		}
	}
}

public class AddressInfo : ChainDisplayNameDto
{
	public string CaAddress { get; set; }
}