using Orleans.TestingHost;
using Volo.Abp.Modularity;

namespace IM.Orleans.TestBase;

public abstract class ImOrleansTestBase<TStartupModule> : ImTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    protected readonly TestCluster Cluster;

    public ImOrleansTestBase()
    {
        Cluster = GetRequiredService<ClusterFixture>().Cluster;
    }
}