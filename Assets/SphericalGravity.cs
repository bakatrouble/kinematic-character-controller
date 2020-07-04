using System;
using UnityEngine;

[RequireComponent(typeof(KinematicCharacterController))]
public class SphericalGravity : MonoBehaviour {
    public Vector3 origin = Vector3.zero;
    public float strength = -9.8f;

    private KinematicCharacterController _controller;

    private void Awake() {
        _controller = GetComponent<KinematicCharacterController>();
    }

    private void Update() {
        _controller.gravity = (transform.position - origin) * strength;
    }
}