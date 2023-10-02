﻿using SlugBase.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RWCustom;
using Random = UnityEngine.Random;
using Color = UnityEngine.Color;
using static Pearlcat.POEffect;

namespace Pearlcat;

public class PlayerModule
{
    public WeakReference<Player> PlayerRef { get; private set; }
    public WeakReference<Player>? PearlpupRef { get; set; }

    public PlayerModule(Player self)
    {
        PlayerRef = new(self);

        PlayerNumber = self.playerState.playerNumber;
        UniqueID = IDCounter++;
        BaseStats = NormalStats;
    }

    public int PlayerNumber { get; }
    public int UniqueID { get; }
    public static int IDCounter { get; set; }

    public SlugcatStats BaseStats { get; set; }
    public SlugcatStats NormalStats { get; private set; } = new(Enums.Pearlcat, false);
    public SlugcatStats MalnourishedStats { get; private set; } = new(Enums.Pearlcat, true);

    public int GraphicsResetCounter { get; set; }

    public bool JustWarped { get; set; }
    public AbstractRoom? LastRoom { get; set; }
    public int FirstSprite { get; set; }
    public int LastSprite { get; set; }

    public int ScarfSprite { get; set; }
    public int SleeveLSprite { get; set; }
    public int SleeveRSprite { get; set; }
    public int FeetSprite { get; set; }
    public int ShieldSprite { get; set; }
    public int HoloLightSprite { get; set; }

    public int AgilityCount => ModOptions.DisableAgility.Value ? 0 : MajorEffectCount(MajorEffectType.AGILITY);
    public int CamoCount => ModOptions.DisableCamoflague.Value ? 0 : MajorEffectCount(MajorEffectType.CAMOFLAGUE);
    public int RageCount => ModOptions.DisableRage.Value ? 0 : MajorEffectCount(MajorEffectType.RAGE);
    public int ReviveCount => ModOptions.DisableRevive.Value ? 0 : MajorEffectCount(MajorEffectType.REVIVE);
    public int SpearCount => ModOptions.DisableSpear.Value ? 0 : MajorEffectCount(MajorEffectType.SPEAR_CREATION);
    public int ShieldCount => ModOptions.DisableShield.Value ? 0 : MajorEffectCount(MajorEffectType.SHIELD);

    public int MajorEffectCount(MajorEffectType type)
    {
        var count = -1;

        var inventory = Inventory.Concat(PostDeathInventory);

        foreach (var pearl in inventory)
        {
            if (!pearl.TryGetModule(out var module)) continue;

            if (pearl.TryGetSentry(out _)) continue;

            if (pearl.GetPOEffect().MajorEffect != type) continue;

            if (count < 0)
                count = 0;

            if (module.CooldownTimer == 0)
                count++;
        }

        return count;
    }

    public AbstractPhysicalObject? SetAgilityCooldown(int cooldown) => PutOnCooldown(MajorEffectType.AGILITY, cooldown);
    public AbstractPhysicalObject? SetCamoCooldown (int cooldown) => PutOnCooldown(MajorEffectType.CAMOFLAGUE, cooldown);
    public AbstractPhysicalObject? SetRageCooldown(int cooldown) => PutOnCooldown(MajorEffectType.RAGE, cooldown);
    public AbstractPhysicalObject? SetReviveCooldown(int cooldown)
    {
        var result = PutOnCooldown(MajorEffectType.REVIVE, cooldown);

        if (result?.TryGetModule(out var module) == true)
            module.InventoryFlash = true;

        if (ModOptions.InventoryPings.Value)
            ShowHUD(80);

        return result;
    }
    public AbstractPhysicalObject? SetSpearCooldown(int cooldown) => PutOnCooldown(MajorEffectType.SPEAR_CREATION, cooldown);
    public AbstractPhysicalObject? SetShieldCooldown(int cooldown) => PutOnCooldown(MajorEffectType.SHIELD, cooldown);

    public AbstractPhysicalObject? PutOnCooldown(MajorEffectType type, int cooldown)
    {
        var inventory = Inventory.Concat(PostDeathInventory);

        foreach (var pearl in inventory)
        {
            if (!pearl.TryGetModule(out var module)) continue;

            if (pearl.GetPOEffect().MajorEffect != type) continue;

            if (module.CooldownTimer == 0)
            {
                module.CooldownTimer = cooldown;
                return pearl;
            }
        }

        return null;
    }

    public void ResetAgilityCooldown(int time)
    {
        foreach (var pearl in Inventory)
        {
            if (!pearl.TryGetModule(out var module)) continue;

            if (pearl.GetPOEffect().MajorEffect != MajorEffectType.AGILITY) continue;

            if (module.CooldownTimer == -1)
                module.CooldownTimer = time;
        }
    }


    public int ReviveTimer { get; set; }

    public int ShieldTimer { get; set; }
    public float ShieldAlpha { get; set; }
    public float ShieldScale { get; set; }

    public bool ShieldActive => (ShieldTimer > 0 || ShieldCount > 0) && !ModOptions.DisableShield.Value && PlayerRef.TryGetTarget(out var player) && !player.dead;
    public void ActivateVisualShield()
    {
        if (ShieldTimer > 0) return;

        var obj = SetShieldCooldown(ModOptions.ShieldRechargeTime.Value);

        if (obj?.TryGetModule(out var module) == true)
            module.InventoryFlash = true;
        
        ShieldTimer = ModOptions.ShieldDuration.Value;

        if (PlayerRef.TryGetTarget(out var player))
            player.room?.PlaySound(Enums.Sounds.Pearlcat_ShieldStart, player.firstChunk);

        if (ModOptions.InventoryPings.Value)
            ShowHUD(60);
    }

    public WeakReference<Creature>? RageTarget { get; set; }

    public int AgilityOveruseTimer { get; set; }

    public int SpearTimer { get; set; }
    public int SpearDelay { get; set; }
    public bool ForceLockSpearOnBack { get; set; }
    public float SpearLerp { get; set; }
    public bool WasSpearOnBack { get; set; }

    public float HoloLightAlpha { get; set; } = 1.0f;
    public float HoloLightScale { get; set; }

    public Vector2 PrevHeadRotation { get; set; }
    public Vector2 LastGroundedPos { get; set; }
    public int GroundedTimer { get; set; }
    public int FlyTimer { get; set; }

    public int MaskCounter { get; set; }

    public bool WasSwapLeftInput { get; set; }
    public bool WasSwapRightInput { get; set; }
    public bool WasSwapped { get; set; }
    public bool WasStoreInput { get; set; }
    public bool WasAgilityInput { get; set; }
    public bool WasSentryInput { get; set; }

    public Player.InputPackage UnblockedInput { get; set; }
    public bool BlockInput { get; set; }

    public int SwapIntervalTimer { get; set; }
    public int StoreObjectTimer { get; set; }

    public List<AbstractPhysicalObject> Inventory { get; } = new();
    public List<AbstractPhysicalObject> PostDeathInventory { get; } = new();
    public int? PostDeathActiveObjectIndex { get; set; }

    public AbstractPhysicalObject? ActiveObject => ActiveObjectIndex != null && ActiveObjectIndex < Inventory.Count ? Inventory[(int)ActiveObjectIndex] : null;
    public int? ActiveObjectIndex { get; set; }
    public POEffect CurrentPOEffect { get; set; } = POEffectManager.None;

    public float ShortcutColorTimer { get; set; }
    public int ShortcutColorTimerDirection { get; set; } = 1;

    public void ShowHUD(int duration) => HudFadeTimer = duration;

    public float HudFade { get; set; }
    public float HudFadeTimer { get; set; }

    public bool GivenPearls { get; set; }

    public int ObjectAnimationTimer { get; set; }
    public int ObjectAnimationDuration { get; set; }
    public ObjectAnimation? CurrentObjectAnimation { get; set; }

    public bool IsDazed => DazeTimer > 0;
    public int DazeTimer { get; set; }

    public void PickObjectAnimation(Player player)
    {
        if (!Hooks.MinOATime.TryGet(player, out var minTime)) return;
        if (!Hooks.MaxOATime.TryGet(player, out var maxTime)) return;
        if (!Hooks.DazeDuration.TryGet(player, out var dazeDuration)) return;

        CurrentObjectAnimation = GetObjectAnimation(player);
        ObjectAnimationTimer = 0;

        var randState = Random.state;
        Random.InitState((int)DateTime.Now.Ticks);
        ObjectAnimationDuration = Random.Range(minTime, maxTime);
        Random.state = randState;

        foreach (var abstractObject in Inventory)
            abstractObject.realizedObject?.SwapEffect(player.firstChunk.pos);

        //dazeStacker = dazeDuration;
    }

    public ObjectAnimation GetObjectAnimation(Player player)
    {

        List<ObjectAnimation> animationPool = new()
        {
            new BasicOrbitOA(player),
            new LayerOrbitOA(player),
        };

        List<ObjectAnimation> stillAnimationPool = new()
        {
            new MultiOrbitOA(player),
            new SineWaveOA(player),
            new SineWaveInterOA(player),
        };


        if (player.firstChunk.vel.magnitude < 4.0f)
            animationPool.AddRange(stillAnimationPool);

        if (CurrentObjectAnimation != null && animationPool.Count > 1)
            animationPool.RemoveAll(x => x.GetType() == CurrentObjectAnimation.GetType());

        //if (animationI >= animationPool.Count)
        //    animationI = 0;

        //return animationPool[animationI++];

        return animationPool[Random.Range(0, animationPool.Count)];
    }

    //public static int animationI = 0;


    public void LoadSaveData(Player self)
    {
        var world = self.abstractCreature.world;
        var save = world.game.GetMiscWorld();

        if (save == null) return;

        var playerNumber = self.playerState.playerNumber;

        if (!ModOptions.InventoryOverride.Value)
        {
            Inventory.Clear();
            
            if (save.Inventory.TryGetValue(playerNumber, out var inventory))
                foreach (var item in inventory)
                    self.AddToInventory(SaveState.AbstractPhysicalObjectFromString(world, item), addToEnd: true);
        }

        ActiveObjectIndex = null;

        if (save.ActiveObjectIndex.TryGetValue(playerNumber, out var activeObjectIndex) && Inventory.Count > 0)
            ActiveObjectIndex = activeObjectIndex < Inventory.Count ? activeObjectIndex : 0;

        PickObjectAnimation(self);

        //Plugin.Logger.LogWarning("LOAD SAVE DATA IN PLAYER MODULE");
        //foreach (var a in Inventory)
        //{
        //    if (a is DataPearl.AbstractDataPearl pearl)
        //        Plugin.Logger.LogWarning(pearl.dataPearlType);
        //}
        //Plugin.Logger.LogWarning(ActiveObjectIndex);
    }


    #region Sounds

    public DynamicSoundLoop MenuCrackleLoop { get; set; } = null!;
    public DynamicSoundLoop ShieldHoldLoop { get; set; } = null!;

    public void InitSounds(Player player)
    {
        MenuCrackleLoop = new ChunkDynamicSoundLoop(player.firstChunk)
        {
            sound = Enums.Sounds.Pearlcat_MenuCrackle,
            destroyClipWhenDone = false,
            Pitch = 1.0f,
            Volume = 1.0f,
        };

        ShieldHoldLoop = new ChunkDynamicSoundLoop(player.firstChunk)
        {
            sound = Enums.Sounds.Pearlcat_ShieldHold,
            destroyClipWhenDone = false,
            Pitch = 1.0f,
            Volume = 1.0f,
        };
    }

    #endregion

    #region Colours

    public int TextureUpdateTimer { get; set; }

    public Color CamoColor { get; set; }
    public float CamoLerp { get; set; }

    public Color LastBodyColor { get; set; }
    public Color LastAccentColor { get; set; }

    public Color BodyColor { get; set; }
    public Color FaceColor { get; set; }
    public Color AccentColor { get; set; }
    public Color CloakColor { get; set; }

    public Color BaseBodyColor { get; set; }
    public Color BaseFaceColor { get; set; }
    public Color BaseAccentColor { get; set; }
    public Color BaseCloakColor { get; set; }

    public static Color DefaultBodyColor => Custom.hexToColor("122626");
    public static Color DefaultFaceColor => Color.white;
    public static Color DefaultAccentColor => Color.white;
    public static Color DefaultCloakColor => Custom.hexToColor("ad2424");

    public Color ActiveColor => ActiveObject?.GetObjectColor() ?? Color.white;

    public void InitColors(PlayerGraphics self)
    {
        BaseBodyColor = PlayerColor.Body.GetColor(self) ?? DefaultBodyColor;
        BaseFaceColor = PlayerColor.Eyes.GetColor(self) ?? DefaultFaceColor;

        BaseAccentColor = new PlayerColor("Accent").GetColor(self) ?? DefaultAccentColor;
        BaseCloakColor = new PlayerColor("Cloak").GetColor(self) ?? DefaultCloakColor;
    }

    public void UpdateColors(PlayerGraphics self)
    {
        BodyColor = self.HypothermiaColorBlend(Color.Lerp(BaseBodyColor, CamoColor, CamoLerp));
        AccentColor = self.HypothermiaColorBlend(Color.Lerp(BaseAccentColor, CamoColor, CamoLerp));
        CloakColor = self.HypothermiaColorBlend(Color.Lerp(BaseCloakColor, CamoColor, CamoLerp));

        FaceColor = BaseFaceColor;

        if (self.malnourished > 0.0f)
        {
            float malnourished = self.player.Malnourished ? self.malnourished : Mathf.Max(0f, self.malnourished - 0.005f);
            
            BodyColor = Color.Lerp(BodyColor, Color.gray, 0.4f * malnourished);
            AccentColor = Color.Lerp(AccentColor, Color.gray, 0.4f * malnourished);
        }

        BodyColor = BodyColor.RWColorSafety();
        AccentColor = AccentColor.RWColorSafety();
        CloakColor = CloakColor.RWColorSafety();
        FaceColor = FaceColor.RWColorSafety();
    }

    public static void MapAlphaToColor(Texture2D texture, Dictionary<byte, Color> map)
    {
        var data = texture.GetPixelData<Color32>(0);

        for (int i = 0; i < data.Length; i++)
        {
            if (map.TryGetValue(data[i].a, out var targetColor))
            {
                data[i] = targetColor;
            }
        }

        texture.SetPixelData(data, 0);

        //for (var x = 0; x < texture.width; x++)
        //    for (var y = 0; y < texture.height; y++)
        //        if (map.TryGetValue((byte)(texture.GetPixel(x, y).a * 255), out var targetColor))
        //            texture.SetPixel(x, y, targetColor);

        
        texture.Apply(false);
    }

    #endregion


    #region Ears & Tail

    public TailSegment[]? EarL { get; set; }
    public TailSegment[]? EarR { get; set; }

    public int EarLSprite { get; set; }
    public int EarRSprite { get; set; }

    public FAtlas? EarLAtlas { get; set; }
    public FAtlas? EarRAtlas { get; set; }

    public Vector2 EarLAttachPos { get; set; }
    public Vector2 EarRAttachPos { get; set; }

    public int EarLFlipDirection { get; set; } = 1;
    public int EarRFlipDirection { get; set; } = 1;

    public void LoadEarLTexture(string textureName)
    {   
        var earLTexture = AssetLoader.GetTexture(textureName);
        if (earLTexture == null) return;

        // Apply Colors
        MapAlphaToColor(earLTexture, new Dictionary<byte, Color>()
        {
            { 255, BodyColor },
            { 0, AccentColor },
        });

        var atlasName = Plugin.MOD_ID + textureName + UniqueID;

        if (Futile.atlasManager.DoesContainAtlas(atlasName))
            Futile.atlasManager.ActuallyUnloadAtlasOrImage(atlasName);

        EarLAtlas = Futile.atlasManager.LoadAtlasFromTexture(atlasName, earLTexture, false);
    }

    public void LoadEarRTexture(string textureName)
    {
        var earRTexture = AssetLoader.GetTexture(textureName);
        if (earRTexture == null) return;

        // Apply Colors
        MapAlphaToColor(earRTexture, new Dictionary<byte, Color>()
        {
            { 255, BodyColor },
            { 0, AccentColor },
        });

        var atlasName = Plugin.MOD_ID + textureName + UniqueID;

        if (Futile.atlasManager.DoesContainAtlas(atlasName))
            Futile.atlasManager.ActuallyUnloadAtlasOrImage(atlasName);

        EarRAtlas = Futile.atlasManager.LoadAtlasFromTexture(atlasName, earRTexture, false);
    }

    public void RegenerateEars()
    {
        if (!PlayerRef.TryGetTarget(out var player)) return;

        if (player.graphicsModule == null) return;

        PlayerGraphics self = (PlayerGraphics)player.graphicsModule;

        TailSegment[] newEarL = new TailSegment[3];
        newEarL[0] = new TailSegment(self, 2.5f, 4.0f, null, 0.85f, 1.0f, 1.0f, true);
        newEarL[1] = new TailSegment(self, 3.0f, 6.0f, newEarL[0], 0.85f, 1.0f, 0.05f, true);
        newEarL[2] = new TailSegment(self, 1.0f, 4.0f, newEarL[1], 0.85f, 1.0f, 0.05f, true);


        if (EarL != null)
        {
            for (var i = 0; i < newEarL.Length && i < EarL.Length; i++)
            {
                newEarL[i].pos = EarL[i].pos;
                newEarL[i].lastPos = EarL[i].lastPos;
                newEarL[i].vel = EarL[i].vel;
                newEarL[i].terrainContact = EarL[i].terrainContact;
                newEarL[i].stretched = EarL[i].stretched;
            }
        }


        TailSegment[] newEarR = new TailSegment[3];
        newEarR[0] = new TailSegment(self, 2.5f, 4.0f, null, 0.85f, 1.0f, 1.0f, true);
        newEarR[1] = new TailSegment(self, 3.0f, 6.0f, newEarR[0], 0.85f, 1.0f, 0.05f, true);
        newEarR[2] = new TailSegment(self, 1.0f, 4.0f, newEarR[1], 0.85f, 1.0f, 0.05f, true);

        if (EarR != null)
        {
            for (var i = 0; i < newEarR.Length && i < EarR.Length; i++)
            {
                newEarR[i].pos = EarR[i].pos;
                newEarR[i].lastPos = EarR[i].lastPos;
                newEarR[i].vel = EarR[i].vel;
                newEarR[i].terrainContact = EarR[i].terrainContact;
                newEarR[i].stretched = EarR[i].stretched;
            }
        }

        EarL = newEarL;
        EarR = newEarR;

        List<BodyPart> newBodyParts = self.bodyParts.ToList();

        newBodyParts.AddRange(EarL);
        newBodyParts.AddRange(EarR);

        self.bodyParts = newBodyParts.ToArray();
    }


    public FAtlas? TailAtlas { get; set; }
    public bool SetInvertTailColors { get; set; }
    public bool CurrentlyInvertedTailColors { get; set; }

    public void LoadTailTexture(string textureName)
    {
        var tailTexture = AssetLoader.GetTexture(textureName);
        if (tailTexture == null) return;
        
        CurrentlyInvertedTailColors = SetInvertTailColors;

        // Apply Colors
        MapAlphaToColor(tailTexture, new Dictionary<byte, Color>()
        {
            { 255, CurrentlyInvertedTailColors ? AccentColor : BodyColor },
            { 0, CurrentlyInvertedTailColors ? BodyColor : AccentColor },
        });

        var atlasName = Plugin.MOD_ID + textureName + UniqueID;

        if (Futile.atlasManager.DoesContainAtlas(atlasName))
            Futile.atlasManager.ActuallyUnloadAtlasOrImage(atlasName);

        TailAtlas = Futile.atlasManager.LoadAtlasFromTexture(atlasName, tailTexture, false);

    }


    public void RegenerateTail()
    {
        if (ModOptions.DisableCosmetics.Value) return;

        if (!PlayerRef.TryGetTarget(out var player)) return;

        if (player.graphicsModule == null) return;

        PlayerGraphics self = (PlayerGraphics)player.graphicsModule;

        TailSegment[] newTail = new TailSegment[6];
        newTail[0] = new TailSegment(self, 8.0f, 4.0f, null, 0.85f, 1.0f, 1.0f, true);
        newTail[1] = new TailSegment(self, 7.0f, 7.0f, newTail[0], 0.85f, 1.0f, 0.5f, true);
        newTail[2] = new TailSegment(self, 6.0f, 7.0f, newTail[1], 0.85f, 1.0f, 0.5f, true);
        newTail[3] = new TailSegment(self, 5.0f, 7.0f, newTail[2], 0.85f, 1.0f, 0.5f, true);
        newTail[4] = new TailSegment(self, 2.5f, 7.0f, newTail[3], 0.85f, 1.0f, 0.5f, true);
        newTail[5] = new TailSegment(self, 1.0f, 7.0f, newTail[4], 0.85f, 1.0f, 0.5f, true);

        for (int i = 0; i < newTail.Length && i < self.tail.Length; i++)
        {
            newTail[i].pos = self.tail[i].pos;
            newTail[i].lastPos = self.tail[i].lastPos;
            newTail[i].vel = self.tail[i].vel;
            newTail[i].terrainContact = self.tail[i].terrainContact;
            newTail[i].stretched = self.tail[i].stretched;
        }

        if (self.tail == newTail) return;
        self.tail = newTail;

        var newBodyParts = self.bodyParts.ToList();
        newBodyParts.RemoveAll(x => x is TailSegment);
        newBodyParts.AddRange(self.tail);

        self.bodyParts = newBodyParts.ToArray();
    }

    #endregion

    public int CloakSprite { get; set; }
    public CloakGraphics Cloak { get; set; } = null!;
}
