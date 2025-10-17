namespace DefaultNamespace.UI.Objective
{
    public interface IObjectiveProvider
    {
        string CurrentText { get; }
        bool   HasObjective { get; }
    }
}