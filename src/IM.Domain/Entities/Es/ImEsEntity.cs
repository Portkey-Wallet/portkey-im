using Volo.Abp.Domain.Entities;

namespace IM.Entities.Es;

public abstract class ImEsEntity<TKey> : Entity, IEntity<TKey>
{
    public virtual TKey Id { get; set; }

    public override object[] GetKeys()
    {
        return new object[] { Id };
    }
}