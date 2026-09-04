using UnityEngine;

namespace WanderingCity
{
    [CreateAssetMenu(menuName = "Wandering City/Character presentation settings")]
    public sealed class CharacterPresentationSettings : ScriptableObject
    {
        [Tooltip("A licensed, in-place Humanoid prefab with CharacterRigBindings and its animation set. Empty uses the original prototype.")]
        public GameObject CharacterPrefabSlot;
    }
}
