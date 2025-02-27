using Game.Scripts.DragDrop;

namespace Game.Scripts.Signals
{
    public class MouseDownSignal
    {
        public Drag Drag { get; private set; }

        public MouseDownSignal(Drag drag)
        {
            Drag = drag;
        }
    }
}