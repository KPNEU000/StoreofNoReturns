namespace BossFight
{
    // Anything the Witch animates. When she dies the director calls DropLifeless on every one.
    public interface IBossMinion
    {
        void DropLifeless();
    }
}
