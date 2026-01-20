using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Milkwaters_ArkMod
{
    // custom graphic that hangles per-region colors and multi-direction sprites for pawns
    [StaticConstructorOnStartup]
    public class Graphic_ColorRegionTint : Graphic
    {
        // cached materials for each facing
        private readonly Material[] mats = new Material[4];

        // graphic data passed from xml
        private GraphicData_ColorRegions myData;

        // my shader that supports 7 masks and color slots
        private static readonly Shader ColorRegionShader = ShaderDatabase.LoadShader("ColorRegionTint");

        // the active tint data
        private ColorRegionTintData activeTintData;

        // per facing mask texture lists for that pawn
        private readonly List<Texture2D>[] maskTextures = new List<Texture2D>[4];
        private readonly List<Color>[] maskColors = new List<Color>[4];

        // rotation helpers
        private bool eastFlipped;
        private bool westFlipped;
        private float drawRotatedExtraAngleOffset;

        // material accessors
        public override Material MatSingle => MatSouth;
        public override Material MatNorth => mats[0];
        public override Material MatEast => mats[1];
        public override Material MatSouth => mats[2];
        public override Material MatWest => mats[3];

        // rotation accessors
        public override bool EastFlipped => eastFlipped;
        public override bool WestFlipped => westFlipped;
        public override float DrawRotatedExtraAngleOffset => drawRotatedExtraAngleOffset;

        // determines if the sprite should rotate or stay static
        // !TODO: clean this up?
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

        // main code, loads textures, builds materials, and loads masks
        public override void Init(GraphicRequest req)
        {
            Log.Message($"[ColorRegionTint] Init ENTER: path='{req.path ?? "null"}', gdType='{req.graphicData?.GetType().FullName ?? "null"}'");

            try
            {
                // ensure we got the correct graphicdata type
                myData = req.graphicData as GraphicData_ColorRegions;
                if (myData == null)
                {
                    Log.Error($"[ColorRegionTint] Init called with non-GraphicData_ColorRegions (type={req.graphicData?.GetType().FullName ?? "null"}).");
                    SetAllBadMats();
                    Log.Message("[ColorRegionTint] Init EXIT (BAD MATS, wrong GraphicData).");
                    return;
                }

                // load template
                GraphicDataColorRegionsDef template = myData.template;
                if (template == null)
                {
                    Log.Error("[ColorRegionTint] GraphicData_ColorRegions has null template.");
                    SetAllBadMats();
                    Log.Message("[ColorRegionTint] Init EXIT (BAD MATS, null template).");
                    return;
                }

                // choose override tint if present, otherwise use template tint
                activeTintData = myData.colorRegionTintOverride ?? template.colorRegionTintData;
                if (activeTintData == null)
                {
                    Log.Error("[ColorRegionTint] No ColorRegionTintData found in template or override.");
                    SetAllBadMats();
                    Log.Message("[ColorRegionTint] Init EXIT (BAD MATS, no tint data).");
                    return;
                }

                // copy basic graphic settings
                data = myData;
                color = req.color;
                colorTwo = req.colorTwo;
                drawSize = req.drawSize;

                // resolve texture path priority from: TEMPLATE to GRAPHICDATA to REQUEST
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

                // load directional textures
                Texture2D[] baseTex = new Texture2D[4];
                baseTex[0] = ContentFinder<Texture2D>.Get(path + "_north", false);
                baseTex[1] = ContentFinder<Texture2D>.Get(path + "_east", false);
                baseTex[2] = ContentFinder<Texture2D>.Get(path + "_south", false);
                baseTex[3] = ContentFinder<Texture2D>.Get(path + "_west", false);

                // fallback logic if some facings are missing
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

                // ensure all facings have something
                if (baseTex[2] == null) baseTex[2] = baseTex[0];
                if (baseTex[1] == null) baseTex[1] = baseTex[3] ?? baseTex[0];
                if (baseTex[3] == null) baseTex[3] = baseTex[1] ?? baseTex[0];

                // initialize mask lists
                for (int i = 0; i < 4; i++)
                {
                    maskTextures[i] = new List<Texture2D>();
                    maskColors[i] = new List<Color>();
                }

                // build base materials for each facing using custom function
                BuildFacingMaterial(0, Rot4.North, baseTex[0]);
                BuildFacingMaterial(1, Rot4.East, baseTex[1]);
                BuildFacingMaterial(2, Rot4.South, baseTex[2]);
                BuildFacingMaterial(3, Rot4.West, baseTex[3]);

                Log.Message("[ColorRegionTint] Init EXIT (SUCCESS).");
            }
            catch (Exception ex)
            {
                Log.Error($"[ColorRegionTint] Init EXCEPTION: {ex}");
                SetAllBadMats();
                Log.Message("[ColorRegionTint] Init EXIT (EXCEPTION).");
                throw;
            }
        }

        // builds a base material for a single facing with masks
        private void BuildFacingMaterial(int index, Rot4 rot, Texture2D mainTex)
        {
            if (mainTex == null)
            {
                mats[index] = BaseContent.BadMat;
                return;
            }

            // create a material request similar to Graphic_Multi
            MaterialRequest mr = default(MaterialRequest);
            mr.mainTex = mainTex;
            mr.shader = ColorRegionShader;
            mr.color = color;
            mr.colorTwo = colorTwo;
            mr.maskTex = null;
            mr.shaderParameters = data?.shaderParameters;
            mr.renderQueue = 0;

            Material mat = MaterialPool.MatFrom(mr);

            // attach mask textures but not colors yet
            ApplyColorRegionDataToMaterial(index, rot, mat);

            mats[index] = mat;
        }

        // loads mask textures for a facing and assigns them to the material
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

            // load mask textures for this facing
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

            // assign mask textures to shader slots
            for (int i = 0; i < texList.Count; i++)
            {
                mat.SetTexture("_MaskTex" + i, texList[i]);
            }
        }

        // returns the mask set for a given rotation
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

            // west uses east masks unless explicitly defined
            return activeTintData.East;
        }

        // returns a material for a specific pawn + rotation WITH colors
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

            // if no pawn, return shared material
            if (thing == null)
                return baseMat;

            // grab the pawn's colorregion comp
            var comp = thing.TryGetComp<CompColorRegions>();
            if (comp == null)
                return baseMat;

            // use the facing index (0–3) as the cache key
            int key = rot.AsInt;

            // check if we've already built a material for this pawn + facing
            if (!comp.materialCache.TryGetValue(key, out Material mat))
            {
                // no cached material yet means clone the shared base material once
                mat = new Material(baseMat);

                // apply this pawn's unique region colors to the cloned material
                ApplyPerPawnColors(mat, rot, thing);

                // store the finished material so we never create it again
                comp.materialCache[key] = mat;
            }

            // return the cached per pawn material for this facing
            return mat;
        }

        // applies the pawns chosen colors to the cloned material
        private void ApplyPerPawnColors(Material mat, Rot4 rot, Thing thing)
        {
            if (activeTintData == null || thing == null)
                return;

            FacingMaskSet facingSet = GetFacingMaskSet(rot);
            if (facingSet == null || facingSet.masks == null || facingSet.masks.Count == 0)
                return;

            // get the pawns color storage
            CompColorRegions comp = thing.TryGetComp<CompColorRegions>();
            if (comp == null)
                return;

            int colorSlot = 0;

            foreach (MaskEntry entry in facingSet.masks)
            {
                if (entry == null || entry.regionId.NullOrEmpty())
                {
                    Log.Warning($"[ColorRegionTint] MaskEntry missing regionId for mask '{entry?.maskTexPath}'");
                    continue;
                }

                // load region definition
                ColorRegionDef regionDef = DefDatabase<ColorRegionDef>.GetNamed(entry.regionId, false);
                if (regionDef == null)
                {
                    Log.Error($"[ColorRegionTint] No ColorRegionDef found for regionId '{entry.regionId}'");
                    continue;
                }

                if (regionDef.allowedColors == null || regionDef.allowedColors.Count == 0)
                {
                    Log.Error($"[ColorRegionTint] ColorRegionDef '{entry.regionId}' has no allowedColors");
                    continue;
                }

                // reuse or generate color for this pawn + region
                if (!comp.regionColors.TryGetValue(entry.regionId, out Color chosenColor))
                {
                    Rand.PushState(thing.thingIDNumber ^ entry.regionId.GetHashCode());
                    string chosenName = regionDef.allowedColors.RandomElement();
                    Rand.PopState();

                    ColorRegionTintColorDef colorDef = DefDatabase<ColorRegionTintColorDef>.GetNamedSilentFail(chosenName);
                    if (colorDef == null)
                    {
                        Log.Error($"[ColorRegionTint] Missing ColorRegionTintColorDef '{chosenName}'");
                        chosenColor = Color.white;
                    }
                    else
                    {
                        chosenColor = colorDef.color;
                    }

                    comp.regionColors[entry.regionId] = chosenColor;
                }

                // assign color to shader slot
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

        // assigns badmat to all facings if initialization fails
        private void SetAllBadMats()
        {
            for (int i = 0; i < mats.Length; i++)
                mats[i] = BaseContent.BadMat;
        }
    }
}