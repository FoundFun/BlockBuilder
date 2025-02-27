using Cysharp.Threading.Tasks;
using Game.Scripts.Signals;
using UnityEngine;
using Zenject;

namespace Game.Scripts.Controllers
{
    public class ScrollController : MonoBehaviour
    {
        [SerializeField] private Transform _content;
        [SerializeField] private SpriteRenderer _view;
        [Header("Parameters")]
        [SerializeField] private float _offsetX = 1f;
        [SerializeField] private float _scrollSpeed = 5f;

        [Inject] private Camera _camera;
        [Inject] private SignalBus _signalBus;

        private bool _hasActiveScroll = true;
        
        private Transform _firstElement;
        private Transform _lastElement;
        private Vector3 _dragStartPosition;

        public Transform Content => _content;

        private void OnEnable()
        {
            _signalBus.Subscribe<MouseUpSignal>(EnableScroll);
            _signalBus.Subscribe<MouseDownSignal>(DisableScroll);
        }

        private void OnDisable()
        {
            _signalBus.Unsubscribe<MouseUpSignal>(EnableScroll);
            _signalBus.Unsubscribe<MouseDownSignal>(DisableScroll);
        }

        private void EnableScroll()
        {
            Run().Forget();
        }

        private void DisableScroll()
        {
            _hasActiveScroll = false;
        }

        public void InitElements(Transform first, Transform last)
        {
            _firstElement = first;
            _lastElement = last;
        }

        public async UniTaskVoid Run()
        {
            _hasActiveScroll = true;
            
            while (_hasActiveScroll)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    _dragStartPosition = _camera.ScreenToWorldPoint(Input.mousePosition);
                }

                var currentPosition = _camera.ScreenToWorldPoint(Input.mousePosition);
                currentPosition.z = 0;

                if (Input.GetMouseButton(0) && _view.bounds.Contains(currentPosition))
                {
                    var difference = _dragStartPosition - currentPosition;
                    var newPosition =
                        _content.position + new Vector3(difference.x, 0, 0) * _scrollSpeed * Time.deltaTime;

                    if (CanScrollLeft(difference.x) && CanScrollRight(difference.x))
                    {
                        _content.position = newPosition;
                    }
                }

                await UniTask.Yield();
            }
        }

        private bool CanScrollLeft(float differenceX)
        {
            if (IsElementVisible(_firstElement))
            {
                return differenceX < 0;
            }

            return true;
        }

        private bool CanScrollRight(float differenceX)
        {
            if (IsElementVisible(_lastElement))
            {
                return differenceX > 0;
            }

            return true;
        }

        private bool IsElementVisible(Transform element)
        {
            var elementPosition = element.position;
            var cameraLeftBound = _camera.ViewportToWorldPoint(new Vector3(0, 0, 0)).x;
            var cameraRightBound = _camera.ViewportToWorldPoint(new Vector3(1, 0, 0)).x;

            return elementPosition.x - _offsetX > cameraLeftBound && elementPosition.x + _offsetX < cameraRightBound;
        }
    }
}