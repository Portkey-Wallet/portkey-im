using System.Threading.Tasks;
using IM.Message;

namespace IM.User;

public interface IReportUserGateway
{
    public Task ReportUser(ImUser user, ImUser reportedUser, ReportedMessage reportedMessage);
}