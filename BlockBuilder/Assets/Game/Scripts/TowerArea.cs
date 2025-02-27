using UnityEngine;

namespace Game.Scripts
{
    public class TowerArea : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _view;
        [SerializeField] private Transform _content;

        public SpriteRenderer View => _view;
        public Transform Content => _content;
    }
}