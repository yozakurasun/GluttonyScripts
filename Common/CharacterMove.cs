using UnityEngine;

public class CharacterMove
{
    public void MoveCharacter(Rigidbody rb, Vector3 direction, float speed)
    {
        if (direction.magnitude > 0.01f)
        {
            Vector3 velocity = direction.normalized * speed;
            rb.linearVelocity = new Vector3(velocity.x, rb.linearVelocity.y, velocity.z);
        }
        else
        {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
        }
    }
}
