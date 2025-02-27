using UnityEngine;

namespace Game.Scripts
{
    public class HoleArea : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _targetArea;

        public SpriteRenderer TargetArea => _targetArea;
    }
}