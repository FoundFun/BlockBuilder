using Cysharp.Threading.Tasks;
using DG.Tweening;
using Game.Scripts.Notification;
using Game.Scripts.Signals;
using UnityEngine;
using UnityEngine.Rendering;
using Zenject;
using Random = UnityEngine.Random;

namespace Game.Scripts.DragDrop
{
    public class Drag : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _view;
        [SerializeField] private SortingGroup _sortingGroup;
        [SerializeField] private BoxCollider2D _boxCollider2D;
        [SerializeField] private ParticleSystem _lightEffect;
        [SerializeField] private ParticleSystem _spawnEffect;
        [Header("Parameters")]
        [SerializeField] private float _durationScaleSpawn = 0.2f;
        [SerializeField] private float _scaleDrag = 1.1f;
        [SerializeField] private float _durationScaleDrag = 0.5f;
        [SerializeField] private float _randomOffsetX = 0.45f;
        [SerializeField] private float _peakOffsetAnimationY = 0.5f;
        [SerializeField] private float _durationTowerRotate = 0.3f;
        [SerializeField] private float _durationTowerPath = 0.6f;
        [SerializeField] private float _targetTowerRotateZ = -10;
        [SerializeField] private float _durationDropTowerRotate = 0.15f;
        [SerializeField] private float _durationDropTowerPath = 0.3f;
        [SerializeField] private float _durationHide = 0.5f;
        [SerializeField] private float _durationHole = 0.5f;
        [SerializeField] private int _dragOrder = 20;
        [SerializeField] private int _dropOrder = 5;

        [Inject] private SignalBus _signalBus;
        [Inject] private Camera _camera;
        [Inject] private NotificationController _notificationController;

        private Vector3 _offset;
        private bool _isDragging;

        public SpriteRenderer View => _view;
        public Vector3 SpawnPosition { get; private set; }
        public Vector3 TowerPosition { get; private set; }

        public void Setup(Vector3 spawnPosition, Sprite gameConfigBlockColor)
        {
            SpawnPosition = spawnPosition;
            transform.localPosition = spawnPosition;
            View.sprite = gameConfigBlockColor;
            _boxCollider2D.enabled = true;
            View.transform.localScale = Vector3.zero;
            TowerPosition = Vector3.zero;
            _sortingGroup.sortingOrder = _dropOrder;
            View.transform.DOScale(Vector3.one, _durationScaleSpawn);
        }

        private void OnMouseDown()
        {
            _isDragging = true;
            _notificationController.Show(LocalizationKeys.PickUp);
            View.transform.DOScale(_scaleDrag, _durationScaleDrag).SetEase(Ease.OutBack);
            _offset = transform.position - _camera.ScreenToWorldPoint(Input.mousePosition);
            _sortingGroup.sortingOrder = _dragOrder;
            _signalBus.Fire(new MouseDownSignal(this));
        }

        private void OnMouseDrag()
        {
            if (_isDragging)
            {
                var newPosition = _camera.ScreenToWorldPoint(Input.mousePosition) + _offset;
                newPosition.z = 0;
                transform.position = newPosition;
            }
        }

        private void OnMouseUp()
        {
            _isDragging = false;
            _sortingGroup.sortingOrder = _dropOrder;
            _signalBus.Fire(new MouseUpSignal(this));
        }

        public void PlaySmoke()
        {
            _spawnEffect.Play();
        }

        public async UniTask PlayTowerAnimation(Vector3 targetPosition, Vector3 offset)
        {
            await TowerAnimation(targetPosition, offset, _durationTowerRotate, _durationTowerPath);
        }

        public async UniTask PlayDropTowerAnimation(Vector3 targetPosition, Vector3 offset)
        {
            await TowerAnimation(targetPosition, offset, _durationDropTowerRotate, _durationDropTowerPath);
        }

        public async UniTask PlayHoleAnimation(Transform targetPosition)
        {
            var edgeLength = _view.bounds.size.x;
            var randomOffsetX = Random.Range(-_randomOffsetX, _randomOffsetX) * edgeLength;
            var startPosition = transform.position;
            var peakPosition = new Vector3(startPosition.x, startPosition.y + _peakOffsetAnimationY, startPosition.z);
            var endPosition = new Vector3(targetPosition.position.x + randomOffsetX, targetPosition.position.y,
                targetPosition.position.z);
            var path = new[] { peakPosition, endPosition };

            _boxCollider2D.enabled = false;

            await transform.DOPath(path, _durationHole, PathType.CatmullRom)
                .SetEase(Ease.InQuad)
                .OnStart(() => transform.DOScale(0f, _durationHole).SetEase(Ease.InQuad));

            Remove();
        }

        public async UniTask PlayHideAnimation()
        {
            _boxCollider2D.enabled = false;
            _spawnEffect.Play();

            await View.transform.DOScale(Vector3.zero, _durationHide);

            Remove();
        }

        private async UniTask TowerAnimation(Vector3 targetPosition, Vector3 offset,
            float durationRotate, float durationPath)
        {
            var edgeLength = _view.bounds.size.x;
            var randomOffsetX = Random.Range(-_randomOffsetX, _randomOffsetX) * edgeLength;
            var startPosition = transform.position;
            var peakPosition = new Vector3(startPosition.x, startPosition.y + _peakOffsetAnimationY, startPosition.z);
            var endPosition = new Vector3(targetPosition.x + randomOffsetX, targetPosition.y, targetPosition.z) + offset;
            var path = new[] { peakPosition, endPosition };
            TowerPosition = endPosition;
            _boxCollider2D.enabled = false;

            transform.DOLocalRotate(new Vector3(0, 0, _targetTowerRotateZ), durationRotate).SetEase(Ease.InSine).SetLoops(2, LoopType.Yoyo)
                .ToUniTask().Forget();
            await transform.DOPath(path, durationPath, PathType.CatmullRom)
                .SetEase(Ease.InSine).OnComplete(() => _lightEffect.Play());
            
            _boxCollider2D.enabled = true;
        }

        private void Remove()
        {
            transform.DOKill();
            Destroy(gameObject);
        }
    }
}