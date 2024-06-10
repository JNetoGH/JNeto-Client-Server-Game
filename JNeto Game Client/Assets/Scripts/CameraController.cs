using UnityEngine;

public class CameraController : MonoBehaviour
{
    public PlayerManager player;
    public float sensitivity = 100f;
    public float clampAngle = 85f;

    private float _verticalRotation;
    private float _horizontalRotation;

    private void Start()
    {
        _verticalRotation = transform.localEulerAngles.x;
        _horizontalRotation = player.transform.eulerAngles.y;
    }

    private void Update()
    {
        Look();
        Debug.DrawRay(transform.position, transform.forward * 2, Color.red);
    }

    private void Look()
    {
        float mouseVertical = -Input.GetAxis("Mouse Y");
        float mouseHorizontal = Input.GetAxis("Mouse X");

        _verticalRotation += mouseVertical * sensitivity * Time.deltaTime;
        _horizontalRotation += mouseHorizontal * sensitivity * Time.deltaTime;

        _verticalRotation = Mathf.Clamp(_verticalRotation, -clampAngle, clampAngle);

        transform.localRotation = Quaternion.Euler(_verticalRotation, 0f, 0f);
        player.transform.rotation = Quaternion.Euler(0f, _horizontalRotation, 0f);
    }
}
