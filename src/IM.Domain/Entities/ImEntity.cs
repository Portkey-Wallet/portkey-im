using System;
using Volo.Abp.Domain.Entities;

namespace IM.Entities;

[Serializable]
public abstract class ImEntity <TKey> : Entity, IEntity<TKey>
{
    /// <inheritdoc/>
    public virtual TKey Id { get; set; }

    protected ImEntity()
    {

    }

    protected ImEntity(TKey id)
    {
        Id = id;
    }

    public override object[] GetKeys()
    {
        return new object[] {Id};
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"[ENTITY: {GetType().Name}] Id = {Id}";
    }
}