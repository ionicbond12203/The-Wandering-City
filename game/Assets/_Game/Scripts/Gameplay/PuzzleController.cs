using UnityEngine;

namespace WanderingCity
{
    public sealed class PuzzleController : MonoBehaviour
    {
        public GameSession Session;
        public string Id = "echo-puzzle";
        public PuzzleProgress Progress = new PuzzleProgress("echo-west", "echo-east");
        public bool Activate(string node)
        {
            if (Session.State.completedPuzzleIds.Contains(Id) || !Progress.Activate(node)) return false;
            bool complete = ExplorationRules.CompletePuzzle(Session.State, Id, Progress.Complete);
            Session.Notify(complete ? "共鸣完成 / 对应宝箱已解锁" : "共鸣石点亮 / 寻找另一块石头");
            Session.Save(); return true;
        }
        public void ResetProgress() => Progress.Reset();
    }
}
