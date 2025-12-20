using Unity.Netcode;
using Cinemachine;
using UnityEngine;

public class CameraFollow : NetworkBehaviour
{
    private CinemachineVirtualCamera _virtualCamera;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            // Try to find it immediately (in case we join a game already in progress)
            AssignCamera();
        }
    }

    private void Update()
    {
        // If we are the owner, but we haven't found the camera yet (e.g., scene is still loading), keep trying
        if (IsOwner && _virtualCamera == null)
        {
            AssignCamera();
        }
    }

    private void AssignCamera()
    {
        var vcam = FindObjectOfType<CinemachineVirtualCamera>();

        if (vcam != null)
        {
            _virtualCamera = vcam;
            _virtualCamera.Follow = transform;

            // Optional: Set LookAt if you were 3D, but for 2D top-down Follow is usually enough
            // _virtualCamera.LookAt = transform;

            Debug.Log($"Camera found and assigned to {gameObject.name}");
        }
    }
}
