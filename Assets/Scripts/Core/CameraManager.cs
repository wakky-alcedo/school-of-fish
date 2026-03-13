using UnityEngine;

namespace SchoolOfFish.Core
{
    public class CameraManager : MonoBehaviour
    {
        [SerializeField] private Camera fixedCamera;
        [SerializeField] private Camera followCamera;
        [SerializeField] private Transform followTarget;
        [SerializeField] private Vector3 followOffset = new Vector3(0f, 6f, -14f);
        [SerializeField] private float followSmooth = 4f;
        [SerializeField] private KeyCode switchKey = KeyCode.C;

        private bool _followMode;

        private void Start()
        {
            ApplyMode();
        }

        private void Update()
        {
            if (Input.GetKeyDown(switchKey))
            {
                _followMode = !_followMode;
                ApplyMode();
            }

            if (_followMode)
            {
                UpdateFollowCamera();
            }
        }

        private void ApplyMode()
        {
            if (fixedCamera != null)
            {
                fixedCamera.gameObject.SetActive(!_followMode);
            }

            if (followCamera != null)
            {
                followCamera.gameObject.SetActive(_followMode);
            }
        }

        private void UpdateFollowCamera()
        {
            if (followCamera == null || followTarget == null)
            {
                return;
            }

            Vector3 desired = followTarget.position + followTarget.TransformDirection(followOffset);
            followCamera.transform.position = Vector3.Lerp(
                followCamera.transform.position,
                desired,
                1f - Mathf.Exp(-followSmooth * Time.deltaTime));

            Vector3 lookAt = followTarget.position + Vector3.up * 1.5f;
            Quaternion desiredRot = Quaternion.LookRotation((lookAt - followCamera.transform.position).normalized, Vector3.up);
            followCamera.transform.rotation = Quaternion.Slerp(
                followCamera.transform.rotation,
                desiredRot,
                1f - Mathf.Exp(-followSmooth * Time.deltaTime));
        }
    }
}
