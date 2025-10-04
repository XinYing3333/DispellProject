namespace Player.InteractionSystem
{
    public interface IFocusable
    {
        void OnFocusGained(); // 玩家對準/成為目前可互動目標
        void OnFocusLost();   // 玩家不再對準/離開
    }

}