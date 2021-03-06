﻿using System;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;
using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class Player : MonoBehaviour {

    public Transform HUD;
    public Transform Sprite;
    public Sprite WeaponSheet;
    public Slider playerHealthSlider;
    public GameObject playerDurabilityObject;
    public Text BodyCountTextObject;
        

	// "Physics"
    public float walkSpeed = 0.000001f;
    public float SpeedLimit = 1f;
    public Vector3 Speed;
    public float Accel = 0.1f;
    public float Gravity = 0.1f;

    // Items
    public Weapon CurrentWeapon = null;

    // States
    public bool Knockback = false;
    public float OnFire;
    public bool Dead = false;

    // Weapon Sprites
    public List<Sprite> Sprites = new List<Sprite>(); 

    // Stats
    public Dictionary<string, AudioSource> Sounds = new Dictionary<string, AudioSource>();
    public float playerHealth = 100f;
    private Slider playerDurabilitySlider;
    public int PlayerNumber = 1;

    private float turntarget = 12f;
    public int faceDir = 1;
    private Vector2 actualSize = new Vector2(4f,4f);
    private float fstepTime = 0f;
    internal float attackCooldown = 0f;

    private tk2dSpriteAnimator legsAnim;
    private tk2dSpriteAnimator torsoAnim;
    private tk2dSpriteAnimator armsAnim;
    private tk2dSpriteAnimator headAnim;
    private tk2dSpriteAnimator hairAnim;
    private tk2dSpriteAnimator clothesAnim;

    internal int headStyle = 0;
    internal int hairStyle = 0;

    void Start(){
     playerHealthSlider.maxValue = playerHealth;
    }
    void Awake()
    {
        turntarget = actualSize.x;

        legsAnim = transform.FindChild("Body/Legs").GetComponent<tk2dSpriteAnimator>();
        torsoAnim = transform.FindChild("Body/Torso").GetComponent<tk2dSpriteAnimator>();
        armsAnim = transform.FindChild("Body/Arms").GetComponent<tk2dSpriteAnimator>();
        headAnim = transform.FindChild("Body/Head").GetComponent<tk2dSpriteAnimator>();
        hairAnim = transform.FindChild("Body/Hair").GetComponent<tk2dSpriteAnimator>();
        clothesAnim = transform.FindChild("Body/Clothes").GetComponent<tk2dSpriteAnimator>();

        GameObject soundsObject = transform.FindChild("Audio").gameObject;
        foreach (AudioSource a in soundsObject.GetComponents<AudioSource>())
        {
            Sounds.Add(a.clip.name, a);
        }

        playerDurabilitySlider = playerDurabilityObject.transform.FindChild("DurabilitySlider").GetComponent<Slider>();
        SetWeapon(WeaponType.Snowball);

        if (playerDurabilitySlider != null)
        {
            playerDurabilitySlider.maxValue = CurrentWeapon.BaseDurability;
        }
        else
        {
            Debug.Log("Player.cs --> playerDurabilitySlider is null");
        }

        hairStyle = Random.Range(0, 3) + 1;
        headStyle = Random.Range(0, 3) + 1;
    }

    private void SetWeapon(WeaponType type)
    {
        CurrentWeapon = new Weapon(type);

        transform.FindChild("Body/Weapon_Swipe").gameObject.SetActive(false);
        transform.FindChild("Body/Weapon_Throw").gameObject.SetActive(false);
        transform.FindChild("Body/Weapon_Use").gameObject.SetActive(false);
        //transform.FindChild("Weapon_Use").gameObject.SetActive(false);
            
        switch (CurrentWeapon.Class)
        {
            case WeaponClass.Melee:

                transform.FindChild("Body/Weapon_Swipe").gameObject.SetActive(true);
                foreach (Sprite s in Sprites)
                    if (s != null && s.name == CurrentWeapon.Type.ToString())
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
        if (CurrentWeapon.BaseDurability > 0) {

            if (!playerDurabilityObject.activeSelf)
            {
                playerDurabilityObject.SetActive(true);
            }
            playerDurabilitySlider.maxValue = CurrentWeapon.BaseDurability;
            //playerDurabilitySlider.value = CurrentWeapon.BaseDurability;
        }
        else
        {
            playerDurabilityObject.SetActive(false);
        }
    }

    void Update()
    {
        if (Dead)
        {
            CurrentWeapon = null;
            transform.FindChild("Body/Weapon_Swipe").gameObject.SetActive(false);
            transform.FindChild("Body/Weapon_Throw").gameObject.SetActive(false);
            if (transform.FindChild("Body/Weapon_Use") != null)
                transform.FindChild("Body/Weapon_Use").gameObject.SetActive(false);

            ToggleWalk(false);

            Sprite.localScale = Vector3.Lerp(Sprite.transform.localScale, new Vector3(turntarget, actualSize.y, 1f), 1f);

            //Sprite.Rotate(0f,0f,(90f * faceDir) * Time.deltaTime);

            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0f, 0f, 90f * faceDir), Time.deltaTime * 5f);

            return;
        }

        //Input
        float h = Input.GetAxis("P" + PlayerNumber + " Horizontal");
        float v = Input.GetAxis("P" + PlayerNumber + " Vertical");

        if (h > 0f)
        {
            turntarget = actualSize.x;
            faceDir = 1;
        }
        if (h < 0f)
        {
            turntarget = -actualSize.x;
            faceDir = -1;
        }

        //Speed = walkSpeed;
        //rigidbody.velocity = transform.TransformDirection(new Vector3(h, 0, v).normalized) * walkSpeed;

        if (!Knockback && new Vector3(h, 0, v).normalized.magnitude>0f)
            rigidbody.velocity = transform.TransformDirection(new Vector3(h, 0, v).normalized) * walkSpeed;

        if (Knockback && rigidbody.velocity.magnitude < 0.3f)
        {
            Knockback = false;
        }

        //rigidbody.AddForce(new Vector3(h, 0, v).normalized);

        //Speed += new Vector3(h, 0, v).normalized * Accel;
        //Speed = Vector3.ClampMagnitude(Speed, SpeedLimit);
        //transform.position += Speed;
        //Speed = Vector3.Lerp(Speed, Vector3.zero, 0f)



        if (rigidbody.velocity.magnitude > 0f)
        {
            fstepTime += Time.deltaTime;
            if (fstepTime > 0.1f)
            {
                //Sounds["fstep"].Play();
                fstepTime = 0f;
            }
        }

        ToggleWalk(rigidbody.velocity.magnitude > 0.01f);
     
        Sprite.localScale = Vector3.Lerp(Sprite.transform.localScale, new Vector3(turntarget, actualSize.y, 1f), 0.25f);
        transform.FindChild("Body/Weapon_Use/FlamethrowerParticles").rotation = Quaternion.Euler(-12, 90*faceDir, 0f);

        attackCooldown -= Time.deltaTime;
        if (Input.GetButtonDown("P"+PlayerNumber+" Weapon") && CurrentWeapon!=null && CurrentWeapon.Class!= WeaponClass.Use && attackCooldown<=0f)
        {
            switch (CurrentWeapon.Class)
            {
                case WeaponClass.Melee:
                    transform.FindChild("Body/Weapon_Swipe").GetComponent<Animation>().Play("Weapon_Swipe");
                    AttackAnim("Attack");
                   playSwingingAudio();
                    break;
                case WeaponClass.Throw:
                    transform.FindChild("Body/Weapon_Throw").GetComponent<Animation>().Play("Weapon_Throw");
                    AttackAnim("Attack");
                    playSwingingAudio();
                    break;
            }

            attackCooldown = CurrentWeapon.Cooldown;
            StartCoroutine("DoAttack");
        }

        if (Input.GetButtonUp("P"+PlayerNumber+" Weapon") && CurrentWeapon.Type== WeaponType.Flamethrower)
        {
            if (Sounds[CurrentWeapon.SwingSoundClip].isPlaying)
            {
                Sounds[CurrentWeapon.SwingSoundClip].Stop();
            }
        }

        transform.FindChild("Body/Weapon_Use/FlamethrowerLight").gameObject.SetActive(false);
        if (Input.GetButton("P1 Weapon") && CurrentWeapon != null && CurrentWeapon.Class == WeaponClass.Use && attackCooldown <= 0f)
        {
            UseWeapon();
            if(CurrentWeapon.Type== WeaponType.Flamethrower) 
                 transform.FindChild("Body/Weapon_Use/FlamethrowerLight").gameObject.SetActive(true);
        }

        if (Input.GetButtonDown("P"+ PlayerNumber +" Throw") && CurrentWeapon != null)
        {
            if (CurrentWeapon.Type == WeaponType.Snowball) return;

            switch (CurrentWeapon.Class)
            {
                case WeaponClass.Melee:
                    //transform.FindChild("Weapon_Swipe").gameObject.SetActive(false);
                    break;
            }

            Item i = ItemManager.Instance.SpawnWeapon(CurrentWeapon.Type, CurrentWeapon.Durability);
            if (i != null)
            {
                i.transform.position = transform.position + new Vector3((float)faceDir * 1f, 1f, 0f);
                Vector3 throwVelocity = new Vector3((float)faceDir * 7f, 3f, 0f);
                i.rigidbody.velocity = throwVelocity;
            }

            SetWeapon(WeaponType.Snowball);

        }


        if (CurrentWeapon != null && CurrentWeapon.Durability <= 0)
        {
            SetWeapon(WeaponType.Snowball);
        }

        if (OnFire > 0f)
        {
            OnFire -= Time.deltaTime;
            playerHealth -= 0.1f;

            transform.FindChild("FireParticles").GetComponent<ParticleSystem>().Emit(5);
        }

        playerHealth = Mathf.Clamp(playerHealth, 0f, 100f);

        if (playerHealth <= 0)
        {
            Dead = true;
        }

        UpdateHealthBar();

    }

    private IEnumerator DoAttack()
    {
        yield return new WaitForSeconds(0.1f);

        switch (CurrentWeapon.Class)
        {
            case WeaponClass.Melee:

                playSwingingAudio();
                    Vector3 testPos = transform.position + new Vector3((float)faceDir * 0.5f, 1f, 0f);
                foreach(Enemy e in EnemyManager.Instance.Enemies)
                    if (Vector3.Distance(testPos, e.transform.position + new Vector3(0f,1f,0f)) < CurrentWeapon.Range && !e.Dead)
                    {
                        e.HitByMelee(this);
                        CurrentWeapon.Durability--;
                    }

                if (PlayerNumber == 1 && GameManager.Instance.P2.gameObject.activeSelf)
                {
                    if (Vector3.Distance(testPos, GameManager.Instance.P2.transform.position + new Vector3(0f, 1f, 0f)) < CurrentWeapon.Range && !GameManager.Instance.P2.GetComponent<Player>().Dead)
                    {
                        GameManager.Instance.P2.GetComponent<Player>().HitByMelee(this);
                        CurrentWeapon.Durability--;
                    }
                }

                if (PlayerNumber == 2 && GameManager.Instance.P1.gameObject.activeSelf)
                {
                    if (Vector3.Distance(testPos, GameManager.Instance.P1.transform.position + new Vector3(0f, 1f, 0f)) < CurrentWeapon.Range && !GameManager.Instance.P1.GetComponent<Player>().Dead)
                    {
                        GameManager.Instance.P1.GetComponent<Player>().HitByMelee(this);
                        CurrentWeapon.Durability--;
                    }
                }
                
                break;
            case WeaponClass.Throw:
                playSwingingAudio();
                Projectile p = ProjectileManager.Instance.Spawn(CurrentWeapon.ProjectileType, transform.position + new Vector3((float)faceDir * 0.3f, 1f, 0f), this);
                if (p != null)
                {
                    Vector3 throwVelocity = (transform.position + new Vector3((float)faceDir * (CurrentWeapon.Range * 0.5f), 0f, 0f) - transform.position);
                    throwVelocity *= CurrentWeapon.Range * 0.5f;
                    throwVelocity.y = CurrentWeapon.Range;
                    p.rigidbody.velocity = throwVelocity;

                    CurrentWeapon.Durability--;
                }
                break;
            case WeaponClass.Use:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void playSwingingAudio()
    {

        
        if (!"".Equals(CurrentWeapon.SwingSoundClip))
        {
            if (!Sounds[CurrentWeapon.SwingSoundClip].isPlaying)
            {
                Sounds[CurrentWeapon.SwingSoundClip].Play();
            }
        }
    }

    void UseWeapon()
    {
        switch (CurrentWeapon.Type)
        {
            case WeaponType.Flamethrower:
                transform.FindChild("Body/Weapon_Use/FlamethrowerParticles").GetComponent<ParticleSystem>().Emit(20);
                playSwingingAudio();
                break;
        }

        CurrentWeapon.Durability--;
    }

    void ToggleWalk(bool walk)
    {
        if (walk)
        {
            legsAnim.Play("Legs_Walk");
            torsoAnim.Play("Torso_Walk");
            headAnim.Play("Head_" + headStyle);
            hairAnim.Play("Hair_" + hairStyle);

            if (!armsAnim.IsPlaying("Arms_Attack") && (CurrentWeapon==null || CurrentWeapon.Class!= WeaponClass.Use))
                armsAnim.Play("Arms_Walk");
            if (!clothesAnim.IsPlaying("Clothes_" + (PlayerNumber == 1 ? "Red" : "Blue") + "_Attack") && (CurrentWeapon == null || CurrentWeapon.Class != WeaponClass.Use))
                clothesAnim.Play("Clothes_" + (PlayerNumber == 1 ? "Red" : "Blue") + "_Walk");

            if (CurrentWeapon != null)
                switch (CurrentWeapon.Class)
                {
                    case WeaponClass.Melee:
                        if (!transform.FindChild("Body/Weapon_Swipe").GetComponent<Animation>().isPlaying)
                        {
                            transform.FindChild("Body/Weapon_Swipe").GetComponent<Animation>().Play("Weapon_Walk");
                            startWalkingAudio(Sounds["Footsteps_Light_Snow"]);
                        }
                        break;
                    case WeaponClass.Throw:
                        if (!transform.FindChild("Body/Weapon_Throw").GetComponent<Animation>().isPlaying)
                        {
                            transform.FindChild("Body/Weapon_Throw").GetComponent<Animation>().Play("Weapon_Walk");
                            startWalkingAudio(Sounds["Footsteps_Light_Snow"]);
                        }
                        break;
                }
            startWalkingAudio(Sounds["Footsteps_Light_Snow"]);  
        }
        else
        {
            legsAnim.Play("Legs_Idle");
            torsoAnim.Play("Torso_Walk");
            headAnim.Play("Head_" + headStyle);
            hairAnim.Play("Hair_" + hairStyle);

            if (!armsAnim.IsPlaying("Arms_Attack"))
                armsAnim.Play("Arms_Idle");
            if (!clothesAnim.IsPlaying("Clothes_" + (PlayerNumber == 1 ? "Red" : "Blue") + "_Attack"))
                clothesAnim.Play("Clothes_" + (PlayerNumber == 1 ? "Red" : "Blue") + "_Idle");

            transform.FindChild("Body/Weapon_Swipe").GetComponent<Animation>().Stop("Weapon_Walk");
            transform.FindChild("Body/Weapon_Throw").GetComponent<Animation>().Stop("Weapon_Walk");
            stopWalkingAudio(Sounds["Footsteps_Light_Snow"]);
        }
    }

    private void OnParticleCollision(GameObject other)
    {
        if (other.name == "FlamethrowerParticles")
        {
            OnFire += 0.1f;
        }
    }

    private void startWalkingAudio(AudioSource source)
    {

        if (!source.isPlaying)
        {
            source.loop = true;
            source.Play();
        }
    }

    private void stopWalkingAudio(AudioSource source)
    {

        if (rigidbody.velocity.sqrMagnitude < 0.01f)
        {
            //Debug.Log("Stopping Player Audio" + source.clip.name);
            source.loop = false;
        }
    }

    void AttackAnim(string anim)
    {
        armsAnim.PlayFromFrame("Arms_Attack",0);
        clothesAnim.PlayFromFrame("Clothes_" + (PlayerNumber==1?"Red":"Blue") + "_Attack",0);
        if (!"".Equals(CurrentWeapon.SwingSoundClip)){
            Sounds[CurrentWeapon.SwingSoundClip].Play();
        }

    }

    public void HitByMelee(object owner)
    {
        
        if (Knockback) return;

        if (owner is Player)
        {
            Vector3 hitAngle = (transform.position - ((Player) owner).transform.position);
            hitAngle.y = Random.Range(0.5f, 1.5f);
            hitAngle.Normalize();

            rigidbody.AddForceAtPosition(hitAngle*100f, transform.position);
            Knockback = true;

            if (Dead) return;

            transform.FindChild("BloodParticles").GetComponent<ParticleSystem>().Emit(10);
            playerHealth -= ((Player)owner).CurrentWeapon.Damage;
            //Debug.Log(((Player)owner).Type + " Playing " + CurrentWeapon.Type);
            if (Sounds.ContainsKey(((Player)owner).CurrentWeapon.HitSoundClip))
                Sounds[((Player)owner).CurrentWeapon.HitSoundClip].Play();
            StartCoroutine("PlayDamagedSound", Sounds[((Player)owner).CurrentWeapon.HitSoundClip].clip.length + 0.05f);
        }
        else
        {
            Vector3 hitAngle = (transform.position - ((Enemy)owner).transform.position);
            hitAngle.y = Random.Range(0.5f, 1.5f);
            hitAngle.Normalize();

            rigidbody.AddForceAtPosition(hitAngle * 100f, transform.position);
            Knockback = true;

            if (Dead) return;

            transform.FindChild("BloodParticles").GetComponent<ParticleSystem>().Emit(10);
            playerHealth -= ((Enemy)owner).CurrentWeapon.Damage;
            //Debug.Log(((Player)owner).Type + " Playing " + CurrentWeapon.Type);
            if (Sounds.ContainsKey(((Enemy)owner).CurrentWeapon.HitSoundClip))
                Sounds[((Enemy)owner).CurrentWeapon.HitSoundClip].Play();
            StartCoroutine("PlayDamagedSound", Sounds[((Enemy)owner).CurrentWeapon.HitSoundClip].clip.length + 0.05f);
        }
    }

    internal void HitByProjectile(Projectile projectile)
    {

        if (Knockback) return;

        Vector3 hitAngle = (transform.position - projectile.transform.position);
        hitAngle.y = Random.Range(0.5f, 1.5f);
        hitAngle.Normalize();

        rigidbody.AddForceAtPosition(hitAngle * 100f, transform.position);
        Knockback = true;

        if (projectile.Type == ProjectileType.Molotov) OnFire += 5f;

        transform.FindChild("BloodParticles").GetComponent<ParticleSystem>().Emit(10);
        playerHealth -= projectile.Damage;
        Debug.Log("Playing " + projectile.Type);
        if(Sounds.ContainsKey(projectile.HitSoundClip))
            Sounds[projectile.HitSoundClip].Play();
        StartCoroutine("PlayDamagedSound",Sounds[projectile.HitSoundClip].clip.length+0.05f);
       

       


    }



    IEnumerator PlayDamagedSound(float delay)
    {
        yield return new WaitForSeconds(delay);

        Sounds["Grunt_Male_pain"].Play();
    }

    public bool Get(Item item)
    {
        switch (item.Type)
        {
            case ItemType.Weapon:
                if((CurrentWeapon!=null && CurrentWeapon.Type!=WeaponType.Snowball)) return false;

                SetWeapon(item.WeaponType);
                CurrentWeapon.Durability = item.Durability;
                //CurrentWeapon = new Weapon(item.WeaponType);


                //switch (CurrentWeapon.Class)
                //{
                //    case WeaponClass.Melee:
                //        SetWeapon(WeaponType.Snowball);
                //        break;
                //}

                break;
            case ItemType.Food:
                playerHealth += 50f;
                break;
        }

        return true;
    }

   

    private void UpdateHealthBar()
    {
        if (playerHealthSlider != null)
        {
            Debug.Log("Player Health: " + playerHealth);
            playerHealthSlider.value = playerHealth;
            Debug.Log("Bar value" + playerHealthSlider.value);
        }
        else
        {
            Debug.Log("playerHealthSlider is null");
        }

        if (playerDurabilitySlider != null)
        {
            //Debug.Log("Player Durability: " + CurrentWeapon.Durability);
            playerDurabilitySlider.value = CurrentWeapon.Durability;
        }
        else
        {
            Debug.Log("playerDurabilitySlider is null");
        }

       
    }



 
}
