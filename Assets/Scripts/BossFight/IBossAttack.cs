namespace BossFight
{
    public interface IBossAttack
    {
        string Id { get; }                           // "Landing" / "Charge"
        System.Collections.IEnumerator Execute(BossContext ctx);
    }
}