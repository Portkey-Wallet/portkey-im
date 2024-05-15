using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AElf.Indexing.Elasticsearch.Exceptions;
using AElf.Indexing.Elasticsearch.Options;
using AElf.Indexing.Elasticsearch.Provider;
using Elasticsearch.Net;
using IM.Options;
using Microsoft.Extensions.Options;
using Nest;
using Volo.Abp.Domain.Entities;

namespace IM.Repository;

public class RefreshRepository<TEntity, TKey> : NESTRepository<TEntity, TKey>, IRefreshRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>, new()
{
    public RefreshRepository(IEsClientProvider esClientProvider,
        IOptionsSnapshot<IndexSettingOptions> indexSettingOptions,
        string index = null, string type = null) : base(
        esClientProvider, indexSettingOptions, index, type)
    {
    }

    public async Task DeleteIndexAsync(TKey id, bool fresh)
    {
        var refresh = Refresh.False;
        if (fresh)
        {
            refresh = Refresh.True;
        }

        var indexName = IndexName;
        var client = GetElasticClient();
        var response = await client.DeleteAsync(new DeleteRequest(indexName, new Id(new { id = id.ToString() }))
        {
            Refresh = refresh
        });
        if (response.ServerError == null) return;
        throw new Exception($"Delete Docuemnt at index {indexName} :{response.ServerError.Error.Reason}");
    }

    public async Task AddOrUpdateIndexAsync(TEntity model, bool fresh)
    {
        var refresh = Refresh.False;
        if (fresh)
        {
            refresh = Refresh.True;
        }

        var indexName = IndexName;
        var client = GetElasticClient();
        var exits = client.DocumentExists(DocumentPath<TEntity>.Id(new Id(model)), dd => dd.Index(indexName));

        if (exits.Exists)
        {
            var result = await client.UpdateAsync(DocumentPath<TEntity>.Id(new Id(model)),
                ss => ss.Index(indexName).Doc(model).RetryOnConflict(3).Refresh(refresh));

            if (result.ServerError == null) return;
            throw new ElasticSearchException($"Update Document failed at index{indexName} :" +
                                             result.ServerError.Error.Reason);
        }
        else
        {
            var result = await client.IndexAsync(model, ss => ss.Index(indexName).Refresh(refresh));
            if (result.ServerError == null) return;
            throw new ElasticSearchException($"Insert Docuemnt failed at index {indexName} :" +
                                             result.ServerError.Error.Reason);
        }
    }
}