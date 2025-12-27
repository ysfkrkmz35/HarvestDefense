using UnityEngine;

public class SwordVisualAligner : MonoBehaviour
{
    private Animator _playerAnimator;
    private Transform _transform;

    void Start()
    {
        _transform = transform;
        // Search for animator in parents (Player)
        _playerAnimator = GetComponentInParent<Animator>();
    }

    void LateUpdate()
    {
        if (_playerAnimator == null) return;

        // Get Direction from Player Animator
        float dirY = _playerAnimator.GetFloat("DirY");
        
        Vector3 pos = _transform.localPosition;
        
        // Adjust Z based on facing direction
        // Up (Back) -> Behind Player -> Z > 0
        // Down/Side -> In Front -> Z < 0
        
        if (dirY > 0.1f)
        {
            pos.z = 0.5f; // Push behind
        }
        else
        {
            pos.z = -0.5f; // Pull forward
        }

        _transform.localPosition = pos;
    }
}
