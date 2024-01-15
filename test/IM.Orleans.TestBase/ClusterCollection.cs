using Xunit;

namespace IM.Orleans.TestBase;


[CollectionDefinition(ClusterCollection.Name)]
public class ClusterCollection:ICollectionFixture<ClusterFixture>
{
    public const string Name = "ClusterCollection";
}