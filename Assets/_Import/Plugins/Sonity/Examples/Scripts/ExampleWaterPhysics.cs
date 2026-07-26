// Created by Victor Engstr�m
// Copyright 2024 Sonigon AB
// http://www.sonity.org/

using UnityEngine;

namespace ExampleSonity {

    [AddComponentMenu("")]
    public class ExampleWaterPhysics : MonoBehaviour {

        public float buoyancy = 1.5f;
        private float dragMax = 2f;
        private float angularDragBase = 0.05f;
        private float angularDragMax = 0.8f;

        private Transform cachedTransform;

        void Start() {
            cachedTransform = GetComponent<Transform>();
        }

        private void OnTriggerStay(Collider other) {
            Rigidbody rigidbody = other.GetComponent<Rigidbody>();
            if (rigidbody != null && !rigidbody.isKinematic) {
                float waterSurfaceY = cachedTransform.position.y + cachedTransform.lossyScale.y * 0.5f;
                float objectMaxWidth = Mathf.Max(
                    Mathf.Abs(other.transform.lossyScale.x),
                    Mathf.Abs(other.transform.lossyScale.y),
                    Mathf.Abs(other.transform.lossyScale.z)
                    );

                float objectTopY = other.transform.position.y + objectMaxWidth * 0.5f;
                float objectSubmergedAmount = 0f;

                if (objectMaxWidth != 0) {
                    float above = (objectTopY - waterSurfaceY) / objectMaxWidth;
                    objectSubmergedAmount = Mathf.Clamp01(1f - above);
                }

                rigidbody.AddForce(transform.up * Mathf.Abs(Physics.gravity.y) * buoyancy * objectSubmergedAmount, ForceMode.Acceleration);
                rigidbody.linearDamping = dragMax * objectSubmergedAmount;
                rigidbody.angularDamping = angularDragBase + ((angularDragMax - angularDragBase) * objectSubmergedAmount);
            }
        }

        private void OnTriggerExit(Collider other) {
            Rigidbody rigidbody = other.GetComponent<Rigidbody>();
            if (rigidbody != null && !rigidbody.isKinematic) {
                // Reset drag and angular drag
                rigidbody.linearDamping = 0f;
                rigidbody.angularDamping = 0.05f;
            }
        }
    }
}
