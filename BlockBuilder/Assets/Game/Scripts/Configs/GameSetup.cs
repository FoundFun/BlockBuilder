using Game.Scripts.DragDrop;
using UnityEngine;

namespace Game.Scripts.Configs
{
    [CreateAssetMenu(fileName = "GameSetup", menuName = "Game/GameSetup", order = 0)]
    public class GameSetup : ScriptableObject
    {
        public Sprite[] BlockSprites;
        public Drag DragPrefab;
    }
}