using UnityEngine;

public class RodController : MonoBehaviour
{
    public Transform yawPivot;
    public Transform pitchPivot;
    public Transform rollPivot;

    void Update()
    {
        // 테스트용 입력
        float yaw = Input.GetAxis("Horizontal");   // ←→ 키
        float pitch = Input.GetAxis("Vertical");   // ↑↓ 키
        float roll = Input.GetKey(KeyCode.Q) ? 1 : (Input.GetKey(KeyCode.E) ? -1 : 0);

        // 회전 적용
        yawPivot.localRotation = Quaternion.Euler(0, yaw * 45f, 0);
        pitchPivot.localRotation = Quaternion.Euler(pitch * 45f, 0, 0);
        rollPivot.localRotation = Quaternion.Euler(0, 0, roll * 45f);
    }
}
