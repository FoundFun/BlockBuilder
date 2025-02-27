using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Game.Scripts.Configs;
using Game.Scripts.DragDrop;
using Game.Scripts.Factories;
using Game.Scripts.Notification;
using Game.Scripts.Signals;
using UnityEngine;
using Zenject;

namespace Game.Scripts.Controllers
{
    public class GameController : MonoBehaviour
    {
        [Header("Parameters")] 
        [SerializeField] private float _delaySpawnDrag = 0.05f;
        [SerializeField] private float _multiplyOffsetDragX = 1.2f;
        
        [Inject] private GameSetup _gameConfig;
        [Inject] private SignalBus _signalBus;
        [Inject] private DragDropFactory _dragDropFactory;
        [Inject] private ScrollController _scrollController;
        [Inject] private Camera _camera;
        [Inject] private TowerArea _towerArea;
        [Inject] private HoleArea _holeArea;
        [Inject] private NotificationController _notificationController;

        private readonly List<Drag> _scrollDrags = new();
        private List<Drag> _towerDrops = new();
        private Drag _firstDropTower;
        private Drag _lastDropTower;
        private Vector3 _firstBlockPosition;

        private void OnEnable()
        {
            _signalBus.Subscribe<MouseUpSignal>(LaunchInput);
            _signalBus.Subscribe<MouseDownSignal>(SpawnDrag);
        }

        private void OnDisable()
        {
            _signalBus.Unsubscribe<MouseUpSignal>(LaunchInput);
            _signalBus.Unsubscribe<MouseDownSignal>(SpawnDrag);
        }

        private void Start()
        {
            GenerateDrags().Forget();
            _scrollController.Run().Forget();
        }

        private async UniTaskVoid GenerateDrags()
        {
            for (var i = 0; i < _gameConfig.BlockSprites.Length; i++)
            {
                var drag = _dragDropFactory.Create(_gameConfig.DragPrefab, _scrollController.Content);
                drag.Setup(new Vector3(i * _multiplyOffsetDragX, 0, 0), _gameConfig.BlockSprites[i]);
                drag.PlaySmoke();
                _scrollDrags.Add(drag);

                await UniTask.Delay(TimeSpan.FromSeconds(_delaySpawnDrag));
            }

            _scrollController.InitElements(_scrollDrags.First().transform, _scrollDrags.Last().transform);
        }

        private void LaunchInput(MouseUpSignal signal)
        {
            if (IsInTowerArea(signal.Drag.transform.position))
            {
                AddToTower(signal.Drag).Forget();
            }
            else if (IsInHoleArea(signal.Drag.transform.position))
            {
                AddToHole(signal.Drag).Forget();
            }
            else
            {
                _notificationController.Show(LocalizationKeys.Threw);
                signal.Drag.PlayHideAnimation().Forget();
            }
        }

        private async UniTask DropTower(Drag drag, bool hasInTower = true)
        {
            _towerDrops.RemoveAll(item => item == null);
            var target = _towerDrops.IndexOf(drag);
            var nextOffset = 0f;
            var firstBottomDrag = _firstBlockPosition;
            var moveBlocksDrag = new List<Drag>();
            
            if (!hasInTower)
            {
                _towerDrops = _towerDrops
                    .Where(x => x != drag)
                    .Concat(new[] { drag })
                    .ToList();
            }

            for (var i = 0; i < _towerDrops.Count; i++)
            {
                if (target > 0 && target - 1 == i)
                {
                    firstBottomDrag = _towerDrops[i].TowerPosition;
                    nextOffset = _towerDrops[i].View.bounds.extents.y;
                }

                if (target < i)
                {
                    moveBlocksDrag.Add(_towerDrops[i]);
                }

                if (!hasInTower && target == i)
                {
                    moveBlocksDrag.Add(_towerDrops[i]);
                }
            }

            if (hasInTower)
            {
                _towerDrops.Remove(drag);
            }
            
            foreach (var moveDrag in moveBlocksDrag)
            {
                var offsetY = nextOffset * 2;
                
                await moveDrag.PlayDropTowerAnimation(firstBottomDrag, new Vector3(0, offsetY, 0));
                
                nextOffset = moveDrag.View.bounds.extents.y;
                firstBottomDrag = moveDrag.TowerPosition;
            }

            _firstDropTower = _towerDrops.FirstOrDefault();
            _lastDropTower = _towerDrops.LastOrDefault();
        }

        private void SpawnDrag(MouseDownSignal signal)
        {
            if (_towerDrops.Contains(signal.Drag))
            {
                return;   
            }

            var currentDrag = _dragDropFactory.Create(_gameConfig.DragPrefab, _scrollController.Content);
            var index = _scrollDrags.IndexOf(signal.Drag);
            
            currentDrag.Setup(signal.Drag.SpawnPosition, signal.Drag.View.sprite);
            _scrollDrags[index] = currentDrag;
            _scrollController.InitElements(_scrollDrags.First().transform, _scrollDrags.Last().transform);
        }

        private async UniTask AddToTower(Drag drag)
        {
            var hasInTowerDrop = true;

            if (!_towerDrops.Contains(drag))
            {
                hasInTowerDrop = false;
                _towerDrops.Add(drag);
            }

            if (_towerDrops.Count == 1 || _firstDropTower == null || _lastDropTower == null)
            {
                SetFirstDrop(drag);
            }
            else
            {
                if (drag.View.transform.position.y > _lastDropTower.View.transform.position.y)
                {
                    await SetDrop(drag, hasInTowerDrop);
                }
                else
                {
                    await HideDrag(drag, hasInTowerDrop);
                }
            }
        }

        private async UniTask HideDrag(Drag drag, bool hasInTowerDrop)
        {
            drag.PlayHideAnimation().Forget();
            _notificationController.Show(LocalizationKeys.Threw);
            
            if (hasInTowerDrop && drag != _lastDropTower)
            { 
                _notificationController.Show(LocalizationKeys.Fall);
                
                await DropTower(drag);
            }

            UpdateFirstLastDrop(drag);
        }

        private async UniTask SetDrop(Drag drag, bool hasInTowerDrop)
        {
            drag.transform.SetParent(_towerArea.Content.transform);

            if (!IsWithinCameraBounds(drag.transform.position))
            {
                drag.PlayHideAnimation().Forget();
                _towerDrops.Remove(drag);
                
                return;
            }
            
            _notificationController.Show(LocalizationKeys.Put);

            await drag.PlayTowerAnimation(_lastDropTower.TowerPosition, new Vector3(0, _lastDropTower.View.bounds.extents.y * 2, 0));
            _lastDropTower = drag;
            
            if (hasInTowerDrop)
            { 
                _notificationController.Show(LocalizationKeys.Fall);
                
                await DropTower(drag, false);
            }
        }

        private void SetFirstDrop(Drag drag)
        {
            if (!IsWithinCameraBounds(drag.transform.position))
            {
                drag.PlayHideAnimation().Forget();
                _towerDrops.Remove(drag);
                
                return;
            }

            _firstDropTower = drag;

            if (_lastDropTower == null)
            {
                _lastDropTower = drag;
            }
            
            drag.transform.SetParent(_towerArea.Content.transform);
            _firstBlockPosition = _lastDropTower.transform.position;
            drag.PlayTowerAnimation(_firstBlockPosition, Vector3.zero).Forget();
            _notificationController.Show(LocalizationKeys.Put);
        }

        private bool IsWithinCameraBounds(Vector3 position)
        {
            var topBoundary = _camera.ViewportToWorldPoint(new Vector3(0, 1, _camera.nearClipPlane)).y;
            var bottomBoundary = _camera.ViewportToWorldPoint(new Vector3(0, 0, _camera.nearClipPlane)).y;

            var isWithinCameraBounds = position.y <= topBoundary && position.y >= bottomBoundary;
            
            if (!isWithinCameraBounds)
            {
                _notificationController.Show(LocalizationKeys.TooHigh);
            }

            return isWithinCameraBounds;
        }

        private async UniTask AddToHole(Drag drag)
        {
            drag.PlayHoleAnimation(_holeArea.TargetArea.transform).Forget();
            _notificationController.Show(LocalizationKeys.Disappeared);

            var hasInTower = _towerDrops.Contains(drag);
            
            if (hasInTower && _towerDrops.Count > 1)
            {
                _notificationController.Show(LocalizationKeys.Fall);
                
                await DropTower(drag);
            }
            else
            {
                _towerDrops.Remove(drag);
            }
            
            UpdateFirstLastDrop(drag);
        }

        private void UpdateFirstLastDrop(Drag drag)
        {
            if (drag == _lastDropTower)
            {
                _lastDropTower = _towerDrops.LastOrDefault();
            }

            if (drag == _firstDropTower)
            {
                _firstDropTower = _towerDrops.FirstOrDefault();
            }
            
            _towerDrops.Remove(drag);
        }

        private bool IsInTowerArea(Vector3 position)
        {
            var towerBounds = _towerArea.View.bounds;
            
            return towerBounds.Contains(position);
        }

        private bool IsInHoleArea(Vector3 position)
        {
            var holeBounds = _holeArea.TargetArea.bounds;
            
            return holeBounds.Contains(position);
        }
    }
}