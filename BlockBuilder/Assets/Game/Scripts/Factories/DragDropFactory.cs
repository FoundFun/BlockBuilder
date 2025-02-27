using Game.Scripts.DragDrop;
using UnityEngine;
using Zenject;

namespace Game.Scripts.Factories
{
    public class DragDropFactory : PlaceholderFactory<Drag, Transform, Drag> { }

    internal class CustomDragDropFactory : IFactory<Drag, Transform, Drag>
    {
        private readonly DiContainer _container;

        public CustomDragDropFactory(DiContainer container)
        {
            _container = container;
        }

        public Drag Create(Drag part, Transform parent)
        {
            var result = _container.InstantiatePrefabForComponent<Drag>(part, parent);
            return result;
        }
    }
}