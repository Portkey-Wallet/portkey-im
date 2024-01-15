using IM.MongoDB;
using Xunit;

namespace IM;

[CollectionDefinition(ImTestConsts.CollectionDefinitionName)]
public class ImApplicationCollection
{
    public const string CollectionDefinitionName = "IM collection";
}
