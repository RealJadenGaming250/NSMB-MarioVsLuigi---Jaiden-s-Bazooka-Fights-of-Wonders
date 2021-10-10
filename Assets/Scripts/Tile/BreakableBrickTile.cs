using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.Tilemaps;

[CreateAssetMenu]
public class BreakableBrickTile : InteractableTile {
    public Color particleColor;
    public override bool Interact(MonoBehaviour interacter, InteractionDirection direction, Vector2 location) {
        Transform tmtf = GameManager.Instance.tilemap.transform;
        int tileX = Mathf.FloorToInt((location.x-tmtf.transform.position.x)/tmtf.localScale.x);
        int tileY = Mathf.FloorToInt((location.y-tmtf.transform.position.y)/tmtf.localScale.y);

        if (direction != InteractionDirection.Down) {
            //check for entities above to bump

            foreach (Collider2D collider in Physics2D.OverlapBoxAll(new Vector2(location.x,location.y+0.5f), new Vector2(0.5f,0.05f), 0f)) {
                GameObject obj = collider.gameObject;
                if (obj == interacter.gameObject) continue;
                switch (obj.tag) {
                case "Player": {
                    PlayerController player = obj.GetComponent<PlayerController>();
                    player.photonView.RPC("Knockback", RpcTarget.All, obj.transform.position.x > location.x, 1);
                    break;
                }
                case "koopa":
                case "goomba": {
                    if (!obj.GetPhotonView())
                        break;
                    if (obj.GetComponent<KillableEntity>().dead)
                        break;
                    obj.GetPhotonView().RPC("SpecialKill", RpcTarget.All, obj.transform.position.x < location.x, false);
                    break;
                }
                case "coin": {
                    if (interacter && interacter is PlayerController) {
                        ((PlayerController) interacter).photonView.RPC("CollectCoin", RpcTarget.All, obj.GetComponentInParent<PhotonView>().ViewID, obj.transform.position.x, obj.transform.position.y);
                    }
                    break;
                }
                case "MainStar":
                case "bigstar": break;
                default: {
                    if (obj.layer != LayerMask.NameToLayer("Entity")) break;
                    Rigidbody2D body = obj.GetComponentInParent<Rigidbody2D>();
                    if (!body) {
                        body = obj.GetComponent<Rigidbody2D>();
                        if (!body) {
                            break;
                        }
                    }
                    body.velocity = new Vector2(body.velocity.x, 5f);
                    break;
                }
                }
            }
        }
        
        if (interacter is PlayerController) {
            PlayerController player = (PlayerController) interacter;

            if (player.state == PlayerController.PlayerState.Small 
                || player.state == PlayerController.PlayerState.Mini) {
                //Bump

                GameManager.Instance.photonView.RPC("BumpBlock", RpcTarget.All, tileX, tileY, "SpecialTiles/" + this.name, (int) BlockBump.SpawnResult.Nothing, direction == InteractionDirection.Down);
                return false;
            }
        }

        //Break
        GameManager.Instance.photonView.RPC("ModifyTilemap", RpcTarget.All, tileX, tileY, null);
        GameManager.Instance.photonView.RPC("SpawnBreakParticle", RpcTarget.All, tileX, tileY, particleColor.r, particleColor.g, particleColor.b);
        if (interacter is MonoBehaviourPun) {
            ((MonoBehaviourPun) interacter).photonView.RPC("PlaySound", RpcTarget.All, "player/brick_break");
        }
        return true;
    }
}