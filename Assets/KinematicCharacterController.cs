using System;
using MyBox;
using UnityEngine;

// ReSharper disable InconsistentNaming
public class KinematicCharacterController : MonoBehaviour {
    public Vector3 gravity = new Vector3(0, -9.8f, 0);
    public LayerMask collisionMask = default;
    public Transform cam = default;
    public float maxSpeed = 1f;
    public float groundFriction = 5f;
    public float airFriction = .1f;
    public float jumpHeight = 2f;
    [Range(0f, 90f)] public float maxGroundAngle = 25f;
    public float gravityAlignSpeed = 90f;

    private Transform _transform;
    private Vector3 _halfExtents;
    private CapsuleCollider _collider;
    private readonly Collider[] _overlapResults = new Collider[16];
    [SerializeField][ReadOnly] private Vector3 _velocity;
    [SerializeField][ReadOnly] private bool _isGrounded;
    [SerializeField][ReadOnly] private Vector3 _groundNormal;
    [SerializeField][ReadOnly] private Vector3 _groundPoint;
    private float _minGroundDotProduct;
    private readonly RaycastHit[] _groundRaycastHits = new RaycastHit[4];
    private Vector3 _desiredVelocity;
    private int _framesSinceGround, _framesSinceJump;

    private void Awake() {
        _transform = transform;
        _collider = GetComponent<CapsuleCollider>();
        var bounds = _collider.bounds;
        _halfExtents = (bounds.max - bounds.min) * .5f;
        OnValidate();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update() {
        var input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        input = Vector3.ClampMagnitude(input, 1f);
        _desiredVelocity = new Vector3(input.x, 0, input.y) * maxSpeed;
        
        AdjustVelocity();
        AlignToGravity();

        if (_isGrounded && Input.GetButtonDown("Jump")) {
            _framesSinceJump = 0;
            _velocity += Mathf.Sqrt(2f * gravity.magnitude * jumpHeight) * -gravity.normalized;
        }
        
        _velocity += gravity * Time.deltaTime;
        _transform.position += _velocity * Time.deltaTime;
        _velocity -= (_isGrounded ? groundFriction : airFriction) * Time.deltaTime * _velocity;
    }

    private void LateUpdate() {
        _framesSinceGround++;
        _framesSinceJump++;
        var colliderRadius = _collider.radius;
        var pos = _transform.position;
        var numOverlaps = Physics.OverlapBoxNonAlloc(pos, _halfExtents, _overlapResults, _transform.rotation, collisionMask, QueryTriggerInteraction.UseGlobal);
        for (var i = 0; i < numOverlaps; i++) {
            if (Physics.ComputePenetration(
                    _collider, pos, _transform.rotation,
                    _overlapResults[i], _overlapResults[i].transform.position, _overlapResults[i].transform.rotation,
                    out var direction, out var distance)
            ) {
                var penetrationVector = direction * distance;
                var velocityProjected = Vector3.Project(_velocity, -direction);
                if (Vector3.Angle(penetrationVector, -gravity) < maxGroundAngle) {
                    _transform.position += penetrationVector;
                    _velocity -= velocityProjected;
                } else {
                    var yAxis = _isGrounded ? _groundNormal : -gravity.normalized;
                    _transform.position += Vector3.ProjectOnPlane(penetrationVector, yAxis);
                    _velocity -= Vector3.ProjectOnPlane(velocityProjected, yAxis);
                }
            }
        }

        _isGrounded = false;
        var point1 = _transform.rotation * new Vector3(0, _collider.height / 2 - colliderRadius - .05f, 0);
        var point2 = -point1;
        var groundHits = Physics.CapsuleCastNonAlloc(pos + point1, pos + point2, colliderRadius, gravity.normalized, _groundRaycastHits, .051f, collisionMask);

        var avgGroundNormal = Vector3.zero;
        var avgGroundPoint = Vector3.zero;
        
        for (var i = 0; i < groundHits; i++) {
            _isGrounded |= Vector3.Dot(_groundRaycastHits[i].normal, -gravity.normalized) >= _minGroundDotProduct;
            avgGroundNormal += _groundRaycastHits[i].normal;
            avgGroundPoint += _groundRaycastHits[i].point;
        }

        if (groundHits == 0) {
            _groundNormal = -gravity.normalized;
            _groundPoint = Vector3.zero;
        } else {
            _groundNormal = (avgGroundNormal / groundHits).normalized;
            _groundPoint = avgGroundPoint / groundHits;
        }

        if (_isGrounded || (_framesSinceGround <= 1 && _framesSinceJump > 2)) {
            if (Physics.CapsuleCast(pos + point1, pos + point2, colliderRadius, gravity.normalized, out var hit, .5f, collisionMask)) {
                if (Vector3.Dot(hit.normal, -gravity.normalized) >= _minGroundDotProduct) {
                    _groundPoint = hit.point;
                    _groundNormal = hit.normal;
                    _isGrounded = true;
                    var speed = _velocity.magnitude;
                    var dot = Vector3.Dot(_velocity, hit.normal);
                    if (dot > 0f) {
                        _velocity = (_velocity - hit.normal * dot).normalized * speed;
                    }
                }
            }
        }

        if (_isGrounded) _framesSinceGround = 0;
    }

    private void AdjustVelocity() {
        var yAxis = _isGrounded ? _groundNormal : -gravity.normalized;
        var xAxis = Vector3.Cross(yAxis, cam.forward).normalized;
        var zAxis = Vector3.Cross(xAxis, yAxis).normalized;

        var currentY = Vector3.Dot(_velocity, yAxis);

        var newX = _desiredVelocity.x;
        var newZ = _desiredVelocity.z;

        _velocity = xAxis * newX + yAxis * currentY + zAxis * newZ;
    }

    private void AlignToGravity() {
        var rot = _transform.rotation;
        var target = -gravity;
        if (Math.Abs(Vector3.Angle(_transform.up, target) - 180f) < 1f) {
            target = Vector3.Cross(target, cam.forward).normalized;
        }
        _transform.rotation = Quaternion.RotateTowards(
            rot,
            Quaternion.FromToRotation(_transform.up, target) * rot,
            gravityAlignSpeed * Time.deltaTime
        );
    }

    private void OnDrawGizmos() {
        if (_isGrounded) {
            Debug.DrawRay(_groundPoint, _groundNormal, Color.white);
        }
    }

    private void OnValidate() {
        _minGroundDotProduct = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);
    }

    private void OnTriggerEnter(Collider other) {
        var toggleGravity = other.gameObject.GetComponent<ToggleGravity>();
        if (toggleGravity != null) {
            gravity = toggleGravity.gravity;
        }
    }
}
