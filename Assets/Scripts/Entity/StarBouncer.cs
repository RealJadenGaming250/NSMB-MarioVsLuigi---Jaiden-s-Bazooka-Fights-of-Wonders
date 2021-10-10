﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class StarBouncer : MonoBehaviourPun {

    public bool stationary = true;
    [SerializeField] float pulseAmount = 0.2f, pulseSpeed = 0.2f, moveSpeed = 3f, rotationSpeed = 30f, bounceAmount = 4f, deathBoostAmount = 20f, blinkingSpeed = 0.5f, lifespan = 15f;
    float counter;
    Vector3 startingScale;
    private Rigidbody2D body;
    new private BoxCollider2D collider;
    public bool passthrough = true;
    private PhysicsEntity physics;

    void Start() {
        startingScale = transform.localScale;
        body = GetComponent<Rigidbody2D>();
        collider = GetComponent<BoxCollider2D>();
        physics = GetComponent<PhysicsEntity>();
        
        object[] data = photonView.InstantiationData;
        if (data != null && data.Length >= 1) {
            stationary = false;
            passthrough = true;
            body.velocity = new Vector2(moveSpeed * ((bool) data[0] ? -1 : 1), deathBoostAmount);
        }
    }

    void FixedUpdate() {
        if (GameManager.Instance && GameManager.Instance.gameover) {
            body.velocity = Vector2.zero;
            body.isKinematic = true;
            return;
        }

        if (stationary) {
            counter += Time.fixedDeltaTime;
            float sin = (Mathf.Sin(counter * pulseSpeed)) * pulseAmount;
            transform.localScale = startingScale + new Vector3(sin, sin, 0);
            return;
        }

        HandleCollision();

        lifespan -= Time.fixedDeltaTime;

        if (lifespan < 5f) {
            if ((lifespan * 2f) % (blinkingSpeed*2) < blinkingSpeed) {
                GetComponentInChildren<SpriteRenderer>().color = new Color(0,0,0,0);
            } else {
                GetComponentInChildren<SpriteRenderer>().color = Color.white;
            }
        }
        
        bool left = body.velocity.x < 0;
        Transform t = transform.Find("Graphic");
        t.Rotate(new Vector3(0,0,rotationSpeed * (left ? 1 : -1)), Space.Self);

        if (passthrough) {
            gameObject.layer = LayerMask.NameToLayer("HitsNothing");
            if (body.velocity.y <= 0 && !Physics2D.OverlapBox(transform.position, Vector2.one / 3, 0, LayerMask.GetMask("Ground"))) {
                passthrough = false;
                gameObject.layer = LayerMask.NameToLayer("Entity");
            }
        }

        if (!photonView.IsMine || stationary) {
            body.isKinematic = true;
            return;
        } else {
            body.isKinematic = false;
            transform.localScale = startingScale;
        }

        if (lifespan < 0) {
            PhotonNetwork.Destroy(photonView);
        }
    }

    void HandleCollision() {
        physics.Update();

        if (physics.hitLeft) {
            body.velocity = new Vector2(moveSpeed, body.velocity.y);
        }
        if (physics.hitRight) {
            body.velocity = new Vector2(-moveSpeed, body.velocity.y);
        }
        if (physics.onGround && physics.hitRoof) {
            photonView.RPC("Crushed", RpcTarget.All);
            return;
        }
        if (physics.onGround) {
            body.velocity = new Vector2(body.velocity.x, bounceAmount);
        }
    }

    [PunRPC]
    public void Crushed() {
        if (photonView.IsMine)
            PhotonNetwork.Destroy(gameObject);
        GameObject.Instantiate(Resources.Load("PuffParticle"), transform.position, Quaternion.identity);
    }
}