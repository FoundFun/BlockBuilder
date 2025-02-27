using Game.Scripts.Notification;
using UnityEngine;
using Zenject;

namespace Game.Scripts.Factories
{
    public class NotificationFactory : PlaceholderFactory<NotificationText, Transform, NotificationText> { }

    internal class CustomNotificationFactory : IFactory<NotificationText, Transform, NotificationText>
    {
        private readonly DiContainer _container;

        public CustomNotificationFactory(DiContainer container)
        {
            _container = container;
        }

        public NotificationText Create(NotificationText part, Transform parent)
        {
            var result = _container.InstantiatePrefabForComponent<NotificationText>(part, parent);
            return result;
        }
    }
}