using System.Threading.Tasks;

namespace IM.Data;

public interface IImDbSchemaMigrator
{
    Task MigrateAsync();
}
