using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Controller_NonPhysics : MonoBehaviour
{
    Vector3 input = Vector3.zero;
    Vector3 _velocity = Vector3.zero;

    [SerializeField, Range(0,100)]
    float moveSpeed = default;
    [SerializeField, Range(0, 100)]
    float maxAcceleration = default;
    [SerializeField, Range(0, 1)]
    float bounciness = default;
    [SerializeField]
    Rect field = new Rect(-5, -5, 10, 10);

    void Update()
    {
        input.x = Input.GetAxis("Horizontal");
        input.z = Input.GetAxis("Vertical");

        //�Է°��� ũ�⸦ ������ 1�� ����
        //input.Normalize();

        //�Է°��� 0~1���̷� ����
        //���� ũ�Ⱑ 1�� �ʰ��� ��츸 1�� ����
        input = Vector3.ClampMagnitude(input, 1);
        Debug.Log(input);
    }

    private void FixedUpdate()
    {

        /*Vector3 velocity = input * moveSpeed;
        Vector3 displacement = velocity * Time.fixedDeltaTime
        transform.position += velocity;*/

        //����(�����ϱ� �����)
        /*Vector3 acceleration = input * moveSpeed;
        _velocity += acceleration * Time.fixedDeltaTime;
        transform.localPosition += _velocity * Time.fixedDeltaTime;*/


         Vector3 desireVelocity = input * moveSpeed;
         float currAccel = maxAcceleration * Time.fixedDeltaTime;
         //���ӵ��� ���ϴ� �ӵ����� �����ų� ������� �������Ӹ��� ���� �ӵ���ŭ �ӵ��� ��ȭ�Ѵ�. 
 /*        if (velocity.x < desireVelocity.x)
         {
             _velocity.x = Mathf.Min(_velocity.x+ currAccel, desireVelocity.x);
         }
         else if(_velocity.x > desireVelocity.x)
         {
             _velocity.x = Mathf.Max(_velocity.x - currAccel, desireVelocity.x);
         }*/

         //Mathf�Լ��� ����Ͽ� ���� ���������ϴ�.
         _velocity.x = Mathf.MoveTowards(_velocity.x, desireVelocity.x, currAccel);
         _velocity.z = Mathf.MoveTowards(_velocity.z, desireVelocity.z, currAccel);

        // transform.localPosition += _velocity * Time.fixedDeltaTime; 

        //���� �̻��
        //Vector3 position = transform.position + input * Time.fixedDeltaTime * moveSpeed;
        
        //���ӻ��
        Vector3 position = _velocity * Time.fixedDeltaTime + transform.position;
        
        /* if (!field.Contains(new Vector2(position.x, position.z)))
         {
             //position = transform.localPosition;
             position.x = Mathf.Clamp(position.x, field.xMin, field.xMax);
             position.z = Mathf.Clamp(position.z, field.yMin, field.yMax);
         }*/

        //���࿡ �ӵ��� ������ ����Ǿ� ���� ���
        //�ش� �������� ������ ��� ����Ǽ� �ݴ� �������� ����� �׸�ŭ �ð��� �ɸ���.
        //�ٷ� ����� ���ؼ� �ش� ������ �ӵ��� 0���� �����.
/*        if (position.x < field.xMin)
        {
            position.x = field.xMin;
            _velocity.x = 0;
        }
        if (position.x > field.xMax)
        {
            position.x = field.xMax;
            _velocity.x = 0;
        }
        if (position.z < field.yMin)
        {
            position.z = field.yMin;
            _velocity.z = 0;
        }
        if (position.z > field.yMax)
        {
            position.z = field.yMax;
            _velocity.z = 0;
        }*/

        //�Ǵ� �ٿ�� �༭ �ش� ������ ������ �̵��ϰ� �Ҽ����ִ�.
        if (position.x < field.xMin)
        {
            position.x = field.xMin;
            _velocity.x = -_velocity.x * bounciness;
        }
        if (position.x > field.xMax)
        {
            position.x = field.xMax;
            _velocity.x = -_velocity.x * bounciness;
        }
        if (position.z < field.yMin)
        {
            position.z = field.yMin;
            _velocity.z = -_velocity.z * bounciness;
        }
        if (position.z > field.yMax)
        {
            position.z = field.yMax;
            _velocity.z = -_velocity.z * bounciness;
        }

        transform.position = position;
    }
}
