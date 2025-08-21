using UnityEngine;

public class MSM_PivotController : MonoBehaviour
{
    public enum Axis { X, Y, Z }
    [Header("Which local axis to rotate this pivot around")]
    public Axis rotateAxis = Axis.Y;      // YawPivot=Y, PitchPivot=X(�Ǵ� Z), RollPivot=Z(�Ǵ� X)�� ����

    [Header("Input keys (hold)")]
    public KeyCode negativeKey = KeyCode.A;   // ��: A(��),   S(�Ʒ�),  Q(�ݽð�)
    public KeyCode positiveKey = KeyCode.D;   // ��: D(����), W(��),   E(�ð�)

    [Header("Motion")]
    public float speedDegPerSec = 90f;
    public float minDeg = -30f;
    public float maxDeg = 30f;
    public bool invert = false;

    float _angle;                    // ���� �� ���� (local ����)
    Quaternion _initialLocalRot;     // ���۽� ������ ������

    void Awake()
    {
        // �÷��� ���� �� ��¦ ������ �ִ� ������ ���� 0���� ����
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

        // �ʱ� ������ ���� ��, ���� �����θ� ȸ��
        transform.localRotation = _initialLocalRot * Quaternion.AngleAxis(_angle, axis);
    }
}
