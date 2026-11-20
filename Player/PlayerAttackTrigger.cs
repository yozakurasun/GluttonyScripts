using UnityEngine;

public class PlayerAttackTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Back"))
        {
            if (other.gameObject.TryGetComponent<PlayerBackTrigger>(out var obj))
            {
                if (obj.transform.root == transform.root) return; //自分の背中は無視
                Debug.Log("Attack!");
                obj.OnDamage();
            }
        }
    }
}
