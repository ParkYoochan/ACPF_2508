using UnityEngine;

public class MSM1_left_AB : MonoBehaviour
{
    public ArticulationBody joint;                 // ����θ� �ڵ�
    public float speedDegPerSec = 90f;
    public float minDeg = -30f, maxDeg = 30f;
    public bool invert = false;                    // ���� �ݴ�� üũ
    float target;

    void Awake()
    {
        if (!joint) joint = GetComponent<ArticulationBody>();
        var d = joint.xDrive;
        d.lowerLimit = minDeg; d.upperLimit = maxDeg;
        d.stiffness = 20000f; d.damping = 700f; d.forceLimit = 10000f;
        joint.xDrive = d; target = d.target;
    }
    void Update()
    {
        float in1 = (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.W) ? -1 : 0)
                  + (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.S) ? 1 : 0);
        if (invert) in1 = -in1;
        target = Mathf.Clamp(target + in1 * speedDegPerSec * Time.deltaTime, minDeg, maxDeg);
        var d = joint.xDrive; d.target = target; joint.xDrive = d;   // ����� �ʼ�
    }
}
