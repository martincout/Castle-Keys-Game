using SimplePlatformer.Assets.Scripts.Player;
using System.Collections;
using UnityEngine;

namespace SimplePlatformer.Player
{
    public partial class PlayerController : MonoBehaviour
    {
        //Jump
        [Tooltip("Hang time counter, decreasing value"), SerializeField] private float hangTimeCounter = 0f;
        public LayerMask groundLayer;
        /// <summary>
        /// Raycast to check if it's grounded
        /// </summary>
        private RaycastHit2D raycastLeft;
        private RaycastHit2D raycastRight;
        /// <summary>
        /// Offset from the center-bottom of the collider 2d
        /// </summary>
        public Vector3 raycastLeftOffset = new Vector2(0.5f, 0);
        public Vector3 raycastRightOffset = new Vector2(0.5f, 0);

        //Movement
        private Vector2 movementDirection;
        [Tooltip("Current speed of the object.")]
        [SerializeField] private float speed = 0f;

        //Hurt Collider
        BoxCollider2D boxCollider;

        //Particles
        public ParticleSystem dustFootsteps;
        //States
        private bool isJumpingAnim = false;
        private bool isFallingAnim = false;
        private bool isAttackingAnim = false;
        private bool jumpingHeld;
        private bool isJumping;

        //Components
        private SpriteRenderer sprRender;
        private AudioSource footsteps;

        public void FixedUpdate()
        {
            CheckGround();
            if (isGrounded)
            {
                hangTimeCounter = PlayerSO.HangTime;
            }
            else
            {
                hangTimeCounter -= Time.deltaTime;
            }

            Move();
            UpdateJump();
            BetterJump();
        }

        private void AnimationUpdate()
        {
            AnimatorStateInfo info = anim.GetCurrentAnimatorStateInfo(0);

            isJumpingAnim = info.IsName(PlayerAnimations.PLAYER_JUMP);
            isFallingAnim = info.IsName(PlayerAnimations.PLAYER_FALLING);
            isAttackingAnim = info.IsName(PlayerAnimations.PLAYER_ATTACK1);
            Flip();
            if (!isJumpingAnim && isGrounded && !isAttackingAnim)
            {
                if (movementDirection.x != 0)
                {
                    anim.Play(PlayerAnimations.PLAYER_WALK);
                    dustFootsteps.Play();
                }
                else
                {
                    anim.Play(PlayerAnimations.PLAYER_IDLE);
                }
            }
            if (isJumping && !isJumpingAnim && !isFallingAnim)
            {
                anim.Play(PlayerAnimations.PLAYER_JUMP);
            }
            if (rb.velocity.y < -0.2 && !isGrounded)
            {
                anim.Play(PlayerAnimations.PLAYER_FALLING);
            }
        }

        private void Flip()
        {
            if (movementDirection.x != 0 && !isAttacking)
            {
                if (movementDirection.x > 0)
                {
                    transform.localScale = new Vector3(1, 1, 1);
                    isFacingRight = true;
                }
                else
                {
                    transform.localScale = new Vector3(-1, 1, 1);
                    isFacingRight = false;
                }
            }
        }

        private void CheckGround()
        {
            //Raycast position calculation. (from the center-bottom of the collider 2d, with an offset)
            Vector3 raycastPositionLeft = boxCollider.bounds.center - new Vector3(0, boxCollider.bounds.extents.y, 0) - raycastLeftOffset;
            Vector3 raycastPositionRight = boxCollider.bounds.center - new Vector3(0, boxCollider.bounds.extents.y, 0) + raycastRightOffset;
            //Raycast2d
            raycastLeft = Physics2D.Raycast(raycastPositionLeft, Vector2.down, PlayerSO.GroundedHeight, groundLayer);
            raycastRight = Physics2D.Raycast(raycastPositionRight, Vector2.down, PlayerSO.GroundedHeight, groundLayer);

            Color color;
            //Check if grounded
            if (raycastLeft.collider != null || raycastRight.collider != null)
            {
                isGrounded = true;
                airAttacked = false;
                color = Color.green;
                Debug.DrawRay(raycastPositionLeft, Vector2.down * PlayerSO.GroundedHeight, color);
                Debug.DrawRay(raycastPositionRight, Vector2.down * PlayerSO.GroundedHeight, color);
            }
            else
            {
                isGrounded = false;
                color = Color.red;
                Debug.DrawRay(raycastPositionLeft, Vector2.down * PlayerSO.GroundedHeight, color);
                Debug.DrawRay(raycastPositionRight, Vector2.down * PlayerSO.GroundedHeight, color);
            }

        }

        //JUMP METHODS --------------

        private void BetterJump()
        {
            if (rb.velocity.y < 0)
            {
                rb.velocity += Vector2.up * Physics2D.gravity.y * (PlayerSO.FallMultiplier - 1) * Time.deltaTime;
            }
            else if (rb.velocity.y > 0 && !jumpingHeld)
            {
                rb.velocity += Vector2.up * Physics2D.gravity.y * (PlayerSO.LowFallMultiplier - 1) * Time.deltaTime;
            }

        }

        public void StartJumping()
        {
            if (isGrounded) SoundManager.instance.Play("Jump");
            this.jumpingHeld = true;
        }

        public void CancelJumping()
        {
            jumpingHeld = false;
            isJumping = false;
        }

        public void UpdateJump()
        {
            if (hangTimeCounter > 0 && !jumpingHeld)
            {
                isJumping = false;
            }
            if (hangTimeCounter > 0 && jumpingHeld)
            {
                isJumping = true;
            }

            if (isJumping)
            {
                hangTimeCounter = 0f;
                rb.velocity = new Vector2(rb.velocity.x, PlayerSO.JumpForce);
            }
        }

        private void Move()
        {
            CalculateSpeed();
            rb.velocity = new Vector2(movementDirection.x * speed, rb.velocity.y);
        }

        private void footstep()
        {
            footsteps.Play();
        }

        /// <summary>
        /// Gradually increments and decreases the speed
        /// </summary>
        private void CalculateSpeed()
        {
            if ((speed < PlayerSO.MaxSpeed) && movementDirection.x != 0)
            {
                speed += PlayerSO.Acceleration * Time.deltaTime;
            }
            else
            {
                if (speed > PlayerSO.Decelaration * Time.deltaTime)
                {
                    speed -= PlayerSO.Decelaration * Time.deltaTime;
                }
                else
                {
                    speed = 0;
                }
            }
        }
    }//end class
}//end namespace

