﻿using RWCustom;
using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Pearlcat;

public static partial class Hooks
{
    public static void ApplyPlayerGraphicsHooks()
    {
        On.PlayerGraphics.InitiateSprites += PlayerGraphics_InitiateSprites;
        On.PlayerGraphics.AddToContainer += PlayerGraphics_AddToContainer;
        
        On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;

        On.PlayerGraphics.Update += PlayerGraphics_Update;
        On.PlayerGraphics.Reset += PlayerGraphics_Reset;


        On.PlayerGraphics.PlayerObjectLooker.HowInterestingIsThisObject += PlayerObjectLooker_HowInterestingIsThisObject;
        On.Player.ShortCutColor += Player_ShortCutColor;

        On.JollyCoop.JollyHUD.JollyPlayerSpecificHud.Draw += JollyPlayerSpecificHud_Draw;
    }


    public const int BODY_SPRITE = 0;
    public const int HIPS_SPRITE = 1;
    public const int TAIL_SPRITE = 2;
    public const int HEAD_SPRITE = 3;
    public const int LEGS_SPRITE = 4;
    public const int ARM_L_SPRITE = 5;
    public const int ARM_R_SPRITE = 6;
    public const int HAND_L_SPRITE = 7;
    public const int HAND_R_SPRITE = 8;
    public const int FACE_SPRITE = 9;
    public const int GLOW_SPRITE = 10;
    public const int MARK_SPRITE = 11;


    // Initialization
    private static void PlayerGraphics_InitiateSprites(On.PlayerGraphics.orig_InitiateSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
    {
        orig(self, sLeaser, rCam);

        if (!self.player.TryGetPearlcatModule(out var playerModule)) return;

        playerModule.InitColors(self);
        playerModule.InitSounds(self.player);

        playerModule.FirstSprite = sLeaser.sprites.Length;
        int spriteIndex = playerModule.FirstSprite;

        playerModule.ScarfSprite = spriteIndex++;

        playerModule.SleeveLSprite = spriteIndex++;
        playerModule.SleeveRSprite = spriteIndex++;

        playerModule.FeetSprite = spriteIndex++;

        playerModule.EarLSprite = spriteIndex++;
        playerModule.EarRSprite = spriteIndex++;

        playerModule.CloakSprite = spriteIndex++;

        playerModule.ShieldSprite = spriteIndex++;
        playerModule.HoloLightSprite = spriteIndex++;

        playerModule.LastSprite = spriteIndex;
        Array.Resize(ref sLeaser.sprites, spriteIndex);

        sLeaser.sprites[playerModule.ScarfSprite] = new("pearlcatScarfA0");

        sLeaser.sprites[playerModule.SleeveLSprite] = new("pearlcatSleeve0");
        sLeaser.sprites[playerModule.SleeveRSprite] = new("pearlcatSleeve0");

        sLeaser.sprites[playerModule.FeetSprite] = new("pearlcatFeetA0");

        sLeaser.sprites[playerModule.ShieldSprite] = new("Futile_White")
        {
            shader = rCam.room.game.rainWorld.Shaders["GravityDisruptor"],
        };

        sLeaser.sprites[playerModule.HoloLightSprite] = new("Futile_White")
        {
            shader = rCam.game.rainWorld.Shaders["HoloGrid"],
        };


        playerModule.RegenerateTail();
        playerModule.RegenerateEars();

        playerModule.Cloak = new CloakGraphics(self, playerModule);
        playerModule.Cloak.InitiateSprite(sLeaser, rCam);

        GenerateEarMesh(sLeaser, playerModule.EarL, playerModule.EarLSprite);
        GenerateEarMesh(sLeaser, playerModule.EarR, playerModule.EarRSprite);

        // Color meshes
        playerModule.LoadTailTexture(playerModule.IsPearlpupAppearance ? "pearlpup_alttail" : "tail");
        playerModule.LoadEarLTexture("ear_l");
        playerModule.LoadEarRTexture("ear_r");

        self.AddToContainer(sLeaser, rCam, null);

        if (self.player.inVoidSea || self.player.playerState.isGhost)
        {
            playerModule.GraphicsResetCounter = 20;
        }
    }

    public static void GenerateEarMesh(RoomCamera.SpriteLeaser sLeaser, TailSegment[]? ear, int earSprite)
    {
        if (ear == null) return;

        int earMeshTriesLength = (ear.Length - 1) * 4;
        var earMeshTries = new TriangleMesh.Triangle[earMeshTriesLength + 1];

        for (int i = 0; i < ear.Length - 1; i++)
        {
            int indexTimesFour = i * 4;

            for (int j = 0; j <= 3; j++)
            {
                earMeshTries[indexTimesFour + j] = new TriangleMesh.Triangle(indexTimesFour + j, indexTimesFour + j + 1, indexTimesFour + j + 2);
            }
        }

        earMeshTries[earMeshTriesLength] = new TriangleMesh.Triangle(earMeshTriesLength, earMeshTriesLength + 1, earMeshTriesLength + 2);
        sLeaser.sprites[earSprite] = new TriangleMesh("Futile_White", earMeshTries, false, false);
    }

    private static void PlayerGraphics_AddToContainer(On.PlayerGraphics.orig_AddToContainer orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
    {
        orig(self, sLeaser, rCam, newContatiner);

        if (!self.player.TryGetPearlcatModule(out var playerModule)) return;

        if (playerModule.FirstSprite <= 0 || sLeaser.sprites.Length < playerModule.LastSprite) return;

        newContatiner ??= rCam.ReturnFContainer("Midground");
        OrderAndColorSprites(self, sLeaser, rCam, playerModule, newContatiner);
    }

    private static void PlayerGraphics_Reset(On.PlayerGraphics.orig_Reset orig, PlayerGraphics self)
    {
        orig(self);

        if (!self.player.TryGetPearlcatModule(out var playerModule)) return;


        if (playerModule.EarL == null || playerModule.EarR == null) return;

        if (!EarLOffset.TryGet(self.player, out var earLOffset)) return;
        if (!EarROffset.TryGet(self.player, out var earROffset)) return;


        playerModule.EarLAttachPos = GetEarAttachPos(self, 1.0f, playerModule, earLOffset);

        for (int segment = 0; segment < playerModule.EarL.Length; segment++)
        {
            playerModule.EarL[segment].Reset(playerModule.EarLAttachPos);
        }

        playerModule.EarRAttachPos = GetEarAttachPos(self, 1.0f, playerModule, earROffset);

        for (int segment = 0; segment < playerModule.EarR.Length; segment++)
        {
            playerModule.EarR[segment].Reset(playerModule.EarRAttachPos);
        }

        playerModule.Cloak.needsReset = true;
    }


    // Draw
    private static void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        orig(self, sLeaser, rCam, timeStacker, camPos);

        if (!self.player.TryGetPearlcatModule(out var playerModule)) return;

        UpdateLightSource(self, playerModule);

        if (ModOptions.DisableCosmetics.Value)
        {
            OrderAndColorSprites(self, sLeaser, rCam, playerModule, null);
            return;
        }


        UpdateCustomPlayerSprite(sLeaser, HEAD_SPRITE, "Head", "scarf", "Scarf", playerModule.ScarfSprite);
        UpdateCustomPlayerSprite(sLeaser, LEGS_SPRITE, "Legs", "feet", "Feet", playerModule.FeetSprite);


        UpdateReplacementPlayerSprite(sLeaser, BODY_SPRITE, "Body", "body");
   
        UpdateReplacementPlayerSprite(sLeaser, HIPS_SPRITE, "Hips", "hips");
        UpdateReplacementPlayerSprite(sLeaser, HEAD_SPRITE, "Head", "head");

        UpdateReplacementPlayerSprite(sLeaser, LEGS_SPRITE, "Legs", "legs");

        UpdateReplacementPlayerSprite(sLeaser, ARM_L_SPRITE, "PlayerArm", "arm");
        UpdateReplacementPlayerSprite(sLeaser, ARM_R_SPRITE, "PlayerArm", "arm");


        if (playerModule.IsPearlpupAppearance)
        {
            UpdateCustomPlayerSprite(sLeaser, ARM_L_SPRITE, "PlayerArm", "pearlpup_sleeve", "SleevePearlpup", playerModule.SleeveLSprite);
            UpdateCustomPlayerSprite(sLeaser, ARM_R_SPRITE, "PlayerArm", "pearlpup_sleeve", "SleevePearlpup", playerModule.SleeveRSprite);
        }
        else
        {
            UpdateCustomPlayerSprite(sLeaser, ARM_L_SPRITE, "PlayerArm", "sleeve", "Sleeve", playerModule.SleeveLSprite);
            UpdateCustomPlayerSprite(sLeaser, ARM_R_SPRITE, "PlayerArm", "sleeve", "Sleeve", playerModule.SleeveRSprite);
        }


        var save = self.player.abstractCreature.world.game.GetMiscProgression();

        if (self.RenderAsPup)
        {
            if (self.player.firstChunk.vel.magnitude < 2.0f && self.objectLooker.currentMostInteresting is Player pup && pup.IsPearlpup() && (save.IsPearlpupSick || pup.dead))
            {
                UpdateReplacementPlayerSprite(sLeaser, FACE_SPRITE, "PFace", "pearlpup_face_sick", nameSuffix: "Sick");
            }
            else
            {
                UpdateReplacementPlayerSprite(sLeaser, FACE_SPRITE, "PFace", "pearlpup_face");
            }

            UpdateReplacementPlayerSprite(sLeaser, HEAD_SPRITE, "Head", "pearlpup_head");
        }
        else
        {
            if (self.player.firstChunk.vel.magnitude < 2.0f && self.objectLooker.currentMostInteresting is Player pup && pup.IsPearlpup() && (save.IsPearlpupSick || pup.dead))
            {
                UpdateReplacementPlayerSprite(sLeaser, FACE_SPRITE, "Face", "face_sick", nameSuffix: "Sick");
            }
            else
            {
                UpdateReplacementPlayerSprite(sLeaser, FACE_SPRITE, "Face", "face");
            }
        }

        DrawEars(self, sLeaser, timeStacker, camPos, playerModule);
        DrawTail(self, sLeaser, playerModule);

        playerModule.Cloak.DrawSprite(sLeaser, rCam, timeStacker, camPos);

        OrderAndColorSprites(self, sLeaser, rCam, playerModule, null);
    }

    public static void DrawEars(PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, float timestacker, Vector2 camPos, PlayerModule playerModule)
    {
        if (!EarLOffset.TryGet(self.player, out var earLOffset)) return;

        playerModule.EarLAttachPos = GetEarAttachPos(self, timestacker, playerModule, earLOffset);
        DrawEar(sLeaser, timestacker, camPos, playerModule.EarL, playerModule.EarLSprite, playerModule.EarLAtlas, playerModule.EarLAttachPos, playerModule.EarLFlipDirection);

        if (!EarROffset.TryGet(self.player, out var earROffset)) return;

        playerModule.EarRAttachPos = GetEarAttachPos(self, timestacker, playerModule, earROffset);
        DrawEar(sLeaser, timestacker, camPos, playerModule.EarR, playerModule.EarRSprite, playerModule.EarRAtlas, playerModule.EarRAttachPos, playerModule.EarRFlipDirection);
    }

    public static void DrawEar(RoomCamera.SpriteLeaser sLeaser, float timestacker, Vector2 camPos, TailSegment[]? ear, int earSprite, FAtlas? earAtlas, Vector2 attachPos, int earFlipDirection)
    {
        if (ear == null || ear.Length == 0) return;

        if (sLeaser.sprites[earSprite] is not TriangleMesh earMesh) return;

        // Draw Mesh
        var earRad = ear[0].rad;

        for (var segment = 0; segment < ear.Length; segment++)
        {
            var earPos = Vector2.Lerp(ear[segment].lastPos, ear[segment].pos, timestacker);


            var normalized = (earPos - attachPos).normalized;
            var perpendicularNormalized = Custom.PerpendicularVector(normalized);

            var distance = Vector2.Distance(earPos, attachPos) / 5.0f;

            if (segment == 0)
                distance = 0.0f;

            earMesh.MoveVertice(segment * 4, attachPos - earFlipDirection * perpendicularNormalized * earRad + normalized * distance - camPos);
            earMesh.MoveVertice(segment * 4 + 1, attachPos + earFlipDirection * perpendicularNormalized * earRad + normalized * distance - camPos);

            if (segment >= ear.Length - 1)
            {
                earMesh.MoveVertice(segment * 4 + 2, earPos - camPos);
            }
            else
            {
                earMesh.MoveVertice(segment * 4 + 2, earPos - earFlipDirection * perpendicularNormalized * ear[segment].StretchedRad - normalized * distance - camPos);
                earMesh.MoveVertice(segment * 4 + 3, earPos + earFlipDirection * perpendicularNormalized * ear[segment].StretchedRad - normalized * distance - camPos);
            }

            earRad = ear[segment].StretchedRad;
            attachPos = earPos;
        }



        // Apply Texture
        if (earAtlas == null) return;

        if (earAtlas.elements.Count == 0) return;

        sLeaser.sprites[earSprite].color = Color.white;
        earMesh.element = earAtlas.elements[0];

        if (earMesh.verticeColors == null || earMesh.verticeColors.Length != earMesh.vertices.Length)
            earMesh.verticeColors = new Color[earMesh.vertices.Length];

        for (int vertex = earMesh.verticeColors.Length - 1; vertex >= 0; vertex--)
        {
            var interpolation = (vertex / 2.0f) / (earMesh.verticeColors.Length / 2.0f);
            Vector2 uvInterpolation;

            // Even vertexes
            if (vertex % 2 == 0)
                uvInterpolation = new Vector2(interpolation, 0.0f);

            // Last vertex
            else if (vertex == earMesh.verticeColors.Length - 1)
                uvInterpolation = new Vector2(1.0f, 0.0f);

            else
                uvInterpolation = new Vector2(interpolation, 1.0f);

            Vector2 uv;
            uv.x = Mathf.Lerp(earMesh.element.uvBottomLeft.x, earMesh.element.uvTopRight.x, uvInterpolation.x);
            uv.y = Mathf.Lerp(earMesh.element.uvBottomLeft.y, earMesh.element.uvTopRight.y, uvInterpolation.y);

            earMesh.UVvertices[vertex] = uv;
        }
    }

    public static void DrawTail(PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, PlayerModule playerModule)
    {
        if (ModOptions.DisableCosmetics.Value) return;

        FAtlas? tailAtlas = playerModule.TailAtlas;
        if (tailAtlas == null) return;

        if (tailAtlas.elements.Count == 0) return;

        if (sLeaser.sprites[TAIL_SPRITE] is not TriangleMesh tailMesh) return;

        tailMesh.element = tailAtlas.elements[0];

        if (tailMesh.verticeColors == null || tailMesh.verticeColors.Length != tailMesh.vertices.Length)
            tailMesh.verticeColors = new Color[tailMesh.vertices.Length];


        Vector2 legsPos = self.legs.pos;
        Vector2 tailPos = self.tail[0].pos;

        // Find the difference between the x positions and convert it into a 0.0 - 1.0 ratio between the two
        float difference = tailPos.x - legsPos.x;

        if (!MinEffectiveOffset.TryGet(self.player, out var minEffectiveOffset)) return;
        if (!MaxEffectiveOffset.TryGet(self.player, out var maxEffectiveOffset)) return;

        float leftRightRatio = Mathf.InverseLerp(minEffectiveOffset, maxEffectiveOffset, difference);


        // Multiplier determines how many times larger the texture is vertically relative to the displayed portion
        const float TRUE_SIZE_MULT = 3.0f;
        float uvYOffset = Mathf.Lerp(0.0f, tailMesh.element.uvTopRight.y - (tailMesh.element.uvTopRight.y / TRUE_SIZE_MULT), leftRightRatio);

        for (int vertex = tailMesh.verticeColors.Length - 1; vertex >= 0; vertex--)
        {
            float interpolation = (vertex / 2.0f) / (tailMesh.verticeColors.Length / 2.0f);
            Vector2 uvInterpolation;

            // Even vertexes
            if (vertex % 2 == 0)
                uvInterpolation = new Vector2(interpolation, 0.0f);

            // Last vertex
            else if (vertex == tailMesh.verticeColors.Length - 1)
                uvInterpolation = new Vector2(1.0f, 0.0f);

            else
                uvInterpolation = new Vector2(interpolation, 1.0f);

            Vector2 uv;
            uv.x = Mathf.Lerp(tailMesh.element.uvBottomLeft.x, tailMesh.element.uvTopRight.x, uvInterpolation.x);
            uv.y = Mathf.Lerp(tailMesh.element.uvBottomLeft.y + uvYOffset, (tailMesh.element.uvTopRight.y / TRUE_SIZE_MULT) + uvYOffset, uvInterpolation.y);

            tailMesh.UVvertices[vertex] = uv;
        }
    }

    public static Vector2 GetEarAttachPos(PlayerGraphics self, float timestacker, PlayerModule playerModule, Vector2 offset) =>
        Vector2.Lerp(self.head.lastPos + offset, self.head.pos + offset, timestacker) + Vector3.Slerp(playerModule.PrevHeadRotation, self.head.connection.Rotation, timestacker).ToVector2InPoints() * 15.0f;


    // Update
    public static void OrderAndColorSprites(PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, PlayerModule playerModule, FContainer? newContainer = null)
    {
        var bodySprite = sLeaser.sprites[BODY_SPRITE];
        var armLSprite = sLeaser.sprites[ARM_L_SPRITE];
        var armRSprite = sLeaser.sprites[ARM_R_SPRITE];
        var hipsSprite = sLeaser.sprites[HIPS_SPRITE];
        var tailSprite = sLeaser.sprites[TAIL_SPRITE];
        var headSprite = sLeaser.sprites[HEAD_SPRITE];
        var handLSprite = sLeaser.sprites[HAND_L_SPRITE];
        var handRSprite = sLeaser.sprites[HAND_R_SPRITE];
        var legsSprite = sLeaser.sprites[LEGS_SPRITE];
        var markSprite = sLeaser.sprites[MARK_SPRITE];

        var feetSprite = sLeaser.sprites[playerModule.FeetSprite];
        
        var cloakSprite = sLeaser.sprites[playerModule.CloakSprite];
        var scarfSprite = sLeaser.sprites[playerModule.ScarfSprite];

        var sleeveLSprite = sLeaser.sprites[playerModule.SleeveLSprite];
        var sleeveRSprite = sLeaser.sprites[playerModule.SleeveRSprite];


        var earLSprite = sLeaser.sprites[playerModule.EarLSprite];
        var earRSprite = sLeaser.sprites[playerModule.EarRSprite];


        var shieldSprite = sLeaser.sprites[playerModule.ShieldSprite];
        var holoLightSprite = sLeaser.sprites[playerModule.HoloLightSprite];

        // Container
        if (newContainer != null)
        {
            var hudContainer = rCam.ReturnFContainer("HUD");

            newContainer.AddChild(scarfSprite);

            newContainer.AddChild(sleeveLSprite);
            newContainer.AddChild(sleeveRSprite);

            newContainer.AddChild(feetSprite);

            newContainer.AddChild(earLSprite);
            newContainer.AddChild(earRSprite);

            newContainer.AddChild(cloakSprite);

            hudContainer.AddChild(shieldSprite);
        }

        shieldSprite.alpha = playerModule.ShieldAlpha;
        shieldSprite.scale = playerModule.ShieldScale;
        shieldSprite.SetPosition(bodySprite.GetPosition());

        holoLightSprite.alpha = playerModule.HoloLightAlpha;
        holoLightSprite.scale = playerModule.HoloLightScale;
        holoLightSprite.SetPosition(bodySprite.GetPosition());
        holoLightSprite.color = new(0.384f, 0.184f, 0.984f, 1.0f);

        if (self.player.inVoidSea)
        {
            markSprite.alpha = 0.0f;
        }

        if (playerModule.ActiveObject != null)
        {
            markSprite.y += 10.0f;
        }

        if (ModOptions.DisableCosmetics.Value)
        {
            feetSprite.isVisible = false;

            cloakSprite.isVisible = false;
            scarfSprite.isVisible = false;

            sleeveLSprite.isVisible = false;
            sleeveRSprite.isVisible = false;

            earLSprite.isVisible = false;
            earRSprite.isVisible = false;
            return;
        }


        // Order
        // Generally, move behind body, move infront of head
        sleeveLSprite.MoveInFrontOfOtherNode(armLSprite);
        sleeveRSprite.MoveInFrontOfOtherNode(armRSprite);

        earLSprite.MoveBehindOtherNode(bodySprite);
        earRSprite.MoveBehindOtherNode(bodySprite);

        tailSprite.MoveBehindOtherNode(bodySprite);
        legsSprite.MoveBehindOtherNode(hipsSprite);

        cloakSprite.MoveBehindOtherNode(headSprite);

        feetSprite.MoveBehindOtherNode(cloakSprite);
        feetSprite.MoveInFrontOfOtherNode(legsSprite);


        var upsideDown = self.head.pos.y < self.legs.pos.y || self.player.bodyMode == Player.BodyModeIndex.ZeroG;
        
        if (upsideDown)
        {
            earLSprite.MoveInFrontOfOtherNode(headSprite);
            earRSprite.MoveInFrontOfOtherNode(headSprite);
        }
        else
        {
            earLSprite.MoveBehindOtherNode(headSprite);
            earRSprite.MoveBehindOtherNode(headSprite);
        }

        if (self.player.bodyMode == Player.BodyModeIndex.Crawl || upsideDown)
        {
            earLSprite.MoveInFrontOfOtherNode(cloakSprite);
            earRSprite.MoveInFrontOfOtherNode(cloakSprite);
        }
        else
        {
            earLSprite.MoveBehindOtherNode(cloakSprite);
            earRSprite.MoveBehindOtherNode(cloakSprite);
        }

        if (upsideDown)
        {
            scarfSprite.MoveBehindOtherNode(headSprite);
        }
        else
        {
            scarfSprite.MoveInFrontOfOtherNode(headSprite);
        }


        if (self.player.firstChunk.vel.x <= 0.3f)
        {
            armLSprite.MoveBehindOtherNode(bodySprite);
            armRSprite.MoveBehindOtherNode(bodySprite);
        }
        else
        {
            // this is confusing because the left and rights of arms and ears are different, it's not intuitive lol

            // Right
            if (self.player.flipDirection == 1)
            {
                armLSprite.MoveInFrontOfOtherNode(headSprite);
                armRSprite.MoveBehindOtherNode(bodySprite);

                earLSprite.MoveInFrontOfOtherNode(earRSprite);
            }
            // Left
            else
            {
                armRSprite.MoveInFrontOfOtherNode(headSprite);
                armLSprite.MoveBehindOtherNode(bodySprite);

                earRSprite.MoveInFrontOfOtherNode(earLSprite);
            }
        }


        playerModule.UpdateColors(self);

        var bodyColor = playerModule.BodyColor;
        var accentColor = playerModule.AccentColor;
        var cloakColor = playerModule.CloakColor;

        // Color
        bodySprite.color = bodyColor;
        hipsSprite.color = bodyColor;
        headSprite.color = bodyColor;
        legsSprite.color = bodyColor;

        feetSprite.color = accentColor;
        armLSprite.color = accentColor;
        armRSprite.color = accentColor;

        handLSprite.color = accentColor;
        handRSprite.color = accentColor;

        scarfSprite.color = (cloakColor * Custom.HSL2RGB(1.0f, 1.0f, 0.4f)).RWColorSafety();

        sleeveLSprite.color = cloakColor;
        sleeveRSprite.color = cloakColor;

        markSprite.color = playerModule.ActiveColor;

        tailSprite.color = Color.white;
        earLSprite.color = Color.white;
        earRSprite.color = Color.white;
        cloakSprite.color = Color.white;

        playerModule.Cloak.UpdateColor(sLeaser);

        playerModule.SetInvertTailColors = self.player.inVoidSea && upsideDown;


        if (playerModule.IsPearlpupAppearance)
        {
            sleeveLSprite.color = bodyColor;
            sleeveRSprite.color = bodyColor;
        }
        else
        {
            bodySprite.isVisible = false;
        }
    }
    
    public static void UpdateLightSource(PlayerGraphics self, PlayerModule playerModule)
    {
        if (self.lightSource == null) return;

        if (self.player.room == null) return;

        if (self.player.inVoidSea)
        {
            self.lightSource.color = Color.white;
            return;
        }

        var maxAlpha = playerModule.ActiveObject?.realizedObject == null ? 0.6f : 1.0f;
        
        self.lightSource.color = playerModule.ActiveColor * 1.5f;
        self.lightSource.alpha = Custom.LerpMap(self.player.room.Darkness(self.player.mainBodyChunk.pos), 0.5f, 0.9f, 0.0f, maxAlpha);
    }

    public static void UpdateCustomPlayerSprite(RoomCamera.SpriteLeaser sLeaser, int spriteIndexToCopy, string toCopy, string atlasName, string customName, int spriteIndex)
    {
        sLeaser.sprites[spriteIndex].isVisible = false;

        FAtlas? atlas = AssetLoader.GetAtlas(atlasName);
        if (atlas == null) return;

        string? name = sLeaser.sprites[spriteIndexToCopy]?.element?.name;
        if (name == null) return;

        name = name.Replace(toCopy, customName);

        if (!atlas._elementsByName.TryGetValue(Plugin.MOD_ID + name, out FAtlasElement element)) return;

        sLeaser.sprites[spriteIndex].element = element;


        FSprite spriteToCopy = sLeaser.sprites[spriteIndexToCopy];

        sLeaser.sprites[spriteIndex].isVisible = spriteToCopy.isVisible;

        sLeaser.sprites[spriteIndex].SetPosition(spriteToCopy.GetPosition());
        sLeaser.sprites[spriteIndex].SetAnchor(spriteToCopy.GetAnchor());

        sLeaser.sprites[spriteIndex].scaleX = spriteToCopy.scaleX;
        sLeaser.sprites[spriteIndex].scaleY = spriteToCopy.scaleY;
        sLeaser.sprites[spriteIndex].rotation = spriteToCopy.rotation;
    }

    public static void UpdateReplacementPlayerSprite(RoomCamera.SpriteLeaser sLeaser, int spriteIndex, string toReplace, string atlasName, string nameSuffix = "")
    {
        var atlas = AssetLoader.GetAtlas(atlasName);
        if (atlas == null) return;

        var name = sLeaser.sprites[spriteIndex]?.element?.name;
        if (name == null) return;


        if (!name.StartsWith(toReplace)) return;

        if (!atlas._elementsByName.TryGetValue(Plugin.MOD_ID + name + nameSuffix, out FAtlasElement element)) return;
        
        sLeaser.sprites[spriteIndex].element = element;
    }


    // Movement
    private static void PlayerGraphics_Update(On.PlayerGraphics.orig_Update orig, PlayerGraphics self)
    {
        orig(self);

        if (!self.player.TryGetPearlcatModule(out var playerModule)) return;

        ApplyTailMovement(self);
        ApplyEarMovement(self);

        playerModule.Cloak.Update();
        playerModule.PrevHeadRotation = self.head.connection.Rotation;
    }

    public static void ApplyTailMovement(PlayerGraphics self)
    {
        if (!self.player.TryGetPearlcatModule(out var playerModule)) return;

        if (playerModule.IsPearlpupAppearance) return;


        if (ModOptions.DisableCosmetics.Value) return;

        if (self.player.onBack != null) return;


        var upsideDown = self.head.pos.y < self.legs.pos.y;

        if (self.player.bodyMode == Player.BodyModeIndex.CorridorClimb && upsideDown)
        {
            foreach (var segment in self.tail)
            {
                segment.vel += new Vector2(0.0f, 2.0f);
            }

            return;
        }


        var excludeFromTailOffsetBodyMode = new List<Player.BodyModeIndex>()
        {
            Player.BodyModeIndex.ZeroG,
            Player.BodyModeIndex.Swimming,
            Player.BodyModeIndex.ClimbingOnBeam,
            Player.BodyModeIndex.CorridorClimb,
            Player.BodyModeIndex.Stunned,
            Player.BodyModeIndex.Dead,
        };

        var excludeFromTailOffsetAnimation = new List<Player.AnimationIndex>()
        {
            Player.AnimationIndex.Roll,
        };

        if (excludeFromTailOffsetBodyMode.Contains(self.player.bodyMode)) return;

        if (excludeFromTailOffsetAnimation.Contains(self.player.animation)) return;

        if (!TailSegmentVelocities.TryGet(self.player, out var tailSegmentVelocities)) return;

        for (int i = 0; i < self.tail.Length; i++)
        {
            if (!tailSegmentVelocities.ContainsKey(i)) continue;

            var segmentVel = tailSegmentVelocities[i];
            var facingDir = new Vector2(self.player.flipDirection, 1.0f);

            if (self.player.bodyMode == Player.BodyModeIndex.Crawl)
                segmentVel.y /= 2.0f;

            if (self.player.superLaunchJump >= 20)
                segmentVel.y += i == self.tail.Length - 1 ? 0.8f : 0.15f;

            self.tail[i].vel += segmentVel * facingDir;
        }
    }

    public static void ApplyEarMovement(PlayerGraphics self)
    {
        if (!self.player.TryGetPearlcatModule(out var playerModule)) return;

        TailSegment[]? earL = playerModule.EarL;
        TailSegment[]? earR = playerModule.EarR;

        if (earL == null || earR == null) return;

        UpdateEarSegments(self, earL, playerModule.EarLAttachPos);
        UpdateEarSegments(self, earR, playerModule.EarRAttachPos);

        // pups play with ears
        if (self.player.slugOnBack?.slugcat is Player slugpup && slugpup.isNPC && !slugpup.dead)
        {
            earL[0].vel.x -= 0.05f;
            earL[1].vel.x -= 0.05f;
            earL[2].vel.x -= 0.05f;

            earR[0].vel.x += 0.05f;
            earR[1].vel.x += 0.05f;
            earR[2].vel.x += 0.05f;


            var dir = Random.Range(0, 2) == 0 ? 1 : -1;
            var mult = Random.Range(0.8f, 1.3f);

            if (Random.Range(0, 400) == 0)
            {
                earL[0].vel.x += 2.0f * dir * mult;
                earL[1].vel.x += 2.0f * dir * mult;
                earL[2].vel.x += 2.0f * dir * mult;
            }

            if (Random.Range(0, 400) == 0)
            {
                earR[0].vel.x += 2.0f * dir * mult;
                earR[1].vel.x += 2.0f * dir * mult;
                earR[2].vel.x += 2.0f * dir * mult;
            }
        }
    }

    public static void UpdateEarSegments(PlayerGraphics self, TailSegment[]? ear, Vector2 earAttachPos)
    {
        if (ear == null) return;

        ear[0].connectedPoint = earAttachPos;

        for (int segment = 0; segment < ear.Length; segment++)
            ear[segment].Update();
        
        int negFlipDir = -self.player.flipDirection;
        
        // Dead or Alive

        // Simulate friction
        ear[0].vel.x *= 0.9f;
        ear[1].vel.x *= 0.7f;
        if (ear.Length >= 3) ear[2].vel.x *= 0.7f;


        if (self.player.dead) return;
        
        // Alive

        if (self.player.bodyMode == Player.BodyModeIndex.ZeroG)
        {
            var playerRot = self.player.firstChunk.Rotation;

            ear[0].vel += 5.0f * playerRot;
            ear[1].vel += 5.0f * playerRot;
            if (ear.Length >= 3) ear[2].vel += 5.0f * playerRot;
        }
        else
        {
            ear[0].vel.y += self.player.EffectiveRoomGravity * 0.5f;
            ear[1].vel.y += self.player.EffectiveRoomGravity * 0.3f;
            if (ear.Length >= 3) ear[2].vel.y += self.player.EffectiveRoomGravity * 0.3f;

            if (self.player.bodyMode == Player.BodyModeIndex.Crawl && self.player.input[0].x == 0)
            {
                // Ears go back when pouncing
                if (self.player.superLaunchJump >= 20)
                {
                    ear[0].vel.x += 0.65f * negFlipDir;
                    ear[1].vel.x += 0.65f * negFlipDir;
                    if (ear.Length >= 3) ear[2].vel.x += 0.65f * negFlipDir;
                }
                else
                {
                    ear[0].vel.x += 0.25f * negFlipDir;
                    ear[1].vel.x += 0.25f * negFlipDir;
                    if (ear.Length >= 3) ear[2].vel.x += 0.25f * negFlipDir;
                }
            }
        }
    }


    // Extra
    private static float PlayerObjectLooker_HowInterestingIsThisObject(On.PlayerGraphics.PlayerObjectLooker.orig_HowInterestingIsThisObject orig, PlayerGraphics.PlayerObjectLooker self, PhysicalObject obj)
    {
        var result = orig(self, obj);

        if (obj != null && obj.abstractPhysicalObject.IsPlayerObject())
            return 0.0f;

        return result;
    }

    private static Color Player_ShortCutColor(On.Player.orig_ShortCutColor orig, Player self)
    {
        var result = orig(self);

        if (!self.TryGetPearlcatModule(out var playerModule))
            return result;


        List<Color> colors = new()
        {
            playerModule.ActiveColor * Custom.HSL2RGB(1.0f, 1.0f, 1.25f),
            playerModule.ActiveColor
        };


        if (colors.Count == 0)
            return result;

        playerModule.ShortcutColorTimer += ShortcutColorIncrement * playerModule.ShortcutColorTimerDirection;

        if (playerModule.ShortcutColorTimerDirection == 1 && playerModule.ShortcutColorTimer > 1.0f)
        {
            playerModule.ShortcutColorTimerDirection = -1;
            playerModule.ShortcutColorTimer += ShortcutColorIncrement * playerModule.ShortcutColorTimerDirection;

        }
        else if (playerModule.ShortcutColorTimerDirection == -1 && playerModule.ShortcutColorTimer < 0.0f)
        {
            playerModule.ShortcutColorTimerDirection = 1;
            playerModule.ShortcutColorTimer += ShortcutColorIncrement * playerModule.ShortcutColorTimerDirection;
        }

        // https://gamedev.stackexchange.com/questions/98740/how-to-color-lerp-between-multiple-colors
        float scaledTime = playerModule.ShortcutColorTimer * (colors.Count - 1);
        Color oldColor = colors[(int)scaledTime];

        int nextIndex = (int)(scaledTime + 1.0f);
        Color newColor = nextIndex >= colors.Count ? oldColor : colors[nextIndex];

        float newTime = scaledTime - Mathf.Floor(scaledTime);
        return Color.Lerp(oldColor, newColor, newTime);
    }

    private static void JollyPlayerSpecificHud_Draw(On.JollyCoop.JollyHUD.JollyPlayerSpecificHud.orig_Draw orig, JollyCoop.JollyHUD.JollyPlayerSpecificHud self, float timeStacker)
    {
        orig(self, timeStacker);

        if (self.abstractPlayer.realizedCreature is not Player player) return;

        if (!player.TryGetPearlcatModule(out var playerModule)) return;

        self.playerColor = playerModule.ActiveColor;
    }
}