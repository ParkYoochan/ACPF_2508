using UnityEngine;

public class MSM2AxisController : MonoBehaviour
{
    [Header("Pivots")]
    public Transform yawPivot;    // 자신(YawPivot)
    public Transform pitchPivot;  // 자식(PitchPivot)

    [Header("Speeds (deg/sec)")]
    public float yawSpeed = 90f;
    public float pitchSpeed = 90f;

    [Header("Angle Limits (deg, local)")]
    public float yawMin = -35f;
    public float yawMax = 35f;
    public float pitchMin = -25f;
    public float pitchMax = 25f;

    [Header("Invert")]
    public bool invertYaw = false;
    public bool invertPitch = false;

    [Header("Start Options")]
    public bool resetStraightOnPlay = true;   // ▶️ 재생 시 강제로 1자로 정렬

    float _yaw;   // 누적 각도(로컬)
    float _pitch;

    void Awake()
    {
        if (!yawPivot) yawPivot = transform;
        if (!pitchPivot) pitchPivot = transform.GetChild(0);

        if (resetStraightOnPlay)
        {
            // 재생하면 항상 1자로(기준자세) 시작
            yawPivot.localRotation = Quaternion.identity;
            pitchPivot.localRotation = Quaternion.identity;
        }

        _yaw = Angle180(yawPivot.localEulerAngles.y);
        _pitch = Angle180(pitchPivot.localEulerAngles.x);
    }

    void Update()
    {
        float yawInput = (Input.GetKey(KeyCode.A) ? -1f : 0f) + (Input.GetKey(KeyCode.D) ? 1f : 0f);
        float pitchInput = (Input.GetKey(KeyCode.W) ? 1f : 0f) + (Input.GetKey(KeyCode.S) ? -1f : 0f);

        if (invertYaw) yawInput = -yawInput;
        if (invertPitch) pitchInput = -pitchInput;

        _yaw = Mathf.Clamp(_yaw + yawInput * yawSpeed * Time.deltaTime, yawMin, yawMax);
        _pitch = Mathf.Clamp(_pitch + pitchInput * pitchSpeed * Time.deltaTime, pitchMin, pitchMax);

        // Yaw = 로컬 Y, Pitch = 로컬 X  (축이 다르면 X↔Z 바꿔서 사용)
        var yawEuler = yawPivot.localEulerAngles;
        yawEuler.y = _yaw;
        yawPivot.localEulerAngles = yawEuler;

        var pitchEuler = pitchPivot.localEulerAngles;
        pitchEuler.x = _pitch;
        pitchPivot.localEulerAngles = pitchEuler;
    }

    float Angle180(float a) => (a > 180f) ? a - 360f : a;
}
