using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Volo.Abp.Domain.Entities;

namespace IM.Repository;


public interface IRefreshRepository<TEntity, TKey> : INESTReaderRepository<TEntity, TKey>,
    INESTWriterRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>, new()
{
    public Task AddOrUpdateIndexAsync(TEntity model, bool fresh = true);

    public Task DeleteIndexAsync(TKey id, bool fresh = true);
}