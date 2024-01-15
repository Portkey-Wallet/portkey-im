using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace IM.Data;

/* This is used if database provider does't define
 * IIMDbSchemaMigrator implementation.
 */
public class NullIMDbSchemaMigrator : IImDbSchemaMigrator, ITransientDependency
{
    public Task MigrateAsync()
    {
        return Task.CompletedTask;
    }
}
