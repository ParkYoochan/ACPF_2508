using UnityEngine;

public class MSM_PivotController : MonoBehaviour
{
    public enum Axis { X, Y, Z }
    [Header("Which local axis to rotate this pivot around")]
    public Axis rotateAxis = Axis.Y;      // YawPivot=Y, PitchPivot=X(또는 Z), RollPivot=Z(또는 X)로 세팅

    [Header("Input keys (hold)")]
    public KeyCode negativeKey = KeyCode.A;   // 예: A(왼),   S(아래),  Q(반시계)
    public KeyCode positiveKey = KeyCode.D;   // 예: D(오른), W(위),   E(시계)

    [Header("Motion")]
    public float speedDegPerSec = 90f;
    public float minDeg = -30f;
    public float maxDeg = 30f;
    public bool invert = false;

    float _angle;                    // 현재 축 각도 (local 기준)
    Quaternion _initialLocalRot;     // 시작시 기울어짐 보정용

    void Awake()
    {
        // 플레이 시작 시 살짝 기울어져 있던 각도를 기준 0도로 정렬
        _initialLocalRot = transform.localRotation;
        transform.localRotation = Quaternion.identity;
        _angle = 0f;
    }

    void Update()
    {
        int dir = 0;
        if (Input.GetKey(negativeKey)) dir -= 1;
        if (Input.GetKey(positiveKey)) dir += 1;
        if (invert) dir = -dir;
        if (dir == 0) return;

        _angle += dir * speedDegPerSec * Time.deltaTime;
        _angle = Mathf.Clamp(_angle, minDeg, maxDeg);

        Vector3 axis = Vector3.right;
        switch (rotateAxis)
        {
            case Axis.X: axis = Vector3.right; break;
            case Axis.Y: axis = Vector3.up; break;
            case Axis.Z: axis = Vector3.forward; break;
        }

        // 초기 기울어짐 보정 후, 지정 축으로만 회전
        transform.localRotation = _initialLocalRot * Quaternion.AngleAxis(_angle, axis);
    }
}
