using UnityEngine;

namespace WanderingCity
{
    public sealed class PuzzleController : MonoBehaviour
    {
        public GameSession Session;
        public readonly PuzzleProgress Progress = new PuzzleProgress("echo-west", "echo-east");
        public bool Activate(string node)
        {
            if (Session.State.completedPuzzleIds.Contains("echo-puzzle") || !Progress.Activate(node)) return false;
            bool complete = ExplorationRules.CompletePuzzle(Session.State, "echo-puzzle", Progress.Complete);
            Session.Notify(complete ? "双石共鸣 / 回声匣已解锁" : "共鸣石点亮 / 寻找另一块石头");
            Session.Save(); return true;
        }
        public void ResetProgress() => Progress.Reset();
    }
}
