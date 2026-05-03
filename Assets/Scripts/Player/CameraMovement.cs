using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// ReSharper disable InconsistentNaming

public class CameraMovement : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform CameraTarget;

    [Header("Follow Settings")] 
    [SerializeField] private float HorizontalFollowSmoothTime = .5f;
    [SerializeField] private float VerticalFollowSmoothTime = .5f;
    [SerializeField] private float FollowSpeed = 1f;
    [SerializeField] private float CameraZoom = 5;
    [SerializeField] private Vector2 DeadZone = Vector2.zero;
    [SerializeField] private Vector2 TargetOffset =  Vector2.zero;
    
    [Header("Vertical Bias Settings")]
    [SerializeField] private float VerticalBias;
    [SerializeField] private float FallingBias;
    [SerializeField] private float FallingVelocityThreshold;
    
    [Header("Look Ahead Settings")]
    [SerializeField] private float LookAheadSmoothTime;
    [SerializeField] private float LookAheadDistance;
    [SerializeField] private float LookAheadSpeed;
    
    [Header("Camera Bounds")]
    [SerializeField] private float CameraBounds;
    [SerializeField] private bool UseCameraBounds;
    
    
    
    private Camera MainCamera;
    private float LookAhead;
    private Vector3 FollowVelocity;
    
    // Start is called before the first frame update
    private void Awake()
    {
        MainCamera = GetComponent<Camera>();

        if (!CameraTarget)
        {
            return;
        }
        
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (!CameraTarget)
        {
            return;
        }
        
      
     
        MoveFollowCamera();
    }

    public void ZoomCamera()
    {
        MainCamera.orthographicSize = +1;
        
    }

    public bool WithinDeadZone(float CameraAxis, float TargetAxis, float DeadZoneAxis)
    {
        return Mathf.Abs(TargetAxis - CameraAxis) <= DeadZoneAxis;
    }
    
    private void MoveFollowCamera()
    {
        Vector3 CurrentPosition = transform.position;
        Vector3 DesiredPosition = CurrentPosition;
        
        Vector3 TargetPosition = CameraTarget.position + (Vector3)TargetOffset;
        
        if (WithinDeadZone(CurrentPosition.x, TargetPosition.x, DeadZone.x))
        {
            DesiredPosition.x = CurrentPosition.x;
        } 
        else
        {
            float Direction = Mathf.Sign(TargetPosition.x - CurrentPosition.x);
            DesiredPosition.x = TargetPosition.x - Direction * DeadZone.x;
        }

        if (WithinDeadZone(CurrentPosition.y,TargetPosition.y,DeadZone.y))
        {
            DesiredPosition.y = CurrentPosition.y;
        } 
        else
        {
            float Direction = Mathf.Sign(TargetPosition.y - CurrentPosition.y);
            DesiredPosition.y = TargetPosition.y - Direction * DeadZone.y;
        }

        DesiredPosition.z = CurrentPosition.z;

        transform.position = new Vector3(
            Mathf.SmoothDamp(
                CurrentPosition.x,
                DesiredPosition.x,
                ref FollowVelocity.x,
                HorizontalFollowSmoothTime),
            Mathf.SmoothDamp(
                CurrentPosition.y,
                DesiredPosition.y,
                ref FollowVelocity.y,
                VerticalFollowSmoothTime),
            DesiredPosition.z);
    }

    private float GetLookAhead()
    {
        return 1;
    }
    public void ShakeCamera()
    {
        
    }

    public void ChangeCameraTarget(Transform newTarget)
    {
        CameraTarget =  newTarget;
        
    }


}
