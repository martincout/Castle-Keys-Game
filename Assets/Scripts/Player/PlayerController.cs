using SimplePlatformer.Assets.Scripts.Player;
using SimplePlatformer.Assets.Scripts.Player.Input;
using SimplePlatformer.Assets.Scripts.Player.States;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;


namespace SimplePlatformer.Player
{
    /// <summary>
    /// Handles all the player general behaviour.
    /// - Input
    /// - Health, Damage, Knockback
    /// </summary>
    public partial class PlayerController : MonoBehaviour, IDamageable, IItem, IPlayer
    {
        public float MaxHealth = 200;

        public HealthBar healthBar;

        //Aux
        protected float invincibleTime = 1f;

        //Take damage
        private float stunTime = 0.3f;
        internal HealthSystem healthSystem;

        protected CharacterParticles characterParticles;
        protected Animator anim;
        protected Rigidbody2D rb;
        protected Renderer render;
        protected InputHandler inputHandler;
        protected InputState CurrentInput;
        protected PlayerState CurrentState;
        [SerializeField] private PlayerData PlayerSO;

        public bool movePrevent; // Inpunt Hanlder
        public bool cannotAttack; // Input Handler
        public bool isFacingRight = true;
        public bool isAttacking;
        public bool isStunned;
        //public bool itsDying;
        public bool invincible;
        public bool airAttacked;
        public bool isGrounded;
        public bool canInteract = true;
        public bool isBowAttacking;

        public float thrust = 10f;
        private string currentControlScheme;

        private void Awake()
        {
            CurrentState = PlayerState.IDLE;
            comboState = ComboState.NONE;
            inputHandler = GetComponent<InputHandler>();
            CurrentInput = new();
            //General
            healthSystem = gameObject.AddComponent<HealthSystem>();
            characterParticles = GetComponent<CharacterParticles>();
            anim = GetComponent<Animator>();
            rb = GetComponent<Rigidbody2D>();
            render = transform.GetChild(0).GetComponent<Renderer>();
            //Combat
            hitBoxPos = transform.GetChild(1).transform;
            //Movement
            sprRender = GetComponent<SpriteRenderer>();
            groundLayer = 1 << LayerMask.NameToLayer("Ground");
            boxCollider = GetComponent<BoxCollider2D>();
            footsteps = GetComponent<AudioSource>();
            dustFootsteps = transform.GetChild(2).GetComponent<ParticleSystem>();
            
        }
        private void Start()
        {
            if (healthBar == null) healthBar = GameObject.Find("HealthBar").GetComponent<HealthBar>();
            healthSystem.SetHealthBar(healthBar);
            healthSystem.SetMaxHealth(MaxHealth);
        }

        private void Update()
        {
            // Let the death animation play until Respawn
            if (CurrentState == PlayerState.DEAD) return;

            CurrentInput = inputHandler.GetInputState();

            movementDirection = CurrentInput.MovementDirection;

            // Movement and Idle States
            if (rb.velocity.magnitude > 0)
                CurrentState = PlayerState.MOVEMENT;
            else
                CurrentState = PlayerState.IDLE;

            //if (Time.timeScale == 0) return;

            if (!isStunned && !movePrevent)
            {
                if (!isAttacking)
                {
                    AnimationUpdate();
                }
            }

            //Suspend the player in air when air attacking
            SuspendInAir();

            //Used to exit the animation state when we are doing combos
            anim.SetFloat("timeCombo", elapsedNextCombo);

            //Combos
            if (elapsedNextCombo > 0)
            {
                elapsedNextCombo -= Time.deltaTime;
            }
            //Finished Attacking
            else if (!comboState.Equals(ComboState.NONE))
            {
                isAttacking = false;
                airAttacked = true;
                isBowAttacking = false;
                comboState = ComboState.NONE;
                elapsedAttackRate = PlayerSO.AttackRate;
                rb.drag = PlayerSO.InitialDrag;
            }

            //Attack rate
            if (elapsedAttackRate > 0)
            {
                elapsedAttackRate -= Time.deltaTime;
            }

            InteractableUpdate();
        }

        internal IEnumerator InvinbibleCo()
        {
            yield return new WaitForSeconds(invincibleTime);
            SetAlpha(1f);
            invincible = false;
        }

        internal void SetInvincible()
        {
            invincible = true;
            SetAlpha(0.7f);
            StartCoroutine(InvinbibleCo());
        }

        internal void SetAlpha(float alpha)
        {
            Color c = render.material.color;
            c.a = alpha;
            render.material.color = c;
        }

        public void TakeDamage(float damage, Vector3 attackerPos)
        {
            if (!invincible)
            {
                //Decrease Health
                healthSystem.DealDamage(damage);
                characterParticles.PlayParticle(Assets.Scripts.Player.Type.HURT);
                SetInvincible();
                //Check
                if (healthSystem.GetHealth() > 0)
                {
                    SoundManager.instance.Play("Damage");
                    anim.Play(PlayerAnimations.PLAYER_HURT);
                    Knockback(attackerPos, thrust);
                }
                else
                {
                    SoundManager.instance.Play("Death");
                    StopAllCoroutines();
                    Knockback(attackerPos, thrust);
                    Die();
                }
            }
        }

        public void DieInstantly()
        {
            SoundManager.instance.Play("Death");
            characterParticles.PlayParticle(Assets.Scripts.Player.Type.HURT);
            healthSystem.SetHealth(0);
            StopAllCoroutines();
            Die();
        }

        private void Die()
        {
            // Set to a Dying State and Prevent Input
            // Fade player
            // INFO: Only these two things it's enough then delegate to a LevelManager to respawn
            GameManager.GetInstance().TogglePlayerDeath(true);
            CurrentState = PlayerState.DEAD;
            rb.velocity = Vector2.zero;
            anim.Play("playerDie");
            GameEvents.OnPlayerDeath?.Invoke();
        }

        internal void Knockback(Vector3 attackerPos, float thrust)
        {
            if (!isStunned)
            {
                StartCoroutine(StunCo());
                StartCoroutine(KnockCo(attackerPos, thrust));
            }
        }

        private IEnumerator StunCo()
        {
            isStunned = true;
            yield return new WaitForSeconds(stunTime);
            isStunned = false;
        }

        private IEnumerator KnockCo(Vector3 attackerPos, float _thrust)
        {
            float force = _thrust;
            Vector2 forceDirection = transform.TransformDirection(transform.position - attackerPos);
            if (forceDirection.x > 0)
            {
                forceDirection = new Vector2(force, forceDirection.normalized.y);
            }
            else
            {
                forceDirection = new Vector2(-force, forceDirection.normalized.y);

            }
            rb.velocity = new Vector2();
            rb.AddForce(forceDirection, ForceMode2D.Impulse);
            yield return new WaitForSeconds(stunTime);
            rb.velocity = new Vector2();
        }

        /// <summary>
        /// Item interaction, only with health items
        /// </summary>
        /// <param name="item"></param>
        public void ItemInteraction(Item item)
        {
            if (item.category.Equals(Item.Category.CONSUMABLE))
            {
                healthSystem.Heal(item.value);
            }
        }

        public void Respawn()
        {
            // Restore health
            this.healthSystem.SetHealth(PlayerSO.MaxHealth);
            this.invincible = false;
            CurrentState = PlayerState.IDLE;
            SetAlpha(1);
            // Respawn to position
            this.transform.position = LevelManager.instance.currentRespawnPoint.position;
        }

        private void OnEnable()
        {
            inputHandler.OnAttack += Attack;
            inputHandler.OnJumpStarted += StartJumping;
            inputHandler.OnJumpPerformed += CancelJumping;
            inputHandler.OnInteract += Interact;
            GameEvents.RespawnHandler += Respawn;
        }

        private void OnDisable()
        {
            inputHandler.OnAttack -= Attack;
            inputHandler.OnJumpStarted -= StartJumping;
            inputHandler.OnJumpPerformed -= CancelJumping;
            inputHandler.OnInteract -= Interact;
            GameEvents.RespawnHandler -= Respawn;
        }

        public void OnDrawGizmosSelected()
        {
            Gizmos.DrawWireCube(transform.position, size);
            Gizmos.color = Color.white;

            if (hitBoxPos == null)
                return;
            Gizmos.color = Color.red;

            Gizmos.matrix = Matrix4x4.TRS(hitBoxPos.position, hitBoxPos.rotation, hitBoxPos.localScale);

            Gizmos.DrawCube(Vector3.zero, boxSize); // Because size is halfExtents
        }

    }
}

