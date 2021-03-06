﻿using UnityEngine;
using System.Collections;

public class Snowman : Enemy {

    internal override void Awake()
    {
        actualSize = new Vector2(3f, 3f);

        hairStyle = Random.Range(0, 3) + 1;
        headStyle = Random.Range(0, 3) + 1;

        legsAnim = transform.FindChild("Body/Legs").GetComponent<tk2dSpriteAnimator>();
        //hairAnim = transform.FindChild("Hair").GetComponent<tk2dSpriteAnimator>();

        CooldownModifier = 2f;

        BaseHealth = 15f;
        Health = BaseHealth;
        CanUseWeapons = false;

        base.Awake();

        Target = arena.FindChild("Center").position + (Random.insideUnitSphere * 6f);
        Target.y = 0f;

        if(Random.Range(0,2)==0)
            CurrentWeapon = new Weapon(WeaponType.SnowmanMelee);
        else
            CurrentWeapon = new Weapon(WeaponType.Carrot);

    }


    internal override void ToggleWalk(bool walk)
    {
        if (walk && !armsAnim.IsPlaying("Arms_Attack") && !armsAnim.IsPlaying("Arms_Throw"))
        {
            legsAnim.Play("Legs_Walk");
            torsoAnim.Play("Torso_Walk");
            headAnim.Play("Head_Walk");
            armsAnim.Play("Arms_Walk");
           // hairAnim.Play("Hair_" + hairStyle);

            //if (!armsAnim.IsPlaying("Arms_Attack"))
            
            //if (!clothesAnim.IsPlaying("Clothes_Attack"))
            //clothesAnim.Play("Clothes_Walk");

          /*  if (CurrentWeapon != null && !transform.FindChild("Weapon_Swipe").GetComponent<Animation>().isPlaying)
            {


                transform.FindChild("Weapon_Swipe").GetComponent<Animation>().Play("Weapon_Walk");
            }
           * 
           */
        }
        else
        {
            if (!armsAnim.IsPlaying("Arms_Attack") && !armsAnim.IsPlaying("Arms_Throw"))
            {
                legsAnim.Play("Legs_Idle");
                torsoAnim.Play("Torso_Idle");
                headAnim.Play("Head_Idle");
                armsAnim.Play("Arms_Idle");
            }
            // hairAnim.Play("Hair_" + hairStyle);

            //if (!armsAnim.IsPlaying("Arms_Attack"))
            
            //if (!clothesAnim.IsPlaying("Clothes_Attack"))
           // clothesAnim.Play("Clothes_Idle");

            //transform.FindChild("Weapon_Swipe").GetComponent<Animation>().Stop("Weapon_Walk");
        }
    }

    internal override void DoAI()
    {
        attackCooldown -= Time.deltaTime;

        if (attackCooldown <= 0f)
        {
            switch (CurrentWeapon.Class)
            {
                case WeaponClass.Throw:
                    Vector3 forward = new Vector3(faceDir, 0f, 0f).normalized;
                    if (Vector3.Angle(p1.transform.position - transform.position, forward) < 10f)
                    {
                        AttackAnim("Throw");
                        attackCooldown = CurrentWeapon.Cooldown*CooldownModifier;
                        StartCoroutine("DoAttack");
                    }
                    if (Vector3.Angle(p2.transform.position - transform.position, forward) < 10f)
                    {
                        AttackAnim("Throw");
                        attackCooldown = CurrentWeapon.Cooldown * CooldownModifier;
                        StartCoroutine("DoAttack");
                    }
                    break;
                case WeaponClass.Melee:

                    if (Vector3.Distance(transform.position, p1.transform.position) < 2f)
                    {
                        //transform.FindChild("Weapon_Swipe").GetComponent<Animation>().Play("Weapon_Swipe");
                        AttackAnim("Attack");
                        attackCooldown = CurrentWeapon.Cooldown * CooldownModifier;
                        StartCoroutine("DoAttack");
                    }
                    if (Vector3.Distance(transform.position, p2.transform.position) < 2f)
                    {
                        //transform.FindChild("Weapon_Swipe").GetComponent<Animation>().Play("Weapon_Swipe");
                        AttackAnim("Attack");
                        attackCooldown = CurrentWeapon.Cooldown * CooldownModifier;
                        StartCoroutine("DoAttack");
                    }
                    break;
            }
        }



        //break;
        //                case WeaponClass.Throw:
        //                    Vector3 forward = new Vector3(faceDir, 0f, 0f).normalized;
        //                    if (Vector3.Angle(p1.transform.position - transform.position, forward) < 10f)
        //                    {
        //                        transform.FindChild("Weapon_Throw").GetComponent<Animation>().Play("Weapon_Throw");
        //                        AttackAnim("Attack");
        //                        attackCooldown = CurrentWeapon.Cooldown * CooldownModifier;
        //                        StartCoroutine("DoAttack");
        //                    }

        //                    break;
        //            }


        //        }
        //    }
        //}

        // Movement
        //reset target to avoid stuckness
        if (Vector3.Distance(Target, arena.FindChild("Center").position) > 6f && Random.Range(0, 1000) == 0)
            Target = arena.FindChild("Center").position + (Random.insideUnitSphere * 6f);

        if (Vector3.Distance(transform.position, Target) < 0.01f)
        {
            if (Random.Range(0, 100) == 0)
            {
                Target = arena.FindChild("Center").position + (Random.insideUnitSphere * 6f);
                Target.y = 0f;
            }
            else if (Random.Range(0, 200) == 0)
            {
                int p = Random.Range(0, 2);
                if (p == 0 && p1.gameObject.activeSelf)
                {
                    Target = p1.transform.position + (Random.insideUnitSphere * 1.5f);
                    Target.y = 0f;
                }
                else if (p == 1 && p2.gameObject.activeSelf)
                {
                    Target = p2.transform.position + (Random.insideUnitSphere * 1.5f);
                    Target.y = 0f;
                }

            }
        }

        // Swap weapons occasionally
        if (Random.Range(0, 500) == 0)
        {
            if(CurrentWeapon.Type == WeaponType.SnowmanMelee)
                CurrentWeapon = new Weapon(WeaponType.Carrot);
            else
                CurrentWeapon = new Weapon(WeaponType.SnowmanMelee);

        }
    }

    internal override void IdleAnim()
    {
        legsAnim.Play("Legs_Idle");
        torsoAnim.Play("Torso_Idle");
        headAnim.Play("Head_Idle");
        armsAnim.Play("Arms_Idle");
    }

    internal override void AttackAnim(string anim)
    {
        armsAnim.PlayFromFrame("Arms_" + anim, 0);
        legsAnim.PlayFromFrame("Legs_" + anim, 0);
        headAnim.PlayFromFrame("Head_" + anim, 0);
        torsoAnim.PlayFromFrame("Torso_" + anim, 0);
    }


    /**
     * Override these with creature specific sounds 
     * 
     */
   internal override void playPain()
    {
        if (Sounds.ContainsKey("Snow_man_roar"))
            Sounds["Snow_man_roar"].Play();

    }

    internal override void playThrowingWeapon()
    {
        if (Sounds.ContainsKey("Footsteps_Heavy_Snow"))
            Sounds["Footsteps_Heavy_Snow"].Play();
    }

     internal override void playMeleeWeapon()
    {
        if (Sounds.ContainsKey("Club"))
            Sounds["Club"].Play();
    }

     internal  override void startWalkingAudio()
    {
        if (Sounds.ContainsKey("Footsteps_Heavy_Snow"))
            playWalkAudio(Sounds["Footsteps_Heavy_Snow"]);
    }

     internal override void stopWalkingAudio()
    {
        if (Sounds.ContainsKey("Footsteps_Heavy_Snow"))
            stopWalkAudio(Sounds["Footsteps_Heavy_Snow"]);
    }
}
