using UnityEngine;
using UnityEngine.SceneManagement;
namespace WanderingCity { public sealed class Boot : MonoBehaviour { void Start() { SceneManager.LoadScene("World"); } } }
