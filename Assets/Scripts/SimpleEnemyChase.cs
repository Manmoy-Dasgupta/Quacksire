using UnityEngine;

/// <summary>
/// Simple ground chase toward the object tagged Player; drives Animator Speed like the player rig.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class SimpleEnemyChase : MonoBehaviour
{
    [SerializeField] float moveSpeed = 2.2f;
    [SerializeField] float rotateSpeed = 540f;
    [SerializeField] string speedParameter = "Speed";

    CharacterController _cc;
    Animator _anim;
    Transform _player;

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
        _anim = GetComponent<Animator>();
    }

    void Start()
    {
        GameObject go = GameObject.FindGameObjectWithTag("Player");
        if (go != null)
            _player = go.transform;
    }

    void Update()
    {
        if (_player == null)
            return;

        Vector3 flat = _player.position - transform.position;
        flat.y = 0f;
        if (flat.sqrMagnitude < 0.01f)
        {
            if (_anim != null)
                _anim.SetFloat(speedParameter, 0f);
            return;
        }

        Vector3 dir = flat.normalized;
        Quaternion target = Quaternion.LookRotation(dir, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, target, rotateSpeed * Time.deltaTime);

        Vector3 worldMove = transform.forward * moveSpeed;
        _cc.SimpleMove(worldMove);

        if (_anim != null)
            _anim.SetFloat(speedParameter, Mathf.Clamp01(_cc.velocity.magnitude / Mathf.Max(0.01f, moveSpeed)));
    }
}
