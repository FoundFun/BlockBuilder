using Game.Scripts.Configs;
using Game.Scripts.Controllers;
using Game.Scripts.DragDrop;
using Game.Scripts.Factories;
using Game.Scripts.Notification;
using Game.Scripts.Signals;
using UnityEngine;
using Zenject;

namespace Game.Scripts.Installers
{
    public class GameInstaller : MonoInstaller
    {
        [SerializeField] private GameSetup _gameSetup;
        [SerializeField] private Camera _camera;
        [SerializeField] private GameController _gameController;
        [SerializeField] private ScrollController _scrollController;
        [SerializeField] private TowerArea _towerArea;
        [SerializeField] private HoleArea _holeArea;
        [SerializeField] private NotificationController _notificationController;

        public override void InstallBindings()
        {
            Container.BindInstance(_gameSetup);
            Container.BindInstance(_camera);
            Container.BindInstance(_gameController);
            Container.BindInstance(_scrollController);
            Container.BindInstance(_towerArea);
            Container.BindInstance(_holeArea);
            Container.BindInstance(_notificationController);

            BindSignals();
            BindFactories();
        }

        private void BindSignals()
        {
            Container.DeclareSignal<MouseUpSignal>();
            Container.DeclareSignal<MouseDownSignal>();
        }

        private void BindFactories()
        {
            Container.BindFactory<Drag, Transform, Drag, DragDropFactory>()
                .FromFactory<CustomDragDropFactory>();
            Container.BindFactory<NotificationText, Transform, NotificationText, NotificationFactory>()
                .FromFactory<CustomNotificationFactory>();
        }
    }
}