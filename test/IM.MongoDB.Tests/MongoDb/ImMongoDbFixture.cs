using System;
using Mongo2Go;

namespace IM.MongoDB;

public class ImMongoDbFixture : IDisposable
{
    // private static readonly MongoDbRunner MongoDbRunner;
    // public static readonly string ConnectionString;
    //
    // static IMMongoDbFixture()
    // {
    //     MongoDbRunner = MongoDbRunner.Start(singleNodeReplSet: true, singleNodeReplSetWaitTimeout: 10);
    //     ConnectionString = MongoDbRunner.ConnectionString;
    // }

    public void Dispose()
    {
        //MongoDbRunner?.Dispose();
    }
}
