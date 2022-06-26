public interface ITickable
{
    // For growing or whatever
    void Tick();
    // For lightweight visual-only things so that stuff goes smoother
    void SubTick();
}