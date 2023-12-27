using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Photon.Pun;
using System.Threading;
using Photon.Pun.UtilityScripts;
using System.Numerics;

public class PlayerMovement : MonoBehaviour, IPunObservable
{

    PhotonView view;

    public Rigidbody2D rb;
    public Transform groundCheck;
    public LayerMask groundLayer;
    public Animator animator;
    private float horizontal;
    private float speed = 8f;
    public float jumpingPower = 10f;
    private bool isFacingRight = true;

    private UnityEngine.Vector3 smoothMove;

    bool isDead = false;

    public int maxHealth = 100;
	public int currentHealth;
    private int extraJumps;
    public int extraJumpValue = 2;

    public int attackNum = 1;

    public HealthBar healthBar;
    
    public Joystick joystick;

    public SpriteRenderer spriteRenderer;


    void Start()
    {
		currentHealth = maxHealth;
		healthBar.SetMaxHealth(maxHealth);
        extraJumps = extraJumpValue;
        joystick = GameObject.Find("Floating Joystick").GetComponent<Joystick>();
        view = GetComponent<PhotonView>();
    }
    
    void Update()
    {
        if(view.IsMine)
        {
            if (!isDead/*&& !isFacingRight*/ && horizontal > 0f)
            {
                //Flip();
                spriteRenderer.flipX = false;
                view.RPC("OnDirectionChange_RIGHT", RpcTarget.Others);
            }
            else if (!isDead/*&& isFacingRight*/ && horizontal < 0f)
            {
                //Flip();
                spriteRenderer.flipX = true;
                view.RPC("OnDirectionChange_LEFT", RpcTarget.Others);
            }

            if (!isDead && !IsGrounded())
            {
                animator.SetBool("isJumping", true);
            } else
            {
                animator.SetBool("isJumping", false);
            }

            if(currentHealth <= 0)
            {
                animator.SetBool("isDead", true);
                isDead = true;
                horizontal = 0;
                rb.velocity = new UnityEngine.Vector2(horizontal, rb.velocity.y);
            }

            if(!isDead)
            {
                if(joystick.Horizontal >= .2f)
                {
                    horizontal = speed;
                }else if(joystick.Horizontal <= -.2f)
                {
                    horizontal = -speed;
                }else
                {
                    horizontal = 0f;
                }
                rb.velocity = new UnityEngine.Vector2(horizontal, rb.velocity.y);

                animator.SetFloat("speed", Math.Abs(horizontal));
            }
        }else
        {
            SmoothSyncMovement();
        }     
    }

    private void SmoothSyncMovement()
    {
        transform.position = UnityEngine.Vector3.Lerp(transform.position, smoothMove, Time.deltaTime * 10);
    }

    [PunRPC]
    void OnDirectionChange_LEFT()
    {
        spriteRenderer.flipX = true;
    }
    [PunRPC]
    void OnDirectionChange_RIGHT()
    {
        spriteRenderer.flipX = false;
    }
    

/*
    private void FixedUpdate()
    {
        if(!isDead)
        {
            if(joystick.Horizontal >= .2f)
            {
                horizontal = speed;
            }else if(joystick.Horizontal <= -.2f)
            {
                horizontal = -speed;
            }else
            {
                horizontal = 0f;
            }
            rb.velocity = new Vector2(horizontal, rb.velocity.y);

            animator.SetFloat("speed", Math.Abs(horizontal));
        }
    }
*/

    private void OnTriggerEnter2D(Collider2D collider)
    {
        TakeDamage(10);
    }

    public void Jump(InputAction.CallbackContext context)
    {
        if(view.IsMine)
        {
            if(!isDead && IsGrounded())
            {
                extraJumps = extraJumpValue;
            }

            if(!isDead && context.performed && extraJumps > 0)
            {
                rb.velocity = UnityEngine.Vector2.up * jumpingPower;
                extraJumps--;
            }else if(!isDead && context.performed && extraJumps == 0 && IsGrounded())
            {
                rb.velocity = UnityEngine.Vector2.up * jumpingPower;
            }
        }

        /*
        if(!isDead && IsGrounded())
        {
            extraJumps = extraJumpValue;
        }

        if(!isDead && context.performed && extraJumps > 0)
        {
            rb.velocity = Vector2.up * jumpingPower;
            extraJumps--;
        }else if(!isDead && context.performed && extraJumps == 0 && IsGrounded())
        {
            rb.velocity = Vector2.up * jumpingPower;
        }
        */

    }

    public void Attack(InputAction.CallbackContext context)
    {
        if(!isDead && context.performed && view.IsMine)
        {
            if(attackNum == 1)
            {
                animator.SetTrigger("Attack1");
                attackNum = 2;
            }else if(attackNum == 2)
            {
                animator.SetTrigger("Attack2");
                attackNum = 1;
            }
        }
    }
    private bool IsGrounded()
    {
        return Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);
        
    }

    private void Flip()
    {
        if (!isDead && view.IsMine)
        {
            isFacingRight = !isFacingRight;
            UnityEngine.Vector3 localScale = transform.localScale;
            localScale.x *= -1f;
            transform.localScale = localScale;
        }
    }

    public void Move(InputAction.CallbackContext context)
    {
        if (!isDead && view.IsMine)
        {
            horizontal = context.ReadValue<UnityEngine.Vector2>().x;
        }
    }

    void TakeDamage(int damage)
	{
        if(currentHealth <= maxHealth && currentHealth > 0)
        {
            currentHealth -= damage;

		    healthBar.SetHealth(currentHealth);
        }
	}

    void AddHealth(int amount)
    {
        if(currentHealth < maxHealth && currentHealth >= 0)
        {
            currentHealth += amount;

            healthBar.SetHealth(currentHealth);
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if(stream.IsWriting)
        {
            stream.SendNext(transform.position);
        }else if(stream.IsReading)
        {
            smoothMove = (UnityEngine.Vector3)stream.ReceiveNext();
        }
    }
}