using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;

namespace Game.Scripts.Notification
{
    public class NotificationText : MonoBehaviour
    {
        [SerializeField] private TextMeshPro _text;
        [SerializeField] private LocalizeStringEvent _localizeEvent;

        public TextMeshPro Text => _text;
        public LocalizeStringEvent LocalizeEvent => _localizeEvent;
    }
}