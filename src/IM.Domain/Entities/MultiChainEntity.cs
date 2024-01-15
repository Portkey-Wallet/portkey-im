namespace IM.Entities;

public class MultiChainEntity<TKey> : ImEntity<TKey>, IMultiChain
{
    public virtual int ChainId { get; set; }


    protected MultiChainEntity()
    {
    }

    protected MultiChainEntity(TKey id)
        : base(id)
    {
    }
}