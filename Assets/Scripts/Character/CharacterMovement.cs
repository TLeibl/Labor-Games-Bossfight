using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//the class for character movement and animations
public class CharacterMovement : MonoBehaviour
{
    //general
    [SerializeField] private Transform cam = null; //Main camera controlled by Cinemachine camera
    private CharController charController = null; //CharController script
    private CharacterController controller = null; 
    private Animator animator = null;

    //sounds
    [SerializeField] private AudioClip[] footstepSounds;    // an array of footstep sounds that will be randomly selected from.
    [SerializeField] private AudioClip[] weaponSounds;      //an array of weapon slash sounds that will be randomly selected from.
    [SerializeField] private AudioClip rollSound;           // the sound played when character dodges (rolls).
    private AudioSource audioSource; //current audio source
    private float elapsedTime = 0; //time since last step sound
    private float soundDelay = 0.5f; //delay for sound to play

    //attack combo values
    [SerializeField] private float cooldownTime = 0.8f; //attack cooldown
    private float nextAttackStartTime = 0f; //time the next attack can start
    private static int noOfClicks = 0; //number of current clicks in combo
    private float lastClickedTime = 0; //last clicked time in combo
    private float maxComboDelay = 1; //max time to click to stay in combo

    [SerializeField] private float stunDuration = 3; //duration of stun


    // Start is called before the first frame update
    private void Start()
    {
        controller = GetComponent<CharacterController>();
        charController = GetComponent<CharController>();
        audioSource = GetComponent<AudioSource>();
        animator = GetComponent<Animator>(); //TODO Animator von Men� setzen lassen je nach Waffentyp
        animator.fireEvents = false; //no sounds directly from the animations (read-only)
    }

    // Update is called once per frame
    private void Update()
    {
        //--------------------------STAMINA-----------------------
        //set value in animator - check whether enough stamina to use a skill
        animator.SetFloat("currentStamina", charController.GetCurrentStamina());


        //--------------------------MOVEMENT-----------------------
        //direction calculation
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;

        //check if button is pressed
        if (direction.magnitude >= 0.01f)
        {
            //run
            if (Input.GetKey(KeyCode.LeftShift) && !animator.GetBool("Run"))
            {
                animator.SetBool("Run", true);
            }
            //walk
            else if (!animator.GetBool("Walk"))
            {
                animator.SetBool("Walk", true);
            }
        }
        else
        {
            animator.SetBool("Walk", false);
            animator.SetBool("Run", false);
        }

        //roll
        if (Input.GetKey(KeyCode.Space) && !animator.GetBool("Roll"))
        {
            animator.SetBool("Roll", true);
        }


        //--------------------------FIGHT-----------------------
        //attack (combo)
        //!is aborted if player is hit/stunned/dies
        if (Input.GetKey(KeyCode.Mouse0))
        {
            //if weapon is no lance - attack combo possible
            if (!(charController.GetCurrentWeapon().Contains("Lance")))
            {
                AttackCombo(); //start combo
            }
            else
            {
                Debug.Log("Lanze");

                animator.SetBool("Attack01R", true);
            }
            
        }
        //reset animation after time
        if(animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 0.7f && animator.GetCurrentAnimatorStateInfo(0).IsName("Attack01R"))
        {
            animator.SetBool("Attack01R", false);
            charController.SetRegStamina(true); //regenerate stamina again
        }
        if (animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 0.7f && animator.GetCurrentAnimatorStateInfo(0).IsName("Attack02R"))
        {
            animator.SetBool("Attack02R", false);
            charController.SetRegStamina(true); //regenerate stamina again
        }
        if (animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 0.7f && animator.GetCurrentAnimatorStateInfo(0).IsName("Attack03R"))
        {
            animator.SetBool("Attack03R", false);
            noOfClicks = 0;
        }
        if(Time.time - lastClickedTime > maxComboDelay) //clicked not fast enough to continue combo
        {
            noOfClicks = 0;
        }
        if(!(charController.GetCurrentWeapon().Contains("Lance")) && Time.time > nextAttackStartTime) //enough time went by to start new combo
        {
            if (Input.GetKey(KeyCode.Mouse0))
            {
                AttackCombo();
            }
        }


        //heavy attack
        if (Input.GetKey(KeyCode.Mouse1))
        {
            Debug.Log("Heavy");

            animator.SetBool("HeavyAttack", true);

            Debug.Log(animator.GetBool("HeavyAttack"));
        }


        //use potion (if enough potions left, pressing E and not currently using a potion)
        if (Input.GetKey(KeyCode.E) && charController.GetCurrentPotions() >= 1 && !animator.GetBool("UsePotion"))
        {
            animator.SetBool("UsePotion", true);
        }
    }


    //method called for attack combo (player clicking left mouse)
    //lastClickedTime = when player clicked last time
    //noOfClicks = counted to continue combo (if player clicks fast enough)
    public void AttackCombo()
    {
        lastClickedTime = Time.time;
        noOfClicks++;

        charController.SetRegStamina(false); //no stamina reg during the combo
        //start Attack01
        if(noOfClicks == 1)
        {
            animator.SetBool("Attack01R", true);
        }
        noOfClicks = Mathf.Clamp(noOfClicks, 0, 3);

        //start Attack02 if clicked fast enough
        if(noOfClicks >= 2 && animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 0.7f && animator.GetCurrentAnimatorStateInfo(0).IsName("Attack01R"))
        {
            animator.SetBool("Attack01R", false);
            animator.SetBool("Attack02R", true);
        }
        //start Attack03 if clicked fast enough
        if (noOfClicks >= 3 && animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 0.7f && animator.GetCurrentAnimatorStateInfo(0).IsName("Attack02R"))
        {
            animator.SetBool("Attack02R", false);
            animator.SetBool("Attack03R", true);
        }
    }


    //method that is called when the character got stunned
    public void GotStunned()
    {
        if (!animator.GetBool("GotStunned")) //if not already stunned atm
        {
            animator.SetBool("GotStunned", true);

            //TODO show stunned UI bar?

            Invoke("EndStun", stunDuration);
        }
    }

    private void EndStun()
    {
        animator.SetBool("GotStunned", false);
        animator.SetBool("alreadyStunned", false);
    }


    //method to trigger animation when hit - called in CharController
    public void GotHit()
    {
        animator.SetBool("GotHit", true);
    }


    //method called by GotHit() if player got more damage than or same damage as he has currentHealth
    public void Death()
    {
        if (!animator.GetBool("diedBefore"))
        {
            animator.SetBool("Death", true);
        }
    }


    //method used by UsePotionState to heal the player after using a potion
    public void UsePotion()
    {
        if (charController.GetCurrentPotions() >= 1)
        {
            charController.Heal();
           
        }
    }

    //--------------------------GETTER METHODS-----------------------
    public Transform GetCam()
    {
        return cam;
    }

    public CharacterController GetController()
    {
        return controller;
    }

    public Animator GetAnimator()
    {
        return animator;
    }
}
