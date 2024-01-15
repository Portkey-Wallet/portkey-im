using Orleans.TestingHost;
using Volo.Abp.Caching;

namespace IM.Grain.Tests;

public class ImGrainTestBase :ImTestBase<ImGrainTestModule>
{
    protected readonly TestCluster Cluster;

    public ImGrainTestBase()
    {
        Cluster = GetRequiredService<ClusterFixture>().Cluster;

    }
}