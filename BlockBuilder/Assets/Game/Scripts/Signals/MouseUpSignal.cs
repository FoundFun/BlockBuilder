using Game.Scripts.DragDrop;

namespace Game.Scripts.Signals
{
    public class MouseUpSignal
    {
        public Drag Drag { get; private set; }

        public MouseUpSignal(Drag drag)
        {
            Drag = drag;
        }
    }
}