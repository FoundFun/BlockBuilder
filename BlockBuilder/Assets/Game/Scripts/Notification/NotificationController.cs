using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Game.Scripts.Factories;
using UnityEngine;
using Zenject;

namespace Game.Scripts.Notification
{
    public class NotificationController : MonoBehaviour
    {
        [SerializeField] private NotificationText _notificationPrefab;
        [Header("Parameters")] 
        [SerializeField] private float _delayHideTextColor = 0.5f;

        [SerializeField] private float _delayNextNotification = 0.5f;
        [SerializeField] private float _durationAnimationText = 1f;
        [SerializeField] private float _durationScaleText = 0.3f;
        [SerializeField] private float _offsetY = 1f;

        [Inject] private NotificationFactory _notificationFactory;

        private readonly Queue<LocalizationKeys> _notificationQueue = new();
        private bool _isNotificationActive;

        public void Show(LocalizationKeys key)
        {
            _notificationQueue.Enqueue(key);

            if (!_isNotificationActive)
            {
                ShowNextNotification().Forget();
            }
        }

        private async UniTask ShowNextNotification()
        {
            if (_notificationQueue.Count == 0)
            {
                return;
            }
            
            _isNotificationActive = true;

            var key = _notificationQueue.Dequeue();
            var notification = _notificationFactory.Create(_notificationPrefab, transform);
            
            notification.transform.localScale = Vector3.zero;
            notification.LocalizeEvent.SetEntry(key.ToString());

            MoveUpAndFade(notification).Forget();

            await UniTask.Delay(TimeSpan.FromSeconds(_delayNextNotification));

            _isNotificationActive = false;

            if (_notificationQueue.Count > 0)
            {
                ShowNextNotification().Forget();
            }
        }

        private async UniTask MoveUpAndFade(NotificationText notification)
        {
            notification.transform.DOScale(Vector3.one, _durationScaleText).SetEase(Ease.OutBack).ToUniTask().Forget();
            notification.transform.DOMoveY(notification.transform.position.y + _offsetY, _durationAnimationText)
                .ToUniTask().Forget();

            await UniTask.Delay(TimeSpan.FromSeconds(_delayHideTextColor));

            await DOTween.To(() => notification.Text.color, x => notification.Text.color = x, Color.clear,
                _durationAnimationText);

            notification.transform.DOKill();
            notification.DOKill();

            Destroy(notification.gameObject);
        }
    }
}