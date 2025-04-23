using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgentMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 180f;
    [SerializeField] private float strafeSpeed = 3f;
    
    private Rigidbody rb;
    
    public delegate void MovementEvent(Vector3 position, Vector3 velocity);
    public event MovementEvent OnMove;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }
    }
    
    public void MoveForward()
    {
        Vector3 forwardMovement = transform.forward * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + forwardMovement);
        OnMove?.Invoke(transform.position, rb.velocity);
    }
    
    public void MoveBackward()
    {
        Vector3 backwardMovement = -transform.forward * moveSpeed * 0.7f * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + backwardMovement);
        OnMove?.Invoke(transform.position, rb.velocity);
    }
    
    public void MoveLeft()
    {
        Vector3 leftMovement = -transform.right * strafeSpeed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + leftMovement);
        OnMove?.Invoke(transform.position, rb.velocity);
    }
    
    public void MoveRight()
    {
        Vector3 rightMovement = transform.right * strafeSpeed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + rightMovement);
        OnMove?.Invoke(transform.position, rb.velocity);
    }
    
    public void RotateLeft()
    {
        float rotationAmount = -rotationSpeed * Time.fixedDeltaTime;
        Quaternion deltaRotation = Quaternion.Euler(0f, rotationAmount, 0f);
        rb.MoveRotation(rb.rotation * deltaRotation);
    }
    
    public void RotateRight()
    {
        float rotationAmount = rotationSpeed * Time.fixedDeltaTime;
        Quaternion deltaRotation = Quaternion.Euler(0f, rotationAmount, 0f);
        rb.MoveRotation(rb.rotation * deltaRotation);
    }
    
    public void MoveInDirection(Vector3 direction, float speedMultiplier = 1f)
    {
        Vector3 movement = direction.normalized * moveSpeed * speedMultiplier * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + movement);
        OnMove?.Invoke(transform.position, rb.velocity);
    }
    
    public void SetVelocity(Vector3 velocity)
    {
        rb.velocity = velocity;
    }
    
    public void StopMovement()
    {
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }
    
    // Getter method for move speed
    public float GetMoveSpeed()
    {
        return moveSpeed;
    }
    
    // Setter method for move speed
    public void SetMoveSpeed(float newSpeed)
    {
        moveSpeed = newSpeed;
        // Also update strafe speed proportionally
        strafeSpeed = newSpeed * 0.6f;
    }
} 