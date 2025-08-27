using UnityEngine;

[DisallowMultipleComponent]
public class MasterController : MonoBehaviour
{
    [Header("Movement (Position-based)")]
    public float moveSpeed = 6f;         // m/s
    public bool cameraRelative = false;  // true면 카메라 기준 WASD

    Vector2 input;

    void Update()
    {
        // 입력: WASD/화살표
        input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        Vector3 dir;

        if (cameraRelative && Camera.main != null)
        {
            // 카메라 기준 평면 이동
            Vector3 fwd = Camera.main.transform.forward;
            Vector3 right = Camera.main.transform.right;
            fwd.y = 0f; right.y = 0f;
            fwd.Normalize(); right.Normalize();
            dir = (right * input.x + fwd * input.y).normalized;
        }
        else
        {
            // 월드 기준 XZ 이동
            dir = new Vector3(input.x, 0f, input.y).normalized;
        }

        transform.position += dir * moveSpeed * Time.deltaTime;
    }
}
