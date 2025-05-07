using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float dragSpeed = 6f;
    public float zoomSpeed = 5f;
    public float minZoom = 2f;
    public float maxZoom = 6f;

    public Vector2 mapMin = new Vector2(-20, -10);
    public Vector2 mapMax = new Vector2(20, 10);

    public float edgeScrollSpeed = 6f;
    public float edgeThreshold = 30f; // 像素距離

    private Vector3 lastMousePos;
    private Camera cam;

    void Start()
    {
        cam = Camera.main;
    }

    void Update()
    {
        if (UIMode.IsMouseDragAllowed)
            HandleDrag();

        if (UIMode.IsMouseEdgeScrollAllowed)
            HandleEdgeScroll();

        HandleZoom();
        ClampCamera();
    }

    void HandleDrag()
    {
        if (Input.GetMouseButtonDown(0))
            lastMousePos = Input.mousePosition;

        if (Input.GetMouseButton(0))
        {
            Vector3 delta = Input.mousePosition - lastMousePos;
            Vector3 move = new Vector3(-delta.x, -delta.y, 0) * Time.deltaTime * dragSpeed;
            transform.Translate(move, Space.Self);
            lastMousePos = Input.mousePosition;
        }
    }

    void HandleEdgeScroll()
    {
        Vector3 move = Vector3.zero;
        Vector3 mouse = Input.mousePosition;

        if (mouse.x < edgeThreshold) move.x -= 1;
        if (mouse.x > Screen.width - edgeThreshold) move.x += 1;
        if (mouse.y < edgeThreshold) move.y -= 1;
        if (mouse.y > Screen.height - edgeThreshold) move.y += 1;

        if (move != Vector3.zero)
            transform.Translate(move * edgeScrollSpeed * Time.deltaTime, Space.Self);
    }

    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
        {
            cam.orthographicSize -= scroll * zoomSpeed;
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
        }
    }

    void ClampCamera()
    {
        float vertExtent = cam.orthographicSize;
        float horzExtent = vertExtent * Screen.width / Screen.height;

        float minX = mapMin.x + horzExtent;
        float maxX = mapMax.x - horzExtent;
        float minY = mapMin.y + vertExtent;
        float maxY = mapMax.y - vertExtent;

        float clampedX = Mathf.Clamp(transform.position.x, minX, maxX);
        float clampedY = Mathf.Clamp(transform.position.y, minY, maxY);

        transform.position = new Vector3(clampedX, clampedY, transform.position.z);
    }
}
