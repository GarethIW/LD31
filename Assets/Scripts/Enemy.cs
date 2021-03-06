﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public enum EnemyType
{
    Elf,
    Snowman,
    IceQueen
}

public class Enemy : MonoBehaviour
{
    public EnemyType Type;

    public Transform HUD;
    public Transform Sprite;
    public Sprite WeaponSheet;

    public Vector3 Target;

	// "Physics"
    public float walkSpeed = 0.000001f;
    public float SpeedLimit = 1f;
    public float Speed = 10;

    // Items
    public Weapon CurrentWeapon = null;

    // States
    public bool Knockback = false;
    public bool Dead = false;
    public float OnFire;

    // Stats
    public float CooldownModifier;
    public float BaseHealth;
    public float Health;
    public bool CanUseWeapons;

    // Weapon Sprites
    public List<Sprite> Sprites = new List<Sprite>(); 

    public Dictionary<string, AudioSource> Sounds = new Dictionary<string, AudioSource>();

    internal float turntarget = 12f;
    internal Vector2 actualSize = new Vector2(4f,4f);
    internal float fstepTime = 0f;
    internal float attackCooldown = 0f;

    internal Transform arena;

    internal tk2dSpriteAnimator legsAnim;
    internal tk2dSpriteAnimator torsoAnim;
    internal tk2dSpriteAnimator armsAnim;
    internal tk2dSpriteAnimator headAnim;
    internal tk2dSpriteAnimator hairAnim;
    internal tk2dSpriteAnimator clothesAnim;

    internal int headStyle = 0;
    internal int hairStyle = 0;

    internal Player p1;
    internal Player p2;

    internal int faceDir = 1;

    internal virtual void Awake()
    {
        turntarget = actualSize.x;

        
        torsoAnim = transform.FindChild("Body/Torso").GetComponent<tk2dSpriteAnimator>();
        armsAnim = transform.FindChild("Body/Arms").GetComponent<tk2dSpriteAnimator>();
        headAnim = transform.FindChild("Body/Head").GetComponent<tk2dSpriteAnimator>();
        
        

        GameObject soundsObject = transform.FindChild("Audio").gameObject;
        foreach (AudioSource a in soundsObject.GetComponents<AudioSource>())
        {
            Sounds.Add(a.clip.name, a);
        }

        p1 = GameManager.Instance.P1.GetComponent<Player>();
        p2 = GameManager.Instance.P2.GetComponent<Player>();
        //p2 = GameObject.Find("Kid").GetComponent<Player>();

        arena = GameObject.Find("Arena").transform;

        if(CanUseWeapons)
            SetWeapon(WeaponType.Snowball);
    }

    internal virtual void Update()
    {
        if (Dead)
        {
            CurrentWeapon = null;
            transform.FindChild("Body/Weapon_Swipe").gameObject.SetActive(false);
            transform.FindChild("Body/Weapon_Throw").gameObject.SetActive(false);
            if (transform.FindChild("Body/Weapon_Use")!=null)
                transform.FindChild("Body/Weapon_Use").gameObject.SetActive(false);

            IdleAnim();

            Sprite.localScale = Vector3.Lerp(Sprite.transform.localScale, new Vector3(turntarget, actualSize.y, 1f), 1f);
            
            //Sprite.Rotate(0f,0f,(90f * faceDir) * Time.deltaTime);

            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0f,0f,90f * faceDir), Time.deltaTime * 5f);

            return;
        }

        Player closestPlayer = null;
        var dist1 = Vector3.Distance(p1.transform.position, transform.position);
        var dist2 = Vector3.Distance(p2.transform.position, transform.position);
        if (p1.gameObject.activeSelf && dist1 < dist2) closestPlayer = p1;
        if (p2.gameObject.activeSelf && dist2 < dist1) closestPlayer = p2;

        if (closestPlayer != null)
        {
            if (closestPlayer.transform.position.x > transform.position.x)
            {
                turntarget = actualSize.x;
                faceDir = 1;
            }
            if (closestPlayer.transform.position.x < transform.position.x)
            {
                turntarget = -actualSize.x;
                faceDir = -1;
            }
        }

        Speed = walkSpeed;

        //if (Vector3.Distance(Target, transform.position) > 0.05f)
        //{
        if (!Knockback && Vector3.Distance(Target, transform.position) < 0.2f)
            Target = transform.position;
        else if (!Knockback)
            rigidbody.velocity = transform.TransformDirection((Target - transform.position).normalized) * Speed;
       
        if (Knockback && rigidbody.velocity.magnitude < 0.1f)
        {
            Knockback = false;
        }
        //}

        

        if (rigidbody.velocity.magnitude > 0f)
        {
            fstepTime += Time.deltaTime;
            if (fstepTime > 0.1f)
            {
                //Sounds["fstep"].Play();
                fstepTime = 0f;
            }
        }

        //if (!anim.IsPlaying("MonsterLadderTransferOn") && !anim.IsPlaying("MonsterLadderTransferOff"))
        //{
        ToggleWalk(rigidbody.velocity.magnitude > 0.5f);
        //}
     
        Sprite.localScale = Vector3.Lerp(Sprite.transform.localScale, new Vector3(turntarget, actualSize.y, 1f), 0.25f);
        if (transform.FindChild("Body/Weapon_Use/FlamethrowerParticles")!=null)
            transform.FindChild("Body/Weapon_Use/FlamethrowerParticles").rotation = Quaternion.Euler(-12, 90 * faceDir, 0f);
        // ARTIFICIAL INTELLIGENCE YO
        // Attack
        DoAI();


        //if (Input.GetButtonDown("P1 Weapon") && CurrentWeapon!=null)
        //{
        //    switch (CurrentWeapon.Class)
        //    {
        //        case WeaponClass.Swipe:
        //            transform.FindChild("Body/Weapon_Swipe").GetComponent<Animation>().Play("Weapon_Swipe");
        //            break;
        //    }
        //}
        //if (Input.GetButtonDown("P1 Throw") && CurrentWeapon != null)
        //{
        //    switch (CurrentWeapon.Class)
        //    {
        //        case WeaponClass.Swipe:
        //            transform.FindChild("Body/Weapon_Swipe").gameObject.SetActive(false);
        //            break;
        //    }
        //    CurrentWeapon = null;

        //    Item i = ItemManager.Instance.SpawnWeapon(WeaponType.Stick);
        //    if (i != null)
        //    {
        //        i.transform.position = transform.position + new Vector3(turntarget<0f?-1f:1f,1f,0f);
        //        Vector3 throwVelocity = new Vector3(turntarget<0f?-7f:7f,3f,0f);
        //        i.rigidbody.velocity = throwVelocity;
        //    }

        //}

        if (CurrentWeapon!=null && CurrentWeapon.CanBreak && CurrentWeapon.Durability <= 0)
        {
            SetWeapon(WeaponType.Snowball);
        }


        if (OnFire > 0f)
        {
            OnFire -= Time.deltaTime;
            Health -= 0.02f;

            transform.FindChild("FireParticles").GetComponent<ParticleSystem>().Emit(5);

            if (Health <= 0)
            {
                updateBodycount();
            }

            if(Type == EnemyType.Elf)
                if (Random.Range(0, 100) == 0 && !Sounds["Burning_Elf"].isPlaying)
                {
                    Sounds["Burning_Elf"].pitch = Random.Range(0.8f, 1.2f);
                    Sounds["Burning_Elf"].Play();
                }

        }

        if (Health < 0f) Dead = true;

    }

    internal virtual void DoAI()
    {
        attackCooldown -= Time.deltaTime;
        Vector3 forward = Vector3.zero;

        if (CurrentWeapon != null && attackCooldown <= 0f)
        {
            if (Vector3.Distance(transform.position, p1.transform.position) < CurrentWeapon.Range && p1.gameObject.activeSelf)
            {
                if (CurrentWeapon != null)
                {
                    switch (CurrentWeapon.Class)
                    {
                        case WeaponClass.Melee:

                            transform.FindChild("Body/Weapon_Swipe").GetComponent<Animation>().Play("Weapon_Swipe");
                            AttackAnim("Attack");
                            attackCooldown = CurrentWeapon.Cooldown * CooldownModifier;
                            StartCoroutine("DoAttack");

                            break;
                        case WeaponClass.Throw:
                            forward = new Vector3(faceDir, 0f, 0f).normalized;
                            if (Vector3.Angle(p1.transform.position - transform.position, forward) < 10f)
                            {
                                transform.FindChild("Body/Weapon_Throw").GetComponent<Animation>().Play("Weapon_Throw");
                                AttackAnim("Attack");
                                attackCooldown = CurrentWeapon.Cooldown * CooldownModifier;
                                StartCoroutine("DoAttack");
                            }

                            break;

                        case WeaponClass.Use:
                            forward = new Vector3(faceDir, 0f, 0f).normalized;
                            if (Vector3.Angle(p1.transform.position - transform.position, forward) < 10f)
                            {
                                UseWeapon();
                            }

                            break;
                    }


                }
            }

            if (Vector3.Distance(transform.position, p2.transform.position) < CurrentWeapon.Range && p2.gameObject.activeSelf)
            {
                if (CurrentWeapon != null)
                {
                    switch (CurrentWeapon.Class)
                    {
                        case WeaponClass.Melee:

                            transform.FindChild("Body/Weapon_Swipe").GetComponent<Animation>().Play("Weapon_Swipe");
                            AttackAnim("Attack");
                            attackCooldown = CurrentWeapon.Cooldown * CooldownModifier;
                            StartCoroutine("DoAttack");

                            break;
                        case WeaponClass.Throw:
                            forward = new Vector3(faceDir, 0f, 0f).normalized;
                            if (Vector3.Angle(p2.transform.position - transform.position, forward) < 10f)
                            {
                                transform.FindChild("Body/Weapon_Throw").GetComponent<Animation>().Play("Weapon_Throw");
                                AttackAnim("Attack");
                                attackCooldown = CurrentWeapon.Cooldown * CooldownModifier;
                                StartCoroutine("DoAttack");
                            }

                            break;

                        case WeaponClass.Use:
                            forward = new Vector3(faceDir, 0f, 0f).normalized;
                            if (Vector3.Angle(p2.transform.position - transform.position, forward) < 10f)
                            {
                                UseWeapon();
                            }

                            break;
                    }


                }
            }
        }

        // Movement

        //reset target to avoid stuckness
        if (Vector3.Distance(Target, arena.FindChild("Center").position) > 6f && Random.Range(0, 1000) == 0)
        {
            Target = arena.FindChild("Center").position + (Random.insideUnitSphere*6f);
            Target.y = 0f;
        }

        if (Vector3.Distance(transform.position, Target) < 0.01f)
        {
            if (Random.Range(0, 100) == 0)
            {
                Target = arena.FindChild("Center").position + (Random.insideUnitSphere * 6f);
                Target.y = 0f;
            }
            else if (Random.Range(0, 250) == 0 && CanUseWeapons)
            {
                List<Item> stuff = ItemManager.Instance.Items.Where(it => it.Type == ItemType.Weapon && it.gameObject.activeSelf).ToList();
                if (stuff.Count > 0)
                {
                    Target = stuff[Random.Range(0, stuff.Count)].transform.position;
                    Target.y = 0f;
                }
            }
            else if (Random.Range(0, 200) == 0)
            {
                int p = Random.Range(0, 2);
                if (p == 0 && p1.gameObject.activeSelf)
                {
                    Target = p1.transform.position + (Random.insideUnitSphere*1.5f);
                    Target.y = 0f;
                }
                else if (p == 1 && p2.gameObject.activeSelf)
                {
                    Target = p2.transform.position + (Random.insideUnitSphere * 1.5f);
                    Target.y = 0f;
                }

            }
        }
    }

    internal virtual IEnumerator DoAttack()
    {
        yield return new WaitForSeconds(0.1f);

        if (CurrentWeapon == null) yield break;

        switch (CurrentWeapon.Class)
        {
            case WeaponClass.Melee:
                Vector3 testPos = transform.position + new Vector3((float)faceDir * 0.5f, 1f, 0f);
                playMeleeWeapon();
                if (p1.gameObject.activeSelf && Vector3.Distance(testPos, p1.transform.position) < CurrentWeapon.Range)
                    p1.HitByMelee(this);
                if (p2.gameObject.activeSelf && Vector3.Distance(testPos, p2.transform.position) < CurrentWeapon.Range)
                    p2.HitByMelee(this);
                break;
            case WeaponClass.Throw:
                Projectile p = ProjectileManager.Instance.Spawn(CurrentWeapon.ProjectileType, transform.position + new Vector3((float)faceDir * 0.3f, 1f, 0f), this);
                if (p != null)
                {
                    Vector3 throwVelocity = (transform.position + new Vector3((float)faceDir * (CurrentWeapon.Range*0.5f), 0f, 0f) - transform.position);
                    throwVelocity *= CurrentWeapon.Range*0.5f;
                    throwVelocity.y = CurrentWeapon.Range;
                    p.rigidbody.velocity = throwVelocity;
                }
                playThrowingWeapon();
                break;
            case WeaponClass.Use:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    internal virtual void UseWeapon()
    {
        switch (CurrentWeapon.Type)
        {
            case WeaponType.Flamethrower:
                transform.FindChild("Body/Weapon_Use/FlamethrowerParticles").GetComponent<ParticleSystem>().Emit(20);
                //playSwingingAudio();
                break;
        }

        CurrentWeapon.Durability--;
    }

    public void HitByMelee(Player p)
    {
        //

        DoKnockback(p.transform.position, p.CurrentWeapon.Knockback);

        if (Dead) return;
        Health -= p.CurrentWeapon.Damage;
        transform.FindChild("BloodParticles").GetComponent<ParticleSystem>().Emit(10);
        Sounds[p.CurrentWeapon.HitSoundClip].Play();
        StartCoroutine("PlayDamagedSound", Sounds[p.CurrentWeapon.HitSoundClip].clip.length + 0.05f);

        transform.FindChild("BloodParticles").GetComponent<ParticleSystem>().Emit(10);

        if (Health <= 0)
        {
            updateBodycount();
        }

    }

    private static void updateBodycount()
    {
        
          
            GameManager.Instance.TeamBodyCount++;
       
    }

    internal void HitByProjectile(Projectile projectile)
    {
        DoKnockback(projectile.transform.position, projectile.Knockback);

        if (projectile.Type == ProjectileType.Molotov) OnFire += 5f;

        if (Dead) return;
        Health -= projectile.Damage;
        transform.FindChild("BloodParticles").GetComponent<ParticleSystem>().Emit(10);
        Sounds[projectile.HitSoundClip].Play();
        StartCoroutine("PlayDamagedSound", Sounds[projectile.HitSoundClip].clip.length + 0.05f);

        if (Health <= 0)
        {
            updateBodycount();
        }
        
       
       


    }

    private void OnParticleCollision(GameObject other)
    {
        if (other.name == "FlamethrowerParticles")
        {
            OnFire += 0.5f;
        }
    }

    void DoKnockback(Vector3 pos, float force)
    {
        if (Knockback) return;

        Vector3 hitAngle = (transform.position - pos);
        hitAngle.y = Random.Range(0.5f, 1.5f);
        hitAngle.Normalize();

        rigidbody.AddForceAtPosition(hitAngle * force, transform.position);
        Knockback = true;
    }

    internal virtual void ToggleWalk(bool walk)
    {
        if (walk)
        {
            legsAnim.Play("Legs_Walk");
            torsoAnim.Play("Torso_Walk");
            headAnim.Play("Head_" + headStyle);
            hairAnim.Play("Hair_" + hairStyle);

            if (!armsAnim.IsPlaying("Arms_Attack"))
            {
                armsAnim.Play("Arms_Walk");
                startWalkingAudio();
            }
            if (!clothesAnim.IsPlaying("Clothes_Attack"))
            {
                clothesAnim.Play("Clothes_Walk");
                startWalkingAudio();
            }

            if (CurrentWeapon != null && !transform.FindChild("Body/Weapon_Swipe").GetComponent<Animation>().isPlaying)
            {
                transform.FindChild("Body/Weapon_Swipe").GetComponent<Animation>().Play("Weapon_Walk");
                startWalkingAudio();
            }
        }
        else
        {
            legsAnim.Play("Legs_Idle");
            torsoAnim.Play("Torso_Walk");
            headAnim.Play("Head_" + headStyle);
            hairAnim.Play("Hair_" + hairStyle);

            if (!armsAnim.IsPlaying("Arms_Attack"))
                armsAnim.Play("Arms_Idle");
            if (!clothesAnim.IsPlaying("Clothes_Attack"))
                clothesAnim.Play("Clothes_Idle");

            transform.FindChild("Body/Weapon_Swipe").GetComponent<Animation>().Stop("Weapon_Walk");
            stopWalkingAudio();
        }
    }


    IEnumerator PlayDamagedSound(float delay)
    {
        yield return new WaitForSeconds(delay);

        playPain();
    }


    /**
     * Override these with creature specific sounds 
     * 
     */
    internal virtual void playPain(){
        if (Sounds.ContainsKey("Grunt_Male_pain"))
            Sounds["Grunt_Male_pain"].Play();
    }



    internal virtual void playThrowingWeapon()
    {
        if (Sounds.ContainsKey("Footsteps_Heavy_Snow"))
            Sounds["Footsteps_Heavy_Snow"].Play();
    }

    internal virtual void playMeleeWeapon()
    {
        if (Sounds.ContainsKey("Club"))
            Sounds["Club"].Play();
    }

    internal virtual void startWalkingAudio()
    {
        if (Sounds.ContainsKey("Footsteps_Light_Snow"))
            playWalkAudio(Sounds["Footsteps_Light_Snow"]);
    }

    internal virtual void stopWalkingAudio()
    {
        if (Sounds.ContainsKey("Footsteps_Light_Snow"))
            stopWalkAudio(Sounds["Footsteps_Light_Snow"]);
    }


    internal virtual void playWalkAudio(AudioSource source)
    {

        if (!source.isPlaying)
        {
            source.loop = true;
            source.Play();

        }
    }

    internal virtual void stopWalkAudio(AudioSource source)
    {
        if (rigidbody.velocity.sqrMagnitude < 0.01f)
        {
            //Debug.Log("Stopping Player Audio" + source.clip.name);
            source.loop = false;
        }
    }

    internal virtual void IdleAnim()
    {
        legsAnim.Play("Legs_Idle");
        torsoAnim.Play("Torso_Walk");
        headAnim.Play("Head_" + headStyle);
        hairAnim.Play("Hair_" + hairStyle);

        armsAnim.Play("Arms_Idle");
        clothesAnim.Play("Clothes_Idle");
    }

    internal virtual void AttackAnim(string anim)
    {
        armsAnim.PlayFromFrame("Arms_Attack", 0);
        clothesAnim.PlayFromFrame("Clothes_Attack", 0);
    }

    public bool Get(Item item)
    {
        if (Dead) return false;
        if (!CanUseWeapons) return false;

        switch (item.Type)
        {
            case ItemType.Weapon:
                if ((CurrentWeapon != null && CurrentWeapon.Type != WeaponType.Snowball)) return false;

                SetWeapon(item.WeaponType);

                break;
        }

        return true;
    }







    internal void SetWeapon(WeaponType type)
    {
        CurrentWeapon = new Weapon(type);

        transform.FindChild("Body/Weapon_Swipe").gameObject.SetActive(false);
        transform.FindChild("Body/Weapon_Throw").gameObject.SetActive(false);
        transform.FindChild("Body/Weapon_Use").gameObject.SetActive(false);

        switch (CurrentWeapon.Class)
        {
            case WeaponClass.Melee:
                transform.FindChild("Body/Weapon_Swipe").gameObject.SetActive(true);
                foreach (Sprite s in Sprites)
                    if (s!=null && s.name == CurrentWeapon.Type.ToString())
                        transform.FindChild("Body/Weapon_Swipe").GetComponent<SpriteRenderer>().sprite = s;
                break;
            case WeaponClass.Throw:
                transform.FindChild("Body/Weapon_Throw").gameObject.SetActive(true);
                foreach (Sprite s in Sprites)
                    if (s != null && s.name == CurrentWeapon.Type.ToString())
                        transform.FindChild("Body/Weapon_Throw").GetComponent<SpriteRenderer>().sprite = s;
                break;
            case WeaponClass.Use:
                transform.FindChild("Body/Weapon_Use").gameObject.SetActive(true);
                foreach (Sprite s in Sprites)
                    if (s != null && s.name == CurrentWeapon.Type.ToString())
                        transform.FindChild("Body/Weapon_Use").GetComponent<SpriteRenderer>().sprite = s;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

    }
}
