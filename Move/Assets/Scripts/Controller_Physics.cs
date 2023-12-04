using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class Controller_Physics : MonoBehaviour
{
    [SerializeField]
    LayerMask probeMask = -1;
    [SerializeField]
    LayerMask stairMask = -1;
    [SerializeField]
    Rigidbody rb;
    [SerializeField]
    Transform playerInputSpace = default;
    [SerializeField]
    TrailRenderer trailRenderer;

    Vector3 input = Vector3.zero;
    Vector3 inputMouse = Vector3.zero;
    Vector3 velocity = Vector3.zero;
    Vector3 desireVelocity = Vector3.zero;
    Vector3 contactNormal;
    Vector3 steepNormal;

    float minGroundDotProduct;
    float minObjectDotProduct;
    float currMouseSpeed = 0;

    bool desireJump = false;
    bool OnGround => groundContactCount>0;
    bool OnSteep => steepContactCount>0;

    int jumpPhase = 0;
    int groundContactCount = 0;
    int steepContactCount = 0;
    int stepsSinceLastGrounded = 0;
    int stepsSinceLastJump = 0;

    [SerializeField, Range(0, 100)]
    float MouseSpeed = default;
    [SerializeField, Range(0,100)]
    float moveSpeed = default;
    [SerializeField, Range(0, 10f)]
    float jumpHeight = default;
    [SerializeField, Min(0f)]
    float probeDistance = default;

    [SerializeField, Range(0, 100)]
    float maxMouseAcceleration = default;
    [SerializeField, Range(0, 100)]
    float maxAcceleration = default;
    [SerializeField, Range(0, 100)]
    float maxAirAcceleration = default;
    [SerializeField, Range(0, 90f)]
    float maxGroundAngle = default;
    [SerializeField, Range(0, 90f)]
    float maxObjectAngle = default;
    [SerializeField, Range(0f, 100f)]
    float maxSnapSpeed = default;
    [SerializeField, Range(0, 5)]
    int maxAirJumps = default;

    private void OnValidate()
    {
        minGroundDotProduct = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);
        minObjectDotProduct = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);
    }

    private void Awake()
    {
        OnValidate();
    }

    void Update()
    {
        //�Է�
        input.x = Input.GetAxis("Horizontal");
        input.z = Input.GetAxis("Vertical");
        desireJump |= Input.GetButtonDown("Jump");

        inputMouse.x = Input.GetAxis("Mouse X");
        inputMouse.y = Input.GetAxis("Mouse Y");

        //�Է°� ��ȯ
        input = Vector3.ClampMagnitude(input, 1);
        inputMouse = Vector3.ClampMagnitude(inputMouse, 1);

        if (inputMouse.magnitude != 0)
        {
            currMouseSpeed += inputMouse.x * MouseSpeed;
            currMouseSpeed = Mathf.Clamp(currMouseSpeed, -maxMouseAcceleration, maxMouseAcceleration);
        }
        else
        {
            currMouseSpeed = 0;
        }

        transform.Rotate(Vector3.up, currMouseSpeed);

        if (playerInputSpace)
        {
            Vector3 forward = playerInputSpace.forward;
            Vector3 right = playerInputSpace.right;
            forward.y = 0f;
            forward.Normalize();
            right.y = 0f;
            right.Normalize();

            desireVelocity = (forward*input.z + right*input.x) * moveSpeed;
        }
        else
        {

            desireVelocity = input * moveSpeed;
        }

        //�����
        GetComponent<Renderer>().material.SetColor(
            "_Color", OnGround ? Color.black : Color.white
            //Color.white * (groundContactCount * 0.25f)
        );
    }

    private void FixedUpdate()
    {
        //���� ������Ʈ
        UpdateState();
        //�ӵ� ���
        AdjustVelocity();

        //����
        if (desireJump)
        {
            desireJump = false;
            Jump();
        }
        //�̵� 
        rb.velocity = velocity;
        
        //���� �ʱ�ȭ
        ClearState();
    }

 

    private void OnCollisionEnter(Collision collision)
    {
        EvaluateCollision(collision);
    }


    private void OnCollisionStay(Collision collision)
    {
        EvaluateCollision(collision);
    }

    private void UpdateState()
    {
        //������ �׶��忡�� �� �������� �������� �����ϱ� ���� ����
        stepsSinceLastGrounded += 1;
        stepsSinceLastJump += 1;
        velocity = rb.velocity;

        //Ȯ���� ���̰ų�, ���� �پ����� ���
        if (OnGround || SnapToGround() || CheckSteepContacts())
        {
            //�׶��� ���� �ʱ�ȭ
            //���� Ƚ�� �ʱ�ȭ
            //���� ���� 1�� �̻� ��� ���� ��� ���� ���ͷ� �ʱ�ȭ
            stepsSinceLastGrounded = 0;
            if (stepsSinceLastJump > 1)
            {
                jumpPhase = 0;
            }
            if (groundContactCount > 1)
            {
                contactNormal.Normalize();
            }
        }

        //���߿� ���� ���
        else
        {
            //���� ���� Vector3.up(����)
            contactNormal = Vector3.up;
        }
    }

    void AdjustVelocity()
    {
        //������ �Ǵ� ���� �ش� ������ �ø���.
        Vector3 xAxis = ProjectOnContactPlane(Vector3.right).normalized;
        Vector3 zAxis = ProjectOnContactPlane(Vector3.forward).normalized;

        //�̵� ������ ���� ���⿡ ���� �� �����Ѵ�. 
        float currX = Vector3.Dot(velocity, xAxis);
        float currZ = Vector3.Dot(velocity, zAxis);

        //����
        float acceleration = OnGround ? maxAcceleration : maxAirAcceleration;
        float maxSpeedChange = acceleration * Time.deltaTime;

        //���ϴ� �ӵ��� ����/�����Ѵ�.
        float newX = Mathf.MoveTowards(currX, desireVelocity.x, maxSpeedChange);
        float newZ = Mathf.MoveTowards(currZ, desireVelocity.z, maxSpeedChange);

        //���ο� �ӵ��� ���� �ӵ��� ���̸�ŭ ���� ��Ų��.
        velocity += xAxis * (newX - currX) + zAxis * (newZ - currZ);
    }

    Vector3 ProjectOnContactPlane(Vector3 vector)
    {
        //vector�� normal�� ���� ��ŭ Projection �Ѵ�.
        return vector - contactNormal * Vector3.Dot(vector, contactNormal);
    }

    void Jump()
    {
        Vector3 jumpDirection;
        if (OnGround)
        {
            jumpDirection = contactNormal;
        }
        else if (OnSteep)
        {
            jumpDirection = steepNormal;
            jumpPhase = 0;
        }
        else if (maxAirJumps > 0 &&jumpPhase <= maxAirJumps)
        {
            if (jumpPhase == 0)
            {
                jumpPhase = 1;
            }
            jumpDirection = contactNormal;
        }
        else
        {
            return;
        }

        jumpDirection = (jumpDirection + Vector3.up).normalized;


        //���� �ְų�, ���� ���� ��ȸ�� �������� ���
        //if (OnGround || jumpPhase < maxAirJumps)
        {
            stepsSinceLastJump = 0;
            jumpPhase += 1;

            //Root(-2*g*h) = ���� �ӵ�
            float jumpSpeed = Mathf.Sqrt(-2f * Physics.gravity.y * jumpHeight);

            //float alignSpeed = Vector3.Dot(velocity, contactNormal);
            float alignSpeed = Vector3.Dot(velocity, jumpDirection);
            //������� �׻� ���� �ӵ��� ����.
            //���� ���� �ӵ��� �����ӵ����� ������츦 ����Ͽ� 0���� ������ ������ ��� ��� ���߰� �����.
            if (alignSpeed > 0f)
            {
                jumpSpeed = Mathf.Max(jumpSpeed - alignSpeed, 0f);
            }
            //velocity.y += jumpSpeed;
            //velocity += contactNormal * jumpSpeed;
            velocity += jumpDirection * jumpSpeed;
        }
    }
    void EvaluateCollision(Collision collision)
    {
        float minDot = GetMinDot(collision.gameObject.layer);
        //��� contact�� �˻��Ͽ� ���� ���� �̻��� ����� ��� �����Ѵ�.
        for (int i = 0; i < collision.contactCount; i++)
        {
            Vector3 normal = collision.GetContact(i).normal;

            //onGround |= normal.y >= minGroundDotProduct;

            //cos���� y���� 1->-1�� ���Ƿ� �������� ������ ���� ����
            if (normal.y >= minDot)
            {
                groundContactCount += 1;
                contactNormal += normal;
            }
            else if (normal.y > -0.01f)
            {

                steepContactCount += 1;
                steepNormal += normal;
            }
        }
    }

    private void ClearState()
    {
        //�� ������ ���� �ʱ�ȭ�Ѵ�.(FixedUpdate�� ������)
        //�ൿ(fixedUpdate)->�ʱ�ȭ(fixedUpdate)->�Է�(update)->�浹ó��(oncollision)->...
        //��
        groundContactCount = 0;
        contactNormal = Vector3.zero;
        //���ĸ� ���
        steepContactCount = 0;
        steepNormal = Vector3.zero;
    }

    bool SnapToGround()
    {
        //���� ������ ����� 2������ �̻� ���� �������
        if (stepsSinceLastGrounded > 1 || stepsSinceLastJump<=2)
        {
            return false;
        }
        float speed = velocity.magnitude;
        //���� ���� �ӵ��� ���� �ӵ� �̻��̶��
        if(speed> maxSnapSpeed)
        {
            return false;
        }

        //������ ���̸� ���� �� hit�������� ���
        if(!Physics.Raycast(rb.position, Vector3.down, out RaycastHit hit, probeDistance, probeMask))
        {
            return false;
        }

        //hit�� ���� ��ȿ���� ���� ����ϰ��(maxSlopeAngle�� �ѱ���)
        if(hit.normal.y < GetMinDot(hit.collider.gameObject.layer))
        {
            return false;
        }

        //1�����ӱ����� �پ��ִٰ� �Ǵ�.
        groundContactCount = 1;
        contactNormal = hit.normal;

        float dot = Vector3.Dot(velocity, hit.normal);

        //���� velocity�� �ٴ��� ���ϰ� �ִٸ� �� �������� ��찡 �ֱ� ������ �� ��츦 �����Ѵ�.
        if (dot > 0f)
        {
            //velocity�� �翵�Ͽ� ������ �ٲٰ� magnitude�� ���ؼ� ������ ���� �ش�.
           velocity = (velocity - hit.normal * dot).normalized * speed;
        }

        return true;
    }

    float GetMinDot(int layer)
    {
        return (stairMask & (1 << layer)) == 0 ? minGroundDotProduct : minObjectDotProduct;
    }

    bool CheckSteepContacts()
    {
        //���� ���ĸ� ��縦 2�� �̻� ����ִٸ�
        //������ ��縦 �����.
        if (steepContactCount > 1)
        {
            steepNormal.Normalize();
            if (steepNormal.y >= minGroundDotProduct)
            {
                groundContactCount = 1;
                contactNormal = steepNormal;
                return true;
            }
        }
        return false;
    }

}
