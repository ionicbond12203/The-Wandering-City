using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

namespace WanderingCity
{
    [DefaultExecutionOrder(100)]
    public sealed class OrbitCamera : MonoBehaviour
    {
        public Transform Target; public GameSession Session;
        public float Yaw, Pitch = 23;
        CinemachineCamera virtualCamera;
        float distance = 6;
        void Start()
        {
            var brain = gameObject.AddComponent<CinemachineBrain>(); brain.UpdateMethod = CinemachineBrain.UpdateMethods.LateUpdate;
            virtualCamera = new GameObject("Traveler / Cinemachine camera").AddComponent<CinemachineCamera>();
            virtualCamera.Lens.FieldOfView = 58;
        }
        void LateUpdate()
        {
            if (Target == null || virtualCamera == null) return;
            if (Session.InputReady && Mouse.current != null) { Vector2 delta = Mouse.current.delta.ReadValue(); Yaw += delta.x * .12f; Pitch = Mathf.Clamp(Pitch - delta.y * .1f, -25, 65); distance = Mathf.Clamp(distance - Mouse.current.scroll.ReadValue().y * .005f, 3, 9); }
            var traversal = Session.Player.Traversal; var balance = Session.Balance;
            float desiredFov = traversal.State == TraversalState.Sprint ? balance.sprintFov : balance.cameraFov;
            virtualCamera.Lens.FieldOfView = Mathf.Lerp(virtualCamera.Lens.FieldOfView, desiredFov, 1 - Mathf.Exp(-balance.cameraBlend * Time.deltaTime));
            Quaternion rotation = Quaternion.Euler(Pitch, Yaw, 0); Vector3 focus = Target.position + Vector3.up * (1.5f + (traversal.State == TraversalState.Climb ? balance.climbCameraHeight : 0)); Vector3 back = rotation * Vector3.back;
            float desiredDistance = distance + (traversal.State == TraversalState.Glide ? balance.glideCameraDistance : 0);
            smoothDistance = Mathf.Lerp(smoothDistance, desiredDistance, 1 - Mathf.Exp(-balance.cameraBlend * Time.deltaTime));
            float actual = smoothDistance;
            if (Physics.SphereCast(focus, balance.cameraRadius, back, out var hit, actual, balance.solidMask, QueryTriggerInteraction.Ignore)) actual = Mathf.Max(.1f, hit.distance - .1f);
            virtualCamera.transform.SetPositionAndRotation(focus + back * actual, rotation);
        }
        float smoothDistance = 6;
    }
}
