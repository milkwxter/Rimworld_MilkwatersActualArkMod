using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Milkwaters_ArkMod
{
    [StaticConstructorOnStartup]
    public class Graphic_ColorRegionTint : Graphic
    {
        // 0 = North, 1 = East, 2 = South, 3 = West
        private readonly Material[] mats = new Material[4];

        private GraphicData_ColorRegions myData;

        private static readonly Shader ColorRegionShader = ShaderDatabase.LoadShader("ColorRegionTint");


        private ColorRegionTintData activeTintData;

        // Per-facing mask textures and colors
        private readonly List<Texture2D>[] maskTextures = new List<Texture2D>[4];
        private readonly List<Color>[] maskColors = new List<Color>[4];

        private bool eastFlipped;
        private bool westFlipped;
        private float drawRotatedExtraAngleOffset;

        public override Material MatSingle => MatSouth;
        public override Material MatNorth => mats[0];
        public override Material MatEast => mats[1];
        public override Material MatSouth => mats[2];
        public override Material MatWest => mats[3];

        public override bool EastFlipped => eastFlipped;
        public override bool WestFlipped => westFlipped;

        public override bool ShouldDrawRotated
        {
            get
            {
                if (data != null && !data.drawRotated)
                    return false;

                if (MatEast == MatNorth)
                    return MatWest == MatNorth;

                return true;
            }
        }

        public override float DrawRotatedExtraAngleOffset => drawRotatedExtraAngleOffset;

        public override void Init(GraphicRequest req)
        {
            Log.Message($"[ColorRegionTint] Init ENTER: path='{req.path ?? "null"}', gdType='{req.graphicData?.GetType().FullName ?? "null"}'");

            try
            {
                // ----------------- YOUR CURRENT INIT BODY -----------------
                // base.Init(req);

                myData = req.graphicData as GraphicData_ColorRegions;
                if (myData == null)
                {
                    Log.Error($"[ColorRegionTint] Init called with non-GraphicData_ColorRegions (type={req.graphicData?.GetType().FullName ?? "null"}).");
                    SetAllBadMats();
                    Log.Message("[ColorRegionTint] Init EXIT (BAD MATS, wrong GraphicData).");
                    return;
                }

                GraphicDataColorRegionsDef template = myData.template;
                if (template == null)
                {
                    Log.Error("[ColorRegionTint] GraphicData_ColorRegions has null template.");
                    SetAllBadMats();
                    Log.Message("[ColorRegionTint] Init EXIT (BAD MATS, null template).");
                    return;
                }

                activeTintData = myData.colorRegionTintOverride ?? template.colorRegionTintData;
                if (activeTintData == null)
                {
                    Log.Error("[ColorRegionTint] No ColorRegionTintData found in template or override.");
                    SetAllBadMats();
                    Log.Message("[ColorRegionTint] Init EXIT (BAD MATS, no tint data).");
                    return;
                }

                data = myData;
                color = req.color;
                colorTwo = req.colorTwo;
                drawSize = req.drawSize;

                string resolvedPath = null;

                if (!template.texPath.NullOrEmpty())
                    resolvedPath = template.texPath;

                if (!myData.texPath.NullOrEmpty())
                    resolvedPath = myData.texPath;

                if (!req.path.NullOrEmpty())
                    resolvedPath = req.path;

                if (resolvedPath.NullOrEmpty())
                {
                    Log.Error("[ColorRegionTint] No texPath provided in template, GraphicData_ColorRegions, or GraphicRequest.");
                    SetAllBadMats();
                    Log.Message("[ColorRegionTint] Init EXIT (BAD MATS, no texPath).");
                    return;
                }

                path = resolvedPath;

                Log.Message($"[ColorRegionTint] Using path='{path}', template='{template.defName}'");

                Texture2D[] baseTex = new Texture2D[4];
                baseTex[0] = ContentFinder<Texture2D>.Get(path + "_north", false);
                baseTex[1] = ContentFinder<Texture2D>.Get(path + "_east", false);
                baseTex[2] = ContentFinder<Texture2D>.Get(path + "_south", false);
                baseTex[3] = ContentFinder<Texture2D>.Get(path + "_west", false);

                if (baseTex[0] == null)
                {
                    if (baseTex[2] != null)
                    {
                        baseTex[0] = baseTex[2];
                        drawRotatedExtraAngleOffset = 180f;
                    }
                    else if (baseTex[1] != null)
                    {
                        baseTex[0] = baseTex[1];
                        drawRotatedExtraAngleOffset = -90f;
                    }
                    else if (baseTex[3] != null)
                    {
                        baseTex[0] = baseTex[3];
                        drawRotatedExtraAngleOffset = 90f;
                    }
                    else
                    {
                        baseTex[0] = ContentFinder<Texture2D>.Get(path, false);
                    }
                }

                if (baseTex[0] == null)
                {
                    Log.Error("[ColorRegionTint] Failed to find any textures at " + path);
                    SetAllBadMats();
                    Log.Message("[ColorRegionTint] Init EXIT (BAD MATS, no baseTex).");
                    return;
                }

                if (baseTex[2] == null) baseTex[2] = baseTex[0];
                if (baseTex[1] == null) baseTex[1] = baseTex[3] ?? baseTex[0];
                if (baseTex[3] == null) baseTex[3] = baseTex[1] ?? baseTex[0];

                for (int i = 0; i < 4; i++)
                {
                    maskTextures[i] = new List<Texture2D>();
                    maskColors[i] = new List<Color>();
                }

                BuildFacingMaterial(0, Rot4.North, baseTex[0]);
                BuildFacingMaterial(1, Rot4.East, baseTex[1]);
                BuildFacingMaterial(2, Rot4.South, baseTex[2]);
                BuildFacingMaterial(3, Rot4.West, baseTex[3]);

                Log.Message("[ColorRegionTint] Init EXIT (SUCCESS).");
                // ----------------- END BODY -----------------
            }
            catch (Exception ex)
            {
                Log.Error($"[ColorRegionTint] Init EXCEPTION: {ex}");
                SetAllBadMats();
                Log.Message("[ColorRegionTint] Init EXIT (EXCEPTION).");
                throw; // IMPORTANT: rethrow so vanilla logs point to the same call
            }
        }

        private void BuildFacingMaterial(int index, Rot4 rot, Texture2D mainTex)
        {
            if (mainTex == null)
            {
                mats[index] = BaseContent.BadMat;
                return;
            }

            // Create base material like Graphic_Multi
            MaterialRequest mr = default(MaterialRequest);
            mr.mainTex = mainTex;
            mr.shader = ColorRegionShader;
            mr.color = color;
            mr.colorTwo = colorTwo;
            mr.maskTex = null;
            mr.shaderParameters = data?.shaderParameters;
            mr.renderQueue = 0;

            Material mat = MaterialPool.MatFrom(mr);

            // Apply our color-region masks and colors
            ApplyColorRegionDataToMaterial(index, rot, mat);

            mats[index] = mat;
        }

        private void ApplyColorRegionDataToMaterial(int index, Rot4 rot, Material mat)
        {
            if (activeTintData == null)
                return;

            FacingMaskSet facingSet = GetFacingMaskSet(rot);
            if (facingSet == null || facingSet.masks == null || facingSet.masks.Count == 0)
                return;

            List<Texture2D> texList = maskTextures[index];
            texList.Clear();
            maskColors[index].Clear();

            foreach (MaskEntry entry in facingSet.masks)
            {
                if (entry == null)
                    continue;

                Texture2D maskTex = ContentFinder<Texture2D>.Get(entry.maskTexPath, reportFailure: false);
                if (maskTex == null)
                {
                    Log.Warning($"Graphic_ColorRegionTint: Could not find mask texture at '{entry.maskTexPath}'");
                    continue;
                }

                texList.Add(maskTex);
            }

            // Apply only mask textures here; colors will be set per-pawn later
            for (int i = 0; i < texList.Count; i++)
            {
                mat.SetTexture("_MaskTex" + i, texList[i]);
            }
        }


        private FacingMaskSet GetFacingMaskSet(Rot4 rot)
        {
            if (activeTintData == null)
                return null;

            if (rot == Rot4.North)
                return activeTintData.North;
            if (rot == Rot4.East)
                return activeTintData.East;
            if (rot == Rot4.South)
                return activeTintData.South;

            // west fallback, use east instead
            return activeTintData.East;
        }



        public override Material MatAt(Rot4 rot, Thing thing = null)
        {
            int index = rot.AsInt;
            Material baseMat;

            if (rot == Rot4.North)
                baseMat = mats[0];
            else if (rot == Rot4.East)
                baseMat = mats[1];
            else if (rot == Rot4.South)
                baseMat = mats[2];
            else
                baseMat = mats[3];

            // No pawn? Just use the shared base material.
            if (thing == null)
                return baseMat;

            // Clone the base material so this pawn can have its own colors.
            Material mat = new Material(baseMat);

            ApplyPerPawnColors(mat, rot, thing);

            return mat;
        }

        private void ApplyPerPawnColors(Material mat, Rot4 rot, Thing thing)
        {
            if (activeTintData == null || thing == null)
                return;

            FacingMaskSet facingSet = GetFacingMaskSet(rot);
            if (facingSet == null || facingSet.masks == null || facingSet.masks.Count == 0)
                return;

            // Get the pawn's color comp
            CompColorRegions comp = thing.TryGetComp<CompColorRegions>();
            if (comp == null)
                return;

            int colorSlot = 0;

            foreach (MaskEntry entry in facingSet.masks)
            {
                if (entry == null || entry.allowedColors == null || entry.allowedColors.Count == 0)
                    continue;

                if (entry.regionId.NullOrEmpty())
                {
                    Log.Warning($"[ColorRegionTint] MaskEntry missing regionId for mask '{entry.maskTexPath}'");
                    continue;
                }

                // If this region already has a chosen color, reuse it
                if (!comp.regionColors.TryGetValue(entry.regionId, out Color chosenColor))
                {
                    // Pick a deterministic color for this pawn + region
                    Rand.PushState(thing.thingIDNumber ^ entry.regionId.GetHashCode());
                    string chosenName = entry.allowedColors.RandomElement();
                    Rand.PopState();

                    ColorRegionTintColorDef colorDef = DefDatabase<ColorRegionTintColorDef>.GetNamedSilentFail(chosenName);
                    if (colorDef == null)
                    {
                        Log.Warning($"[ColorRegionTint] Could not find ColorRegionTintColorDef '{chosenName}'");
                        chosenColor = Color.white;
                    }
                    else
                    {
                        chosenColor = colorDef.color;
                    }

                    // Store it permanently
                    comp.regionColors[entry.regionId] = chosenColor;
                }

                // Apply color to shader slot
                mat.SetColor("_Color" + colorSlot, chosenColor);
                colorSlot++;
            }
        }

        public override Graphic GetColoredVersion(Shader newShader, Color newColor, Color newColorTwo)
        {
            return GraphicDatabase.Get<Graphic_ColorRegionTint>(path, newShader, drawSize, newColor, newColorTwo, data);
        }

        public override string ToString()
        {
            return $"ColorRegionTint(path={path}, color={color}, colorTwo={colorTwo})";
        }

        public override int GetHashCode()
        {
            return Gen.HashCombineStruct(
                Gen.HashCombineStruct(
                    Gen.HashCombine(0, path),
                    color),
                colorTwo);
        }

        private void SetAllBadMats()
        {
            for (int i = 0; i < mats.Length; i++)
                mats[i] = BaseContent.BadMat;
        }
    }
}