using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

namespace WanderingCity
{
    [DefaultExecutionOrder(100)]
    public sealed class OrbitCamera : MonoBehaviour
    {
        public bool AcceptInput = true;
        public Transform Target; public GameSession Session;
        public float Yaw, Pitch = 12;
        CinemachineCamera virtualCamera;
        float distance = 4.6f;
        void Start()
        {
            var brain = gameObject.AddComponent<CinemachineBrain>(); brain.UpdateMethod = CinemachineBrain.UpdateMethods.LateUpdate;
            virtualCamera = new GameObject("Traveler / Cinemachine camera").AddComponent<CinemachineCamera>();
            float initialFov = Session != null && Session.Balance != null ? Session.Balance.cameraFov : 54f;
            virtualCamera.Lens.FieldOfView = initialFov;
            virtualCamera.Lens.FarClipPlane = 1200;
            virtualCamera.Lens.NearClipPlane = .1f;
            if (Session != null && Session.Balance != null)
            {
                distance = Session.Balance.cameraDistance;
                smoothDistance = distance;
            }
        }
        void LateUpdate()
        {
            if (Target == null || virtualCamera == null) return;
            if (AcceptInput && Session.InputReady && Mouse.current != null) { Vector2 delta = Mouse.current.delta.ReadValue(); Yaw += delta.x * .12f; Pitch = Mathf.Clamp(Pitch - delta.y * .1f, -25, 65); distance = Mathf.Clamp(distance - Mouse.current.scroll.ReadValue().y * .005f, 2.5f, 8f); }
            var traversal = Session.Player.Traversal; var balance = Session.Balance;
            float desiredFov = traversal.State == TraversalState.Sprint ? balance.sprintFov : balance.cameraFov;
            virtualCamera.Lens.FieldOfView = Mathf.Lerp(virtualCamera.Lens.FieldOfView, desiredFov, 1 - Mathf.Exp(-balance.cameraBlend * Time.deltaTime));
            Quaternion rotation = Quaternion.Euler(Pitch, Yaw, 0); Vector3 focus = Target.position + Vector3.up * (balance.cameraFocusHeight + (traversal.State == TraversalState.Climb ? balance.climbCameraHeight : 0)); Vector3 back = rotation * Vector3.back;
            float desiredDistance = distance + (traversal.State == TraversalState.Glide ? balance.glideCameraDistance : 0);
            smoothDistance = Mathf.Lerp(smoothDistance, desiredDistance, 1 - Mathf.Exp(-balance.cameraBlend * Time.deltaTime));
            float actual = smoothDistance;
            if (Physics.SphereCast(focus, balance.cameraRadius, back, out var hit, actual, balance.solidMask, QueryTriggerInteraction.Ignore)) actual = Mathf.Max(.1f, hit.distance - .1f);
            virtualCamera.transform.SetPositionAndRotation(focus + back * actual, rotation);
        }
        public void SetDistance(float value) => distance = Mathf.Clamp(value, 2.5f, 8f);
        float smoothDistance = 4.6f;
    }
}
