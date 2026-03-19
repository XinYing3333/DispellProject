namespace DefaultNamespace.UI.Objective
{
    public interface IObjectiveProvider
    {
        string   CurrentKey { get; }
        object[] CurrentArgs { get; }
        bool     HasObjective { get; }
    }
}