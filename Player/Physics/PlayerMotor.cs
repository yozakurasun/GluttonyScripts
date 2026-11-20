using UnityEngine;

public class PlayerMotor
{
    private const float MOVE_THRESHOLD = 0.01f;
    private Rigidbody _rb;

    public Vector3 Position => _rb.position;
    public Vector3 Forward => _rb.rotation * Vector3.forward;

    public PlayerMotor(Rigidbody rb)
    {
        _rb = rb;
    }

    public void Move(Vector2 input, float speed)
    {
        Quaternion rot = _rb.rotation;
        Vector3 direction = rot * Vector3.forward * input.y + rot * Vector3.right * input.x;

        Vector3 velocity = Vector3.zero;
        if (direction.sqrMagnitude > MOVE_THRESHOLD * MOVE_THRESHOLD) velocity = direction.normalized * speed;

        _rb.linearVelocity = new Vector3(velocity.x, _rb.linearVelocity.y, velocity.z);
    }

    public void Rotate(float yawDegrees)
    {
        if (Mathf.Approximately(yawDegrees, 0f)) return;
        //回転Yをフリーズしているので直接回す
        _rb.rotation *= Quaternion.Euler(0f, yawDegrees, 0f);
    }

    public void Stop()
    {
        _rb.linearVelocity = new Vector3(0f, _rb.linearVelocity.y, 0f);
    }
}
