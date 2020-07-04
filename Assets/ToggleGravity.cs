using System;
using UnityEngine;

public class ToggleGravity : MonoBehaviour {
    public Vector3 gravity = new Vector3(0, -9.8f, 0);

    private void OnDrawGizmos() {
        DrawArrow.ForGizmo(transform.position, gravity.normalized);
    }
}