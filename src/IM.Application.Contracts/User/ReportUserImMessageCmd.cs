using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using IM.Message;
using JetBrains.Annotations;

namespace IM.User;

public class ReportUserImMessageCmd : IValidatableObject
{
	[Required(ErrorMessage = "please input the user id")]
    public string UserId { get; set; }
    
	[Required(ErrorMessage = "please input the user address")]
	public string UserAddress { get; set; }
	
	[Required(ErrorMessage = "please input the reported user id")]
	public string ReportedUserId {get; set; }
	
	[Required(ErrorMessage = "please input the reported user address")]
	public string ReportedUserAddress {get; set; }
	
	[Required(ErrorMessage = "please input the report type")]
	[Range(typeof(int), "1", "8")]
	public int ReportType {get; set; }
	
	[Required(ErrorMessage = "please input the original message")]
	[Range(typeof(string), "1", "500")]
	public string Message {get; set; }
	
	[Required(ErrorMessage = "please input the message id")]
	public string MessageId {get; set; }
	
	[CanBeNull]
	public string Description {get; set; }
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