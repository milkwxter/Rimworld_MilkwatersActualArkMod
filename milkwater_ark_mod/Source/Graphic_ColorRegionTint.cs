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

        private static readonly int[] MaskTexIDs = 
            { Shader.PropertyToID("_MaskTex0"), Shader.PropertyToID("_MaskTex1"), Shader.PropertyToID("_MaskTex2"),
              Shader.PropertyToID("_MaskTex3"), Shader.PropertyToID("_MaskTex4"), Shader.PropertyToID("_MaskTex5"),
              Shader.PropertyToID("_MaskTex6") };
        private static readonly int[] ColorIDs =
            { Shader.PropertyToID("_Color0"), Shader.PropertyToID("_Color1"), Shader.PropertyToID("_Color2"),
              Shader.PropertyToID("_Color3"), Shader.PropertyToID("_Color4"), Shader.PropertyToID("_Color5"),
              Shader.PropertyToID("_Color6") };


        // the active tint data
        private ColorRegionTintData activeTintData;

        // per facing mask texture lists for that pawn
        private readonly List<Texture2D>[] maskTextures = new List<Texture2D>[4];

        // cached mask sets per facing (0=north,1=east,2=south,3=west)
        private readonly FacingMaskSet[] facingSets = new FacingMaskSet[4];

        // cached region defs per facing per mask slot
        private readonly List<ColorRegionDef>[] regionDefs = new List<ColorRegionDef>[4];

        // cached allowed color defs per facing per mask slot
        private readonly List<List<ColorRegionTintColorDef>>[] allowedColorDefs = new List<List<ColorRegionTintColorDef>>[4];

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

                // cache facing mask sets
                facingSets[0] = activeTintData.North;
                facingSets[1] = activeTintData.East;
                facingSets[2] = activeTintData.South;
                facingSets[3] = activeTintData.East;

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

                // initialize per-facing caches
                for (int i = 0; i < 4; i++)
                {
                    maskTextures[i] = new List<Texture2D>();
                    regionDefs[i] = new List<ColorRegionDef>();
                    allowedColorDefs[i] = new List<List<ColorRegionTintColorDef>>();
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
            MaterialRequest mr = default;
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

            FacingMaskSet facingSet = facingSets[rot.AsInt];
            if (facingSet == null || facingSet.masks == null || facingSet.masks.Count == 0)
                return;

            List<Texture2D> texList = maskTextures[index];
            texList.Clear();
            regionDefs[index].Clear();
            allowedColorDefs[index].Clear();

            // load mask textures and preload region/color defs for this facing
            foreach (MaskEntry entry in facingSet.masks)
            {
                if (entry == null)
                    continue;

                // mask texture
                Texture2D maskTex = ContentFinder<Texture2D>.Get(entry.maskTexPath, reportFailure: false);
                if (maskTex == null)
                {
                    Log.Warning($"Graphic_ColorRegionTint: Could not find mask texture at '{entry.maskTexPath}'");
                    continue;
                }
                texList.Add(maskTex);

                // region def
                if (entry.regionId.NullOrEmpty())
                {
                    Log.Warning($"[ColorRegionTint] MaskEntry missing regionId for mask '{entry.maskTexPath}'");
                    regionDefs[index].Add(null);
                    allowedColorDefs[index].Add(null);
                    continue;
                }

                ColorRegionDef regionDef = DefDatabase<ColorRegionDef>.GetNamed(entry.regionId, false);
                if (regionDef == null)
                {
                    Log.Error($"[ColorRegionTint] No ColorRegionDef found for regionId '{entry.regionId}'");
                    regionDefs[index].Add(null);
                    allowedColorDefs[index].Add(null);
                    continue;
                }

                if (regionDef.allowedColors == null || regionDef.allowedColors.Count == 0)
                {
                    Log.Error($"[ColorRegionTint] ColorRegionDef '{entry.regionId}' has no allowedColors");
                    regionDefs[index].Add(regionDef);
                    allowedColorDefs[index].Add(null);
                    continue;
                }

                regionDefs[index].Add(regionDef);

                // preload allowed color defs for this region
                List<ColorRegionTintColorDef> colorList = new List<ColorRegionTintColorDef>();
                foreach (string name in regionDef.allowedColors)
                {
                    ColorRegionTintColorDef colorDef = DefDatabase<ColorRegionTintColorDef>.GetNamedSilentFail(name);
                    if (colorDef == null)
                    {
                        Log.Error($"[ColorRegionTint] Missing ColorRegionTintColorDef '{name}' for region '{entry.regionId}'");
                        continue;
                    }
                    colorList.Add(colorDef);
                }
                allowedColorDefs[index].Add(colorList);
            }

            // assign mask textures to shader slots
            for (int i = 0; i < texList.Count; i++)
            {
                mat.SetTexture(MaskTexIDs[i], texList[i]);
            }
        }

        // returns a material for a specific pawn + rotation WITH colors
        public override Material MatAt(Rot4 rot, Thing thing = null)
        {
            int index = rot.AsInt;
            Material baseMat = mats[index];

            // if no pawn, return shared material
            if (thing == null)
                return baseMat;

            // grab the pawn's colorregion comp
            var comp = thing.TryGetComp<CompColorRegions>();
            if (comp == null)
                return baseMat;

            // use the facing index (0-3) as the cache key
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

            int index = rot.AsInt;
            FacingMaskSet facingSet = facingSets[index];
            if (facingSet == null || facingSet.masks == null || facingSet.masks.Count == 0)
                return;

            // get the pawns color storage
            CompColorRegions comp = thing.TryGetComp<CompColorRegions>();
            if (comp == null)
                return;

            List<ColorRegionDef> regionList = regionDefs[index];
            List<List<ColorRegionTintColorDef>> colorLists = allowedColorDefs[index];
            if (regionList == null || colorLists == null)
                return;

            int colorSlot = 0;

            for (int i = 0; i < facingSet.masks.Count; i++)
            {
                MaskEntry entry = facingSet.masks[i];
                if (entry == null || entry.regionId.NullOrEmpty())
                {
                    Log.Warning($"[ColorRegionTint] MaskEntry missing regionId for mask '{entry?.maskTexPath}'");
                    continue;
                }

                ColorRegionDef regionDef = (i < regionList.Count) ? regionList[i] : null;
                if (regionDef == null)
                    continue;

                List<ColorRegionTintColorDef> colorList = (i < colorLists.Count) ? colorLists[i] : null;
                if (colorList == null || colorList.Count == 0)
                    continue;

                // reuse or generate color for this pawn + region
                if (!comp.regionColors.TryGetValue(entry.regionId, out Color chosenColor))
                {
                    // deterministic index based on pawn + region
                    int seed = Gen.HashCombine(thing.thingIDNumber, entry.regionId);
                    int idx = Mathf.Abs(seed) % colorList.Count;

                    ColorRegionTintColorDef colorDef = colorList[idx];
                    if (colorDef == null)
                    {
                        Log.Error($"[ColorRegionTint] Null ColorRegionTintColorDef in cached list for region '{entry.regionId}'");
                        chosenColor = Color.white;
                    }
                    else
                    {
                        chosenColor = colorDef.color;
                    }

                    comp.regionColors[entry.regionId] = chosenColor;
                }

                // assign color to shader slot
                if (colorSlot < ColorIDs.Length)
                {
                    mat.SetColor(ColorIDs[colorSlot], chosenColor);
                }
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
            return Gen.HashCombineStruct(Gen.HashCombineStruct(Gen.HashCombine(0, path), color), colorTwo);
        }

        // assigns badmat to all facings if initialization fails
        private void SetAllBadMats()
        {
            for (int i = 0; i < mats.Length; i++)
                mats[i] = BaseContent.BadMat;
        }
    }
}