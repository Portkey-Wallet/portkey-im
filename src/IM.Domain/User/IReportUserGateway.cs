using System.Threading.Tasks;
using IM.Message;
using Volo.Abp.DependencyInjection;

namespace IM.User;

public interface IReportUserGateway
{
    public Task ReportUser(ImUser user, ImUser reportedUser, ReportedMessage reportedMessage);
}

public class ReportUserGatewayImpl : IReportUserGateway, ISingletonDependency
{
    public Task ReportUser(ImUser user, ImUser reportedUser, ReportedMessage reportedMessage)
    {
        throw new System.NotImplementedException();
    }
}